/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestRegexMatcher.h
Responsibility:
Last reviewed:

	Unit tests for the RegexMatcher class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTRegexMatcher_H_INCLUDED
#define TESTRegexMatcher_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for LgCharacterPropertyEngine (ICU based implementation)
	 ******************************************************************************************/
	class TestRegexMatcher : public unitpp::suite
	{

		IRegexMatcherPtr m_qrem;

		void CheckMatch(int ichStart, int ichStart0, int ichStart1, int ichStart2, int ichEnd0,
			int ichEnd1, int ichEnd2)
		{
			ComBool fFound;
			m_qrem->Find(ichStart, &fFound);
			if (ichStart0 == -1)
				unitpp::assert_true("not found", !fFound);
			else
				unitpp::assert_true("find", fFound);
			int ich;
			m_qrem->get_Start(0, &ich);
			unitpp::assert_eq("match whole", ichStart0, ich);
			m_qrem->get_End(0, &ich);
			unitpp::assert_eq("end match whole", ichEnd0, ich);
			m_qrem->get_Start(1, &ich);
			unitpp::assert_eq("match whole", ichStart1, ich);
			m_qrem->get_End(1, &ich);
			unitpp::assert_eq("end match whole", ichEnd1, ich);
			m_qrem->get_Start(2, &ich);
			unitpp::assert_eq("match whole", ichStart2, ich);
			m_qrem->get_End(2, &ich);
			unitpp::assert_eq("end match whole", ichEnd2, ich);
		}

		void testRegexMatcher()
		{
			StrUni stuPattern(L"(a|e)(n|m)");
			StrUni stuInput(L"An ant am I? Am I not?");

			m_qrem->Init(stuPattern.Bstr(), false);
			m_qrem->Reset(stuInput.Bstr());
			CheckMatch(0, 0, 0, 1, 2, 1, 2); // Initial 'An'
			CheckMatch(1, 3, 3, 4, 5, 4, 5); // start of 'ant'
			CheckMatch(4, 7, 7, 8, 9, 8, 9); // 'am'
			CheckMatch(8, 13, 13, 14, 15, 14, 15); // 'Am'
			CheckMatch(14, -1, -1, -1, -1, -1, -1); // no more

			m_qrem->Init(stuPattern.Bstr(), true);
			m_qrem->Reset(stuInput.Bstr());
			CheckMatch(0, 3, 3, 4, 5, 4, 5); // start of 'ant'
			CheckMatch(4, 7, 7, 8, 9, 8, 9); // 'am'
			CheckMatch(8, -1, -1, -1, -1, -1, -1); // no more

			StrUni stuPattern2(L"a(.*)d .* b(.*)d");
			StrUni stuInput2(L"This and that is baaad");
			m_qrem->Init(stuPattern2.Bstr(), false);
			m_qrem->Reset(stuInput2.Bstr());
			// whole match runs from 5 to end;
			// first .* matches 'n' in 'and'
			// second .* matches 'aaa' in 'baaad'.
			CheckMatch(0, 5, 6, stuInput2.Length() - 4, stuInput2.Length(), 7, stuInput2.Length() - 1);
			CheckMatch(6, -1, -1, -1, -1, -1, -1); // no more
		}

	public:
		TestRegexMatcher();
		virtual void SuiteSetup()
		{
			m_qrem.CreateInstance(CLSID_RegexMatcherWrapper);
		}
		virtual void SuiteTeardown()
		{
			m_qrem.Clear();
		}
	};
}

#endif /*TESTRegexMatcher_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

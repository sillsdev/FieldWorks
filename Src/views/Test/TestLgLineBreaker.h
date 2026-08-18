/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2016 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGLINEBREAKER_H_INCLUDED
#define TESTLGLINEBREAKER_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
	/*******************************************************************************************
		Tests for LgLineBreaker
	 ******************************************************************************************/
	class TestLgLineBreaker : public unitpp::suite
	{
		ILgLineBreakerPtr m_qlb;

		void testNullArgs()
		{
			unitpp::assert_true("m_qlb", m_qlb.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qlb->GetLineBreakInfo(NULL, 0, 0, 0, NULL, NULL));
				unitpp::assert_eq("GetLineBreakInfo(NULL, 0, 0, 0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("GetLineBreakInfo(NULL, 0, 0, 0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qlb->put_LineBreakText(NULL, 0));
				unitpp::assert_eq("put_LineBreakText(NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("put_LineBreakText(NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qlb->GetLineBreakText(0, NULL, NULL));
				unitpp::assert_eq("GetLineBreakText(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("GetLineBreakText(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qlb->LineBreakBefore(0, NULL, NULL));
				unitpp::assert_eq("LineBreakBefore(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("LineBreakBefore(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qlb->LineBreakAfter(0, NULL, NULL));
				unitpp::assert_eq("LineBreakAfter(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("LineBreakAfter(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

		void testPutLineBreakTextUsesLength()
		{
			unitpp::assert_true("m_qlb", m_qlb.Ptr());
			// The text sits inside a larger buffer with no NUL terminator; the characters
			// after cch must not become part of the break text, and the copy must not
			// scan past cch looking for a terminator.
			OLECHAR rgch[] = { 'o','n','e',' ','t','w','o',' ','s','i','x','X','X','X','X','X' };
			const int cch = 11; // "one two six"
			CheckHr(m_qlb->put_LineBreakText(rgch, cch));

			OLECHAR rgchOut[32];
			int cchOut = 0;
			CheckHr(m_qlb->GetLineBreakText(32, rgchOut, &cchOut));
			unitpp::assert_eq("break text length", cch, cchOut);
			unitpp::assert_true("break text content",
				memcmp(rgch, rgchOut, cch * isizeof(OLECHAR)) == 0);

			// Word boundaries inside the text must be found.
			int ichBreak = -1;
			LgLineBreak lbWeight;
			CheckHr(m_qlb->LineBreakBefore(10, &ichBreak, &lbWeight));
			unitpp::assert_eq("line break before 'six'", 8, ichBreak);
		}

	public:
		TestLgLineBreaker();
		virtual void SuiteSetup()
		{
			LgLineBreaker::CreateCom(NULL,
				IID_ILgLineBreaker, (void **)&m_qlb);
		}
		virtual void SuiteTeardown()
		{
			m_qlb.Clear();
		}
	};
}

#endif /*TESTLGLINEBREAKER_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestLgCollatingEngine.h
Responsibility:
Last reviewed:

	Unit tests for the LgCollatingEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGCOLLATINGENGINE_H_INCLUDED
#define TESTLGCOLLATINGENGINE_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
	/*******************************************************************************************
		Tests for LgCollatingEngine
	 ******************************************************************************************/
	class TestLgCollatingEngine : public unitpp::suite
	{
		ILgCollatingEnginePtr m_qcoleng;

		void testNullArgs()
		{
			unitpp::assert_true("m_qcoleng0", m_qcoleng.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qcoleng->get_SortKey(NULL, fcoDefault, NULL));
				unitpp::assert_eq("Unicode:get_SortKey(NULL, fcoDefault, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Unicode:get_SortKey(NULL, fcoDefault, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoleng->SortKeyRgch(NULL, 0, fcoDefault, 0, NULL, NULL));
				unitpp::assert_eq("Unicode:SortKeyRgch(NULL, 0, fcoDefault, 0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Unicode:SortKeyRgch(NULL, 0, fcoDefault, 0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoleng->Compare(NULL, NULL, fcoDefault, NULL));
				unitpp::assert_eq("Unicode:Compare(NULL, NULL, fcoDefault, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Unicode:Compare(NULL, NULL, fcoDefault, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoleng->get_WritingSystemFactory(NULL));
				unitpp::assert_eq("Unicode:get_WritingSystemFactory(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Unicode:get_WritingSystemFactory(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qcoleng->putref_WritingSystemFactory(NULL));
			unitpp::assert_eq("Unicode:putref_WritingSystemFactory(NULL) HRESULT", S_OK, hr);
		}

	public:
		TestLgCollatingEngine();
		virtual void SuiteSetup()
		{
			LgUnicodeCollater::CreateCom(NULL, IID_ILgCollatingEngine, (void **)&m_qcoleng);
		}
		virtual void SuiteTeardown()
		{
			m_qcoleng.Clear();
		}
	};


}

#endif /*TESTLGCOLLATINGENGINE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)

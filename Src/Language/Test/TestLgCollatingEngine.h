/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgCollatingEngine.h
Responsibility:
Last reviewed:

	Unit tests for the LgCollatingEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGCOLLATINGENGINE_H_INCLUDED
#define TESTLGCOLLATINGENGINE_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for LgCollatingEngine
	 ******************************************************************************************/
	class TestLgCollatingEngine : public unitpp::suite
	{
		ILgCollatingEnginePtr m_qcoleng0;
		ILgCollatingEnginePtr m_qcoleng1;

		void testNullArgs()
		{
			unitpp::assert_true("m_qcoleng0", m_qcoleng0.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qcoleng0->get_SortKey(NULL, fcoDefault, NULL));
				unitpp::assert_eq("Unicode:get_SortKey(NULL, fcoDefault, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Unicode:get_SortKey(NULL, fcoDefault, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoleng0->SortKeyRgch(NULL, 0, fcoDefault, 0, NULL, NULL));
				unitpp::assert_eq("Unicode:SortKeyRgch(NULL, 0, fcoDefault, 0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Unicode:SortKeyRgch(NULL, 0, fcoDefault, 0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoleng0->Compare(NULL, NULL, fcoDefault, NULL));
				unitpp::assert_eq("Unicode:Compare(NULL, NULL, fcoDefault, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Unicode:Compare(NULL, NULL, fcoDefault, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoleng0->get_WritingSystemFactory(NULL));
				unitpp::assert_eq("Unicode:get_WritingSystemFactory(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Unicode:get_WritingSystemFactory(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qcoleng0->putref_WritingSystemFactory(NULL));
			unitpp::assert_eq("Unicode:putref_WritingSystemFactory(NULL) HRESULT", S_OK, hr);

			unitpp::assert_true("m_qcoleng1", m_qcoleng1.Ptr());
			try{
				CheckHr(hr = m_qcoleng1->get_SortKey(NULL, fcoDefault, NULL));
				unitpp::assert_eq("System:get_SortKey(NULL, fcoDefault, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("System:get_SortKey(NULL, fcoDefault, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoleng1->SortKeyRgch(NULL, 0, fcoDefault, 0, NULL, NULL));
				unitpp::assert_eq("System:SortKeyRgch(NULL, 0, fcoDefault, 0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("System:SortKeyRgch(NULL, 0, fcoDefault, 0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoleng1->Compare(NULL, NULL, fcoDefault, NULL));
				unitpp::assert_eq("System:Compare(NULL, NULL, fcoDefault, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("System:Compare(NULL, NULL, fcoDefault, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoleng1->get_WritingSystemFactory(NULL));
				unitpp::assert_eq("System:get_WritingSystemFactory(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("System:get_WritingSystemFactory(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qcoleng1->putref_WritingSystemFactory(NULL));
			unitpp::assert_eq("System:putref_WritingSystemFactory(NULL) HRESULT", S_OK, hr);
		}

	public:
		TestLgCollatingEngine();
		virtual void SuiteSetup()
		{
			LgUnicodeCollater::CreateCom(NULL, IID_ILgCollatingEngine, (void **)&m_qcoleng0);
			LgSystemCollater::CreateCom(NULL, IID_ILgCollatingEngine, (void **)&m_qcoleng1);
		}
		virtual void SuiteTeardown()
		{
			m_qcoleng0.Clear();
			m_qcoleng1.Clear();
		}
	};


}

#endif /*TESTLGCOLLATINGENGINE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)

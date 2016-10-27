/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestLgIcuCharPropEngine.h
Responsibility:
Last reviewed:

	Unit tests for the LgIcuCharPropEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGICUCHARPROPENGINE_H_INCLUDED
#define TESTLGICUCHARPROPENGINE_H_INCLUDED

#pragma once

#include "testFwKernel.h"

namespace TestFwKernel
{
	/*******************************************************************************************
		Tests for LgCharacterPropertyEngine (ICU based implementation)
	 ******************************************************************************************/
	class TestLgIcuCharPropEngine : public unitpp::suite
	{
		ILgCharacterPropertyEnginePtr m_qpropeng;

		void testNullArgs()
		{
			unitpp::assert_true("m_qpropeng", m_qpropeng.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qpropeng->get_IsWordForming(0, NULL));
				unitpp::assert_eq("get_IsWordForming(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("get_IsWordForming(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qpropeng->GetLineBreakInfo(NULL, 0, 0, 0, NULL, NULL));
				unitpp::assert_eq("GetLineBreakInfo(NULL, 0, 0, 0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("GetLineBreakInfo(NULL, 0, 0, 0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qpropeng->put_LineBreakText(NULL, 0));
				unitpp::assert_eq("put_LineBreakText(NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("put_LineBreakText(NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qpropeng->GetLineBreakText(0, NULL, NULL));
				unitpp::assert_eq("GetLineBreakText(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("GetLineBreakText(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qpropeng->LineBreakBefore(0, NULL, NULL));
				unitpp::assert_eq("LineBreakBefore(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("LineBreakBefore(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qpropeng->LineBreakAfter(0, NULL, NULL));
				unitpp::assert_eq("LineBreakAfter(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch (Throwable& thr)
			{
				unitpp::assert_eq("LineBreakAfter(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

		// TODO (LT-9311) Unignore this test
		void ignore_testIsWordForming()
		{
			HRESULT hr;
			ComBool fRet;
			hr = m_qpropeng->get_IsWordForming(0x0041, &fRet); // capital A
			unitpp::assert_eq("get_IsWordForming(0x0041, &fRet) HRESULT", S_OK, hr);
			unitpp::assert_true("'A' should be word-forming", fRet);

			hr = m_qpropeng->get_IsWordForming(0x0021, &fRet); // exclamation point
			unitpp::assert_eq("get_IsWordForming(0x0021, &fRet) HRESULT", S_OK, hr);
			unitpp::assert_true("Exclamation point should not be word-forming", !fRet);

			hr = m_qpropeng->get_IsWordForming(0x0027, &fRet); // apostrophe
			unitpp::assert_eq("get_IsWordForming(0x0027, &fRet) HRESULT", S_OK, hr);
			unitpp::assert_true("Apostrophe should not be word-forming", !fRet);

			hr = m_qpropeng->get_IsWordForming(0x002D, &fRet); // Hyphen-minus
			unitpp::assert_eq("get_IsWordForming(0x002D, &fRet) HRESULT", S_OK, hr);
			unitpp::assert_true("Hyphen-minus should not be word-forming", !fRet);
		}

	public:
		TestLgIcuCharPropEngine();
		virtual void SuiteSetup()
		{
			LgIcuCharPropEngine::CreateCom(NULL,
				IID_ILgCharacterPropertyEngine, (void **)&m_qpropeng);
		}
		virtual void SuiteTeardown()
		{
			m_qpropeng.Clear();
		}
	};
}

#endif /*TESTLGICUCHARPROPENGINE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

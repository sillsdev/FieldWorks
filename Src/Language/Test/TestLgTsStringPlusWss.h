/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgTsStringPlusWss.h
Responsibility:
Last reviewed:

	Unit tests for the LgTsStringPlusWss class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGTSSTRINGPLUSENCS_H_INCLUDED
#define TESTLGTSSTRINGPLUSENCS_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for LgTsStringPlusWss
	 ******************************************************************************************/
	class TestLgTsStringPlusWss : public unitpp::suite
	{
		ILgTsStringPlusWssPtr m_qtsswss;

		void testNullArgs()
		{
			unitpp::assert_true("m_qtsswss", m_qtsswss.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qtsswss->putref_String(NULL, NULL));
				unitpp::assert_eq("putref_String(NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("putref_String(NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtsswss->get_String(NULL, NULL));
				unitpp::assert_eq("get_String(NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_String(NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtsswss->get_Text(NULL));
				unitpp::assert_eq("get_Text(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Text(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtsswss->Serialize(NULL));
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtsswss->Deserialize(NULL));
				unitpp::assert_eq("Deserialize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Deserialize(NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

		// Test that LgTsStringPlusWss::get_StringUsingWs correctly replaces all writing systems with
		// the given replacement WS. TE-8770
		void testGetStringUsingWs()
		{
			ILgWritingSystemFactoryPtr qwsf;
			CreateTestWritingSystemFactory(&qwsf);
			CreateTestWritingSystem(qwsf, kwsTest, kszTest);
			CreateTestWritingSystem(qwsf, kwsTest2, kszTest2);
			CreateTestWritingSystem(qwsf, kwsFrn, kszFrn);

			ITsStrBldrPtr qtsb;
			qtsb.CreateInstance(CLSID_TsStrBldr);
			StrUni stu1(L"Texto. ");
			StrUni stu2(L"Some more text. ");
			StrUni stu3(L"El resto del texto.");

			ITsPropsBldrPtr qtpb;
			qtpb.CreateInstance(CLSID_TsPropsBldr);
			CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, kwsTest));
			ITsTextPropsPtr qttp1;
			CheckHr(qtpb->GetTextProps(&qttp1));

			CheckHr(qtpb->SetIntPropValues(ktptWs, ktpvDefault, kwsTest2));
			ITsTextPropsPtr qttp2;
			CheckHr(qtpb->GetTextProps(&qttp2));

			CheckHr(qtsb->Replace(0, 0, stu3.Bstr(), qttp1));
			CheckHr(qtsb->Replace(0, 0, stu2.Bstr(), qttp2));
			CheckHr(qtsb->Replace(0, 0, stu1.Bstr(), qttp1));

			ITsStringPtr qtssIn;
			CheckHr(qtsb->GetString(&qtssIn));

			CheckHr(m_qtsswss->putref_String(qwsf, qtssIn));

			ITsStringPtr qtssOut;
			CheckHr(m_qtsswss->get_StringUsingWs(kwsFrn, &qtssOut));
			int crun;
			CheckHr(qtssOut->get_RunCount(&crun));
			unitpp::assert_eq("Modified string should have a single run", 1, crun);
			ITsTextPropsPtr qttpOut;
			CheckHr(qtssOut->get_Properties(0, &qttpOut));
			unitpp::assert_true("Props should not be null", qttpOut);
			int ws;
			int var;
			CheckHr(qttpOut->GetIntPropValues(ktptWs, &var, &ws));
			unitpp::assert_eq("All text should have been converted to French.", kwsFrn, ws);
			CheckHr(qwsf->Shutdown());
		}

	public:
		TestLgTsStringPlusWss();
		virtual void SuiteSetup()
		{
			LgTsStringPlusWss::CreateCom(NULL, IID_ILgTsStringPlusWss, (void **)&m_qtsswss);
		}
		virtual void SuiteTeardown()
		{
			m_qtsswss.Clear();
		}
	};


}

#endif /*TESTLGTSSTRINGPLUSENCS_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)

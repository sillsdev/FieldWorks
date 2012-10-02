/*-------------------------------------------------------------------*//*:Ignore these comments.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestTsStrBldr.h
Responsibility:
Last reviewed:

	Unit tests for the TsStrBldr class.
----------------------------------------------------------------------------------------------*/
#ifndef TESTTSSTRBLDR_H_INCLUDED
#define TESTTSSTRBLDR_H_INCLUDED

#pragma once

#include "testFwKernel.h"


namespace TestFwKernel
{
	class TestTsStrBldr : public unitpp::suite
	{
		TsStrBldr * m_pztsb1;
		TsStrBldr * m_pztsb0;

		/*--------------------------------------------------------------------------------------

		--------------------------------------------------------------------------------------*/
		void testGenString()
		{
			unitpp::assert_true("Non-NULL m_pztsb1 after setup", m_pztsb1 != 0 );
			ITsString * ptss = 0;
			HRESULT hr = m_pztsb1->GetString(&ptss);
			unitpp::assert_eq("GetString succeeded", S_OK, hr);
			unitpp::assert_true("GetString returned a valid pointer", ptss != 0 );
			int cch = 0;
			hr = m_pztsb1->get_Length(&cch);
			unitpp::assert_eq("get_Length() succeeded", S_OK, hr);
			unitpp::assert_eq("get_Length() returned proper value", g_cchTest, cch);
			SmartBstr sbstr;
			hr = m_pztsb1->get_Text(&sbstr);
			unitpp::assert_eq("get_Text succeeded", S_OK, hr);
			unitpp::assert_true("get_Text returned proper value", sbstr == g_pszTest );
			int crun = 0;
			hr = m_pztsb1->get_RunCount(&crun);
			unitpp::assert_eq("get_RunCount succeeded", S_OK, hr);
			unitpp::assert_eq("get_RunCount returned proper value", 1, crun);

			int cref = ptss->Release();
			unitpp::assert_eq("Release returned proper cref", 0, cref);
			ptss = 0;
		}

		void testModString()
		{
			unitpp::assert_true("Non-NULL m_pztsb0 after setup", m_pztsb0 != 0 );

			int cch = 0;
			HRESULT hr = m_pztsb0->get_Length(&cch);
			unitpp::assert_eq("get_Length() succeeded", S_OK, hr);
			unitpp::assert_eq("get_Length() returned proper value", 0, cch);
			int crun = 0;
			hr = m_pztsb0->get_RunCount(&crun);
			unitpp::assert_eq("get_RunCount succeeded", S_OK, hr);
			unitpp::assert_eq("get_RunCount returned proper value", 1, crun);
			SmartBstr sbstr;
			hr = m_pztsb0->get_Text(&sbstr);
			unitpp::assert_eq("get_Text succeeded with FALSE", S_FALSE, hr);
			unitpp::assert_true("get_Text returned proper value", sbstr == L"" );

			ITsTextPropsPtr qttp;
			TsIntProp tip;
			tip.m_tpt = ktptWs;
			tip.m_nVar = 0;
			tip.m_nVal = kwsSPN;
			TsTextProps::Create(&tip, 1, NULL, 0, &qttp);
			hr = m_pztsb0->ReplaceRgch(0, cch, g_pszTest2, g_cchTest2, qttp);
			unitpp::assert_eq("ReplaceRgch succeeded", S_OK, hr);

			hr = m_pztsb0->get_Length(&cch);
			unitpp::assert_eq("get_Length() succeeded", S_OK, hr);
			unitpp::assert_eq("get_Length() returned proper value", g_cchTest2, cch);
			hr = m_pztsb0->get_RunCount(&crun);
			unitpp::assert_eq("get_RunCount succeeded", S_OK, hr);
			unitpp::assert_eq("get_RunCount returned proper value", 1, crun);
			hr = m_pztsb0->get_Text(&sbstr);
			unitpp::assert_eq("get_Text succeeded", S_OK, hr);
			unitpp::assert_true("get_Text returned proper value", sbstr == g_pszTest2 );
		}

		void testNullArgs()
		{
			unitpp::assert_true("Non-NULL m_pztsb0 after setup", m_pztsb0 != 0 );
			HRESULT hr;
			try{
				CheckHr(hr = m_pztsb0->get_Text(NULL));
				unitpp::assert_eq("get_Text(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Text(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->get_Length(NULL));
				unitpp::assert_eq("get_Length(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Length(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->get_RunCount(NULL));
				unitpp::assert_eq("get_RunCount(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_RunCount(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->get_RunAt(0, NULL));
				unitpp::assert_eq("get_RunAt(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_RunAt(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->GetBoundsOfRun(0, NULL, NULL));
				unitpp::assert_eq("GetBoundsOfRun(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetBoundsOfRun(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->FetchRunInfoAt(0, NULL, NULL));
				unitpp::assert_eq("FetchRunInfoAt(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("FetchRunInfoAt(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->FetchRunInfo(0, NULL, NULL));
				unitpp::assert_eq("FetchRunInfo(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("FetchRunInfo(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->get_RunText(0, NULL));
				unitpp::assert_eq("get_RunText(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_RunText(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->GetChars(0, 0, NULL));
				unitpp::assert_eq("GetChars(0, 0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetChars(0, 0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_pztsb0->FetchChars(0, 0, NULL));
			unitpp::assert_eq("FetchChars(0, 0, NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_pztsb0->get_PropertiesAt(0, NULL));
				unitpp::assert_eq("get_PropertiesAt(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_PropertiesAt(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->get_Properties(0, NULL));
				unitpp::assert_eq("get_Properties(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Properties(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_pztsb0->Replace(0, 0, NULL, NULL));
			unitpp::assert_eq("Replace(0, 0, NULL, NULL) HRESULT", S_OK, hr);
			CheckHr(hr = m_pztsb0->ReplaceTsString(0, 0, NULL));
			unitpp::assert_eq("ReplaceTsString(0, 0, NULL) HRESULT", S_OK, hr);
			CheckHr(hr = m_pztsb0->ReplaceRgch(0, 0, NULL, 0, NULL));
			unitpp::assert_eq("ReplaceRgch(0, 0, NULL, 0, NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_pztsb0->SetProperties(0, 0, NULL));
				unitpp::assert_eq("SetProperties(0, 0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("SetProperties(0, 0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_pztsb0->SetIntPropValues(0, 0, 0, 0, 0));
			unitpp::assert_eq("SetIntPropValues(0, 0, 0, 0, 0) HRESULT", S_OK, hr);
			CheckHr(hr = m_pztsb0->SetStrPropValue(0, 0, 0, NULL));
			unitpp::assert_eq("SetStrPropValue(0, 0, 0, NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_pztsb0->GetString(NULL));
				unitpp::assert_eq("GetString(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetString(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->GetBldrClsid(NULL));
				unitpp::assert_eq("GetBldrClsid(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetBldrClsid(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->SerializeFmt(NULL));
				unitpp::assert_eq("SerializeFmt(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("SerializeFmt(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_pztsb0->SerializeFmtRgb(NULL, 0, NULL));
				unitpp::assert_eq("SerializeFmtRgb(NULL, 0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("SerializeFmtRgb(NULL, 0, NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

	public:
		TestTsStrBldr();

		virtual void Setup()
		{
			m_pztsb1 = 0;
			m_pztsb0 = 0;

			TxtRun run;
			run.m_ichLim = g_cchTest;
			TsIntProp tip;
			tip.m_tpt = ktptWs;
			tip.m_nVar = 0;
			tip.m_nVal = kwsENG;
			TsTextProps::Create(&tip, 1, NULL, 0, &run.m_qttp);
			TsStrBldr::Create(g_pszTest, g_cchTest, &run, 1, &m_pztsb1);
			TsStrBldr::Create(NULL, 0, NULL, 0, &m_pztsb0);
		}
		virtual void Teardown()
		{
			if (m_pztsb0)
			{
				m_pztsb0->Release();
				m_pztsb0 = 0;
			}
			if (m_pztsb1)
			{
				m_pztsb1->Release();
				m_pztsb1 = 0;
			}
		}
	};
}

#endif /*TESTTSSTRBLDR_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkfwk-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
/*:End Ignore*/

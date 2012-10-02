/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgWritingSystemFactory.h
Responsibility:
Last reviewed:

	Unit tests for the LgWritingSystemFactory class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTWRITINGSYSTEMFACTORY_H_INCLUDED
#define TESTWRITINGSYSTEMFACTORY_H_INCLUDED

#pragma once

#include "testLanguage.h"
#include "LgWritingSystemFactory.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for LgWritingSystemFactory
	 ******************************************************************************************/
	class TestLgWritingSystemFactory : public unitpp::suite
	{
		ILgWritingSystemFactoryPtr m_qwsf0;

		void testEngineOrNull()
		{
			unitpp::assert_true("m_qwsf0", m_qwsf0.Ptr());
			int cws;
			HRESULT hr = m_qwsf0->get_NumberOfWs(&cws);
			unitpp::assert_eq("get_NumberOfWs() HRESULT", S_OK, hr);
			unitpp::assert_eq("get_NumberOfWs(&cws)", 1, cws);

			IWritingSystemPtr qws;
			int ws = 1234567;		// Arbitrary random ws code not used anywhere else.
			hr = m_qwsf0->get_EngineOrNull(ws, &qws);
			unitpp::assert_eq("get_EngineOrNull() HRESULT", S_OK, hr);
			unitpp::assert_eq("get_EngineOrNull(1234567)", (IWritingSystem *)NULL, qws.Ptr());
		}
		void testNullArgs()
		{
			unitpp::assert_true("m_qwsf0", m_qwsf0.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qwsf0->get_Engine(0, NULL));
				unitpp::assert_eq("get_Engine(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Engine(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->get_EngineOrNull(0, NULL));
				unitpp::assert_eq("get_EngineOrNull(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_EngineOrNull(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->get_NumberOfWs(NULL));
				unitpp::assert_eq("get_NumberOfWs(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_NumberOfWs(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->get_UnicodeCharProps(NULL));
				unitpp::assert_eq("get_UnicodeCharProps(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_UnicodeCharProps(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->get_UnicodeCharProps(NULL));
				unitpp::assert_eq("get_UnicodeCharProps(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_UnicodeCharProps(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->get_DefaultCollater(0, NULL));
				unitpp::assert_eq("get_DefaultCollater(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_DefaultCollater(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->get_CharPropEngine(0, NULL));
				unitpp::assert_eq("get_CharPropEngine(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_CharPropEngine(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->get_Renderer(0,  NULL, NULL));
				unitpp::assert_eq("get_Renderer(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Renderer(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->AddEngine(NULL));
				unitpp::assert_eq("AddEngine(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("AddEngine(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->GetWsFromStr(NULL, NULL));
				unitpp::assert_eq("GetWsFromStr(NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetWsFromStr(NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qwsf0->GetStrFromWs(0, NULL));
				unitpp::assert_eq("GetStrFromWs(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetStrFromWs(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qwsf0->GetWritingSystems(NULL, 0));
			unitpp::assert_eq("GetWritingSystems(NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_qwsf0->Serialize(NULL));
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

		void testMaps()
		{
			ILgWritingSystemFactoryPtr qwsf;
			CreateTestWritingSystemFactory(&qwsf);

			// Needed for reference counting (in Linux at least).
			IWritingSystemPtr qws;
			qwsf->get_EngineOrNull(kwsEng, &qws);

			CreateTestWritingSystem(qwsf, kwsTest, kszTest);
			CreateTestWritingSystem(qwsf, kwsTest2, kszTest2);

			HRESULT hr;
			StrAnsi staMsg;
			SmartBstr sbstrOut;

			hr = qwsf->GetStrFromWs(kwsEng, &sbstrOut);
			unitpp::assert_eq("GetStrFromWs(kwsEng) HRESULT", S_OK, hr);
			staMsg.Format("GetStrFromWs(%d, &sbstrOut); expect=%S; actual=%S",
				kwsEng, kszEng.Chars(), sbstrOut.Chars());
#ifdef WIN32
			unitpp::assert_true((char*)staMsg.Chars(), wcscmp(kszEng, sbstrOut.Chars()) == 0);
#else
			unitpp::assert_true((char*)staMsg.Chars(), u_strcmp(kszEng, sbstrOut.Chars()) == 0);
#endif

			hr = qwsf->GetStrFromWs(kwsTest, &sbstrOut);
			unitpp::assert_eq("GetStrFromWs(kwsTest) HRESULT", S_OK, hr);
			staMsg.Format("GetStrFromWs(%d, &sbstrOut); expect=%S; actual=%S",
				kwsTest, kszTest.Chars(), sbstrOut.Chars());
#ifdef WIN32
			unitpp::assert_true((char*)staMsg.Chars(), wcscmp(kszTest, sbstrOut.Chars()) == 0);
#else
			unitpp::assert_true((char*)staMsg.Chars(), u_strcmp(kszTest, sbstrOut.Chars()) == 0);
#endif

			hr = qwsf->GetStrFromWs(kwsTest2, &sbstrOut);
			unitpp::assert_eq("GetStrFromWs(kwsTest2) HRESULT", S_OK, hr);
			staMsg.Format("GetStrFromWs(%d, &sbstrOut); expect=%S; actual=%S",
				kwsTest2, kszTest2.Chars(), sbstrOut.Chars());
#ifdef WIN32
			unitpp::assert_true((char*)staMsg.Chars(), wcscmp(kszTest2, sbstrOut.Chars()) == 0);
#else
			unitpp::assert_true((char*)staMsg.Chars(), u_strcmp(kszTest2, sbstrOut.Chars()) == 0);
#endif

			hr = qwsf->GetStrFromWs(123456, &sbstrOut);
			unitpp::assert_eq("GetStrFromWs(123456) HRESULT", S_FALSE, hr);
			unitpp::assert_eq("GetStrFromWs(123456) bstr length", 0, sbstrOut.Length());
			unitpp::assert_eq("GetStrFromWs(123456) bstr", (wchar *)0, sbstrOut.Bstr());

			SmartBstr sbstrWs;
			int ws;

			sbstrWs.Assign(kszEng);
			hr = qwsf->GetWsFromStr(sbstrWs, &ws);
			unitpp::assert_eq("GetWsFromStr(kszEng) HRESULT", S_OK, hr);
			unitpp::assert_eq("GetWsFromStr(kszEng) ws", kwsEng, ws);

			sbstrWs.Assign(kszTest);
			hr = qwsf->GetWsFromStr(sbstrWs, &ws);
			unitpp::assert_eq("GetWsFromStr(kszTest) HRESULT", S_OK, hr);
			unitpp::assert_eq("GetWsFromStr(kszTest) ws", kwsTest, ws);

			sbstrWs.Assign(kszTest2);
			hr = qwsf->GetWsFromStr(sbstrWs, &ws);
			unitpp::assert_eq("GetWsFromStr(kszTest2) HRESULT", S_OK, hr);
			unitpp::assert_eq("GetWsFromStr(kszTest2) ws", kwsTest2, ws);

			sbstrWs.Assign(L"123456");
			hr = qwsf->GetWsFromStr(sbstrWs, &ws);
			unitpp::assert_eq("GetWsFromStr(L\"123456\") HRESULT", S_FALSE, hr);
			unitpp::assert_eq("GetWsFromStr(L\"123456\") ws", 0, ws);

			qwsf->Shutdown();
			qwsf.Clear();
		}

	public:
		TestLgWritingSystemFactory();
		~TestLgWritingSystemFactory()
		{
		}

		// These must be public to be accessible from test_mfun::operator()().  Read the code
		// if you must know.  :-)
		virtual void Setup()
		{
		}
		virtual void Teardown()
		{
		}
		virtual void SuiteSetup()
		{
			CreateTestWritingSystemFactory(&m_qwsf0);
		}
		virtual void SuiteTeardown()
		{
			m_qwsf0->Shutdown();
			m_qwsf0.Clear();
		}

	};


}

#endif /*TESTWRITINGSYSTEMFACTORY_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

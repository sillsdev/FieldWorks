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
			CreateTestWritingSystem(qwsf, kwsTest, kszTest);
			CreateTestWritingSystem(qwsf, kwsTest2, kszTest2);

			HRESULT hr;
			StrAnsi staMsg;
			SmartBstr sbstrOut;

			hr = qwsf->GetStrFromWs(kwsEng, &sbstrOut);
			unitpp::assert_eq("GetStrFromWs(kwsEng) HRESULT", S_OK, hr);
			staMsg.Format("GetStrFromWs(%d, &sbstrOut); expect=%S; actual=%S",
				kwsEng, kszEng, sbstrOut.Chars());
			unitpp::assert_true((char*)staMsg.Chars(), wcscmp(kszEng, sbstrOut.Chars()) == 0);

			hr = qwsf->GetStrFromWs(kwsTest, &sbstrOut);
			unitpp::assert_eq("GetStrFromWs(kwsTest) HRESULT", S_OK, hr);
			staMsg.Format("GetStrFromWs(%d, &sbstrOut); expect=%S; actual=%S",
				kwsTest, kszTest, sbstrOut.Chars());
			unitpp::assert_true((char*)staMsg.Chars(), wcscmp(kszTest, sbstrOut.Chars()) == 0);

			hr = qwsf->GetStrFromWs(kwsTest2, &sbstrOut);
			unitpp::assert_eq("GetStrFromWs(kwsTest2) HRESULT", S_OK, hr);
			staMsg.Format("GetStrFromWs(%d, &sbstrOut); expect=%S; actual=%S",
				kwsTest2, kszTest2, sbstrOut.Chars());
			unitpp::assert_true((char*)staMsg.Chars(), wcscmp(kszTest2, sbstrOut.Chars()) == 0);

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

		// Usually, it's a very bad idea to use TestLangProj in a unit test.  In this case,
		// we can do it because we're using data from TestLangProj to verify that a
		// writing system factory is properly loaded from TestLangProj.  There's a chance
		// the test will be empty and void, but it can (and most likely will) test real data.
		void testLoadFactoryFromDb()
		{
			HRESULT hr;
			achar psz[MAX_COMPUTERNAME_LENGTH + 1];
			ulong cch = isizeof(psz);
			::GetComputerName(psz, &cch);
			StrUni stuMachine(psz);
			StrUni stuLocalServer;
			stuLocalServer.Format(L"%s\\SILFW", stuMachine.Chars());
			StrUni stuDbName = L"TestLangProj"; // Set the test database name.

			IOleDbEncapPtr qode; // Declare before qodc.
			qode.CreateInstance(CLSID_OleDbEncap);
			hr = qode->Init(stuLocalServer.Bstr(), stuDbName.Bstr(), NULL, koltReturnError,
				1000);
			if (FAILED(hr))
				return;			// cannot test anything.

			LgWritingSystemFactoryBuilderPtr qzwsfb = NewObj LgWritingSystemFactoryBuilder;
			ILgWritingSystemFactoryPtr qwsf;
			hr = qzwsfb->GetWritingSystemFactory(qode, NULL, &qwsf);
			unitpp::assert_eq("LgWritingSystemFactoryBuilder::GetWritingSystemFactory hr",
				S_OK, hr);
			unitpp::assert_true("LgWritingSystemFactoryBuilder::GetWritingSystemFactory",
				qwsf.Ptr());

			int cwsObj;
			hr = qwsf->get_NumberOfWs(&cwsObj);
			if (FAILED(hr))
				return;			// cannot test anything.
			if (!cwsObj)
				return;			// nothing to test.

			IOleDbCommandPtr qodc;
			ComBool fIsNull;
			ComBool fMoreRows;
			ULONG cbSpaceTaken;

			hr = qode->CreateCommand(&qodc);
			if (FAILED(hr))
				return;			// cannot test anything.

			StrUni stu(L"select [Id], ICULocale from LgWritingSystem");
			hr = qode->CreateCommand(&qodc);
			if (FAILED(hr))
				return;			// cannot test anything.
			// Get test data from database
			hr = qodc->ExecCommand(stu.Bstr(), knSqlStmtSelectWithOneRowset);
			if (FAILED(hr))
				return;			// cannot test anything.
			hr = qodc->GetRowset(0);
			if (FAILED(hr))
				return;			// cannot test anything.
			hr = qodc->NextRow(&fMoreRows);
			if (FAILED(hr))
				return;			// cannot test anything.
			int hvoWs;
			StrUni stuLocale;
			const int kcchBuffer = MAX_PATH;
			OLECHAR rgchLocale[kcchBuffer];
			int nLoop = 0;
			StrAnsi staMsg;
			while (fMoreRows)
			{
				nLoop++;
				if (nLoop > cwsObj)
					break;
				hr = qodc->GetColValue(1, reinterpret_cast <BYTE *>(&hvoWs), isizeof(hvoWs),
					&cbSpaceTaken, &fIsNull, 0);
				if (SUCCEEDED(hr))
				{
					hr = qodc->GetColValue(2, reinterpret_cast <BYTE *>(rgchLocale),
						kcchBuffer * isizeof(OLECHAR), &cbSpaceTaken, &fIsNull, 2);
				}
				if (SUCCEEDED(hr) && !fIsNull && rgchLocale[0])
				{
					stuLocale = rgchLocale;
					int wsId = -1;
					SmartBstr out;
					hr = qwsf->GetStrFromWs(hvoWs, &out);
					staMsg.Format("[%d] GetStrFromWs(%d, &out) HRESULT", nLoop, hvoWs);
					unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
					staMsg.Format("[%d] GetStrFromWs(%d, &out); expect=%s; actual=%s",
						nLoop, hvoWs, stuLocale.Chars(), out.Chars());
					unitpp::assert_true(staMsg.Chars(),
						wcscmp(stuLocale.Chars(), out.Chars()) == 0);
					hr = qwsf->GetWsFromStr(stuLocale.Bstr(), &wsId);
					staMsg.Format("[%d] GetWsFromStr(\"%S\", &out) HRESULT",
						nLoop, stuLocale.Chars());
					unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
					staMsg.Format("[%d] GetWsFromStr(\"%S\", &out)", nLoop, stuLocale.Chars());
					unitpp::assert_eq(staMsg.Chars(), hvoWs, wsId);
				}
				hr = qodc->NextRow(&fMoreRows);
				if (FAILED(hr))
					break;
			}
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

/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgCollation.h
Responsibility:
Last reviewed:

	Unit tests for the Collation class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGCOLLATION_H_INCLUDED
#define TESTLGCOLLATION_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for Collation
	 ******************************************************************************************/
	class TestLgCollation : public unitpp::suite
	{
		ICollationPtr m_qcoll0;

		void testNullArgs()
		{
			unitpp::assert_true("m_qcoll0", m_qcoll0.Ptr());
			HRESULT hr;
			try{
				CheckHr(hr = m_qcoll0->get_Name(0, NULL));
				unitpp::assert_eq("get_Name(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Name(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qcoll0->put_Name(0, NULL));
			unitpp::assert_eq("put_Name(0, NULL) HRESULT", S_OK, hr);		// removes name.
			try{
				CheckHr(hr = m_qcoll0->get_NameWsCount(NULL));
				unitpp::assert_eq("get_NameWsCount(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_NameWsCount(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qcoll0->get_NameWss(0, NULL));
			unitpp::assert_eq("get_NameWss(0, NULL) HRESULT", S_OK, hr);	// empty array
			try{
				CheckHr(hr = m_qcoll0->get_WinLCID(NULL));
				unitpp::assert_eq("get_WinLCID(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_WinLCID(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoll0->get_WinCollation(NULL));
				unitpp::assert_eq("get_WinCollation(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_WinCollation(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qcoll0->put_WinCollation(NULL));
			unitpp::assert_eq("put_WinCollation(NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_qcoll0->get_IcuResourceName(NULL));
				unitpp::assert_eq("get_IcuResourceName(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_IcuResourceName(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qcoll0->put_IcuResourceName(NULL));
			unitpp::assert_eq("put_IcuResourceName(NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_qcoll0->get_IcuResourceText(NULL));
				unitpp::assert_eq("get_IcuResourceText(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_IcuResourceText(NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qcoll0->put_IcuResourceText(NULL));
			unitpp::assert_eq("put_IcuResourceText(NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_qcoll0->get_Dirty(NULL));
				unitpp::assert_eq("get_Dirty(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Dirty(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoll0->WriteAsXml(NULL, 0));
				unitpp::assert_eq("WriteAsXml(NULL, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("WriteAsXml(NULL, 0) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoll0->Serialize(NULL));
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Serialize(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qcoll0->Deserialize(NULL));
				unitpp::assert_eq("Deserialize(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Deserialize(NULL) HRESULT", E_POINTER, thr.Result());
			}
		}

		void testIcuRules()
		{
#if 3
			printf("NOTE: Collation.IcuRules -- Rewrite to not use TestLangProj DB\r\n");
#else
// THIS IS TOTALLY BOGUS -- A UNIT TEST SHOULD NEVER DEPEND ON A DATABASE UNLESS IT CREATES THE
// DATABASE ITSELF (possibly in suite setup)!!
// FURTHERMORE, IT SHOULD NOT BE USING CheckHr()!!!!!
//-			HRESULT hr;
//-			// Get the local server name.
//-			achar psz[MAX_COMPUTERNAME_LENGTH + 1];
//-			ulong cch = isizeof(psz);
//-			::GetComputerName(psz, &cch);
//-			StrUni stuMachine(psz);
//-			StrUni stuServer;
//-			stuServer.Format(L"%s\\SILFW", stuMachine.Chars());
//-
//-			// Set the test database name.
//-			StrUni stuDbName = L"TestLangProj";
//-
//-			IOleDbEncapPtr qode; // Declare before qodc.
//-			qode.CreateInstance(CLSID_OleDbEncap);
//-			CheckHr(qode->Init(stuServer.Bstr(), stuDbName.Bstr(), NULL,
//-				koltReturnError, 1000));
//-
//-			ILgWritingSystemFactoryPtr qwsf;
//-			CreateTestWritingSystemFactory(&qwsf);	// DON'T USE THE DATABASE!!
//-
//-			IWritingSystemPtr qws;
//-			int ws;
//-			SmartBstr sbstrWs(L"en");
//-			hr = qwsf->GetWsFromStr(sbstrWs, &ws);
//-
//-			hr = qwsf->get_Engine(ws, &qws);
//-
//-			int ccoll;
//-			qws->get_CollationCount(&ccoll);
//-			unitpp::assert_true("count of ENG-collations is 2", ccoll == 2);
//-			ICollationPtr qcoll;
//-			CheckHr(qws->get_Collation(1, &qcoll));
//-
//-			SmartBstr sbstrRules;
//-			qcoll->get_IcuRules(&sbstrRules);
//-			unitpp::assert_true("Second ENG collation check",
//-				wcscmp(sbstrRules.Chars(), L"& N < n\\u0303, N\\u0303") == 0);
//-
//-			SmartBstr sbstrNewRules(L"New Rules");
//-			SmartBstr sbstrRulesT;
//-			qcoll->put_IcuRules(sbstrNewRules);
//-			qcoll->get_IcuRules(&sbstrRulesT);
//-			unitpp::assert_true("Put collation check",
//-				wcscmp(sbstrNewRules.Chars(), sbstrRulesT.Chars()) == 0);
//-
//-			qcoll->put_IcuRules(sbstrRules);
//-			qcoll->get_IcuRules(&sbstrRulesT);
//-			unitpp::assert_true("Put collation check",
//-				wcscmp(sbstrRules.Chars(), sbstrRulesT.Chars()) == 0);
//-			qwsf->Shutdown();
#endif
		}

	public:
		TestLgCollation();
		virtual void SuiteSetup()
		{
			Collation::CreateCom(NULL, IID_ICollation, (void **)&m_qcoll0);
		}
		virtual void SuiteTeardown()
		{
			m_qcoll0.Clear();
		}
	};


}

#endif /*TESTLGCOLLATION_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

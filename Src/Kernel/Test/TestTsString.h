/*-------------------------------------------------------------------*//*:Ignore these comments.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestTsString.h
Responsibility:
Last reviewed:

	Unit tests for the TsString classes (ITsString interface, TsStrSingle, and TsStrMulti).
----------------------------------------------------------------------------------------------*/
#ifndef TESTTSSTRING_H_INCLUDED
#define TESTTSSTRING_H_INCLUDED

#pragma once

#include "testFwKernel.h"
//#include "LanguageTlb.h"
#include <stdio.h>

#if !WIN32 // TODO-Linux FWNX-198: thread callback for testThreadedMakeString
void * TestThreadedMakeString( void *arg )
{
	int wsEng = *(int*)arg;

	SmartBstr sbstr1;
	SmartBstr sbstr2;
	StrUni str1 = L"This is a longer string for more complex tests";
	StrUni str2 = L"This is a longer still string for more complex tests";
	str1.GetBstr(&sbstr1);
	str2.GetBstr(&sbstr2);

	ITsStrFactoryPtr qtsf;
	qtsf.CreateInstance(CLSID_TsStrFactory);
	for(int i = 0; i < 100; ++i)
	{
		ITsStringPtr qts1;
		ITsStringPtr qts2;
		qtsf->MakeString(sbstr1, wsEng, &qts1);
		qtsf->MakeString(sbstr2, wsEng, &qts2);
	}

	return( 0 );
}
#endif

namespace TestFwKernel
{
	// Note: knmFCD is not tested because we don't know what it's supposed to do.
	FwNormalizationMode g_rgnmNormalizations[] = {knmNFD, knmNFKD, knmNFC, knmNFKC, knmNFSC };
	const int g_cnmNormalizations = isizeof(g_rgnmNormalizations)/isizeof(FwNormalizationMode);
	class TestTsString : public unitpp::suite
	{
		ITsStringPtr m_qtssEmpty;  // empty, except for the (required) writing system.
		ITsStringPtr m_qtssOneRun; // "This is a test!"
		ITsStringPtr m_qtssTwoRuns; // "This is<bold> a test!</bold>"
		ITsStrFactoryPtr m_qtsf;
		ILgWritingSystemFactoryPtr m_qwsf;
		int m_wsEng;
		int m_wsStk;

		/*--------------------------------------------------------------------------------------
			Test the COM methods that fetch the underlying character string.
		--------------------------------------------------------------------------------------*/
		void testStringText()
		{
			unitpp::assert_true("Non-NULL m_qtssEmpty after setup", m_qtssEmpty.Ptr() != 0);
			unitpp::assert_true("Non-NULL m_qtssOneRun after setup", m_qtssOneRun.Ptr() != 0);
			unitpp::assert_true("Non-NULL m_qtssTwoRuns after setup", m_qtssTwoRuns.Ptr() != 0);

			HRESULT hr;
			SmartBstr sbstr0;
			hr = m_qtssEmpty->get_Text(&sbstr0);
			unitpp::assert_eq("qtss0->get_Text succeeded", S_FALSE, hr);
			unitpp::assert_true("qtss0->get_Text returned proper value", sbstr0 == L"");
			SmartBstr sbstr1;
			hr = m_qtssOneRun->get_Text(&sbstr1);
			unitpp::assert_eq("qtss1->get_Text succeeded", S_OK, hr);
			unitpp::assert_true("qtss1->get_Text returned proper value", sbstr1 == g_pszTest.Chars());
			SmartBstr sbstr2;
			hr = m_qtssTwoRuns->get_Text(&sbstr2);
			unitpp::assert_eq("qtss2->get_Text succeeded", S_OK, hr);
			unitpp::assert_true("qtss2->get_Text returned proper value", sbstr2 == g_pszTest.Chars());

			int cch0 = 0;
			hr = m_qtssEmpty->get_Length(&cch0);
			unitpp::assert_eq("qtss0->get_Length() succeeded", S_OK, hr);
			unitpp::assert_eq("qtss0->get_Length() returned proper value", 0, cch0);
			int cch1 = 0;
			hr = m_qtssOneRun->get_Length(&cch1);
			unitpp::assert_eq("qtss1->get_Length() succeeded", S_OK, hr);
			unitpp::assert_eq("qtss1->get_Length() returned proper value", g_cchTest, cch1);
			int cch2 = 0;
			hr = m_qtssTwoRuns->get_Length(&cch2);
			unitpp::assert_eq("qtss2->get_Length() succeeded", S_OK, hr);
			unitpp::assert_eq("qtss2->get_Length() returned proper value", g_cchTest, cch2);

			hr = m_qtssEmpty->GetChars(0, cch0, &sbstr0);
			unitpp::assert_eq("qtss0->GetChars succeeded", S_OK, hr);
			unitpp::assert_true("qtss0->GetChars returned proper value", sbstr0 == L"");
			hr = m_qtssOneRun->GetChars(0, cch1, &sbstr1);
			unitpp::assert_eq("qtss1->GetChars succeeded", S_OK, hr);
			unitpp::assert_true("qtss1->GetChars returned proper value", sbstr1 == g_pszTest.Chars());
			hr = m_qtssTwoRuns->GetChars(0, cch2, &sbstr2);
			unitpp::assert_eq("qtss2->GetChars succeeded", S_OK, hr);
			unitpp::assert_true("qtss2->GetChars returned proper value", sbstr2 == g_pszTest.Chars());

			int cch = cch0;
			if (cch < cch1)
				cch = cch1;
			if (cch < cch2)
				cch = cch2;
			Vector<wchar> vch;
			vch.Resize(cch);
			hr = m_qtssEmpty->FetchChars(0, cch0, vch.Begin());
			StrUni stu0(vch.Begin(), cch0);
			unitpp::assert_eq("qtss0->FetchChars succeeded", S_OK, hr);
			unitpp::assert_true("qtss0->FetchChars returned proper value", stu0 == L"");
			hr = m_qtssOneRun->FetchChars(0, cch1, vch.Begin());
			StrUni stu1(vch.Begin(), cch1);
			unitpp::assert_eq("qtss1->FetchChars succeeded", S_OK, hr);
			unitpp::assert_true("qtss1->FetchChars returned proper value", stu1 == g_pszTest.Chars());
			hr = m_qtssTwoRuns->FetchChars(0, cch2, vch.Begin());
			StrUni stu2(vch.Begin(), cch2);
			unitpp::assert_eq("qtss2->FetchChars succeeded", S_OK, hr);
			unitpp::assert_true("qtss2->FetchChars returned proper value", stu2 == g_pszTest.Chars());

			const wchar * psz0;
			hr = m_qtssEmpty->LockText(&psz0, &cch0);
			stu0.Assign(psz0, cch0);
			unitpp::assert_eq("qtss0->LockText succeeded", S_OK, hr);
			unitpp::assert_true("qtss0->LockText returned proper value", stu0 == L"");
			hr = m_qtssEmpty->UnlockText(psz0);
			unitpp::assert_eq("qtss0->UnlockText succeeded", S_OK, hr);
			const wchar * psz1;
			hr = m_qtssOneRun->LockText(&psz1, &cch1);
			stu1.Assign(psz1, cch1);
			unitpp::assert_eq("qtss1->LockText succeeded", S_OK, hr);
			unitpp::assert_true("qtss1->LockText returned proper value", stu1 == g_pszTest.Chars());
			hr = m_qtssOneRun->UnlockText(psz1);
			unitpp::assert_eq("qtss1->UnlockText succeeded", S_OK, hr);
			const wchar * psz2;
			hr = m_qtssTwoRuns->LockText(&psz2, &cch2);
			stu2.Assign(psz2, cch2);
			unitpp::assert_eq("qtss2->LockText succeeded", S_OK, hr);
			unitpp::assert_true("qtss2->LockText returned proper value", stu2 == g_pszTest.Chars());
			hr = m_qtssTwoRuns->UnlockText(psz2);
			unitpp::assert_eq("qtss2->UnlockText succeeded", S_OK, hr);
		}

		/*--------------------------------------------------------------------------------------
			Test the COM methods that fetch the underlying run information.
		--------------------------------------------------------------------------------------*/
		void testStringRuns()
		{
			unitpp::assert_true("Non-NULL m_qtssEmpty after setup", m_qtssEmpty.Ptr() != 0);
			unitpp::assert_true("Non-NULL m_qtssOneRun after setup", m_qtssOneRun.Ptr() != 0);
			unitpp::assert_true("Non-NULL m_qtssTwoRuns after setup", m_qtssTwoRuns.Ptr() != 0);

			HRESULT hr;
			int crun0 = 0;
			hr = m_qtssEmpty->get_RunCount(&crun0);
			unitpp::assert_eq("qtss0->get_RunCount succeeded", S_OK, hr);
			unitpp::assert_eq("qtss0->get_RunCount returned proper value", 1, crun0);
			int crun1 = 0;
			hr = m_qtssOneRun->get_RunCount(&crun1);
			unitpp::assert_eq("qtss1->get_RunCount succeeded", S_OK, hr);
			unitpp::assert_eq("qtss1->get_RunCount returned proper value", 1, crun1);
			int crun2 = 0;
			hr = m_qtssTwoRuns->get_RunCount(&crun2);
			unitpp::assert_eq("qtss2->get_RunCount succeeded", S_OK, hr);
			unitpp::assert_eq("qtss2->get_RunCount returned proper value", 2, crun2);
		}

		/*--------------------------------------------------------------------------------------
			Test shortcut methods that optimize access to some key information from C#
		--------------------------------------------------------------------------------------*/
		void testShortcuts()
		{
			// Make a string using two writing systems and with a style in the second run.
			StrUni stuInput(L"This is a string");
			int cchFirstRun = wcslen(L"This is a ");
			ITsStringPtr qtss;
			m_qtsf->MakeStringRgch(stuInput.Chars(), stuInput.Length(), m_wsEng, &qtss);
			ITsStrBldrPtr qtsb;
			HRESULT hr = qtss->GetBldr(&qtsb);
			qtsb->SetIntPropValues(cchFirstRun,stuInput.Length(), ktptWs, ktpvDefault, m_wsStk);
			StrUni stuStyleName(L"special");
			qtsb->SetStrPropValue(cchFirstRun, stuInput.Length(), ktptNamedStyle, stuStyleName.Bstr());
			hr = qtsb->GetString(&qtss);

			int ws;
			//--get_WritingSystem
			hr = m_qtssEmpty->get_WritingSystem(0, &ws);
			unitpp::assert_eq("m_qtssEmpty->get_WritingSystem succeeded", S_OK, hr);
			unitpp::assert_eq("m_qtssEmpty->get_WritingSystem returned proper value", m_wsEng, ws);

			hr = m_qtssOneRun->get_WritingSystem(0, &ws);
			unitpp::assert_eq("m_qtssOneRun->get_WritingSystem succeeded", S_OK, hr);
			unitpp::assert_eq("m_qtssOneRun->get_WritingSystem returned proper value", m_wsEng, ws);

			hr = qtss->get_WritingSystem(1, &ws);
			unitpp::assert_eq("qtss->get_WritingSystem(1) returned proper value", m_wsStk, ws);

			//--get_WritingSystemAt
			hr = m_qtssEmpty->get_WritingSystemAt(0, &ws);
			unitpp::assert_eq("m_qtssEmpty->get_WritingSystemAt succeeded", S_OK, hr);
			unitpp::assert_eq("m_qtssEmpty->get_WritingSystemAt returned proper value", m_wsEng, ws);

			hr = m_qtssOneRun->get_WritingSystemAt(0, &ws);
			unitpp::assert_eq("m_qtssOneRun->get_WritingSystemAt succeeded", S_OK, hr);
			unitpp::assert_eq("m_qtssOneRun->get_WritingSystemAt returned proper value", m_wsEng, ws);

			hr = qtss->get_WritingSystemAt(1, &ws);
			unitpp::assert_eq("qtss->get_WritingSystemAt(1) returned proper value", m_wsEng, ws);
			hr = qtss->get_WritingSystemAt(cchFirstRun, &ws);
			unitpp::assert_eq("qtss->get_WritingSystemAt(cchFirstRun) returned proper value", m_wsStk, ws);
			hr = qtss->get_WritingSystemAt(cchFirstRun - 1, &ws);
			unitpp::assert_eq("qtss->get_WritingSystemAt(cchFirstRun - 1) returned proper value", m_wsEng, ws);
			hr = qtss->get_WritingSystemAt(stuInput.Length(), &ws);
			unitpp::assert_eq("qtss->get_WritingSystemAt(stuInput1.Length()) returned proper value", m_wsStk, ws);

			//----get_StringProperty---
			SmartBstr bstr;
			hr = m_qtssEmpty->get_StringProperty(0, ktptNamedStyle, &bstr);
			unitpp::assert_true("no style on empty string", bstr == NULL);
			hr = qtss->get_StringProperty(0, ktptNamedStyle, &bstr);
			unitpp::assert_true("no style on first run of string", bstr == NULL);
			hr = qtss->get_StringProperty(1, ktptNamedStyle, &bstr);
			unitpp::assert_true("got style on second run of string", wcscmp(bstr.Chars(), stuStyleName.Chars()) == 0);

			// ---get_StringPropertyAt
			hr = m_qtssEmpty->get_StringPropertyAt(0, ktptNamedStyle, &bstr);
			unitpp::assert_true("no style on empty string", bstr == NULL);
			hr = qtss->get_StringPropertyAt(0, ktptNamedStyle, &bstr);
			unitpp::assert_true("no style on first char of string", bstr == NULL);
			hr = qtss->get_StringPropertyAt(cchFirstRun, ktptNamedStyle, &bstr);
			unitpp::assert_true("got style on secnd run of string by chars", wcscmp(bstr.Chars(), stuStyleName.Chars()) == 0);

			// -- get_IsRunOrc
			ComBool fIsOrc;
			hr = m_qtssEmpty->get_IsRunOrc(0, &fIsOrc);
			unitpp::assert_false("no ORC on empty string", fIsOrc);
			hr = qtss->get_IsRunOrc(0, &fIsOrc);
			unitpp::assert_false("no ORC on first run", fIsOrc);
			hr = qtss->get_IsRunOrc(1, &fIsOrc);
			unitpp::assert_false("no ORC on second run", fIsOrc);

			OleStringLiteral hexValue = L"\xfffcQ";
			qtsb->ReplaceRgch(stuInput.Length(), stuInput.Length(), hexValue, 2, NULL);
			qtsb->SetIntPropValues(stuInput.Length(),stuInput.Length() + 2, ktptWs, ktpvDefault, m_wsEng);
			hr = qtsb->GetString(&qtss);
			hr = qtss->get_IsRunOrc(2, &fIsOrc);
			unitpp::assert_false("no ORC on two-char run starting with ORC", fIsOrc);
			qtsb->SetIntPropValues(stuInput.Length() + 1,stuInput.Length() + 2, ktptWs, ktpvDefault, m_wsStk);
			hr = qtsb->GetString(&qtss);
			hr = qtss->get_IsRunOrc(2, &fIsOrc);
			unitpp::assert_true(" finally an ORC!", fIsOrc);
			hr = qtss->get_IsRunOrc(3, &fIsOrc);
			unitpp::assert_false("no ORC on one-char run after ORC", fIsOrc);

		}

		/*--------------------------------------------------------------------------------------
			Test the COM methods for handling NULL pointer arguments properly.
		--------------------------------------------------------------------------------------*/
		void testNullArgs()
		{
			unitpp::assert_true("Non-NULL m_qtssEmpty after setup", m_qtssEmpty.Ptr() != 0);

			HRESULT hr;
			try{
				CheckHr(hr = m_qtssEmpty->get_Text(NULL));
				unitpp::assert_eq("get_Text(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable &thr)
			{
				unitpp::assert_eq("get_Text(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->get_Length(NULL));
				unitpp::assert_eq("get_Length(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Length(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->get_RunCount(NULL));
				unitpp::assert_eq("get_RunCount(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_RunCount(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->get_RunAt(0, NULL));
				unitpp::assert_eq("get_RunAt(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_RunAt(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->get_MinOfRun(0, NULL));
				unitpp::assert_eq("get_MinOfRun(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_MinOfRun(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->get_LimOfRun(0, NULL));
				unitpp::assert_eq("get_LimOfRun(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_LimOfRun(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->GetBoundsOfRun(0, NULL, NULL));
				unitpp::assert_eq("GetBoundsOfRun(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetBoundsOfRun(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->FetchRunInfoAt(0, NULL, NULL));
				unitpp::assert_eq("FetchRunInfoAt(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("FetchRunInfoAt(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->FetchRunInfo(0, NULL, NULL));
				unitpp::assert_eq("FetchRunInfo(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("FetchRunInfo(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->get_RunText(0, NULL));
				unitpp::assert_eq("get_RunText(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_RunText(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->GetChars(0, 0, NULL));
				unitpp::assert_eq("GetChars(0, 0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetChars(0, 0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			CheckHr(hr = m_qtssEmpty->FetchChars(0, 0, NULL));
			unitpp::assert_eq("FetchChars(0, 0, NULL) HRESULT", S_OK, hr);
			try{
				CheckHr(hr = m_qtssEmpty->LockText(NULL, NULL));
				unitpp::assert_eq("LockText(NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("LockText(NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->UnlockText(NULL));
				unitpp::assert_eq("UnlockText(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("UnlockText(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->LockRun(0, NULL, NULL));
				unitpp::assert_eq("LockRun(0, NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("LockRun(0, NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->UnlockRun(0, NULL));
				unitpp::assert_eq("UnlockRun(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("UnlockRun(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->get_PropertiesAt(0, NULL));
				unitpp::assert_eq("get_PropertiesAt(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_PropertiesAt(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->get_Properties(0, NULL));
				unitpp::assert_eq("get_Properties(0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("get_Properties(0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->GetBldr(NULL));
				unitpp::assert_eq("GetBldr(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetBldr(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->GetIncBldr(NULL));
				unitpp::assert_eq("GetIncBldr(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetIncBldr(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->GetFactoryClsid(NULL));
				unitpp::assert_eq("GetFactoryClsid(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("GetFactoryClsid(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->SerializeFmt(NULL));
				unitpp::assert_eq("SerializeFmt(NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("SerializeFmt(NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->SerializeFmtRgb(NULL, 0, NULL));
				unitpp::assert_eq("SerializeFmtRgb(NULL, 0, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("SerializeFmtRgb(NULL, 0, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->Equals(NULL, NULL));
				unitpp::assert_eq("Equals(NULL, NULL) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("Equals(NULL, NULL) HRESULT", E_POINTER, thr.Result());
			}
			try{
				CheckHr(hr = m_qtssEmpty->WriteAsXml(NULL, NULL, 0, 0, 0));
				unitpp::assert_eq("WriteAsXml(NULL, NULL, 0, 0, 0) HRESULT", E_POINTER, hr);
			}
			catch(Throwable& thr)
			{
				unitpp::assert_eq("WriteAsXml(NULL, NULL, 0, 0, 0) HRESULT", E_POINTER, thr.Result());
			}
		}

		/*--------------------------------------------------------------------------------------
			Test typecasting to the underlying class objects.
		--------------------------------------------------------------------------------------*/
		void testUnderlying()
		{
			unitpp::assert_true("Non-NULL m_qtssEmpty after setup", m_qtssEmpty.Ptr() != 0);
			unitpp::assert_true("Non-NULL m_qtssOneRun after setup", m_qtssOneRun.Ptr() != 0);
			unitpp::assert_true("Non-NULL m_qtssTwoRuns after setup", m_qtssTwoRuns.Ptr() != 0);

			TsStrSingle * psts0 = dynamic_cast<TsStrSingle *>(m_qtssEmpty.Ptr());
			unitpp::assert_true("qtss0 typecast to TsStrSingle * okay", psts0 != NULL);
			TsStrSingle * psts1 = dynamic_cast<TsStrSingle *>(m_qtssOneRun.Ptr());
			unitpp::assert_true("qtss1 typecast to TsStrSingle * okay", psts1 != NULL);
			TsStrSingle * psts2 = dynamic_cast<TsStrSingle *>(m_qtssTwoRuns.Ptr());
			unitpp::assert_true("qtss2 typecast to TsStrSingle * okay", psts2 == NULL);

			TsStrMulti * pstm0 = dynamic_cast<TsStrMulti *>(m_qtssEmpty.Ptr());
			unitpp::assert_true("qtss0 typecast to TsStrMulti * okay", pstm0 == NULL);
			TsStrMulti * pstm1 = dynamic_cast<TsStrMulti *>(m_qtssOneRun.Ptr());
			unitpp::assert_true("qtss1 typecast to TsStrMulti * okay", pstm1 == NULL);
			TsStrMulti * pstm2 = dynamic_cast<TsStrMulti *>(m_qtssTwoRuns.Ptr());
			unitpp::assert_true("qtss2 typecast to TsStrMulti * okay", pstm2 != NULL);
		}

		void resetInputString(ITsStrBldr **qtsb, ITsStringPtr qtssInit)
		{
			if (*qtsb != NULL)
				(*qtsb)->Release();
			qtssInit->GetBldr(qtsb);
		}

		void testApplyWsToRunsAndCheckStringEquality_LT1417()
		{
			unitpp::assert_true("Non-NULL m_qtssOneRun after setup", m_qtssOneRun.Ptr() != 0);

			ITsStringPtr qtssResult;
			ITsStringPtr qtssTwoWsRuns;
			ITsTextPropsPtr qttpInput;
			ITsTextPropsPtr qttpResult;
			int ichMinRun1 = 0;
			int ichLimRun1 = -1;
			int ichLimRun2 = -1;
			int wsResult;
			int nVar;
			int crun;

			ITsStrBldr *qtsb = NULL;

			// Setup the text property with the new ws.
			// 1) apply a different ws to a string with one run.
			// 1.0) apply the same ws resulting in the same string.
			resetInputString(&qtsb, m_qtssOneRun);
			TsIntProp rgtip[1];
			rgtip[0].m_tpt = ktptWs;
			rgtip[0].m_nVar = 0;
			rgtip[0].m_nVal = m_wsEng;
			TsTextProps::Create(rgtip, 1, NULL, 0, &qttpInput);
			qtsb->get_Length(&ichLimRun1);
			qtsb->SetProperties(ichMinRun1, ichLimRun1, qttpInput);
			qtsb->GetString(&qtssResult);
			SmartBstr sbstr0;
			SmartBstr sbstr1;
			m_qtssOneRun->get_Text(&sbstr0);
			qtssResult->get_Text(&sbstr1);
			ComBool fEqual;
			qtssResult->Equals(m_qtssOneRun, &fEqual);
			unitpp::assert_true("1.0 Applying identical ws property should result in identical string.", bool(fEqual));

			// 1.1) apply a different ws resulting in 1 run.
			resetInputString(&qtsb, m_qtssOneRun);
			rgtip[0].m_nVal = m_wsStk;
			TsTextProps::Create(rgtip, 1, NULL, 0, &qttpInput);
			qtsb->get_Length(&ichLimRun1);
			qtsb->SetProperties(ichMinRun1, ichLimRun1, qttpInput);
			qtsb->GetString(&qtssResult);

			qtssResult->get_RunCount(&crun);
			unitpp::assert_eq("1.1 Setting ws should result in 1 run.", 1, crun);

			qtssResult->get_PropertiesAt(ichMinRun1, &qttpResult);
			qttpResult->GetIntPropValues(ktptWs, &nVar, &wsResult);
			unitpp::assert_eq("1.1 Setting ws should result in new ws for run.", m_wsStk, wsResult);

			qtssResult->Equals(m_qtssOneRun, &fEqual);
			unitpp::assert_true("1.1 Applying different ws property should result in different string.", !(bool(fEqual)));

			// 1.2) apply a different ws resulting in 2 runs.
			resetInputString(&qtsb, m_qtssOneRun);
			rgtip[0].m_nVal = m_wsStk;
			TsTextProps::Create(rgtip, 1, NULL, 0, &qttpInput);
			qtsb->get_Length(&ichLimRun2);
			int ichMinRun2Init = ichLimRun2 / 2;
			ichLimRun1 = ichMinRun2Init;
			qtsb->SetProperties(ichLimRun1, ichLimRun2, qttpInput);
			qtsb->GetString(&qtssResult);
			qtsb->GetString(&qtssTwoWsRuns);  // required for section 2 tests below.

			qtssResult->get_RunCount(&crun);
			unitpp::assert_eq("1.2 Setting ws should result in 2 runs.", 2, crun);

			qtssResult->get_PropertiesAt(ichMinRun1, &qttpResult);
			qttpResult->GetIntPropValues(ktptWs, &nVar, &wsResult);
			unitpp::assert_eq("1.2 Setting ws should result in same ws for first run.", m_wsEng, wsResult);

			qtssResult->get_PropertiesAt(ichLimRun1, &qttpResult);
			qttpResult->GetIntPropValues(ktptWs, &nVar, &wsResult);
			unitpp::assert_eq("1.2 Setting ws should result in different ws for second run.", m_wsStk, wsResult);

			qtssResult->Equals(m_qtssOneRun, &fEqual);
			unitpp::assert_true("1.2 Applying new ws run should result in different string.", !(bool(fEqual)));

			// 1.3) apply a different ws resulting in 3 runs.
			// ...TODO

			// 2) apply a different ws to a string with two ws runs.
			// 2.1) apply a different ws resulting in 1 run.
			resetInputString(&qtsb, qtssTwoWsRuns);
			rgtip[0].m_nVal = m_wsStk;
			TsTextProps::Create(rgtip, 1, NULL, 0, &qttpInput);
			qtsb->get_Length(&ichLimRun2);
			qtsb->SetProperties(ichMinRun1, ichLimRun2, qttpInput);
			qtsb->GetString(&qtssResult);

			qtssResult->get_RunCount(&crun);
			unitpp::assert_eq("2.1 Setting ws should result in 1 run.", 1, crun);

			qtssResult->get_PropertiesAt(ichMinRun1, &qttpResult);
			qttpResult->GetIntPropValues(ktptWs, &nVar, &wsResult);
			unitpp::assert_eq("2.1 Setting ws should result in new ws for run.", m_wsStk, wsResult);

			qtssResult->Equals(qtssTwoWsRuns, &fEqual);
			unitpp::assert_true("2.1 Applying ws should result in different string.", !(bool(fEqual)));

			// 2.2) apply a different ws resulting in 2 runs.
			// 2.2.1 Extend the first ws of a double run string. (cf. LT-1417)
			resetInputString(&qtsb, qtssTwoWsRuns);
			rgtip[0].m_nVal = m_wsEng;
			TsTextProps::Create(rgtip, 1, NULL, 0, &qttpInput);
			qtsb->get_Length(&ichLimRun2);
			ichLimRun1 = ichMinRun2Init + 2; // try to extend the first run by 2 characters.
			qtsb->SetProperties(ichMinRun2Init, ichLimRun1, qttpInput);
			qtsb->GetString(&qtssResult);

			qtssResult->get_RunCount(&crun);
			unitpp::assert_eq("2.2.1 Setting ws should result in 2 runs.", 2, crun);

			qtssResult->get_PropertiesAt(ichMinRun1, &qttpResult);
			qttpResult->GetIntPropValues(ktptWs, &nVar, &wsResult);
			unitpp::assert_eq("2.2.1 Setting ws should result in same ws for first run.", m_wsEng, wsResult);

			qtssResult->get_PropertiesAt(ichMinRun2Init, &qttpResult);
			qttpResult->GetIntPropValues(ktptWs, &nVar, &wsResult);
			unitpp::assert_eq("2.2.1 Setting ws should have extended ws for first run.", m_wsEng, wsResult);

			qtssResult->get_PropertiesAt(ichLimRun1, &qttpResult);
			qttpResult->GetIntPropValues(ktptWs, &nVar, &wsResult);
			unitpp::assert_eq("2.2.1 Setting ws should have shortened the range for second run.", m_wsStk, wsResult);

			qtssResult->Equals(qtssTwoWsRuns, &fEqual);
			unitpp::assert_true("2.2.1 extending run of first ws should result in different strings.", !(bool(fEqual)));

			// 2.3) apply a different ws resulting in 3 runs.
			qtsb->Release();
			qttpInput.Clear();
			qttpResult.Clear();
			qtssResult.Clear();
			qtssTwoWsRuns.Clear();
		}

		/*--------------------------------------------------------------------------------------
			Test trivial normalization: for ordinary ASCII text, the original string should
			be found to be normalized.
		--------------------------------------------------------------------------------------*/
		void testTrivialNormalization()
		{
			unitpp::assert_true("Non-NULL m_qtssEmpty after setup", m_qtssEmpty.Ptr() != 0);

			HRESULT hr;

			ComBool fOutput;
			for (int inm = 0; inm < g_cnmNormalizations; inm++)
			{
				// Check that all normalizations are true of m_qtssEmpty, m_qtssOneRun,
				// m_qtssTwoRuns.
				hr = m_qtssEmpty->get_IsNormalizedForm(g_rgnmNormalizations[inm], &fOutput);
				unitpp::assert_eq("get_IsNormalizedForm(m_qtssEmpty)", fOutput, ComBool(true));
				hr = m_qtssOneRun->get_IsNormalizedForm(g_rgnmNormalizations[inm], &fOutput);
				unitpp::assert_eq("get_IsNormalizedForm(m_qtssOneRun)", fOutput, ComBool(true));
				hr = m_qtssTwoRuns->get_IsNormalizedForm(g_rgnmNormalizations[inm], &fOutput);
				unitpp::assert_eq("get_IsNormalizedForm(m_qtssTwoRuns)",
					fOutput, ComBool(true));
				// Check that we return the same object when asked for any normalization form.
				ITsStringPtr qtssResult;
				hr = m_qtssEmpty->get_NormalizedForm(g_rgnmNormalizations[inm], &qtssResult);
				unitpp::assert_eq("get_NormalizedForm(m_qtssEmpty)",
					m_qtssEmpty.Ptr(), qtssResult.Ptr());
				hr = m_qtssOneRun->get_NormalizedForm(g_rgnmNormalizations[inm], &qtssResult);
				unitpp::assert_eq("get_NormalizedForm(m_qtssOneRun)",
					m_qtssOneRun.Ptr(), qtssResult.Ptr());
				hr = m_qtssTwoRuns->get_NormalizedForm(g_rgnmNormalizations[inm], &qtssResult);
				unitpp::assert_eq("get_NormalizedForm(m_qtssTwoRuns)",
					m_qtssTwoRuns.Ptr(), qtssResult.Ptr());
			}
		}

#define LATIN_CAPITAL_A L"\x0041"
#define COMBINING_DIAERESIS L"\x0308" // cc 230
#define COMBINING_MACRON L"\x0304" // cc 230
#define A_WITH_DIAERESIS L"\x00C4" // decomposes to 0041 0308
#define A_WITH_DIAERESIS_AND_MACRON L"\x01DE"	// decomposes to 00C4 0304 and hence to
												// 0041 0308 0304
#define SMALL_A L"\x0061"
#define COMBINING_DOT_BELOW L"\x0323" // cc 220
#define a_WITH_DOT_BELOW L"\x1EA1" // decomposes to 0061 0323
#define COMBINING_OVERLINE L"\x0305" // not involved in any compositions with characters; cc 230
#define COMBINING_LEFT_HALF_RING_BELOW L"\x031C" // not involved in any compositions; cc 220.
#define SPACE L"\x0020"
#define COMBINING_BREVE L"\x0306" // cc 230
#define BREVE L"\x02D8" // compatibility decomposition to 0020 0306
#define a_WITH_DIAERESIS L"\x00E4" // decomposes to 0061 0308.
#define a_WITH_DIAERESIS_AND_MACRON L"\x01DF"
#if WIN32
#define MUSICAL_SYMBOL_MINIMA L"\xD834\xDDBB" // 1D1BB decomposes to 1D1B9 1D165
#define MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"\xD834\xDDB9" // 1D1B9
#define MUSICAL_SYMBOL_COMBINING_STEM L"\xD834\xDD65" // 1D165
#else
#define MUSICAL_SYMBOL_MINIMA L"\x1D1BB" // 1D1BB decomposes to 1D1B9 1D165
#define MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"\x1D1B9" // 1D1B9
#define MUSICAL_SYMBOL_COMBINING_STEM L"\x1D165" // 1D165
#endif



		/*--------------------------------------------------------------------------------------
			Test basic normalization: single and multi-run strings with no complications.
			(Also tests surrogate decomposition.)
		--------------------------------------------------------------------------------------*/
		void testBasicNormalization()
		{
			HRESULT hr;
			// For each normaliztion make a single and multi-run string that requires
			// changes to be normalized. Try to test several aspects, for example, collapsing
			// and expanding, re-ordering diacritics,...
			// Note: as NFC and NFD are the only currently interesting forms, maybe it is
			// enough to test those for now?
			StrUni stuInput1 = L"abc" A_WITH_DIAERESIS_AND_MACRON L"A" COMBINING_DIAERESIS
				COMBINING_MACRON L"C" COMBINING_OVERLINE COMBINING_LEFT_HALF_RING_BELOW L"XYZ"
				BREVE L"GAP" SPACE COMBINING_BREVE L"QED" MUSICAL_SYMBOL_MINIMA;
			// outputs. All reorder overline and half ring.
			OleStringLiteral rgpsz[] = {
				// knmNFD: decompose A_WITH_DIAERESIS_AND_MACRON
				L"abcA" COMBINING_DIAERESIS COMBINING_MACRON L"A" COMBINING_DIAERESIS
					COMBINING_MACRON L"C" COMBINING_LEFT_HALF_RING_BELOW COMBINING_OVERLINE
					L"XYZ" BREVE L"GAP" SPACE COMBINING_BREVE L"QED"
					MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM,
				// knmNFKD: same plus decompose BREVE
				L"abcA" COMBINING_DIAERESIS COMBINING_MACRON L"A" COMBINING_DIAERESIS
					COMBINING_MACRON L"C" COMBINING_LEFT_HALF_RING_BELOW COMBINING_OVERLINE
					L"XYZ" SPACE COMBINING_BREVE L"GAP" SPACE COMBINING_BREVE L"QED"
					MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM,
				// knmNFC: compose to A_WITH_DIAERESIS_AND_MACRON
				// note: the composed surrogate pair gets decomposed due to backwards
				// compatibility with the Unicode 3.2 algorithm.
				L"abc" A_WITH_DIAERESIS_AND_MACRON A_WITH_DIAERESIS_AND_MACRON
					L"C" COMBINING_LEFT_HALF_RING_BELOW COMBINING_OVERLINE L"XYZ"
					BREVE L"GAP" SPACE COMBINING_BREVE L"QED"
					MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM,
				// knmNFKC : same plus decompose BREVE (This is surprising, but NFKC
				// DEcomposes compatibility equivalents while composing canonical ones.)
				L"abc" A_WITH_DIAERESIS_AND_MACRON A_WITH_DIAERESIS_AND_MACRON
					L"C" COMBINING_LEFT_HALF_RING_BELOW COMBINING_OVERLINE L"XYZ"
					SPACE COMBINING_BREVE L"GAP" SPACE COMBINING_BREVE L"QED"
					MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM,
				// knmNFSC : same as NFC
				L"abc" A_WITH_DIAERESIS_AND_MACRON A_WITH_DIAERESIS_AND_MACRON
					L"C" COMBINING_LEFT_HALF_RING_BELOW COMBINING_OVERLINE L"XYZ"
					BREVE L"GAP" SPACE COMBINING_BREVE L"QED"
					MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM,
			};
			int rgichMinSecondRun[] = {9, 9, 5, 5, 5}; // for two-run test.

			ITsStringPtr rgqtssInput[2];
			m_qtsf->MakeStringRgch(stuInput1.Chars(), stuInput1.Length(), m_wsEng,
				&rgqtssInput[0]);
			ITsStrBldrPtr qtsb;
			hr = rgqtssInput[0]->GetBldr(&qtsb);
			qtsb->SetIntPropValues(7,stuInput1.Length(), ktptBold, ktpvEnum, kttvForceOn);
			hr = qtsb->GetString(&rgqtssInput[1]);

			StrAnsi sta;
			for (int itss = 0; itss <= 1; itss++)
			{
				ComBool fOutput;
				for (int inm = 0; inm < g_cnmNormalizations; inm++)
				{
					// Check that all normalizations are true of m_qtssEmpty, m_qtssOneRun,
					// m_qtssTwoRuns.
					hr = rgqtssInput[itss]->get_IsNormalizedForm(g_rgnmNormalizations[inm],
						&fOutput);
					sta.Format("BasicNormalization itss=%d: get_IsNormalizedForm(inm=%d)",
						itss, inm);
					unitpp::assert_eq("get_IsNormalizedForm(simple normalization)",
						ComBool(false), fOutput);
					// Check that we get the expected normalization form.
					ITsStringPtr qtssResult;
					hr = rgqtssInput[itss]->get_NormalizedForm(g_rgnmNormalizations[inm],
						&qtssResult);
					SmartBstr sbstrResult;
					hr = qtssResult->get_Text(&sbstrResult);
					sta.Format("BasicNormalization itss=%d: get_Normalization(inm=%d)",
						itss, inm);
					unitpp::assert_true(sta.Chars(),
						u_strcmp(sbstrResult.Chars(), rgpsz[inm]) == 0);
					ITsTextPropsPtr qttpInput;
					ITsTextPropsPtr qttpResult;
					hr = rgqtssInput[itss]->get_PropertiesAt(0, &qttpInput);
					hr = qtssResult->get_PropertiesAt(0, &qttpResult);
					unitpp::assert_eq("props at start of normalized string",
						qttpInput.Ptr(), qttpResult.Ptr());
					int ichMinSecondRun = rgichMinSecondRun[inm];
					hr = rgqtssInput[itss]->get_PropertiesAt(7, &qttpInput);
					hr = qtssResult->get_PropertiesAt(ichMinSecondRun, &qttpResult);
					unitpp::assert_eq("props middle of normalized string",
						qttpInput.Ptr(), qttpResult.Ptr());
					if (itss == 1)
					{
						int ichMin, ichLim;
						hr = qtssResult->GetBoundsOfRun(0, &ichMin, &ichLim);
						unitpp::assert_eq("run boundary correct", ichMinSecondRun, ichLim);
					}
					int crun;
					hr = qtssResult->get_RunCount(&crun);
					unitpp::assert_eq("number of runs", itss + 1, crun);

					// Now loop over the forms again, and verify the expected answers for
					// whether the string is already normalized. Skip this test for NFSC because
					// in this example it gives the same answer as NFC
					if (g_rgnmNormalizations[inm] != knmNFSC)
					{
						for (int inm2 = 0; inm2 < g_cnmNormalizations; inm2++)
						{
							if (g_rgnmNormalizations[inm2] != knmNFSC)
							{
								hr = qtssResult->get_IsNormalizedForm(
									g_rgnmNormalizations[inm2], &fOutput);
								sta.Format(
						"BasicNormalization itss=%d: get_IsNormalizedForm(inm=%d, inm2 = %d)",
									itss, inm, inm2);
								unitpp::assert_eq(sta.Chars(),
									ComBool(inm == inm2 ||
									(g_rgnmNormalizations[inm] == knmNFKD &&
										g_rgnmNormalizations[inm2] == knmNFD) ||
									(g_rgnmNormalizations[inm] == knmNFKC &&
										g_rgnmNormalizations[inm2] == knmNFC)),
									fOutput);
							}
						}
					}
				}
			}
		}

		/*--------------------------------------------------------------------------------------
			Test the tricky normalization case where the un-normalized form has a run boundary
			between a base and diacritic that NFC will combine into one character.
			We expect that the characters will be collapsed by NFC but not by NFSC.
		--------------------------------------------------------------------------------------*/
		void testSplitRunComposition()
		{
			HRESULT hr;
			// Make a string with the problem condition and apply NFC normalization.
			// Also test that it is correctly found to be already in NFD.
			// String should have an A in one run followed by a combining diaresis in another.
			StrUni stuInput1 = L"A" COMBINING_DIAERESIS;
			OleStringLiteral rgpsz[] = {
				// knmNFD: no change
				L"A" COMBINING_DIAERESIS,
				// knmNFKD: no change
				L"A" COMBINING_DIAERESIS,
				// knmNFC:
				A_WITH_DIAERESIS,
				// knmNFKC :
				A_WITH_DIAERESIS,
				// knmNFSC : no change, because of run boundary
				L"A" COMBINING_DIAERESIS,
			};
			ITsStringPtr qtssInputT;
			ITsStringPtr qtssInput;
			m_qtsf->MakeStringRgch(stuInput1.Chars(), stuInput1.Length(), m_wsEng, &qtssInputT);
			ITsStrBldrPtr qtsb;
			hr = qtssInputT->GetBldr(&qtsb);
			qtsb->SetIntPropValues(1,stuInput1.Length(), ktptBold, ktpvEnum, kttvForceOn);
			hr = qtsb->GetString(&qtssInput);


			ComBool fOutput;
			for (int inm = 0; inm < g_cnmNormalizations; inm++)
			{
				FwNormalizationMode nm = g_rgnmNormalizations[inm];
				hr = qtssInput->get_IsNormalizedForm(nm, &fOutput);
				unitpp::assert_eq("get_IsNormalizedForm(SplitRunComposition)",
					ComBool(nm == knmNFD || nm == knmNFKD || nm == knmNFSC), fOutput);
				// Check that we get the expected normalization form.
				ITsStringPtr qtssResult;
				hr = qtssInput->get_NormalizedForm(nm, &qtssResult);
				SmartBstr sbstrResult;
				hr = qtssResult->get_Text(&sbstrResult);
				unitpp::assert_true("get_NormalizedForm(split run composition)",
					u_strcmp(sbstrResult.Chars(), rgpsz[inm]) == 0);
			}
		}
		/*--------------------------------------------------------------------------------------
			Test the tricky normalization case where normalization re-orders diacritics, and the
			re-ordered diacritics were in different runs.
		--------------------------------------------------------------------------------------*/
		void testReorderingRuns()
		{
			HRESULT hr;
			// Make a string with the problem condition and test NFD and NFC normalization.
			// Also test that a string with multiple diacritics in different runs but in the
			// correct order is found to be already normalized.
			// Either normalization should re-order smallA + diaresis + underdot
			// to smallA + underdot + diaresis (but NFC will then combine the first two
			// to A_WITH_DOT_BELOW, if they have the same properties.
			StrUni stuInput1 = L"a" COMBINING_DIAERESIS COMBINING_DOT_BELOW
				a_WITH_DIAERESIS COMBINING_DOT_BELOW;
			OleStringLiteral rgpsz[] = {
				// knmNFD: decompose a_WITH_DIAERESIS, reorder both sequences
				L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS
				L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS,
				// knmNFKD: same as NFD
				L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS
				L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS,
				// knmNFC: expand, reorder, recombine
				a_WITH_DOT_BELOW COMBINING_DIAERESIS
				a_WITH_DOT_BELOW COMBINING_DIAERESIS,
				// knmNFKC : same as NFC
				a_WITH_DOT_BELOW COMBINING_DIAERESIS
				a_WITH_DOT_BELOW COMBINING_DIAERESIS,
				// knmNFSC : expand, reorder, compose is blocked by run boundaries
				L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS
				L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS,
			};
			int rgcRun[] = {6, 6, 2, 2, 6};
			int rgichMinSecondRun[] = {1,1,2,2,1};
			int rgichLimSecondRun[] = {2,2,4,4,2};

			ITsStringPtr qtssInputT;
			ITsStringPtr qtssInput;
			m_qtsf->MakeStringRgch(stuInput1.Chars(), stuInput1.Length(), m_wsEng, &qtssInputT);
			ITsStrBldrPtr qtsb;
			hr = qtssInputT->GetBldr(&qtsb);
			// This makes three runs, in each case with the underdot having different props
			// from the a and the diaeresis.
			qtsb->SetIntPropValues(2,4, ktptBold, ktpvEnum, kttvForceOn);
			hr = qtsb->GetString(&qtssInput);

			StrAnsi sta;
			ComBool fOutput;
			for (int inm = 0; inm < g_cnmNormalizations; inm++)
			{
				FwNormalizationMode nm = g_rgnmNormalizations[inm];
				hr = qtssInput->get_IsNormalizedForm(nm, &fOutput);
				sta.Format("ReorderingRuns inm=%d: get_IsNormalizedForm()", inm);
				unitpp::assert_eq(sta.Chars(), ComBool(false), fOutput);
				// Check that we get the expected normalization form.
				ITsStringPtr qtssResult;
				hr = qtssInput->get_NormalizedForm(nm, &qtssResult);
				SmartBstr sbstrResult;
				hr = qtssResult->get_Text(&sbstrResult);
				sta.Format("ReorderingRuns inm=%d: get_NormalizedForm()", inm);
				unitpp::assert_true(sta.Chars(),
					u_strcmp(sbstrResult.Chars(), rgpsz[inm]) == 0);
				int crun;
				hr = qtssResult->get_RunCount(&crun);
				sta.Format("ReorderingRuns inm=%d: get_RunCount()", inm);
				unitpp::assert_eq(sta.Chars(), rgcRun[inm], crun);

				ITsTextPropsPtr qttpInput;
				ITsTextPropsPtr qttpResult;
				hr = qtssInput->get_PropertiesAt(0, &qttpInput);
				hr = qtssResult->get_PropertiesAt(0, &qttpResult);
				sta.Format("ReorderingRuns inm=%d: get_PropertiesAt(0)", inm);
				unitpp::assert_eq(sta.Chars(), qttpInput.Ptr(), qttpResult.Ptr());

				int ichMinSecondRun = rgichMinSecondRun[inm];
				int ichLimSecondRun = rgichLimSecondRun[inm];
				hr = qtssInput->get_PropertiesAt(2, &qttpInput);
				hr = qtssResult->get_PropertiesAt(ichMinSecondRun, &qttpResult);
				sta.Format("ReorderingRuns inm=%d: get_PropertiesAt(2 || %d)",
					inm, ichMinSecondRun);
				unitpp::assert_eq(sta.Chars(), qttpInput.Ptr(), qttpResult.Ptr());
				int ichMin, ichLim;
				hr = qtssResult->GetBoundsOfRun(1, &ichMin, &ichLim);
				sta.Format("ReorderingRuns inm=%d: GetBoundsOfRun(min = %d)", inm, ichMin);
				unitpp::assert_eq(sta.Chars(), ichMinSecondRun, ichMin);
				sta.Format("ReorderingRuns inm=%d: GetBoundsOfRun(lim = %d)", inm, ichLim);
				unitpp::assert_eq(sta.Chars(), ichLimSecondRun, ichLim);
			}
		}

		/*--------------------------------------------------------------------------------------
			Test for the case where an underdot re-orders around several different runs.
		--------------------------------------------------------------------------------------*/
		void testRepeatedCharReorder()
		{
			HRESULT hr;
			// The initial a is plain; first dieresis is red, second green, third blue;
			// dot and following a-diaresis are bold; final dot is plain
			StrUni stuInput1 = L"a" COMBINING_DIAERESIS COMBINING_DIAERESIS COMBINING_DIAERESIS
				COMBINING_DOT_BELOW
				a_WITH_DIAERESIS COMBINING_DOT_BELOW;
			OleStringLiteral rgpsz[] = {
				// knmNFD: decompose a_WITH_DIAERESIS, reorder both sequences
				L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS
					COMBINING_DIAERESIS L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS,
				// knmNFKD: same as NFD
				L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS
					COMBINING_DIAERESIS L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS,
				// knmNFC: expand, reorder, recombine
				a_WITH_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS COMBINING_DIAERESIS
					a_WITH_DOT_BELOW COMBINING_DIAERESIS,
				// knmNFKC : same as NFC
				a_WITH_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS COMBINING_DIAERESIS
					a_WITH_DOT_BELOW COMBINING_DIAERESIS,
				// knmNFSC : expand, reorder, compose is blocked by run boundaries
				L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS
					COMBINING_DIAERESIS L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS,
			};
			int rgcRun[] = {8, 8, 2, 2, 8};
			int rgichMinSecondRun[] = {1,1,4,4,1};
			int rgichLimSecondRun[] = {2,2,6,6,2};

			ITsStringPtr qtssInputT;
			ITsStringPtr qtssInput;
			m_qtsf->MakeStringRgch(stuInput1.Chars(), stuInput1.Length(), m_wsEng, &qtssInputT);
			ITsStrBldrPtr qtsb;
			hr = qtssInputT->GetBldr(&qtsb);
			// This makes three runs, in each case with the underdot having different props
			// from the a and the diaeresis.
			qtsb->SetIntPropValues(1,2, ktptForeColor, ktpvDefault, kclrRed);
			qtsb->SetIntPropValues(2,3, ktptForeColor, ktpvDefault, kclrGreen);
			qtsb->SetIntPropValues(3,4, ktptForeColor, ktpvDefault, kclrBlue);
			qtsb->SetIntPropValues(4,6, ktptBold, ktpvEnum, kttvForceOn);
			hr = qtsb->GetString(&qtssInput);

			StrAnsi sta;
			ComBool fOutput;
			for (int inm = 0; inm < g_cnmNormalizations; inm++)
			{
				FwNormalizationMode nm = g_rgnmNormalizations[inm];
				hr = qtssInput->get_IsNormalizedForm(nm, &fOutput);
				sta.Format("ReorderingRuns inm=%d: get_IsNormalizedForm()", inm);
				unitpp::assert_eq(sta.Chars(), ComBool(false), fOutput);
				// Check that we get the expected normalization form.
				ITsStringPtr qtssResult;
				hr = qtssInput->get_NormalizedForm(nm, &qtssResult);
				SmartBstr sbstrResult;
				hr = qtssResult->get_Text(&sbstrResult);
				sta.Format("ReorderingRuns inm=%d: get_NormalizedForm()", inm);
				unitpp::assert_true(sta.Chars(),
					u_strcmp(sbstrResult.Chars(), rgpsz[inm]) == 0);
				int crun;
				hr = qtssResult->get_RunCount(&crun);
				sta.Format("ReorderingRuns inm=%d: get_RunCount()", inm);
				unitpp::assert_eq(sta.Chars(), rgcRun[inm], crun);

				ITsTextPropsPtr qttpInput;
				ITsTextPropsPtr qttpResult;
				hr = qtssInput->get_PropertiesAt(0, &qttpInput);
				hr = qtssResult->get_PropertiesAt(0, &qttpResult);
				sta.Format("ReorderingRuns inm=%d: get_PropertiesAt(0)", inm);
				unitpp::assert_eq(sta.Chars(), qttpInput.Ptr(), qttpResult.Ptr());

				int ichMinSecondRun = rgichMinSecondRun[inm];
				int ichLimSecondRun = rgichLimSecondRun[inm];
				hr = qtssInput->get_PropertiesAt(4, &qttpInput);
				hr = qtssResult->get_PropertiesAt(ichMinSecondRun, &qttpResult);
				sta.Format("ReorderingRuns inm=%d: get_PropertiesAt(4 || %d)",
					inm, ichMinSecondRun);
				unitpp::assert_eq(sta.Chars(), qttpInput.Ptr(), qttpResult.Ptr());
				int ichMin, ichLim;
				hr = qtssResult->GetBoundsOfRun(1, &ichMin, &ichLim);
				sta.Format("ReorderingRuns inm=%d: GetBoundsOfRun(min = %d)", inm, ichMin);
				unitpp::assert_eq(sta.Chars(), ichMinSecondRun, ichMin);
				sta.Format("ReorderingRuns inm=%d: GetBoundsOfRun(lim = %d)", inm, ichLim);
				unitpp::assert_eq(sta.Chars(), ichLimSecondRun, ichLim);
			}
		}

		/*--------------------------------------------------------------------------------------
			A further special case for NFSC normalization is where we can compress part of
			a character sequence, but because of run boundaries can't compress all of it.
		--------------------------------------------------------------------------------------*/
		void testPartialComposition()
		{
			HRESULT hr;
			// The macron will be in a different run; so while the first two compression
			// schemes produce a single character, NFSC should produce 2.
			StrUni stuInput1 = LATIN_CAPITAL_A COMBINING_DIAERESIS COMBINING_MACRON;
			OleStringLiteral rgpsz[] = {
				// knmNFD:
				LATIN_CAPITAL_A COMBINING_DIAERESIS COMBINING_MACRON,
				// knmNFKD:
				LATIN_CAPITAL_A COMBINING_DIAERESIS COMBINING_MACRON,
				// knmNFC: all the way to one character
				A_WITH_DIAERESIS_AND_MACRON,
				// knmNFKC : same as NFC
				A_WITH_DIAERESIS_AND_MACRON,
				// knmNFSC : expand, reorder, compose is blocked by run boundaries
				A_WITH_DIAERESIS COMBINING_MACRON
			};
			int rgcRun[] = {2, 2, 1, 1, 2};
			int rgichLimFirstRun[] = {2,2,1,1,1};

			ITsStringPtr qtssInputT;
			ITsStringPtr qtssInput;
			m_qtsf->MakeStringRgch(stuInput1.Chars(), stuInput1.Length(), m_wsEng, &qtssInputT);
			ITsStrBldrPtr qtsb;
			hr = qtssInputT->GetBldr(&qtsb);
			// This makes two runs, changing the last character.
			qtsb->SetIntPropValues(2,3, ktptBold, ktpvEnum, kttvForceOn);
			hr = qtsb->GetString(&qtssInput);

			ComBool fOutput;
			for (int inm = 0; inm < g_cnmNormalizations; inm++)
			{
				StrAnsi sta;
				FwNormalizationMode nm = g_rgnmNormalizations[inm];
				hr = qtssInput->get_IsNormalizedForm(nm, &fOutput);
				sta.Format("get_IsNormalizedForm inm=%d", inm);
				unitpp::assert_eq(sta.Chars(),
					ComBool(nm == knmNFD || nm == knmNFKD), fOutput);
				// Check that we get the expected normalization form.
				ITsStringPtr qtssResult;
				hr = qtssInput->get_NormalizedForm(nm, &qtssResult);
				SmartBstr sbstrResult;
				hr = qtssResult->get_Text(&sbstrResult);
				sta.Format("get_NormalizedForm(partial composition) inm=%d", inm);
				unitpp::assert_true(sta.Chars(), u_strcmp(sbstrResult.Chars(), rgpsz[inm]) == 0);
				int crun;
				hr = qtssResult->get_RunCount(&crun);
				unitpp::assert_eq("number of runs", rgcRun[inm], crun);

				ITsTextPropsPtr qttpInput;
				ITsTextPropsPtr qttpResult;
				hr = qtssInput->get_PropertiesAt(0, &qttpInput);
				hr = qtssResult->get_PropertiesAt(0, &qttpResult);
				unitpp::assert_eq("props at start of normalized string",
					qttpInput.Ptr(), qttpResult.Ptr());

				if (crun > 1)
				{
					int ichLimFirstRun = rgichLimFirstRun[inm];
					hr = qtssInput->get_PropertiesAt(2, &qttpInput);
					hr = qtssResult->get_PropertiesAt(ichLimFirstRun, &qttpResult);
					sta.Format("props middle of normalized string inm=%d", inm);
					unitpp::assert_eq(sta.Chars(), qttpInput.Ptr(), qttpResult.Ptr());
					int ichMin, ichLim;
					hr = qtssResult->GetBoundsOfRun(0, &ichMin, &ichLim);
					unitpp::assert_eq("run boundary correct", ichLimFirstRun, ichLim);
				}
			}
		}


		/*--------------------------------------------------------------------------------------
			Test that WriteAsXml writes strings in NFSC normalized form.
		--------------------------------------------------------------------------------------*/
		void testWriteAsXml()
		{
			HRESULT hr;

			ITsStrBldrPtr qtsb;
			ITsStringPtr qtssInput1;
			ITsStringPtr qtssInput2;

			StrUni stuInput = L"a" COMBINING_DIAERESIS COMBINING_DIAERESIS COMBINING_DIAERESIS
				COMBINING_DOT_BELOW a_WITH_DIAERESIS COMBINING_DOT_BELOW;

			m_qtsf->MakeStringRgch(stuInput.Chars(), stuInput.Length(), m_wsEng, &qtssInput1);
			StrAnsi staOutput1;
			// knmNFSC : expand, reorder, compose =>
			// a_WITH_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS COMBINING_DIAERESIS
			//		a_WITH_DOT_BELOW COMBINING_DIAERESIS
			staOutput1.Format("<Str>%n<Run ws=\"en\">"
				"\xE1\xBA\xA1\xCC\x88\xCC\x88\xCC\x88\xE1\xBA\xA1\xCC\x88</Run>%n</Str>%n");
			StrAnsiStreamPtr qstas1;
			StrAnsiStream::Create(&qstas1);
			qtssInput1->WriteAsXml(qstas1.Ptr(), m_qwsf, 0, 0, TRUE);
			unitpp::assert_true("WriteAsXml test 1", staOutput1 == qstas1->m_sta);

			// The initial a is plain; first dieresis is red, second green, third blue;
			// dot and following a-diaresis are bold; final dot is plain
			hr = qtssInput1->GetBldr(&qtsb);
			// This makes multiple runs, in each case with the underdot having different props
			// from the a and the diaeresis.
			qtsb->SetIntPropValues(1,2, ktptForeColor, ktpvDefault, kclrRed);
			qtsb->SetIntPropValues(2,3, ktptForeColor, ktpvDefault, kclrGreen);
			qtsb->SetIntPropValues(3,4, ktptForeColor, ktpvDefault, kclrBlue);
			qtsb->SetIntPropValues(4,6, ktptBold, ktpvEnum, kttvForceOn);
			hr = qtsb->GetString(&qtssInput2);
			/*
			  XML OUTPUT OF UNNORMALIZED STRING:
			  <Str>%n
			  <Run ws="en">a</Run>%n
			  <Run ws="en" forecolor="red">\xCC\x88</Run>%n
			  <Run ws="en" forecolor="green">\xCC\x88</Run>%n
			  <Run ws="en" forecolor="blue">\xCC\x88</Run>%n
			  <Run ws="en" bold="on">\xCC\xA3\xC3\xA4</Run>%n
			  <Run ws="en">\xCC\xA3</Run>%n
			  </Str>%n
			*/
			// knmNFSC : expand, reorder, compose is blocked by run boundaries =>
			// a COMBINING_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS COMBINING_DIAERESIS
			//		a COMBINING_DOT_BELOW COMBINING_DIAERESIS
			StrAnsi staOutput2;
			staOutput2.Format("<Str>%n"
				"<Run ws=\"en\">a</Run>%n"
				"<Run ws=\"en\" bold=\"on\">\xCC\xA3</Run>%n"
				"<Run ws=\"en\" forecolor=\"red\">\xCC\x88</Run>%n"
				"<Run ws=\"en\" forecolor=\"green\">\xCC\x88</Run>%n"
				"<Run ws=\"en\" forecolor=\"blue\">\xCC\x88</Run>%n"
				"<Run ws=\"en\" bold=\"on\">a</Run>%n"
				"<Run ws=\"en\">\xCC\xA3</Run>%n"
				"<Run ws=\"en\" bold=\"on\">\xCC\x88</Run>%n"
				"</Str>%n");
			StrAnsiStreamPtr qstas2;
			StrAnsiStream::Create(&qstas2);
			qtssInput2->WriteAsXml(qstas2.Ptr(), m_qwsf, 0, 0, TRUE);
			unitpp::assert_true("WriteAsXml test 2", staOutput2 == qstas2->m_sta);
		}


#define COMBINING_GRAVE_ACCENT L"\x0300"		// cc 230
#define COMBINING_CIRCUMFLEX_ACCENT L"\x0302"	// cc 230
#define COMBINING_TILDE L"\x0303"				// cc 230
#define COMBINING_DOT_ABOVE L"\x0307"			// cc 230
#define COMBINING_DOUBLE_ACUTE_ACCENT L"\x030B"	// cc 230
#define COMBINING_INVERTED_BREVE L"\x0311"		// cc 230
#define COMBINING_GRAVE_ACCENT_BELOW L"\x0316"	// cc 220
#define COMBINING_ACUTE_ACCENT_BELOW L"\x0317"	// cc 220
#define COMBINING_LEFT_TACK_BELOW L"\x0318"		// cc 220
#define COMBINING_DOWN_TACK_BELOW L"\x031E"		// cc 220
#define COMBINING_MINUS_SIGN_BELOW L"\x0320"	// cc 220
#define COMBINING_RING_BELOW L"\x0325"			// cc 220
#define COMBINING_TILDE_BELOW L"\x0330"			// cc 220
#define COMBINING_SQUARE_BELOW L"\x033B"		// cc 220
#define COMBINING_SEAGULL_BELOW L"\x033C"		// cc 220
#define o_WITH_CIRCUMFLEX L"\x00F4"				// composition of o COMBINING_CIRCUMFLEX_ACCENT
#define e_WITH_GRAVE L"\x00E8"					// composition of e COMBINING_GRAVE_ACCENT
#define o_WITH_DIAERESIS L"\x00F6"				// composition of o COMBINING_DIAERESIS
#define a_WITH_DOT_ABOVE L"\x0227"				// composition of a COMBINING_DOT_ABOVE
#define o_WITH_DOT_ABOVE L"\x022F"				// composition of o COMBINING_DOT_ABOVE
		/*--------------------------------------------------------------------------------------
			Test that strings with stacked diacritics work properly.
		--------------------------------------------------------------------------------------*/
		void testStackedDiacritics()
		{
			HRESULT hr;
			StrUni stuInput(L"Stacked diacritics: We"
				COMBINING_DOUBLE_ACUTE_ACCENT COMBINING_RING_BELOW COMBINING_GRAVE_ACCENT_BELOW
				L"lc" COMBINING_LEFT_TACK_BELOW COMBINING_MINUS_SIGN_BELOW
				L"o" COMBINING_CIRCUMFLEX_ACCENT
				L"m" COMBINING_SEAGULL_BELOW COMBINING_GRAVE_ACCENT COMBINING_DIAERESIS
					COMBINING_MACRON
				L"e" COMBINING_GRAVE_ACCENT
				L" to" COMBINING_DIAERESIS COMBINING_CIRCUMFLEX_ACCENT
				L" Wo" COMBINING_DOT_ABOVE COMBINING_INVERTED_BREVE
				L"r" COMBINING_SQUARE_BELOW
				L"l" COMBINING_TILDE
				L"d" COMBINING_DOWN_TACK_BELOW COMBINING_TILDE_BELOW
				L"Pa" COMBINING_DOT_ABOVE COMBINING_OVERLINE COMBINING_DOUBLE_ACUTE_ACCENT
				L"d" COMBINING_ACUTE_ACCENT_BELOW
				L"!");
			StrUni stuNFC(L"Stacked diacritics: W"
				L"e" COMBINING_RING_BELOW COMBINING_GRAVE_ACCENT_BELOW
					COMBINING_DOUBLE_ACUTE_ACCENT
				L"l"
				L"c" COMBINING_LEFT_TACK_BELOW COMBINING_MINUS_SIGN_BELOW
				o_WITH_CIRCUMFLEX
				L"m" COMBINING_SEAGULL_BELOW COMBINING_GRAVE_ACCENT COMBINING_DIAERESIS
					COMBINING_MACRON
				e_WITH_GRAVE
				L" t"
				o_WITH_DIAERESIS COMBINING_CIRCUMFLEX_ACCENT
				L" W"
				o_WITH_DOT_ABOVE COMBINING_INVERTED_BREVE
				L"r" COMBINING_SQUARE_BELOW
				L"l" COMBINING_TILDE
				L"d" COMBINING_DOWN_TACK_BELOW COMBINING_TILDE_BELOW
				L"P"
				a_WITH_DOT_ABOVE COMBINING_OVERLINE COMBINING_DOUBLE_ACUTE_ACCENT
				L"d" COMBINING_ACUTE_ACCENT_BELOW
				L"!");

			ITsStringPtr qtssInput;
			m_qtsf->MakeStringRgch(stuInput.Chars(), stuInput.Length(), m_wsStk, &qtssInput);
			ITsStrBldrPtr qtsb;
			hr = qtssInput->GetBldr(&qtsb);
			int cch;
			qtsb->get_Length(&cch);
			qtsb->SetIntPropValues(0, cch, ktptFontSize, ktpvMilliPoint, 20000);
			qtsb->SetIntPropValues(0, cch, ktptForeColor, ktpvDefault, kclrGreen);
			qtsb->GetString(&qtssInput);

			// Only one run in the string: NFC == NFSC.
			ITsStringPtr qtssNFC;
			ITsStringPtr qtssNFSC;
			SmartBstr sbstrNFC;
			SmartBstr sbstrNFSC;
			int crunNFC;
			int crunNFSC;
			qtssInput->get_NormalizedForm(knmNFC, &qtssNFC);
			qtssInput->get_NormalizedForm(knmNFSC, &qtssNFSC);
			qtssNFC->get_Text(&sbstrNFC);
			qtssNFSC->get_Text(&sbstrNFSC);
			qtssNFC->get_RunCount(&crunNFC);
			qtssNFSC->get_RunCount(&crunNFSC);
			unitpp::assert_eq("StackedDiacritics - Single run NFC has 1 run", 1, crunNFC);
			unitpp::assert_eq("StackedDiacritics - Single run NFSC has 1 run", 1, crunNFSC);
			ComBool fEqual;
			qtssNFC->Equals(qtssNFSC, &fEqual);
			unitpp::assert_true("StackedDiacritics - Single run NFC == NFSC", bool(fEqual));
			unitpp::assert_true("StackedDiacritics - Single run output NFC",
				stuNFC == sbstrNFC.Chars());
			unitpp::assert_true("StackedDiacritics - Single run output NFSC",
				stuNFC == sbstrNFSC.Chars());
			StrAnsiStreamPtr qstas1;
			StrAnsiStream::Create(&qstas1);
			qtssInput->WriteAsXml(qstas1.Ptr(), m_qwsf, 0, 0, TRUE);
			StrAnsiStreamPtr qstasNFC;
			StrAnsiStream::Create(&qstasNFC);
			qtssNFC->WriteAsXml(qstasNFC.Ptr(), m_qwsf, 0, 0, TRUE);
			StrAnsiStreamPtr qstasNFSC;
			StrAnsiStream::Create(&qstasNFSC);
			qtssNFSC->WriteAsXml(qstasNFSC.Ptr(), m_qwsf, 0, 0, TRUE);
			StrAnsi staOutput1;
			staOutput1.Format("<Str>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
				"Stacked diacritics: We" "\xCC" "\xA5" "\xCC" "\x96" "\xCC" "\x8B" "lc"
				"\xCC" "\x98" "\xCC" "\xA0" "\xC3" "\xB4" "m" "\xCC" "\xBC" "\xCC" "\x80"
				"\xCC" "\x88" "\xCC" "\x84" "\xC3" "\xA8" " t" "\xC3" "\xB6" "\xCC" "\x82"
				" W" "\xC8" "\xAF" "\xCC" "\x91" "r" "\xCC" "\xBB" "l" "\xCC" "\x83" "d"
				"\xCC" "\x9E" "\xCC" "\xB0" "P" "\xC8" "\xA7" "\xCC" "\x85" "\xCC" "\x8B"
				"d" "\xCC" "\x97" "!</Run>%n"
				"</Str>%n");
			unitpp::assert_true("Stacked diacritics single run XML output",
				staOutput1 == qstas1->m_sta);
			unitpp::assert_true("Stacked diacritics NFC single run XML output",
				staOutput1 == qstasNFC->m_sta);
			unitpp::assert_true("Stacked diacritics NFSC single run XML output",
				staOutput1 == qstasNFSC->m_sta);

			// green from 0-22
			qtsb->SetIntPropValues(22, 23, ktptForeColor, ktpvDefault, kclrRed);
			qtsb->SetIntPropValues(23, 24, ktptForeColor, ktpvDefault, 0x00ff602f);
			// green from 24-30
			qtsb->SetIntPropValues(30, 31, ktptForeColor, ktpvDefault, kclrBlue);
			// green from 31-33
			qtsb->SetIntPropValues(33, 34, ktptForeColor, ktpvDefault, kclrRed);
			// green from 34-42
			qtsb->SetIntPropValues(42, 43, ktptForeColor, ktpvDefault, kclrBlack);
			// green from 43-47
			qtsb->SetIntPropValues(47, 48, ktptForeColor, ktpvDefault, kclrBlack);
			// green from 48-51
			qtsb->SetIntPropValues(51, 52, ktptForeColor, ktpvDefault, kclrBlue);
			// green from 52-53
			qtsb->SetIntPropValues(53, 54, ktptForeColor, ktpvDefault, kclrRed);
			// green from 54-58
			qtsb->SetIntPropValues(58, 59, ktptForeColor, ktpvDefault, kclrRed);
			qtsb->SetIntPropValues(59, 60, ktptForeColor, ktpvDefault, kclrBlack);
			// green from 60-61
			qtsb->SetIntPropValues(61, 62, ktptForeColor, ktpvDefault, kclrBlack);
			// green from 62-63
			qtsb->GetString(&qtssInput);
			qtssInput->get_NormalizedForm(knmNFC, &qtssNFC);
			qtssInput->get_NormalizedForm(knmNFSC, &qtssNFSC);
			qtssNFC->get_Text(&sbstrNFC);
			qtssNFSC->get_Text(&sbstrNFSC);
			qtssNFC->get_RunCount(&crunNFC);
			qtssNFSC->get_RunCount(&crunNFSC);

			// With multiple runs,
			StrUni stuNFSC2(L"Stacked diacritics: W"
				L"e" COMBINING_RING_BELOW COMBINING_GRAVE_ACCENT_BELOW
					COMBINING_DOUBLE_ACUTE_ACCENT
				L"l"
				L"c" COMBINING_LEFT_TACK_BELOW COMBINING_MINUS_SIGN_BELOW
				L"o" COMBINING_CIRCUMFLEX_ACCENT
				L"m" COMBINING_SEAGULL_BELOW COMBINING_GRAVE_ACCENT COMBINING_DIAERESIS
					COMBINING_MACRON
				e_WITH_GRAVE
				L" t"
				o_WITH_DIAERESIS COMBINING_CIRCUMFLEX_ACCENT
				L" W"
				o_WITH_DOT_ABOVE COMBINING_INVERTED_BREVE
				L"r" COMBINING_SQUARE_BELOW
				L"l" COMBINING_TILDE
				L"d" COMBINING_DOWN_TACK_BELOW COMBINING_TILDE_BELOW
				L"P"
				a_WITH_DOT_ABOVE COMBINING_OVERLINE COMBINING_DOUBLE_ACUTE_ACCENT
				L"d" COMBINING_ACUTE_ACCENT_BELOW
				L"!");
			unitpp::assert_true("StackedDiacritics - Multiple run output NFSC",
				stuNFSC2 == sbstrNFSC.Chars());

			StrAnsiStreamPtr qstas2;
			StrAnsiStream::Create(&qstas2);
			qtssInput->WriteAsXml(qstas2.Ptr(), m_qwsf, 0, 0, TRUE);

			StrAnsiStreamPtr qstasNFC2;
			StrAnsiStream::Create(&qstasNFC2);
			qtssNFC->WriteAsXml(qstasNFC2.Ptr(), m_qwsf, 0, 0, TRUE);

			StrAnsiStreamPtr qstasNFSC2;
			StrAnsiStream::Create(&qstasNFSC2);
			qtssNFSC->WriteAsXml(qstasNFSC2.Ptr(), m_qwsf, 0, 0, TRUE);

			StrAnsi staOutput2;
			staOutput2.Format("<Str>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"Stacked diacritics: We</Run>%n"
			   "<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"2f60ff\">"
					"\xCC\xA5</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"\xCC\x96</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"red\">"
					"\xCC\x8B</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"lc\xCC\x98\xCC\xA0o</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"blue\">"
					"\xCC\x82</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"m\xCC\xBC</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"red\">"
					"\xCC\x80</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"\xCC\x88\xCC\x84\xC3\xA8 t\xC3\xB6</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"black\">"
					"\xCC\x82</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					" W\xC8\xAF</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"black\">"
					"\xCC\x91</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"r\xCC\xBBl</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"blue\">"
					"\xCC\x83</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"d</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"red\">"
					"\xCC\x9E</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"\xCC\xB0P\xC8\xA7</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"red\">"
					"\xCC\x85</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"black\">"
					"\xCC\x8B</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"d</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"black\">"
					"\xCC\x97</Run>%n"
				"<Run ws=\"x-stk\" fontsize=\"20000\" fontsizeUnit=\"mpt\" forecolor=\"green\">"
					"!</Run>%n"
				"</Str>%n");

			unitpp::assert_true("Stacked diacritics multiple run XML output",
				staOutput2 == qstas2->m_sta);
			unitpp::assert_true("Stacked diacritics NFC multiple run XML output",
				staOutput1 == qstasNFC2->m_sta);
			unitpp::assert_true("Stacked diacritics NFSC multiple run XML output",
				staOutput2 == qstasNFSC2->m_sta);
		}

		/*--------------------------------------------------------------------------------------
			Test that NfdWithOffsets property adjusts the offsets.
		--------------------------------------------------------------------------------------*/
		void testNfdWithOffsets()
		{
			HRESULT hr;
			StrUni stuInput(L"Stacked diacritics: We" // 0..21
				COMBINING_DOUBLE_ACUTE_ACCENT COMBINING_RING_BELOW COMBINING_GRAVE_ACCENT_BELOW // 22..24
				L"lc" COMBINING_LEFT_TACK_BELOW COMBINING_MINUS_SIGN_BELOW // 25..28
				o_WITH_CIRCUMFLEX L"o" // 29..30 (+1)
				L"m" COMBINING_SEAGULL_BELOW COMBINING_GRAVE_ACCENT COMBINING_DIAERESIS //31..34
					COMBINING_MACRON //35
				e_WITH_GRAVE //36 (+1)
				L" to" COMBINING_DIAERESIS COMBINING_CIRCUMFLEX_ACCENT //37..41
				L" Wo" COMBINING_DOT_ABOVE COMBINING_INVERTED_BREVE //42..46
				L"r" COMBINING_SQUARE_BELOW //47..48
				L"l" COMBINING_TILDE //49..50
				L"d" COMBINING_DOWN_TACK_BELOW COMBINING_TILDE_BELOW //51..53
				L"a" COMBINING_DIAERESIS COMBINING_DOT_BELOW //54..56
				a_WITH_DIAERESIS COMBINING_DOT_BELOW //57..58 (+1)
				L"d" COMBINING_ACUTE_ACCENT_BELOW //59..60
				L"!"); //61

			ITsStringPtr qtssInput;
			m_qtsf->MakeStringRgch(stuInput.Chars(), stuInput.Length(), m_wsStk, &qtssInput);
			ITsStrBldrPtr qtsb;
			hr = qtssInput->GetBldr(&qtsb);
			int cch;
			qtsb->get_Length(&cch);
			qtsb->SetIntPropValues(0, 30, ktptFontSize, ktpvMilliPoint, 20000);
			qtsb->SetIntPropValues(30, cch, ktptForeColor, ktpvDefault, kclrGreen);
			qtsb->GetString(&qtssInput);

			ITsStringPtr qtssNFD;
#define NUM_OFFSETS 14
			int rgichOffsets[NUM_OFFSETS]    = { 30, 33, 49, 57, 0, 21, 29,
				stuInput.Length(),   36, 37, 54, 55, 56, 58};
			int rgichOffsetsOut[NUM_OFFSETS] = { 31, 37, 51, 59, 0, 21, 29,
				stuInput.Length()+3, 37, 39, 56, 59, 59, 62};
			int * rgpichOffsets[NUM_OFFSETS];
			int i;
			for (i = 0; i < NUM_OFFSETS; ++i)
				rgpichOffsets[i] = &rgichOffsets[i];
			qtssInput->NfdAndFixOffsets(&qtssNFD, rgpichOffsets, NUM_OFFSETS);
			StrAnsi sta;
			for (i = 0; i < NUM_OFFSETS; ++i)
			{
				sta.Format("test NfdAndFixOffsets [i=%d]", i);
				unitpp::assert_eq(sta.Chars(), rgichOffsetsOut[i], rgichOffsets[i]);
			}
		}


		void GetSubstring(ITsString * ptss, int ichMin, int ichLim, ITsString ** pptss)
		{
			ITsStrBldrPtr qtsb;
			CheckHr(ptss->GetBldr(&qtsb));
			int len;
			CheckHr(ptss->get_Length(&len));
			if (ichLim < len)
				CheckHr(qtsb->Replace(ichLim, len, NULL, NULL));
			if (ichMin > 0)
				CheckHr(qtsb->Replace(0, ichMin, NULL, NULL));
			CheckHr(qtsb->GetString(pptss));
		}

		void TestOneSubstring(ITsString * ptss, int ichMin, int ichLim, const char * label)
		{
			ITsStringPtr qtssTest;
			ITsStringPtr qtssGood;
			ComBool fEqual;
			CheckHr(ptss->GetSubstring(ichMin, ichLim, &qtssTest));
			GetSubstring(ptss, ichMin, ichLim, &qtssGood);
			if (ichMin < ichLim)
			{
				CheckHr(qtssGood->Equals(qtssTest, &fEqual));
				unitpp::assert_true(label, (bool)fEqual);
			}
			else
			{
				// The implementation using Replace ends up getting an empty
				// string with the props of the first character in the original string.
				// Our new method is smarter, giving one with the props at ichMin.
				int cch;
				CheckHr(qtssTest->get_Length(&cch));
				unitpp::assert_eq(label, 0, cch);
				ITsTextPropsPtr qttpTest;
				ITsTextPropsPtr qttpGood;
				ptss->get_PropertiesAt(ichMin, &qttpGood);
				qtssTest->get_PropertiesAt(0, &qttpTest);
				unitpp::assert_eq(label, qttpGood.Ptr(), qttpTest.Ptr());
			}
		}

	public:
		/*--------------------------------------------------------------------------------------
			Test the substring function
		--------------------------------------------------------------------------------------*/
		void testSubstring()
		{
			unitpp::assert_true("Non-NULL m_qtssEmpty after setup", m_qtssEmpty.Ptr() != 0);
			unitpp::assert_true("Non-NULL m_qtssOneRun after setup", m_qtssOneRun.Ptr() != 0);
			unitpp::assert_true("Non-NULL m_qtssTwoRuns after setup", m_qtssTwoRuns.Ptr() != 0);
			// A.1 extract zero-length substring on an empty/null tss.
			TestOneSubstring(m_qtssEmpty, 0, 0, "empty");
			// A.2 extract zero-length substring from a non-empty tss.
			TestOneSubstring(m_qtssOneRun, 0, 0, "empty from non-empty (start)");
			TestOneSubstring(m_qtssOneRun, 2, 2, "empty from non-empty (mid)");
			TestOneSubstring(m_qtssTwoRuns, g_cchTest, g_cchTest, "empty from non-empty (end, 2r)");
			// A.3 extract full-length substring of a non-empty tss.
			TestOneSubstring(m_qtssOneRun, 0, g_cchTest, "whole of 1-run string");

			// B.1 extract the beginning of single run tss
			TestOneSubstring(m_qtssOneRun, 0, 3, "start of 1-run string");
			// B.2 extract the middle of a single run tss
			TestOneSubstring(m_qtssOneRun, 1, 3, "middle of 1-run string");
			// B.3 extract to the end of single run tss.
			TestOneSubstring(m_qtssOneRun, 4, g_cchTest, "end of 1-run string");

			// C.1 single-run results from two-run string.
			TestOneSubstring(m_qtssTwoRuns, 0, 3, "start of 2-run string (1-run out)");
			TestOneSubstring(m_qtssTwoRuns, 2, 4, "mid of 2-run string (1-run out)");
			TestOneSubstring(m_qtssTwoRuns, g_cchTest - 3, g_cchTest, "end of 2-run string(1-run out)");
			int ichMid = g_cchTest / 2; // run boundary
			TestOneSubstring(m_qtssTwoRuns, ichMid, g_cchTest, "2nd run of 2-run string(1-run out)");
			TestOneSubstring(m_qtssTwoRuns, ichMid, ichMid + 3, "start of 2nd run of 2-run string(1-run out)");
			TestOneSubstring(m_qtssTwoRuns, ichMid - 3, ichMid, "end of 1st run of 2-run string(1-run out)");

			// C.2. two-run results from two-run string
			TestOneSubstring(m_qtssTwoRuns, 0, g_cchTest, "whole of 2-run string(2-run out)");
			TestOneSubstring(m_qtssTwoRuns, 0, g_cchTest - 2, "start of 2-run string(2-run out)");
			TestOneSubstring(m_qtssTwoRuns, 2, g_cchTest, "end of 2-run string(2-run out)");
			TestOneSubstring(m_qtssTwoRuns, 2, g_cchTest - 2, "mid of 2-run string(2-run out)");

			// D.1 multi-run tests, one run out.
			ITsStringPtr qtssMulti;
			OleStringLiteral oleTestMulti = L"This is a longer string for more complex tests";
			const OLECHAR * prgchTestMulti = oleTestMulti;
			int cchMulti = wcslen(prgchTestMulti);
			m_qtsf->MakeStringRgch(prgchTestMulti, cchMulti, m_wsEng, &qtssMulti);
			ITsStrBldrPtr qtsb;
			CheckHr(qtssMulti->GetBldr(&qtsb));
			int ichRun2 = 4;
			int ichRun3 = 9;
			int ichRun4 = 15;
			int ichRun5 = 20;
			qtsb->SetIntPropValues(ichRun2, ichRun3, ktptBold, ktpvEnum, kttvForceOn);
			qtsb->SetIntPropValues(ichRun4, ichRun5, ktptItalic, ktpvEnum, kttvForceOn);
			CheckHr(qtsb->GetString(&qtssMulti));
			TestOneSubstring(qtssMulti, 0, 1, "start of multi-run string(1-run out)");
			TestOneSubstring(qtssMulti, 0, ichRun2, "first run of multi-run string(1-run out)");
			TestOneSubstring(qtssMulti, ichRun2, ichRun3, "2nd run of multi-run string(1-run out)");
			TestOneSubstring(qtssMulti, ichRun3, ichRun3 + 2, "start 3rd run of multi-run string(1-run out)");
			TestOneSubstring(qtssMulti, ichRun4 - 2, ichRun4, "end of 3rd of multi-run string(1-run out)");
			TestOneSubstring(qtssMulti, cchMulti - 2, cchMulti, "end of last of multi-run string(1-run out)");
			TestOneSubstring(qtssMulti, ichRun5, cchMulti, "last run of multi-run string(1-run out)");
			// D.2 multi-run tests, multi run out.
			TestOneSubstring(qtssMulti, 0, ichRun3, "1st 3 runs of multi-run string");
			TestOneSubstring(qtssMulti, ichRun3, cchMulti, "last 3 runs of multi-run string");
			TestOneSubstring(qtssMulti, 0, cchMulti, "whole of multi-run string");
			TestOneSubstring(qtssMulti, 0, ichRun2 + 2, "2.5 runs from start of multi-run string");
			TestOneSubstring(qtssMulti, 2, ichRun2 + 2, "end of 1st to part of 3rd of multi-run string");
			TestOneSubstring(qtssMulti, 2, ichRun4, "multi runs up to end of run of multi-run string");
		}


		/*--------------------------------------------------------------------------------------

		--------------------------------------------------------------------------------------*/
		void testThreadedMakeString()
		{
#if !WIN32 // TODO-Linux FWNX-198: possibly port this test to windows?
			int		n;
			pthread_t	htid, wtid;

			if ( n = pthread_create( &htid, NULL, TestThreadedMakeString, &m_wsEng ) ) {
				fprintf( stderr, "pthread_create: %s\n", strerror( n ) );
				exit( 1 );
			}

			if ( n = pthread_create( &wtid, NULL, TestThreadedMakeString, &m_wsEng ) ) {
				fprintf( stderr, "pthread_create: %s\n", strerror( n ) );
				exit( 1 );
			}

			if ( n = pthread_join( wtid, NULL ) ) {
				fprintf( stderr, "pthread_join: %s\n", strerror( n ) );
				exit( 1 );
			}

			if ( n = pthread_join( htid, NULL ) ) {
				fprintf( stderr, "pthread_join: %s\n", strerror( n ) );
				exit( 1 );
			}
#endif
		}


		TestTsString();

		/*--------------------------------------------------------------------------------------
			Create three objects: one empty, one with one run, and one with the same character
			data, but two runs.
		--------------------------------------------------------------------------------------*/
		virtual void Setup()
		{
			TsStrBldr * pztsb0 = 0;
			TsStrBldr::Create(NULL, 0, NULL, 0, &pztsb0);
			CheckHr(pztsb0->SetIntPropValues(0, 0, ktptWs, 0, m_wsEng));

			CheckHr(pztsb0->GetString(&m_qtssEmpty));
			pztsb0->Release();

			TsIntProp rgtip[2];
			rgtip[0].m_tpt = ktptWs;
			rgtip[0].m_nVar = 0;
			rgtip[0].m_nVal = m_wsEng;
			rgtip[1].m_tpt = ktptBold;
			rgtip[1].m_nVar = ktpvEnum;
			rgtip[1].m_nVal = kttvForceOn;

			TxtRun rgrun[2];
			rgrun[0].m_ichLim = g_cchTest;
			TsTextProps::Create(rgtip, 1, NULL, 0, &rgrun[0].m_qttp);

			TsStrBldr * pztsb1 = 0;
			TsStrBldr::Create(g_pszTest.Chars(), g_cchTest, rgrun, 1, &pztsb1);
			CheckHr(pztsb1->GetString(&m_qtssOneRun));
			pztsb1->Release();

			rgrun[0].m_ichLim = g_cchTest / 2;
			rgrun[1].m_ichLim = g_cchTest;
			TsTextProps::Create(rgtip, 2, NULL, 0, &rgrun[1].m_qttp);

			TsStrBldr * pztsb2 = 0;
			TsStrBldr::Create(g_pszTest.Chars(), g_cchTest, rgrun, 2, &pztsb2);
			CheckHr(pztsb2->GetString(&m_qtssTwoRuns));
			pztsb2->Release();

			m_qtsf.CreateInstance(CLSID_TsStrFactory);
		}

		/*--------------------------------------------------------------------------------------
			Delete the objects created in Setup().
		--------------------------------------------------------------------------------------*/
		virtual void Teardown()
		{
			m_qtssEmpty.Clear();
			m_qtssOneRun.Clear();
			m_qtssTwoRuns.Clear();
			m_qtsf.Clear();
		}

		/*--------------------------------------------------------------------------------------
			Create a WritingSystem factory, and populate it with writing systems for "en" and
			"x-stk".
		--------------------------------------------------------------------------------------*/
		virtual void SuiteSetup()
		{
			try
			{
				m_qwsf.Attach(NewObj MockLgWritingSystemFactory);
				ILgWritingSystemPtr qws;
				SmartBstr sbstr;

				sbstr.Assign(L"en");
				m_qwsf->get_Engine(sbstr, &qws);
				qws->get_Handle(&m_wsEng);

				sbstr.Assign(L"x-stk");
				m_qwsf->get_Engine(sbstr, &qws);
				qws->get_Handle(&m_wsStk);
			}
			catch (...)
			{
			}
		}

		/*--------------------------------------------------------------------------------------
			Destroy the WritingSystem factory.
		--------------------------------------------------------------------------------------*/
		virtual void SuiteTeardown()
		{
			m_qwsf.Clear();
		}
	};
}

#endif /*TESTTSSTRING_H_INCLUDED*/

#include "Vector_i.cpp"		// We use Vector<wchar> above.

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkfwk-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)
/*:End Ignore*/

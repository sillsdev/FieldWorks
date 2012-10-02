/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwTxtSrc.h
Responsibility:
Last reviewed:

	Unit tests for the VwEnv class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTVWTXTSRC_H_INCLUDED
#define TESTVWTXTSRC_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
	class TestVwTxtSrc : public unitpp::suite
	{
		ITsStrFactoryPtr m_qtsf;
	public:
		TestVwTxtSrc();

		void testOverride()
		{
			HRESULT hr;
			VwSimpleTxtSrcPtr qsts;
			qsts.Attach(NewObj VwSimpleTxtSrc);
			VwOverrideTxtSrcPtr qots;
			qots.Attach(NewObj VwOverrideTxtSrc(qsts));
			qots->SetWritingSystemFactory(g_qwsf); // Test pass-through.

			StrUni stuTest1(L"This is a string");
			StrUni stuTest2(L"This is another string");
			ITsStringPtr qtss1;
			hr = m_qtsf->MakeString(stuTest1.Bstr(), g_wsEng, &qtss1);
			ITsStringPtr qtss2;
			hr = m_qtsf->MakeString(stuTest2.Bstr(), g_wsEng, &qtss2);
			ITsStrBldrPtr qtsb;
			hr = qtss1->GetBldr(&qtsb);
			// Make the first 'This' bold.
			hr = qtsb->SetIntPropValues(0, 4, ktptBold, ktpvEnum, kttvForceOn);
			hr = qtsb->GetString(&qtss1);
			hr = qtss2->GetBldr(&qtsb);
			// Make the first 'This' italic.
			hr = qtsb->SetIntPropValues(0, 4, ktptItalic, ktpvEnum, kttvForceOn);
			// And the ' another' 18 point
			hr = qtsb->SetIntPropValues(7, 15, ktptFontSize, ktpvMilliPoint, 18000);
			hr = qtsb->GetString(&qtss2);
			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore);
			qots->AddString(qtss1, qzvps, NULL);
			qots->AddString(qtss2, qzvps, NULL);

			int cch1 = stuTest1.Length();
			int cch2 = stuTest2.Length();
			int cchT = cch1 + cch2;

			PropOverrideVec vdpOverrides;
			DispPropOverride dpo;
			::memset(&dpo.chrp, 0, sizeof(dpo.chrp));

			dpo.chrp.clrFore = (COLORREF) 20798;
			dpo.ichMin = 5;
			dpo.ichLim = 7;
			vdpOverrides.Push(dpo);

			dpo.chrp.clrFore = (COLORREF) 37893;
			dpo.ichMin = 5 + cch1;
			dpo.ichLim = 7 + cch1;
			vdpOverrides.Push(dpo);

			dpo.chrp.clrFore = (COLORREF) 54321;
			dpo.ichMin = cchT - 7;
			dpo.ichLim = cchT;
			vdpOverrides.Push(dpo);

			qots->SetOverrides(vdpOverrides);

			int ichMin, ichLim;
			LgCharRenderProps chrp;
			// First range is the bold 'This'
			hr = qots->GetCharProps(0, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range 0 starts at 0", 0, ichMin);
			unitpp::assert_eq("range 0 ends at 4", 4, ichLim);
			unitpp::assert_eq("range 0 is bold", kttvForceOn, chrp.ttvBold);
			// Then a space.
			hr = qots->GetCharProps(4, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range at 4 starts at 4", 4, ichMin);
			unitpp::assert_eq("range at 4 ends at 5", 5, ichLim);
			unitpp::assert_eq("range at 4 is not bold", kttvOff, chrp.ttvBold);
			unitpp::assert_eq("range at 4 is black", (COLORREF)0, chrp.clrFore);
			// Then the overridden value for 'is'.
			hr = qots->GetCharProps(5, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range at 5 starts at 5", 5, ichMin);
			unitpp::assert_eq("range at 5 ends at 7", 7, ichLim);
			unitpp::assert_eq("range at 5 is expected color", (COLORREF)20798, chrp.clrFore);
			hr = qots->GetCharProps(6, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range at 6 starts at 5", 5, ichMin);
			unitpp::assert_eq("range at 6 ends at 7", 7, ichLim);
			unitpp::assert_eq("range at 6 is expected color", (COLORREF)20798, chrp.clrFore);
			// Then the rest of the first string.
			hr = qots->GetCharProps(7, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range at 7 starts at 7", 7, ichMin);
			unitpp::assert_eq("range at 7 ends at cch1", cch1, ichLim);
			unitpp::assert_eq("range at 7 is black", (COLORREF)0, chrp.clrFore);
			// Then the 'This' in the second string.
			hr = qots->GetCharProps(cch1, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range at cch1 starts at cch1", cch1, ichMin);
			unitpp::assert_eq("range at cch1 ends at cch1+4", cch1+4, ichLim);
			unitpp::assert_eq("range at cch1 is black", (COLORREF)0, chrp.clrFore);
			unitpp::assert_eq("range at cch1 is not bold", kttvOff, chrp.ttvBold);
			unitpp::assert_eq("range at cch1 is italic", kttvForceOn, chrp.ttvItalic);
			// Then a space.
			hr = qots->GetCharProps(cch1+4, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range at cch1+4 starts at cch1+4", cch1+4, ichMin);
			unitpp::assert_eq("range at cch1+4 ends at cch1+5", cch1+5, ichLim);
			unitpp::assert_eq("range at cch1+4 is not italic", kttvOff, chrp.ttvItalic);
			unitpp::assert_eq("range at cch1+4 is black", (COLORREF)0, chrp.clrFore);
			// Then the second override.
			hr = qots->GetCharProps(cch1+5, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range at cch1+5 starts at cch1+5", cch1+5, ichMin);
			unitpp::assert_eq("range at cch1+5 ends at cch1+7", cch1+7, ichLim);
			unitpp::assert_eq("range at cch1+5 is expected color", (COLORREF)37893, chrp.clrFore);
			// ' another' is 18 point
			hr = qots->GetCharProps(cch1+7, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range at cch1+7 starts at cch1+7", cch1+7, ichMin);
			unitpp::assert_eq("range at cch1+7 ends at cch1+15", cch1+15, ichLim);
			unitpp::assert_eq("range at cch1+7 is black", (COLORREF)0, chrp.clrFore);
			unitpp::assert_eq("range at cch1+7 is 18 point", 18000, chrp.dympHeight);
			// The rest of it is the final override
			hr = qots->GetCharProps(cch1+15, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("range at cch1+15 starts at cch1+15", cch1+15, ichMin);
			unitpp::assert_eq("range at cch1+15 ends at cchT", cchT, ichLim);
			unitpp::assert_eq("range at cch1+15 is expected color", (COLORREF)54321, chrp.clrFore);
		}

		void testStringFromIch_MiddleOfString()
		{
			VwSimpleTxtSrcPtr qsts;
			qsts.Attach(NewObj VwSimpleTxtSrc);

			StrUni stuTest1(L"First string");
			StrUni stuTest2(L"Middle string");
			StrUni stuTest3(L"Last string");

			int cch1 = stuTest1.Length();
			int ichLim2 = cch1 + stuTest2.Length();

			ITsStringPtr qtss1;
			CheckHr(m_qtsf->MakeString(stuTest1.Bstr(), g_wsEng, &qtss1));
			ITsStringPtr qtss2;
			CheckHr(m_qtsf->MakeString(stuTest2.Bstr(), g_wsEng, &qtss2));
			ITsStringPtr qtss3;
			CheckHr(m_qtsf->MakeString(stuTest3.Bstr(), g_wsEng, &qtss3));

			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore);
			qsts->AddString(qtss1, qzvps, NULL);
			qsts->AddString(qtss2, qzvps, NULL);
			qsts->AddString(qtss3, qzvps, NULL);

			int ichMin, ichLim, itss;
			ITsStringPtr qtssOut;
			VwPropertyStorePtr qvps;
			qsts->StringFromIch(cch1 + 1, true, &qtssOut, &ichMin, &ichLim, &qvps, &itss);
			unitpp::assert_eq("Should have gotten the second string", 1, itss);
			SmartBstr sbstrTStringFromIch;
			CheckHr(qtssOut->get_Text(&sbstrTStringFromIch));
			unitpp::assert_eq("Should have gotten the second string", stuTest2, sbstrTStringFromIch.Chars());
			unitpp::assert_eq("ichMin should be the first character in the second string", cch1, ichMin);
			unitpp::assert_eq("ichLim should be the last character in the second string", ichLim2, ichLim);

			qsts->StringFromIch(ichLim2 - 1, false, &qtssOut, &ichMin, &ichLim, &qvps, &itss);
			unitpp::assert_eq("Should have gotten the second string", 1, itss);
			CheckHr(qtssOut->get_Text(&sbstrTStringFromIch));
			unitpp::assert_eq("Should have gotten the second string", stuTest2, sbstrTStringFromIch.Chars());
			unitpp::assert_eq("ichMin should be the first character in the second string", cch1, ichMin);
			unitpp::assert_eq("ichLim should be the last character in the second string", ichLim2, ichLim);
		}

		void testStringFromIch_AtStringBoundary_AssocPrevTrue()
		{
			VwSimpleTxtSrcPtr qsts;
			qsts.Attach(NewObj VwSimpleTxtSrc);

			StrUni stuTest1(L"This is a string");
			StrUni stuTest2(L"This is another string");

			int cch = stuTest1.Length();

			ITsStringPtr qtss1;
			CheckHr(m_qtsf->MakeString(stuTest1.Bstr(), g_wsEng, &qtss1));
			ITsStringPtr qtss2;
			CheckHr(m_qtsf->MakeString(stuTest2.Bstr(), g_wsEng, &qtss2));

			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore);
			qsts->AddString(qtss1, qzvps, NULL);
			qsts->AddString(qtss2, qzvps, NULL);

			int ichMin, ichLim, itss;
			ITsStringPtr qtssOut;
			VwPropertyStorePtr qvps;
			qsts->StringFromIch(cch, true, &qtssOut, &ichMin, &ichLim, &qvps, &itss);
			unitpp::assert_eq("Should have gotten the first string because AssocPrev is true", 0, itss);
			SmartBstr sbstrTStringFromIch;
			CheckHr(qtssOut->get_Text(&sbstrTStringFromIch));
			unitpp::assert_eq("Should have gotten the first string because AssocPrev is true",
				stuTest1, sbstrTStringFromIch.Chars());
			unitpp::assert_eq("ichMin should be the first character in the data source", 0, ichMin);
			unitpp::assert_eq("ichLim should be the last character in the first string", cch, ichLim);
		}

		void testStringFromIch_AtStringBoundary_AssocPrevFalse()
		{
			VwSimpleTxtSrcPtr qsts;
			qsts.Attach(NewObj VwSimpleTxtSrc);

			StrUni stuTest1(L"This is a string");
			StrUni stuTest2(L"This is another string");

			int cch1 = stuTest1.Length();
			int cch2 = stuTest2.Length();
			int cchT = cch1 + cch2;

			ITsStringPtr qtss1;
			CheckHr(m_qtsf->MakeString(stuTest1.Bstr(), g_wsEng, &qtss1));
			ITsStringPtr qtss2;
			CheckHr(m_qtsf->MakeString(stuTest2.Bstr(), g_wsEng, &qtss2));

			VwPropertyStorePtr qzvps;
			qzvps.Attach(NewObj VwPropertyStore);
			qsts->AddString(qtss1, qzvps, NULL);
			qsts->AddString(qtss2, qzvps, NULL);

			int ichMin, ichLim, itss;
			ITsStringPtr qtssOut;
			VwPropertyStorePtr qvps;
			qsts->StringFromIch(cch1, false, &qtssOut, &ichMin, &ichLim, &qvps, &itss);
			unitpp::assert_eq("Should have gotten the second string because AssocPrev is false", 1, itss);
			SmartBstr sbstrTStringFromIch;
			CheckHr(qtssOut->get_Text(&sbstrTStringFromIch));
			unitpp::assert_eq("Should have gotten the second string because AssocPrev is false",
				stuTest2, sbstrTStringFromIch.Chars());
			unitpp::assert_eq("ichMin should be the first character in the second string", cch1, ichMin);
			unitpp::assert_eq("ichLim should be the last character in the data source", cchT, ichLim);
		}

		virtual void Setup()
		{
			CreateTestWritingSystemFactory();
			m_qtsf.CreateInstance(CLSID_TsStrFactory);
		}
		virtual void Teardown()
		{
			m_qtsf.Clear();
			CloseTestWritingSystemFactory();
		}
	};


	class TestVwMappedTxtSrc : public unitpp::suite
	{
		ITsStrFactoryPtr m_qtsf;
		VwMappedTxtSrcPtr m_qts;
		IVwViewConstructorPtr m_qvc;
		VwPropertyStorePtr m_qzvps;
	public:
		TestVwMappedTxtSrc();

		void testGetLength_NoOrc()
		{
			HRESULT hr;
			// Make a string and then a text source out of it.
			ITsStringPtr qtss;
			ITsStrBldrPtr qtsbStringBuilder;
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			ITsPropsBldrPtr qtpbTextPropsBuilder;
			qtpbTextPropsBuilder.CreateInstance(CLSID_TsPropsBldr);
			hr = qtpbTextPropsBuilder->SetIntPropValues(ktptWs, ktpvDefault, g_wsEng);
			ITsTextPropsPtr qttp;
			hr = qtpbTextPropsBuilder->GetTextProps(&qttp);
			StrUni stuSearch(L"Tha Tant of tha Lord's Prasance");
			hr = qtsbStringBuilder->Replace(0, 0, stuSearch.Bstr(), qttp);
			hr = qtsbStringBuilder->GetString(&qtss);
			m_qts->AddString(qtss, m_qzvps, m_qvc);

			int cchTss;
			qtss->get_Length(&cchTss);

			int cch;
			unitpp::assert_eq("get_Length failed", S_OK, m_qts->get_Length(&cch));
			unitpp::assert_eq("get_Length returned wrong value", cchTss, cch);

			unitpp::assert_eq("get_LengthSearch failed", S_OK, m_qts->get_LengthSearch(&cch));
			unitpp::assert_eq("get_LengthSearch returned wrong value", cchTss, cch);

			unitpp::assert_eq("CchRen returned wrong value", cchTss, m_qts->CchRen());
		}

		void testGetLength_OrcAtBeginning()
		{
			HRESULT hr;
			// Make a string and then a text source out of it.
			ITsStringPtr qtss;
			ITsStrBldrPtr qtsbStringBuilder;
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			ITsPropsBldrPtr qtpbTextPropsBuilder;
			qtpbTextPropsBuilder.CreateInstance(CLSID_TsPropsBldr);
			hr = qtpbTextPropsBuilder->SetIntPropValues(ktptWs, ktpvDefault, g_wsEng);
			ITsTextPropsPtr qttp;
			hr = qtpbTextPropsBuilder->GetTextProps(&qttp);
			StrUni stuSearch(L"Tha Tant of tha Lord's Prasance");
			hr = qtsbStringBuilder->Replace(0, 0, stuSearch.Bstr(), qttp);
			hr = qtsbStringBuilder->GetString(&qtss);

			int cchTssNoOrc;
			qtss->get_Length(&cchTssNoOrc);
			int cchTssWithOrc = cchTssNoOrc + 5; // The ORC is replaced with 5 characters

			// Insert a footnote ORC into the string.
			StrUni stuData;
			OLECHAR * prgchData;
			GUID uidSimulatedFootnote;
			hr = CoCreateGuid(&uidSimulatedFootnote);
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtOwnNameGuidHot;
			memmove(prgchData + 1, &uidSimulatedFootnote, isizeof(uidSimulatedFootnote));
			hr = qtpbTextPropsBuilder->SetStrPropValue(ktptObjData, stuData.Bstr());
			hr = qtpbTextPropsBuilder->GetTextProps(&qttp);
			OLECHAR chObj = kchObject;
			hr = qtsbStringBuilder->ReplaceRgch(0, 0, &chObj, 1, qttp);
			hr = qtsbStringBuilder->GetString(&qtss);
			m_qts->AddString(qtss, m_qzvps, m_qvc);

			int cch;
			unitpp::assert_eq("get_Length failed", S_OK, m_qts->get_Length(&cch));
			unitpp::assert_eq("get_Length returned wrong value", cchTssWithOrc, cch);

			unitpp::assert_eq("get_LengthSearch failed", S_OK, m_qts->get_LengthSearch(&cch));
			unitpp::assert_eq("get_LengthSearch returned wrong value", cchTssNoOrc, cch);

			unitpp::assert_eq("CchRen returned wrong value", cchTssWithOrc, m_qts->CchRen());
		}

		void testGetLength_ZeroLengthOrcAtBeginning()
		{
			HRESULT hr;
			// Make a string and then a text source out of it.
			ITsStringPtr qtss;
			ITsStrBldrPtr qtsbStringBuilder;
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			ITsPropsBldrPtr qtpbTextPropsBuilder;
			qtpbTextPropsBuilder.CreateInstance(CLSID_TsPropsBldr);
			hr = qtpbTextPropsBuilder->SetIntPropValues(ktptWs, ktpvDefault, g_wsEng);
			ITsTextPropsPtr qttp;
			hr = qtpbTextPropsBuilder->GetTextProps(&qttp);
			StrUni stuSearch(L"Tha Tant of tha Lord's Prasance");
			hr = qtsbStringBuilder->Replace(0, 0, stuSearch.Bstr(), qttp);
			hr = qtsbStringBuilder->GetString(&qtss);

			int cchTssNoOrc;
			qtss->get_Length(&cchTssNoOrc);
			int cchTssWithOrc = cchTssNoOrc; // The ORC is replaced with no characters

			// Insert a footnote ORC into the string.
			StrUni stuData;
			OLECHAR * prgchData;
			// {D10C12E2-BF77-4f1d-84EA-79BC983FAB0C}
			const GUID uidSimulatedFootnote =
			{ 0xd10c12e2, 0xbf77, 0x4f1d, { 0x84, 0xea, 0x79, 0xbc, 0x98, 0x3f, 0xab, 0xc } };
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtOwnNameGuidHot;
			memmove(prgchData + 1, &uidSimulatedFootnote, isizeof(uidSimulatedFootnote));
			hr = qtpbTextPropsBuilder->SetStrPropValue(ktptObjData, stuData.Bstr());
			hr = qtpbTextPropsBuilder->GetTextProps(&qttp);
			OLECHAR chObj = kchObject;
			hr = qtsbStringBuilder->ReplaceRgch(0, 0, &chObj, 1, qttp);
			hr = qtsbStringBuilder->GetString(&qtss);
			m_qts->AddString(qtss, m_qzvps, m_qvc);

			int cch;
			unitpp::assert_eq("get_Length failed", S_OK, m_qts->get_Length(&cch));
			unitpp::assert_eq("get_Length returned wrong value", cchTssWithOrc, cch);

			unitpp::assert_eq("get_LengthSearch failed", S_OK, m_qts->get_LengthSearch(&cch));
			unitpp::assert_eq("get_LengthSearch returned wrong value", cchTssNoOrc, cch);

			unitpp::assert_eq("CchRen returned wrong value", cchTssNoOrc, m_qts->CchRen());
		}

		void testSearchToLog()
		{
			HRESULT hr;
			// Make a string and then a text source out of it.
			ITsStringPtr qtss;
			ITsStrBldrPtr qtsbStringBuilder;
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			ITsPropsBldrPtr qtpbTextPropsBuilder;
			qtpbTextPropsBuilder.CreateInstance(CLSID_TsPropsBldr);
			hr = qtpbTextPropsBuilder->SetIntPropValues(ktptWs, ktpvDefault, g_wsEng);
			ITsTextPropsPtr qttp;
			hr = qtpbTextPropsBuilder->GetTextProps(&qttp);
			StrUni stuSearch(L"eee");
			hr = qtsbStringBuilder->Replace(0, 0, stuSearch.Bstr(), qttp);
			hr = qtsbStringBuilder->GetString(&qtss);

			// Insert a footnote ORC into the string.
			StrUni stuData;
			OLECHAR * prgchData;
			GUID uidSimulatedFootnote;
			hr = CoCreateGuid(&uidSimulatedFootnote);
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtOwnNameGuidHot;
			memmove(prgchData + 1, &uidSimulatedFootnote, isizeof(uidSimulatedFootnote));
			hr = qtpbTextPropsBuilder->SetStrPropValue(ktptObjData, stuData.Bstr());
			hr = qtpbTextPropsBuilder->GetTextProps(&qttp);
			OLECHAR chObj = kchObject;
			hr = qtsbStringBuilder->ReplaceRgch(0, 0, &chObj, 1, qttp);
			hr = qtsbStringBuilder->GetString(&qtss);
			m_qts->AddString(qtss, m_qzvps, m_qvc);

			int cch;
			unitpp::assert_eq("SearchToLog failed", S_OK, m_qts->SearchToLog(0, true, &cch));
			unitpp::assert_eq("SearchToLog returned wrong value", 0, cch);

			unitpp::assert_eq("SearchToLog failed", S_OK, m_qts->SearchToLog(0, false, &cch));
			unitpp::assert_eq("SearchToLog returned wrong value", 1, cch);

			unitpp::assert_eq("SearchToLog failed", S_OK, m_qts->SearchToLog(1, false, &cch));
			unitpp::assert_eq("SearchToLog returned wrong value", 2, cch);

			unitpp::assert_eq("SearchToLog failed", S_OK, m_qts->SearchToLog(3, false, &cch));
			unitpp::assert_eq("SearchToLog returned wrong value", 4, cch);
		}

		// Test basics of ConcTxtSrc offset conversion.
		void testConcTxtSrc()
		{
			HRESULT hr;
			// Make a string and then a text source out of it.
			ITsStringPtr qtss;
			ITsStrBldrPtr qtsbStringBuilder;
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			ITsPropsBldrPtr qtpbTextPropsBuilder;
			qtpbTextPropsBuilder.CreateInstance(CLSID_TsPropsBldr);
			hr = qtpbTextPropsBuilder->SetIntPropValues(ktptWs, ktpvDefault, g_wsEng);
			ITsTextPropsPtr qttp;
			hr = qtpbTextPropsBuilder->GetTextProps(&qttp);
			StrUni stuSearch;
			OLECHAR * pchBuf;
			int initSize = 700;
			stuSearch.SetSize(initSize, &pchBuf); // needs to be big enough for 150 discard at start, 200 at end.
			for (int i = 0; i < initSize; i++)
				*(pchBuf + i) = L'e';

			hr = qtsbStringBuilder->Replace(0, 0, stuSearch.Bstr(), qttp);

			// Insert a footnote ORC into the string.
			StrUni stuData;
			OLECHAR * prgchData;
			GUID uidSimulatedFootnote;
			hr = CoCreateGuid(&uidSimulatedFootnote);
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtOwnNameGuidHot;
			memmove(prgchData + 1, &uidSimulatedFootnote, isizeof(uidSimulatedFootnote));
			hr = qtpbTextPropsBuilder->SetStrPropValue(ktptObjData, stuData.Bstr());
			hr = qtpbTextPropsBuilder->GetTextProps(&qttp);
			OLECHAR chObj = kchObject;
			hr = qtsbStringBuilder->ReplaceRgch(0, 0, &chObj, 1, qttp);
			hr = qtsbStringBuilder->ReplaceRgch(250, 250, &chObj, 1, qttp);
			hr = qtsbStringBuilder->ReplaceRgch(350, 350, &chObj, 1, qttp);
			OLECHAR chX = 'X'; // marker
			hr = qtsbStringBuilder->ReplaceRgch(300, 301, &chX, 1, qttp);
			hr = qtsbStringBuilder->GetString(&qtss);
			VwConcTxtSrcPtr qcts;
			qcts.Attach(NewObj VwConcTxtSrc());
			qcts->SetWritingSystemFactory(g_qwsf);
			qcts->Init(301, 306, true);
			qcts->AddString(qtss, m_qzvps, m_qvc);

			// The logical string is now initSize + 3 long, with ORCs at 0, 250, and 350, and an 'X' at 300.
			// The rendered string is initSize + 15 long (since arbitrary orcs are changed to <obj> by the dummy VC),
			// with <obj> at 0, 254, and 358, and 'X' at 308.
			// Since our 'keyword' is at (logical) 301, turns out we will truncate 150 (logical) characters at the start,
			// which works out to 154 rendered characters (it inclueds one orc).
			// The virtual string therefore has an orc at 100 and 200, while the rendered one has <obj> at 100 and 204.
			// the 'X' is at 150/154.
			// The 'bold' range is (in rendered characters) from 155 to 160.

			OLECHAR buf[20];

			hr = qcts->Fetch(154, 156, buf);
			unitpp::assert_true("Fetch got wrong data", wcsncmp(buf, L"Xe", 2) == 0);

			LgCharRenderProps chrp;
			int ichMin;
			int ichLim;
			hr = qcts->GetCharProps(155, &chrp, &ichMin, &ichLim);
			unitpp::assert_eq("wrong min range from GetCharProps", 155, ichMin);
			unitpp::assert_eq("wrong lim range from GetCharProps", 160, ichLim);
			unitpp::assert_eq("GetCharProps not bold", kttvForceOn, chrp.ttvBold);
		}

		virtual void Setup()
		{
			CreateTestWritingSystemFactory();
			//// Get the WS for English...allows us to set properties on it.
			//g_qwsf->get_EngineOrNull(g_wsEng, &m_qwsEng);
			m_qtsf.CreateInstance(CLSID_TsStrFactory);
			// Use this rather than CreateInstance, because for the ORC test the pattern
			// and text source need to be in the same compilation unit.
			//m_qpat.Attach(NewObj VwPattern());
			m_qts.Attach(NewObj VwMappedTxtSrc());
			m_qzvps.Attach(NewObj VwPropertyStore());
			m_qts->SetWritingSystemFactory(g_qwsf);
			m_qzvps->putref_WritingSystemFactory(g_qwsf);
			m_qvc.Attach(NewObj DummyBaseVc());
		}
		virtual void Teardown()
		{
			m_qvc.Clear();
			m_qts.Clear();
			//m_qpat.Clear();
			//m_qwsEng.Clear();
			m_qtsf.Clear();
			m_qzvps.Clear();
			CloseTestWritingSystemFactory();
		}
	};
}

#endif /*TESTVWTXTSRC_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat"
// End: (These 4 lines are useful to Steve McConnel.)

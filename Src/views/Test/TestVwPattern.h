/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestVwPattern.h
Responsibility:
Last reviewed:

	Unit tests for the VwPattern class, particularly the actual search process.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TestVwPattern_H_INCLUDED
#define TestVwPattern_H_INCLUDED

#pragma once

#include "testViews.h"

namespace TestViews
{
#define kichoffsetofAnd 24
#define kfragStText 1
#define kfragStTxtPara 2
#define khvoOrigPara1 998
#define khvoOrigPara2 999

#define LATIN_CAPITAL_A L"\x0041"
#define LATIN_CAPITAL_A_WITH_DIARESIS L"\x00C4"
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
#ifdef WIN32
#define MUSICAL_SYMBOL_MINIMA L"\xD834\xDDBB" // 1D1BB decomposes to 1D1B9 1D165
#define MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"\xD834\xDDB9" // 1D1B9
#define MUSICAL_SYMBOL_COMBINING_STEM L"\xD834\xDD65" // 1D165
#else
#define MUSICAL_SYMBOL_MINIMA L"\x1D1BB" // 1D1BB decomposes to 1D1B9 1D165
#define MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"\x1D1B9" // 1D1B9
#define MUSICAL_SYMBOL_COMBINING_STEM L"\x1D165" // 1D165
#endif

	class DummySimpleParaVc : public DummyBaseVc
	{
	public:
		STDMETHOD(Display)(IVwEnv* pvwenv, HVO hvo, int frag)
		{
			switch(frag)
			{
			case kfragStText: // An StText, display paragraphs not lazily.
				pvwenv->AddObjVecItems(kflidStText_Paragraphs, this, kfragStTxtPara);
				break;
			case kfragStTxtPara: // StTxtPara, display contents
				pvwenv->AddStringProp(kflidStTxtPara_Contents, NULL);
				break;
			}
			return S_OK;
		}
	};

	class TestVwPattern : public unitpp::suite
	{
	public:
		TestVwPattern();
		ILgWritingSystemPtr m_qwsEng;
		ITsStrFactoryPtr m_qtsf;
		IVwPatternPtr m_qpat;
		VwTxtSrcPtr m_qts;
		VwPropertyStorePtr m_qzvps;

		void testRegExpSearch()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			StrUni stuSearch(L"and this sentence uses 'and' a lot and");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L".n.");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Set regular expression matching.
			CheckHr(m_qpat->put_UseRegularExpressions(true));

			// First match should be from 0 to 3
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' at start", 0, ichMin);
			unitpp::assert_eq("End of '.n.' at start", 3, ichLim);

			// Second match should be first 'n' in sentence, from 10 to 13
			CheckHr(m_qpat->FindIn(m_qts, 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' in middle", 10, ichMin);
			unitpp::assert_eq("End of '.n.' in middle", 10 + 3, ichLim);

			// Third match should be second 'n' in sentence, from 13 to 16
			CheckHr(m_qpat->FindIn(m_qts, 12, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' in middle", 13, ichMin);
			unitpp::assert_eq("End of '.n.' in middle", 13 + 3, ichLim);

			// Fourth match should be from 24 to 27
			CheckHr(m_qpat->FindIn(m_qts, 16, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' in middle", kichoffsetofAnd, ichMin);
			unitpp::assert_eq("End of '.n.' in middle", kichoffsetofAnd + 3, ichLim);

			// Should also match it starting exactly there.
			CheckHr(m_qpat->FindIn(m_qts, kichoffsetofAnd, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' in middle starting there", kichoffsetofAnd, ichMin);
			unitpp::assert_eq("End of '.n.' in middle starting there", kichoffsetofAnd + 3, ichLim);

			// Final match should be at end
			CheckHr(m_qpat->FindIn(m_qts, ichMin + 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' at end", stuSearch.Length() - 3, ichMin);
			unitpp::assert_eq("End of '.n.' at end", stuSearch.Length(), ichLim);

			// Now try backwards.
			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' back from end", stuSearch.Length() - 3, ichMin);
			unitpp::assert_eq("End of '.n.' back from end", stuSearch.Length(), ichLim);

			// Second match should be from 24 to 27
			CheckHr(m_qpat->FindIn(m_qts, ichLim - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' backwards in middle", kichoffsetofAnd, ichMin);
			unitpp::assert_eq("End of '.n.' ackwards in middle", kichoffsetofAnd + 3, ichLim);

			// Backwards match exactly at end of match.
			CheckHr(m_qpat->FindIn(m_qts, ichLim, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' backwards in middle", kichoffsetofAnd, ichMin);
			unitpp::assert_eq("End of '.n.' backwards in middle", kichoffsetofAnd + 3, ichLim);

			// Third match should be second 'n' in sentence, from 13 to 16
			CheckHr(m_qpat->FindIn(m_qts, ichLim - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' backwards in middle", 13, ichMin);
			unitpp::assert_eq("End of '.n.' backwards in middle", 13 + 3, ichLim);

			// Fourth match should be first 'n' in sentence, from 10 to 13
			CheckHr(m_qpat->FindIn(m_qts, ichLim - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' backwards in middle", 10, ichMin);
			unitpp::assert_eq("End of '.n.' backwards in middle", 10 + 3, ichLim);

			// First match should be from 0 to 3
			CheckHr(m_qpat->FindIn(m_qts, ichLim - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '.n.' at start", 0, ichMin);
			unitpp::assert_eq("End of '.n.' at start", 3, ichLim);

			// Generate a syntax error. It should produce a message but not a bad hresult.
			ITsStringPtr qtssBadPattern;
			StrUni stuBadPattern(L"(n.");
			CheckHr(m_qtsf->MakeString(stuBadPattern.Bstr(), g_wsEng, &qtssBadPattern));
			CheckHr(m_qpat->putref_Pattern(qtssBadPattern));

			SmartBstr sbstrMsg;
			CheckHr(m_qpat->get_ErrorMessage(&sbstrMsg));
			StrUni stuMsg(L"U_REGEX_MISMATCHED_PAREN");
			unitpp::assert_true("Got expected error message", !wcscmp(sbstrMsg.Chars(), stuMsg.Chars()));

			// Try a more interesting pattern that lets us check group and output text.
			ITsStringPtr qtssGroupPattern;
			StrUni stuGroupPattern(L"(.)n(.*?) ");
			CheckHr(m_qtsf->MakeString(stuGroupPattern.Bstr(), g_wsEng, &qtssGroupPattern));
			CheckHr(m_qpat->putref_Pattern(qtssGroupPattern));

			// Starting at 1 finds a more interesting example than at 0.
			CheckHr(m_qpat->FindIn(m_qts, 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found '(.)n(.*) ' from 1", 10, ichMin);
			unitpp::assert_eq("End of '(.)n(.*) ' from 1", 18, ichLim);

			ITsStringPtr qtssGroup1;
			CheckHr(m_qpat->get_Group(1, &qtssGroup1));
			SmartBstr sbstrGroup1;
			CheckHr(qtssGroup1->get_Text(&sbstrGroup1));
			unitpp::assert_true("group 1 is 'e'", !wcscmp(OleStringLiteral(L"e"), sbstrGroup1.Chars()));

			ITsStringPtr qtssGroup2;
			CheckHr(m_qpat->get_Group(2, &qtssGroup2));
			SmartBstr sbstrGroup2;
			CheckHr(qtssGroup2->get_Text(&sbstrGroup2));
			unitpp::assert_true("group 2 is 'tence'", !wcscmp(OleStringLiteral(L"tence"), sbstrGroup2.Chars()));

			ITsStringPtr qtssGroup0;
			CheckHr(m_qpat->get_Group(0, &qtssGroup0));
			SmartBstr sbstrGroup0;
			CheckHr(qtssGroup0->get_Text(&sbstrGroup0));
			unitpp::assert_true("group 0 is 'entence'", !wcscmp(OleStringLiteral(L"entence "), sbstrGroup0.Chars()));

			ITsStringPtr qtssGroup3;
			CheckHr(m_qpat->get_Group(3, &qtssGroup3));
			int cchBad;
			CheckHr(qtssGroup3->get_Length(&cchBad));
			unitpp::assert_eq("Bad group gives empty string", 0, cchBad);

			ITsStringPtr qtssRepWith;
			// Note that each pair of backslashes collapses to a single one in the C++ compiler.
			// So the actual replacement string is "reversing $1 \\an\d $2 makes $2$1\\$5.
			// In the output, \\ reduces to one backslash (which is two in the wcscmp);
			// \d reduces to simply d.
			StrUni stuRepWith(L"reversing $1 \\\\an\\d $2 makes $2$1\\$5.");
			CheckHr(m_qtsf->MakeString(stuRepWith.Bstr(), g_wsEng, &qtssRepWith));
			CheckHr(m_qpat->putref_ReplaceWith(qtssRepWith));

			ITsStringPtr qtssRepText;
			CheckHr(m_qpat->get_ReplacementText(&qtssRepText));
			SmartBstr sbstrRepText;
			CheckHr(qtssRepText->get_Text(&sbstrRepText));
			unitpp::assert_true("replacement text", !wcscmp(OleStringLiteral(L"reversing e \\and tence makes tencee$5."), sbstrRepText.Chars()));

			// With match writing system set, replacement text conforms to ws of $n.
			// Make a replacement that is simply $2$1 with $1 in French.
			m_qpat->put_MatchOldWritingSystem(true);
			ITsStringPtr qtssRepMow;
			CheckHr(m_qtsf->MakeStringRgch(OleStringLiteral(L"$2$1"), 4, g_wsEng, &qtssRepMow));
			ITsStrBldrPtr qtsb;
			qtssRepMow->GetBldr(&qtsb);
			CheckHr(qtsb->SetIntPropValues(2, 4, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->GetString(&qtssRepMow));
			CheckHr(m_qpat->putref_ReplaceWith(qtssRepMow));

			ITsStringPtr qtssRepTextMow;
			CheckHr(m_qpat->get_ReplacementText(&qtssRepTextMow));
			SmartBstr sbstrRepTextMow;
			CheckHr(qtssRepTextMow->get_Text(&sbstrRepTextMow));
			unitpp::assert_true("replacement text matching WS", !wcscmp(OleStringLiteral(L"tencee"), sbstrRepTextMow.Chars()));

			int crun;
			CheckHr(qtssRepTextMow->get_RunCount(&crun));
			unitpp::assert_eq("string with two wss should have two runs", 2, crun);

			ITsTextPropsPtr qttpFirst, qttpSecond;
			CheckHr(qtssRepTextMow->get_PropertiesAt(4, &qttpFirst));
			CheckHr(qtssRepTextMow->get_PropertiesAt(5, &qttpSecond));
			unitpp::assert_true("should have different properties for last character", qttpFirst != qttpSecond);

			int nval, nvar;
			CheckHr(qttpFirst->GetIntPropValues(ktptWs, &nvar, &nval));
			unitpp::assert_eq("first run should be English", g_wsEng, nval);

			CheckHr(qttpSecond->GetIntPropValues(ktptWs, &nvar, &nval));
			unitpp::assert_eq("second run should be French", g_wsFrn, nval);

			CheckHr(m_qpat->put_UseRegularExpressions(false)); // be sure not to affect any other tests.
		}

		void testSearchForCharStyleAfterORC()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			// the default simple text source won't work for this test.
			VwTxtSrcPtr qts;
			qts.Attach(NewObj VwMappedTxtSrc());
			IVwViewConstructorPtr qvc; // must have a VC to interpret ORCs.
			qvc.Attach(NewObj DummyBaseVc());

			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			ITsStrBldrPtr qtsbStringBuilder;
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			ITsPropsBldrPtr qtpbTextPropsBuilder;
			qtpbTextPropsBuilder.CreateInstance(CLSID_TsPropsBldr);
			CheckHr(qtpbTextPropsBuilder->SetIntPropValues(ktptWs, ktpvDefault, g_wsEng));
			ITsTextPropsPtr qttp;
			CheckHr(qtpbTextPropsBuilder->GetTextProps(&qttp));
			// ORC will go after "text"
			StrUni stuSearch(L"Some text Additional stuff");
			CheckHr(qtsbStringBuilder->Replace(0, 0, stuSearch.Bstr(), qttp));
			// Put the verse number style in the space after 'Additional'
			StrUni stuStyle(L"Verse Number");
			CheckHr(qtsbStringBuilder->SetStrPropValue(20, 21, ktptNamedStyle, stuStyle.Bstr()));

			// Insert a footnote ORC into the string.
			StrUni stuData;
			OLECHAR * prgchData;
			GUID uidSimulatedFootnote;
			CheckHr(CoCreateGuid(&uidSimulatedFootnote));
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtOwnNameGuidHot;
			memmove(prgchData + 1, &uidSimulatedFootnote, isizeof(uidSimulatedFootnote));
			CheckHr(qtpbTextPropsBuilder->SetStrPropValue(ktptObjData, stuData.Bstr()));
			CheckHr(qtpbTextPropsBuilder->GetTextProps(&qttp));
			OLECHAR chObj = kchObject;
			CheckHr(qtsbStringBuilder->ReplaceRgch(9, 9, &chObj, 1, qttp));
			CheckHr(qtsbStringBuilder->GetString(&qtssSearch));
			qts->AddString(qtssSearch, m_qzvps, qvc);

			// Create a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L" ");
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			qtsbStringBuilder->Replace(0, 0, stuPattern.Bstr(), NULL);
			qtsbStringBuilder->SetStrPropValue(0, 1, ktptNamedStyle, stuStyle.Bstr());
			qtsbStringBuilder->SetIntPropValues(0, 1, ktptWs, ktpvDefault, g_wsEng);
			qtsbStringBuilder->GetString(&qtssPattern);
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Match should be from 21 to 22 (because the ORC was put in the string)
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(qts, 0, stuSearch.Length() + 1, true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found ' ' at start", 21, ichMin);
			unitpp::assert_eq("End of ' ' at end", 22, ichLim);
		}

		void testSearchForWSAfterORC()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			// the default simple text source won't work for this test.
			VwTxtSrcPtr qts;
			qts.Attach(NewObj VwMappedTxtSrc());
			IVwViewConstructorPtr qvc; // must have a VC to interpret ORCs.
			qvc.Attach(NewObj DummyBaseVc());

			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			ITsStrBldrPtr qtsbStringBuilder;
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			ITsPropsBldrPtr qtpbTextPropsBuilder;
			qtpbTextPropsBuilder.CreateInstance(CLSID_TsPropsBldr);
			CheckHr(qtpbTextPropsBuilder->SetIntPropValues(ktptWs, ktpvDefault, g_wsEng));
			ITsTextPropsPtr qttp;
			CheckHr(qtpbTextPropsBuilder->GetTextProps(&qttp));
			// ORC will go after "text"
			StrUni stuSearch(L"Some text Additional stuff");
			CheckHr(qtsbStringBuilder->Replace(0, 0, stuSearch.Bstr(), qttp));
			// Put the French WS in the space after 'Additional'
			CheckHr(qtsbStringBuilder->SetIntPropValues(20, 21, ktptWs, ktpvDefault, g_wsFrn));

			// Insert a footnote ORC into the string.
			StrUni stuData;
			OLECHAR * prgchData;
			GUID uidSimulatedFootnote;
			CheckHr(CoCreateGuid(&uidSimulatedFootnote));
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtOwnNameGuidHot;
			memmove(prgchData + 1, &uidSimulatedFootnote, isizeof(uidSimulatedFootnote));
			CheckHr(qtpbTextPropsBuilder->SetStrPropValue(ktptObjData, stuData.Bstr()));
			CheckHr(qtpbTextPropsBuilder->GetTextProps(&qttp));
			OLECHAR chObj = kchObject;
			CheckHr(qtsbStringBuilder->ReplaceRgch(9, 9, &chObj, 1, qttp));
			CheckHr(qtsbStringBuilder->GetString(&qtssSearch));
			qts->AddString(qtssSearch, m_qzvps, qvc);

			// Create a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L" ");
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			qtsbStringBuilder->Replace(0, 0, stuPattern.Bstr(), NULL);
			qtsbStringBuilder->SetIntPropValues(0, 1, ktptWs, ktpvDefault, g_wsFrn);
			qtsbStringBuilder->GetString(&qtssPattern);
			CheckHr(m_qpat->put_MatchOldWritingSystem(true));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Match should be from 21 to 22 (because the ORC was put in the string)
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(qts, 0, stuSearch.Length() + 1, true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found ' ' at start", 21, ichMin);
			unitpp::assert_eq("End of ' ' at end", 22, ichLim);
		}

		void testORCSearch()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			// the default simple text source won't work for this test.
			VwTxtSrcPtr qts;
			qts.Attach(NewObj VwMappedTxtSrc());
			IVwViewConstructorPtr qvc; // must have a VC to interpret ORCs.
			qvc.Attach(NewObj DummyBaseVc());

			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			ITsStrBldrPtr qtsbStringBuilder;
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			ITsPropsBldrPtr qtpbTextPropsBuilder;
			qtpbTextPropsBuilder.CreateInstance(CLSID_TsPropsBldr);
			CheckHr(qtpbTextPropsBuilder->SetIntPropValues(ktptWs, ktpvDefault, g_wsEng));
			ITsTextPropsPtr qttp;
			CheckHr(qtpbTextPropsBuilder->GetTextProps(&qttp));
			// ORC will go after "text"
			StrUni stuSearch(L"Some text Additional stuff");
			CheckHr(qtsbStringBuilder->Replace(0, 0, stuSearch.Bstr(), qttp));

			// Insert a footnote ORC into the string.
			StrUni stuData;
			OLECHAR * prgchData;
			GUID uidSimulatedFootnote;
			CheckHr(CoCreateGuid(&uidSimulatedFootnote));
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtOwnNameGuidHot;
			memmove(prgchData + 1, &uidSimulatedFootnote, isizeof(uidSimulatedFootnote));
			CheckHr(qtpbTextPropsBuilder->SetStrPropValue(ktptObjData, stuData.Bstr()));
			CheckHr(qtpbTextPropsBuilder->GetTextProps(&qttp));
			OLECHAR chObj = kchObject;
			CheckHr(qtsbStringBuilder->ReplaceRgch(9, 9, &chObj, 1, qttp));
			CheckHr(qtsbStringBuilder->GetString(&qtssSearch));
			qts->AddString(qtssSearch, m_qzvps, qvc);

			// Create a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"text Additional");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Since there are no magic characters in the string, the result should be the same
			// with or without regular expression matching.
			for (int testRun = 0; testRun < 2; testRun++)
			{
				// Turn regular expression matching off for the first run, on for the second.
				CheckHr(m_qpat->put_UseRegularExpressions(testRun == 1));

				// First match should be from 5 to 21
				int ichMin, ichLim;
				CheckHr(m_qpat->FindIn(qts, 0, stuSearch.Length() + 1, true, &ichMin, &ichLim, NULL));
				unitpp::assert_eq("Found 'text' at start", 5, ichMin);
				unitpp::assert_eq("End of 'Additional' at end", 21, ichLim);

				ITsStringPtr qtssRepWith;
				StrUni stuRepWith(L"stuff More");
				CheckHr(m_qtsf->MakeString(stuRepWith.Bstr(), g_wsEng, &qtssRepWith));
				CheckHr(m_qpat->putref_ReplaceWith(qtssRepWith));

				ITsStringPtr qtssRepText;
				CheckHr(m_qpat->get_ReplacementText(&qtssRepText));
				SmartBstr sbstrRepText;
				CheckHr(qtssRepText->get_Text(&sbstrRepText));
				int cRun;
				CheckHr(qtssRepText->get_RunCount(&cRun));
				unitpp::assert_eq("Should have one run for the text, plus one for the ORC", 2, cRun);
				SmartBstr sbstrTest;
				CheckHr(qtssRepText->get_RunText(0, &sbstrTest));
				unitpp::assert_true("should be the first run of replace text", !wcscmp(OleStringLiteral(L"stuff More"), sbstrTest.Chars()));
				CheckHr(qtssRepText->get_RunText(1, &sbstrTest));
				unitpp::assert_eq("should be the ORC", kchObject, sbstrTest[0]);
				CheckHr(qtssRepText->get_Properties(1, &qttp));
				SmartBstr sbstrVal;
				qttp->GetStrPropValue(ktptObjData, &sbstrVal);
				unitpp::assert_true("Got the right footnote guid and stuff", !wcscmp(stuData.Chars(), sbstrVal.Chars()));
			}
		}

		void testORCSearch_ReplaceCharPrecedingFinalORC_TE4727()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			// the default simple text source won't work for this test.
			VwTxtSrcPtr qts;
			qts.Attach(NewObj VwMappedTxtSrc());
			IVwViewConstructorPtr qvc; // must have a VC to interpret ORCs.
			qvc.Attach(NewObj DummyBaseVc());

			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			ITsStrBldrPtr qtsbStringBuilder;
			qtsbStringBuilder.CreateInstance(CLSID_TsStrBldr);
			ITsPropsBldrPtr qtpbTextPropsBuilder;
			qtpbTextPropsBuilder.CreateInstance(CLSID_TsPropsBldr);
			CheckHr(qtpbTextPropsBuilder->SetIntPropValues(ktptWs, ktpvDefault, g_wsEng));
			ITsTextPropsPtr qttp;
			CheckHr(qtpbTextPropsBuilder->GetTextProps(&qttp));
			// ORC will go after "text"
			StrUni stuSearch(L"Tha Tant of tha Lord's Prasance");
			CheckHr(qtsbStringBuilder->Replace(0, 0, stuSearch.Bstr(), qttp));

			// Insert a footnote ORC into the string.
			StrUni stuData;
			OLECHAR * prgchData;
			GUID uidSimulatedFootnote;
			CheckHr(CoCreateGuid(&uidSimulatedFootnote));
			// Make large enough for a guid plus the type character at the start.
			stuData.SetSize(isizeof(GUID) / isizeof(OLECHAR) + 1, &prgchData);
			*prgchData = kodtOwnNameGuidHot;
			memmove(prgchData + 1, &uidSimulatedFootnote, isizeof(uidSimulatedFootnote));
			CheckHr(qtpbTextPropsBuilder->SetStrPropValue(ktptObjData, stuData.Bstr()));
			CheckHr(qtpbTextPropsBuilder->GetTextProps(&qttp));
			OLECHAR chObj = kchObject;
			CheckHr(qtsbStringBuilder->ReplaceRgch(31, 31, &chObj, 1, qttp));
			CheckHr(qtsbStringBuilder->GetString(&qtssSearch));
			qts->AddString(qtssSearch, m_qzvps, qvc);

			// Create a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"e");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Turn regular expression matching off.
			CheckHr(m_qpat->put_UseRegularExpressions(false));

			// First match should be from 30 to 31
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(qts, 0, stuSearch.Length() + 1, true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Should have found the 'e'", 30, ichMin);
			unitpp::assert_eq("Should have found a single character", 31, ichLim);
		}

		// Tests that attempting to find a search pattern that contains only ignorable characters
		// (characters that are ignored by the collator during a find) works correctly. (FWR-2176)
		void testOnlyIgnoreableCharacters()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			StrUni stuSearch(L"This is my\x200B favorite subject");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"\x200B");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Should not search for ignoreable characters, so won't find a match
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found ignoreable character", -1, ichMin);
			unitpp::assert_eq("Found ignoreable character", -1, ichLim);
		}

		void testSimpleSearch()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			StrUni stuSearch(L"and this sentence uses 'and' a lot and");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"and");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// First match should be from 0 to 3
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'and' at start", 0, ichMin);
			unitpp::assert_eq("End of 'and' at start", 3, ichLim);

			// Second match should be from 24 to 27
			CheckHr(m_qpat->FindIn(m_qts, 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'and' in middle", kichoffsetofAnd, ichMin);
			unitpp::assert_eq("End of 'and' in middle", kichoffsetofAnd + 3, ichLim);

			// Should also match it starting exactly there.
			CheckHr(m_qpat->FindIn(m_qts, kichoffsetofAnd, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'and' in middle starting there", kichoffsetofAnd, ichMin);
			unitpp::assert_eq("End of 'and' in middle starting there", kichoffsetofAnd + 3, ichLim);

			// Third match should be at end
			CheckHr(m_qpat->FindIn(m_qts, ichMin + 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'and' at end", stuSearch.Length() - 3, ichMin);
			unitpp::assert_eq("End of 'and' at end", stuSearch.Length(), ichLim);

			// Now try backwards.
			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'and' back from end", stuSearch.Length() - 3, ichMin);
			unitpp::assert_eq("End of 'and' back from end", stuSearch.Length(), ichLim);

			// Second match should be from 24 to 27
			CheckHr(m_qpat->FindIn(m_qts, ichLim - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'and' backwards in middle", kichoffsetofAnd, ichMin);
			unitpp::assert_eq("End of 'and' ackwards in middle", kichoffsetofAnd + 3, ichLim);

			// Backwards match exactly at end of match.
			CheckHr(m_qpat->FindIn(m_qts, ichLim, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'and' in middle", kichoffsetofAnd, ichMin);
			unitpp::assert_eq("End of 'and' in middle", kichoffsetofAnd + 3, ichLim);

			// First match should be from 0 to 3
			CheckHr(m_qpat->FindIn(m_qts, ichLim - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'and' at start", 0, ichMin);
			unitpp::assert_eq("End of 'and' at start", 3, ichLim);
		}

		void testMatchingCase()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());
			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			StrUni stuSearch(L"And this sentence uses 'and' a lot");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"and");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// First match should be from 0 to 3
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'And' at start", 0, ichMin);
			unitpp::assert_eq("End of 'And' at start", 3, ichLim);

			// ...should still match requiring diacritics to match
			CheckHr(m_qpat->put_MatchDiacritics(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'And' at start, match diacritics", 0, ichMin);

			// ...but not if we have to match case
			CheckHr(m_qpat->put_MatchCase(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Skipped 'And' at start, match case", kichoffsetofAnd, ichMin);
		}

		void testMatchingDiacritics()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());
			// Make a string to search and make a text source out of it. This one has a
			// diarhesis over the A, so it won't match if we require diacritics.
			ITsStringPtr qtssSearch;
			StrUni stuSearch(LATIN_CAPITAL_A_WITH_DIARESIS L"nd this sentence uses 'and' a lot");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"and");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// First match should be from 0 to 3
			int ichMin, ichLim;
			CheckHr(m_qpat->put_MatchDiacritics(false));	// This is set true by default.
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found 'Änd' at start (ignoring diacritics)", 0, ichMin);
			unitpp::assert_eq("End of 'Änd' at start (ignoring diacritics)", 3, ichLim);

			// ...but not match requiring diacritics to match
			CheckHr(m_qpat->put_MatchDiacritics(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Skipped 'Änd' at start (match diacritics)", kichoffsetofAnd, ichMin);

			// ...still not if we have to match case
			CheckHr(m_qpat->put_MatchDiacritics(false));
			CheckHr(m_qpat->put_MatchCase(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Skipped 'Änd' at start, match case", kichoffsetofAnd, ichMin);
		}

		/*--------------------------------------------------------------------------------------
			Test search when potentially messed up by pattern being canonically equivalent
			to target, but not identical. The search string has a fully composed A with diaresis
			and macron, and one decomposed out of order; the pattern has a correctly
			decomposed one.
		--------------------------------------------------------------------------------------*/
		void testSearchCanonical()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			ITsStringPtr qtssSearchT;
			StrUni stuSearch(L"abc" A_WITH_DIAERESIS COMBINING_DOT_BELOW L"abcA" COMBINING_DOT_BELOW
				COMBINING_DIAERESIS L"rubbish");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearchT));
			ITsStringPtr qtssSearch;
			CheckHr(qtssSearchT->get_NormalizedForm(knmNFD, &qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"cA" COMBINING_DOT_BELOW COMBINING_DIAERESIS);
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// First match should be from 2 to 6
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First canonical find start", 2, ichMin);
			unitpp::assert_eq("First canonical find end", 6, ichLim);

			// Second match should be from 8 to 12
			CheckHr(m_qpat->FindIn(m_qts, 3, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd canonical find start", 8, ichMin);
			unitpp::assert_eq("2nd canonical find end", 12, ichLim);

			// Same results even matching case and diacritics.
			CheckHr(m_qpat->put_MatchDiacritics(true));
			CheckHr(m_qpat->put_MatchCase(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First canonical find start, match case", 2, ichMin);
			unitpp::assert_eq("First canonical find end, match case", 6, ichLim);

			// Second match should be from 8 to 12
			// This is the one that required us to normalize. ICU doesn't currently work with
			// diacritics out of order.
			CheckHr(m_qpat->FindIn(m_qts, 3, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd canonical find start, match case", 8, ichMin);
			unitpp::assert_eq("2nd canonical find end, match case", 12, ichLim);

			// Try searching backwards.
			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd canonical backwards find start, match case/dia", 8, ichMin);
			unitpp::assert_eq("2nd canonical backwards find end, match case/dia", 12, ichLim);

			int ichLimSecond = ichLim;

			// Search backwards for the first occurrence.
			CheckHr(m_qpat->FindIn(m_qts, ichLimSecond - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First canonical find start backwards", 2, ichMin);
			unitpp::assert_eq("First canonical find end backwards", 6, ichLim);

			// If we don't care about diacritics, we would find the second base again, but a shorter
			// sequence of diacritics. (This behavior isn't very helpful to us, but it's worth
			// confirming that it is (still) what happens.)
			CheckHr(m_qpat->put_MatchDiacritics(false));
			CheckHr(m_qpat->put_MatchCase(false));
			CheckHr(m_qpat->FindIn(m_qts, ichLimSecond - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd canonical backwards find start, match case", 8, ichMin);
			unitpp::assert_eq("2nd canonical backwards find end, match case", ichLimSecond - 1, ichLim);
		}

		void testMatchingWs()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			ITsStringPtr qtssSearchT;
			StrUni stuSearch(L"abc" A_WITH_DIAERESIS COMBINING_DOT_BELOW L"abcA" COMBINING_DOT_BELOW
				COMBINING_DIAERESIS L"rubbish");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearchT));
			ITsStringPtr qtssSearch;
			CheckHr(qtssSearchT->get_NormalizedForm(knmNFD, &qtssSearch));
			ITsStrBldrPtr qtsb;
			CheckHr(qtssSearch->GetBldr(&qtsb));
			// Make the first A with diacritics and the later A and combining dot french.
			CheckHr(qtsb->SetIntPropValues(3, 6, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->SetIntPropValues(9, 11, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->GetString(&qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"cA" COMBINING_DOT_BELOW COMBINING_DIAERESIS);
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(qtssPattern->GetBldr(&qtsb));
			// Make the first A with diacritics and the later combining macron french.
			CheckHr(qtsb->SetIntPropValues(1, 4, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// First match should be from 2 to 6. (But default don't match ws.)
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First canonical find start", 2, ichMin);
			unitpp::assert_eq("First canonical find end", 6, ichLim);

			// Second match should be from 8 to 12
			CheckHr(m_qpat->FindIn(m_qts, 3, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd canonical find start", 8, ichMin);
			unitpp::assert_eq("2nd canonical find end", 12, ichLim);

			// Matching writing systems we still succeed, because we're ignoring diacritics.
			CheckHr(m_qpat->put_MatchDiacritics(false));	// This is set true by default.
			CheckHr(m_qpat->put_MatchOldWritingSystem(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First canonical find start, ws", 2, ichMin);
			unitpp::assert_eq("First canonical find end, ws", 6, ichLim);
			CheckHr(m_qpat->FindIn(m_qts, 3, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd canonical find start, ws", 8, ichMin);
			unitpp::assert_eq("2nd canonical find end, ws", 12, ichLim);

			// But if we also match diacritics, only the first occurrence matches.
			CheckHr(m_qpat->put_MatchDiacritics(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First canonical find start, ws/dia", 2, ichMin);
			unitpp::assert_eq("First canonical find end, ws/dia", 6, ichLim);
			CheckHr(m_qpat->FindIn(m_qts, 3, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Failed 2nd match ws & diacritics", -1, ichMin);
			unitpp::assert_eq("Failed 2nd match ws & diacritics", -1, ichLim);
		}

		// Asserts that a string has only one run in the specified writing system.
		void AssertUniformWs(int ws, ITsString * ptss)
		{
			int crun;
			CheckHr(ptss->get_RunCount(&crun));
			unitpp::assert_eq("string has only one run", 1, crun);
			ITsTextPropsPtr qtpt;
			CheckHr(ptss->get_Properties(0, &qtpt));
			int nval, nvar;
			CheckHr(qtpt->GetIntPropValues(ktptWs, &nvar, &nval));
			unitpp::assert_eq("string should be in expeted ws", ws, nval);
		}

		void testReplaceWsWithEmptyStrings()
		{
			ITsStringPtr qtssSearch;
			StrUni stuSearch(L"engFRNenglishFRENCH");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			ITsStrBldrPtr qtsb;
			CheckHr(qtssSearch->GetBldr(&qtsb));
			// Make the first FRN parts french.
			CheckHr(qtsb->SetIntPropValues(3, 6, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->SetIntPropValues(13, 19, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->GetString(&qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it: an empty English string.
			ITsStringPtr qtssPattern;
			CheckHr(m_qtsf->MakeString(NULL, g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->put_MatchOldWritingSystem(true));

			// First match should be from 0 to 3.
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First match start", 0, ichMin);
			unitpp::assert_eq("First match end", 3, ichLim);

			ITsStringPtr qtssRepWith;
			CheckHr(m_qtsf->MakeString(NULL, g_wsFrn, &qtssRepWith));
			CheckHr(m_qpat->putref_ReplaceWith(qtssRepWith));

			// Replacement should be 'eng' in French
			ITsStringPtr qtssRep;
			CheckHr(m_qpat->get_ReplacementText(&qtssRep));
			SmartBstr sbstrRep;
			CheckHr(qtssRep->get_Text(&sbstrRep));
			unitpp::assert_true("first match is 1st Eng string", wcscmp(sbstrRep.Chars(), OleStringLiteral(L"eng")) == 0);
			AssertUniformWs(g_wsFrn, qtssRep);

			CheckHr(m_qpat->FindIn(m_qts, 4, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd match start", 6, ichMin);
			unitpp::assert_eq("2nd match end", 13, ichLim);

			CheckHr(m_qpat->get_ReplacementText(&qtssRep));
			CheckHr(qtssRep->get_Text(&sbstrRep));
			unitpp::assert_true("second match is 2nd Eng string", wcscmp(sbstrRep.Chars(), OleStringLiteral(L"english")) == 0);
			AssertUniformWs(g_wsFrn, qtssRep);
		}


		void testMatchingStyleEtc()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			CheckHr(m_qpat->put_UseRegularExpressions(false)); // in case another test failed
			ITsStringPtr qtssSearchT;
			StrUni stuSearch(L"And this 'andy' sentence has a lot of this and that and");
			int ichAndy = 10;
			int ichLastAnd = stuSearch.Length() - 3;
			int ichThirdAnd = stuSearch.Length() - 12;
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearchT));
			ITsStringPtr qtssSearch;
			CheckHr(qtssSearchT->get_NormalizedForm(knmNFD, &qtssSearch));
			ITsStrBldrPtr qtsb;
			CheckHr(qtssSearch->GetBldr(&qtsb));
			// Make last two letters of first 'and' have a named style, and the third one have tags.
			StrUni stuStyleName(L"MyStyle");
			StrUni stuTag(L"abcd\0efgh", 9);
			CheckHr(qtsb->SetStrPropValue(ichAndy + 1, ichAndy + 5, ktptNamedStyle, stuStyleName.Bstr()));
			CheckHr(qtsb->SetStrPropValue(ichThirdAnd, ichThirdAnd + 3, ktptTags, stuTag.Bstr()));
			CheckHr(qtsb->GetString(&qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"and");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(qtssPattern->GetBldr(&qtsb));
			CheckHr(qtsb->SetStrPropValue(1, 3, ktptNamedStyle, stuStyleName.Bstr()));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Only match should be at ichAndy (match style is automatic if pattern has style)
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First style search find start", ichAndy, ichMin);
			unitpp::assert_eq("First style find end", ichAndy + 3, ichLim);

			// Same searching backwards.
			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First style search find start", ichAndy, ichMin);
			unitpp::assert_eq("First style find end", ichAndy + 3, ichLim);

			// No second match
			CheckHr(m_qpat->FindIn(m_qts, ichAndy + 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Only one style match", -1, ichMin);

			// Now try a pattern with incomplete tag.
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(qtssPattern->GetBldr(&qtsb));
			StrUni stuBadTag(L"abcd", 4);
			CheckHr(qtsb->SetStrPropValue(0, 3, ktptTags, stuBadTag.Bstr()));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			// No match with bad tags
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("No match bad tags", -1, ichMin);

			// Now try a pattern with correct tag.
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(qtssPattern->GetBldr(&qtsb));
			CheckHr(qtsb->SetStrPropValue(0, 3, ktptTags, stuTag.Bstr()));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			// Should find third string
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First tag search find start", ichThirdAnd, ichMin);
			unitpp::assert_eq("First tag find end", ichThirdAnd + 3, ichLim);
			// No second match
			CheckHr(m_qpat->FindIn(m_qts, ichThirdAnd + 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Only one tag match", -1, ichMin);

			// try a simple match, but requiring whole words.
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->put_MatchWholeWord(true));

			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First whole word start", 0, ichMin);
			unitpp::assert_eq("First whole word end", 3, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Second whole word start", ichThirdAnd, ichMin);
			unitpp::assert_eq("Second whole word end", ichThirdAnd + 3, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, ichThirdAnd + 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Third whole word start", ichLastAnd, ichMin);
			unitpp::assert_eq("Third whole word end", stuSearch.Length(), ichLim);

			// and backwards...
			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Third whole word start backwards", ichLastAnd, ichMin);
			unitpp::assert_eq("Third whole word end backwards", stuSearch.Length(), ichLim);

			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length() - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Second whole word start backwards", ichThirdAnd, ichMin);
			unitpp::assert_eq("Second whole word end backwards", ichThirdAnd + 3, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, ichThirdAnd - 1, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First whole word start backwards", 0, ichMin);
			unitpp::assert_eq("First whole word end backwards", 3, ichLim);

			// Try pattern of "and" with "<default chars>" style
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(qtssPattern->GetBldr(&qtsb));
			SmartBstr sbstrDefaultChars = _T("<!default chars!>");
			CheckHr(qtsb->SetStrPropValue(0, 3, ktptNamedStyle, sbstrDefaultChars));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->put_MatchWholeWord(false));

			// First match should be at begining "And"
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First default style search find start", 0, ichMin);
			unitpp::assert_eq("First default style find end", 3, ichLim);

			// Second match should be at second "and" - Andy should be skipped
			CheckHr(m_qpat->FindIn(m_qts, ichMin + 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Second default style search find start", ichThirdAnd, ichMin);
			unitpp::assert_eq("Second default style find end", ichThirdAnd + 3, ichLim);

			// Third match should be at last "and"
			CheckHr(m_qpat->FindIn(m_qts, ichMin + 1, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Third default style search find start", ichLastAnd, ichMin);
			unitpp::assert_eq("Third default style find end", ichLastAnd + 3, ichLim);
		}

		void testMatchingPropsOnly()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());

			ITsStringPtr qtssSearchT;
			StrUni stuSearch(L"And this 'andy' sentence has a lot of this and that and");
			int ichAndy = 10;
			int ichThirdAnd = stuSearch.Length() - 12;
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearchT));
			ITsStringPtr qtssSearch;
			CheckHr(qtssSearchT->get_NormalizedForm(knmNFD, &qtssSearch));
			ITsStrBldrPtr qtsb;
			CheckHr(qtssSearch->GetBldr(&qtsb));
			// Make last two letters of first 'and' have a named style, and the third one have
			// tags.
			StrUni stuStyleName(L"MyStyle");
			StrUni stuTag(L"abcd\0efgh", 9);
			CheckHr(qtsb->SetStrPropValue(ichAndy + 1, ichAndy + 5, ktptNamedStyle,
				stuStyleName.Bstr()));
			CheckHr(qtsb->SetStrPropValue(ichThirdAnd, ichThirdAnd + 3, ktptTags, stuTag.Bstr()));
			CheckHr(qtsb->SetIntPropValues(3, 5, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->SetIntPropValues(10, 15, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->GetString(&qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsFrn, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Two writing system matches.
			CheckHr(m_qpat->put_MatchOldWritingSystem(true));
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First ws-only find start", 3, ichMin);
			unitpp::assert_eq("First ws-only find end", 5, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, 5, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd ws-only find start", 10, ichMin);
			unitpp::assert_eq("2nd ws-only find end", 15, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, 15, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("No 3rd ws-only find start", -1, ichMin);

			CheckHr(m_qpat->FindIn(m_qts, 10, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First ws-only find start backwards", 3, ichMin);
			unitpp::assert_eq("First ws-only find end backwards", 5, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0,  false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd ws-only find start backwards", 10, ichMin);
			unitpp::assert_eq("2nd ws-only find end backwards", 15, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, 3, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("No 3rd ws-only find start backwards", -1, ichMin);

			// Now try a style search. This is implied by setting style on input string,
			// not currently controlled by a separate flag.
			CheckHr(qtssPattern->GetBldr(&qtsb));
			CheckHr(qtsb->SetStrPropValue(0, 0, ktptNamedStyle, stuStyleName.Bstr()));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->put_MatchOldWritingSystem(false));

			// style is set only at ichAndy + 1...ichAndy + 5
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(),  true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("style-only find start", ichAndy + 1, ichMin);
			unitpp::assert_eq("style-only find end", ichAndy + 5, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, ichAndy + 5, stuSearch.Length(),  true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq(" No 2nd style-only find start", -1, ichMin);

			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0,  false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("style-only find start backwards", ichAndy + 1, ichMin);
			unitpp::assert_eq("style-only find end backwards", ichAndy + 5, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, ichAndy, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("No 2nd style-only find backwards", -1, ichMin);

			// Now try a tag search. This is implied by setting style on input string,
			// not currently controlled by a separate flag.
			CheckHr(qtssPattern->GetBldr(&qtsb));
			CheckHr(qtsb->SetStrPropValue(0, 0, ktptNamedStyle, NULL));
			CheckHr(qtsb->SetStrPropValue(0, 0, ktptTags, stuTag.Bstr()));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			// tag is set only at ichThirdAnd...ichThirdAnd + 3
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(),  true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("tag-only find start", ichThirdAnd, ichMin);
			unitpp::assert_eq("tag-only find end", ichThirdAnd + 3, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, ichThirdAnd + 3, stuSearch.Length(),  true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq(" No 2nd tag-only find start", -1, ichMin);

			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0,  false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("tag-only find start backwards", ichThirdAnd, ichMin);
			unitpp::assert_eq("tag-only find end backwards", ichThirdAnd + 3, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, ichThirdAnd, 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("No 2nd tag-only find backwards", -1, ichMin);

			// Try a combination. No character has all three props.
			CheckHr(qtssPattern->GetBldr(&qtsb));
			CheckHr(qtsb->SetStrPropValue(0, 0, ktptNamedStyle, stuStyleName.Bstr()));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->put_MatchOldWritingSystem(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("No 3-prop match", -1, ichMin);

			// But a French/Named style exists...
			CheckHr(qtssPattern->GetBldr(&qtsb));
			CheckHr(qtsb->SetStrPropValue(0, 0, ktptTags, NULL));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(),  true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("style and ws find start", 11, ichMin);
			unitpp::assert_eq("style and ws find end", 15, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, 16, stuSearch.Length(),  true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("No 2nd style and ws match", -1, ichMin);
		}

		void testReorderingDiactritics()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());
#ifdef ICUBugsFixed
			// This test could be reinstated if we think the bug in ICU is fixed. At present it
			// fails to find the first match, not considering A_WITH_DIAERESIS COMBINING_DOT_BELOW
			// to be equal to "A" COMBINING_DOT_BELOW COMBINING_DIAERESIS, though they are canonically
			// equal.
			int g_wsFrn = g_g_wsFrn;

			ITsStringPtr qtssSearch;
			StrUni stuSearch(L"abc" A_WITH_DIAERESIS COMBINING_DOT_BELOW L"abcA" COMBINING_DOT_BELOW
				COMBINING_DIAERESIS L"rubbish");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			ITsStrBldrPtr qtsb;
			CheckHr(qtssSearch->GetBldr(&qtsb));
			// Make the first A with diacritics and the later A and combining dot french.
			CheckHr(qtsb->SetIntPropValues(3, 5, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->SetIntPropValues(8, 10, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->GetString(&qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"cA" COMBINING_DOT_BELOW COMBINING_DIAERESIS);
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(qtssPattern->GetBldr(&qtsb));
			// Make the first A with diacritics and the later combining macron french.
			CheckHr(qtsb->SetIntPropValues(1, 4, ktptWs, ktpvDefault, g_wsFrn));
			CheckHr(qtsb->GetString(&qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// First match should be from 2 to 5. (By default don't match ws.)
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First canonical find start", 2, ichMin);
			unitpp::assert_eq("First canonical find end", 5, ichLim);

			// Second match should be from 7 to 11
			CheckHr(m_qpat->FindIn(m_qts, 3, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd canonical find start", 7, ichMin);
			unitpp::assert_eq("2nd canonical find end", 11, ichLim);

			// Matching writing systems we still succeed, because we're ignoring diacritics.
			CheckHr(m_qpat->put_MatchOldWritingSystem(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First canonical find start, ws", 2, ichMin);
			unitpp::assert_eq("First canonical find end, ws", 5, ichLim);
			CheckHr(m_qpat->FindIn(m_qts, 3, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("2nd canonical find start, ws", 7, ichMin);
			unitpp::assert_eq("2nd canonical find end, ws", 11, ichLim);

			// But if we also match diacritics, there is only one match.
			CheckHr(m_qpat->put_MatchDiacritics(true));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("First canonical find start, ws/dia", 2, ichMin);
			unitpp::assert_eq("First canonical find end, ws/dia", 5, ichLim);
			CheckHr(m_qpat->FindIn(m_qts, 3, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Failed match ws & diacritics", -1, ichMin);
			unitpp::assert_eq("Failed match ws & diacritics", -1, ichLim);
#endif
		}

		// Test searching with surrogate pairs. Note however that if the x's in the source
		// and longer pattern strings are changed to punctuation characters such as space, hyphen,
		// comma, or question mark, the find fails. This has been reported to ICU (bug 3083).
		// There doesn't seem to be much we can do to improve things until ICU is fixed.
		// Todo: when it is fixed, we should reinstate that test to make sure it stays fixed!
		void testSurrogatePairSearch()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());
			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			StrUni stuSearch(L"andx" MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"ythis sentence");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(MUSICAL_SYMBOL_SEMIBREVIS_WHITE);
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Match should be from 0 to 3
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found semibreve", 4, ichMin);
			unitpp::assert_eq("End of semibreve", 6, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found semibreve backwards", 4, ichMin);
			unitpp::assert_eq("End of semibreve backwards", 6, ichLim);

			// Try a pattern that includes the semibreve.
			stuPattern = StrUni(L"x" MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"y");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found semibreve with other Chars", 3, ichMin);
			unitpp::assert_eq("End of semibreve with other Chars", 7, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found semibreve with other Chars backwards", 3, ichMin);
			unitpp::assert_eq("End of semibreve with other Chars backwards", 7, ichLim);
		}

		void testExplicitLocaleSearch()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());
			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			StrUni stuSearch(L"and AE this sentence");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"A_WITH_DIAERESIS");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Default collater it should not be found.
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("By default A-diaresis is not equal to AE", -1, ichMin);

			// But setting Locale to something with an appropriate rule it should..
			StrUni stuGermanLocale(L"de__PHONEBOOK");
			CheckHr(m_qpat->put_IcuLocale(stuGermanLocale.Bstr()));

			// Fails if pattern not normalized...
#ifdef ICUBugsFixed
			//CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			//unitpp::assert_eq("Found A-diaresis equals AE", 4, ichMin);
			//unitpp::assert_eq("End of A-diaresis equals AE", 6, ichLim);
#endif

			CheckHr(m_qpat->put_MatchDiacritics(false));	// This is set true by default.
			// Should also work with pattern normalized.
			stuPattern = StrUni(L"A" COMBINING_DIAERESIS);
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found A + combining diaresis equals AE", 4, ichMin);
			unitpp::assert_eq("End of A + combining diaresis equals AE", 6, ichLim);

			stuPattern = StrUni(L" A" COMBINING_DIAERESIS L" ");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found space A + combining diaresis space equals ' AE '", 3, ichMin);
			unitpp::assert_eq("End of space A + combining diaresis space equals ' AE '", 7, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found space A + combining diaresis space equals ' AE ' backwards", 3, ichMin);
			unitpp::assert_eq("End of space A + combining diaresis space equals ' AE ' backwards", 7, ichLim);
		}

		void testExplicitRulesSearch()
		{
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());
			// Make a string to search and make a text source out of it.
			ITsStringPtr qtssSearch;
			StrUni stuSearch(L"and AE this sentence");
			CheckHr(m_qtsf->MakeString(stuSearch.Bstr(), g_wsEng, &qtssSearch));
			m_qts->AddString(qtssSearch, m_qzvps, NULL);

			// and a pattern to search for and install it.
			ITsStringPtr qtssPattern;
			StrUni stuPattern(L"A_WITH_DIAERESIS");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// Default collater it should not be found.
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("By default A-diaresis is not equal to AE", -1, ichMin);

			// But setting collating rules to something with an appropriate rule it should..
			StrUni stuGermanRules(L"&ae <<< \\u00E4 &AE <<< \\u00C4");
			CheckHr(m_qpat->put_IcuCollatingRules(stuGermanRules.Bstr()));

#ifdef ICUBugsFixed
			// Rest of this doesn't work yet...try it with new version.
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found A-diaresis equals AE", 4, ichMin);
			unitpp::assert_eq("End of A-diaresis equals AE", 6, ichLim);

			 Should work with pattern normalized. Since our rule involves diacritics, make
			 it match them.
			 Balance of this test commented out because it just does not work at present.
			stuPattern = StrUni(L"A" COMBINING_DIAERESIS);
			CheckHr(m_qpat->put_MatchDiacritics(true));
			CheckHr(m_qpat->put_MatchCase(true));
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found A + combining diaresis equals AE", 4, ichMin);
			unitpp::assert_eq("End of A + combining diaresis equals AE", 6, ichLim);

			stuPattern = StrUni(L" A" COMBINING_DIAERESIS L" ");
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));
			CheckHr(m_qpat->FindIn(m_qts, 0, stuSearch.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found space A + combining diaresis space equals ' AE '", 3, ichMin);
			unitpp::assert_eq("End of space A + combining diaresis space equals ' AE '", 7, ichLim);

			CheckHr(m_qpat->FindIn(m_qts, stuSearch.Length(), 0, false, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found space A + combining diaresis space equals ' AE ' backwards", 3, ichMin);
			unitpp::assert_eq("End of space A + combining diaresis space equals ' AE ' backwards", 7, ichLim);
#endif
		}

		void FindAndCheck(IVwSelectionPtr & qselFound, int ichMin, int ichLim, bool fForward = true)
		{
			CheckHr(m_qpat->FindFrom(qselFound, fForward, NULL));
			CheckHr(m_qpat->GetSelection(true, &qselFound));
			unitpp::assert_true("Got a match", qselFound.Ptr() != NULL);
			ITsStringPtr qtssFound;
			int ichFound;
			ComBool fAssocPrev;
			HVO hvoFound;
			PropTag tagFound;
			int wsFound;
			CheckHr(qselFound->TextSelInfo(false, &qtssFound, &ichFound, &fAssocPrev,
				&hvoFound, &tagFound, &wsFound));
			unitpp::assert_eq("Right offset continued search", ichMin, ichFound);
			CheckHr(qselFound->TextSelInfo(true, &qtssFound, &ichFound, &fAssocPrev,
				&hvoFound, &tagFound, &wsFound));
			unitpp::assert_eq("Right end continued search", ichLim, ichFound);
			ComBool fMatch;
			CheckHr(m_qpat->MatchWhole(qselFound, &fMatch));
			unitpp::assert_true("MatchWhole succeeded", fMatch);
		}

		// this one makes a full-fledged view and does some real searches.
		void testRealSearch()
		{
			m_qpat->put_UseRegularExpressions(false); // in case another test failed
			unitpp::assert_true("English writing system exists", m_qwsEng.Ptr());
			IVwCacheDaPtr qcda;
			qcda.CreateInstance(CLSID_VwCacheDa);
			ISilDataAccessPtr qsda;
			qcda->QueryInterface(IID_ISilDataAccess, (void **)&qsda);

			qsda->putref_WritingSystemFactory(g_qwsf);

			ITsStrFactoryPtr qtsf;
			qtsf.CreateInstance(CLSID_TsStrFactory);
			ITsStringPtr qtss;
			// Now make two strings, the contents of paragraphs 1 and 2.
			StrUni stuPara1(L"abc" A_WITH_DIAERESIS COMBINING_DOT_BELOW L"abcA" COMBINING_DOT_BELOW
				COMBINING_DIAERESIS L"rubbish");
			qtsf->MakeString(stuPara1.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara1, kflidStTxtPara_Contents, qtss);
			StrUni stuPara2(L"This is the second abc test paragraph");
			qtsf->MakeString(stuPara2.Bstr(), g_wsEng, &qtss);
			qcda->CacheStringProp(khvoOrigPara2, kflidStTxtPara_Contents, qtss);

			// Now make them the paragraphs of an StText.
			HVO rghvo[2] = {khvoOrigPara1, khvoOrigPara2};
			HVO hvoRoot = 101;
			qcda->CacheVecProp(hvoRoot, kflidStText_Paragraphs, rghvo, 2);

			// Now make the root box and view constructor and Graphics object.
			IVwRootBoxPtr qrootb;
			// must be in same compilation unit as pattern, so don't just use CreateInstance.
			VwRootBox::CreateCom(NULL, IID_IVwRootBox, (void **)&qrootb);
			IVwGraphicsWin32Ptr qvg32;
			HDC hdc = 0;
			try
			{
				qvg32.CreateInstance(CLSID_VwGraphicsWin32);
				hdc = GetTestDC();
				qvg32->Initialize(hdc);

				IVwViewConstructorPtr qvc;
				qvc.Attach(NewObj DummySimpleParaVc());
				qrootb->putref_DataAccess(qsda);
				qrootb->SetRootObject(hvoRoot, qvc, kfragStText, NULL);
				DummyRootSitePtr qdrs;
				qdrs.Attach(NewObj DummyRootSite());
				Rect rcSrc(0, 0, 96, 96);
				qdrs->SetRects(rcSrc, rcSrc);
				qdrs->SetGraphics(qvg32);
				qrootb->SetSite(qdrs);
				CheckHr(qrootb->Layout(qvg32, 300));

				StrUni stuPattern = L"A" COMBINING_DIAERESIS;
				ITsStringPtr qtssPattern;
				CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
				CheckHr(m_qpat->putref_Pattern(qtssPattern));

				CheckHr(m_qpat->put_MatchDiacritics(false));	// This is set true by default.
				// OK, test data set up. Now try some tests...
				CheckHr(m_qpat->Find(qrootb, true, NULL));
				ComBool fFound;
				CheckHr(m_qpat->get_Found(&fFound));
				unitpp::assert_true("Found a match", fFound);
				IVwSelectionPtr qselFound;
				CheckHr(m_qpat->GetSelection(true, &qselFound));
				unitpp::assert_true("Got a selection", qselFound.Ptr() != NULL);
				ITsStringPtr qtssFound;
				int ichFound;
				ComBool fAssocPrev;
				HVO hvoFound;
				PropTag tagFound;
				int wsFound;
				CheckHr(qselFound->TextSelInfo(false, &qtssFound, &ichFound, &fAssocPrev,
					&hvoFound, &tagFound, &wsFound));
				// By default not matching diacritics or case, should find the initial
				// 'a' in the first paragraph.
				unitpp::assert_eq("First find in right obj", khvoOrigPara1, hvoFound);
				unitpp::assert_eq("Right offset first find", 0, ichFound);
				CheckHr(qselFound->TextSelInfo(true, &qtssFound, &ichFound, &fAssocPrev,
					&hvoFound, &tagFound, &wsFound));
				unitpp::assert_eq("Right end first find", 1, ichFound);

				CheckHr(m_qpat->FindFrom(qselFound, true, NULL));
				CheckHr(m_qpat->GetSelection(true, &qselFound));
				CheckHr(qselFound->TextSelInfo(false, &qtssFound, &ichFound, &fAssocPrev,
					&hvoFound, &tagFound, &wsFound));
				// Should find the 'A_WITH_DIARESIS' plus the following diacritic.
				// It should have been normalized so that makes three characters.
				unitpp::assert_eq("Right offset 2nd find", 3, ichFound);
				CheckHr(qselFound->TextSelInfo(true, &qtssFound, &ichFound, &fAssocPrev,
					&hvoFound, &tagFound, &wsFound));
				unitpp::assert_eq("Right end 2nd find", 6, ichFound);

				// We'll make this mid-para position the limit of our subsequent searches.
				// ...but not until we've done one more search or it will stop right here.
				IVwSelectionPtr qselLim = qselFound;

				// The 'a' at the start of the next 'abc'
				FindAndCheck(qselFound, 6, 7);

				// Now we can set the limit.
				CheckHr(m_qpat->putref_Limit(qselLim));

				// The next 'A' with two following diacritics.
				FindAndCheck(qselFound, 9, 12);
				// Try some special cases of MatchWhole
				VwTextSelection * ptsel = dynamic_cast<VwTextSelection *>(qselFound.Ptr());
				// Insert a few tests of MatchWhole
				m_qpat->put_MatchWholeWord(true);
				ComBool fMatch;
				CheckHr(m_qpat->MatchWhole(qselFound, &fMatch));
				unitpp::assert_true("MatchWhole failed because not whole word", !fMatch);
				m_qpat->put_MatchWholeWord(false);
				ptsel->m_ichEnd--;
				CheckHr(m_qpat->MatchWhole(qselFound, &fMatch));
				unitpp::assert_true("MatchWhole succeeded with reduced diacritics", fMatch);
				ptsel->m_ichAnchor--;
				CheckHr(m_qpat->MatchWhole(qselFound, &fMatch));
				unitpp::assert_true("MatchWhole should fail on anchor", !fMatch);
				ptsel->m_ichEnd += 2;
				ptsel->m_ichAnchor++;
				CheckHr(m_qpat->MatchWhole(qselFound, &fMatch));
				unitpp::assert_true("MatchWhole should fail on end", !fMatch);
				// Start of 'abc' in second paragraph.
				FindAndCheck(qselFound, 19, 20);
				// The three 'a's in 'paragraph'
				FindAndCheck(qselFound, 29, 30);
				FindAndCheck(qselFound, 31, 32);
				FindAndCheck(qselFound, 34, 35);
				CheckHr(m_qpat->FindFrom(qselFound, true, NULL));
				CheckHr(m_qpat->get_Found(&fFound));
				unitpp::assert_true("No more", !fFound);
				// But we should not be at the limit...just the end of the doc
				ComBool fAtLimit;
				CheckHr(m_qpat->get_StoppedAtLimit(&fAtLimit));
				unitpp::assert_true("Not at limit", !fAtLimit);

				// Wrap around to first 'a' in first paragraph.
				CheckHr(m_qpat->Find(qrootb, true, NULL));
				CheckHr(m_qpat->get_Found(&fFound));
				unitpp::assert_true("Found first match wrapped", fFound);
				CheckHr(m_qpat->GetSelection(true, &qselFound));
				unitpp::assert_true("Got a selection2", qselFound.Ptr() != NULL);
				CheckHr(qselFound->TextSelInfo(false, &qtssFound, &ichFound, &fAssocPrev,
					&hvoFound, &tagFound, &wsFound));
				// By default not matching diacritics or case, should find the initial
				// 'a' in the first paragraph.
				unitpp::assert_eq("Right offset first find", 0, ichFound);
				// Can also find the second A, right at the limit
				FindAndCheck(qselFound, 3, 6);
				// Now should hit limit.
				CheckHr(m_qpat->FindFrom(qselFound, true, NULL));
				CheckHr(m_qpat->get_Found(&fFound));
				unitpp::assert_true("Hit limit", !fFound);
				CheckHr(m_qpat->get_StoppedAtLimit(&fAtLimit));
				unitpp::assert_true("At limit", fAtLimit);


				// Now try searching backwards.
				CheckHr(m_qpat->Find(qrootb, false, NULL));
				CheckHr(m_qpat->get_Found(&fFound));
				unitpp::assert_true("Found first backwards match", fFound);
				CheckHr(m_qpat->GetSelection(true, &qselFound));
				unitpp::assert_true("Got a selection fb", qselFound.Ptr() != NULL);
				CheckHr(qselFound->TextSelInfo(false, &qtssFound, &ichFound, &fAssocPrev,
					&hvoFound, &tagFound, &wsFound));
				unitpp::assert_eq("Right offset fb", 34, ichFound);
				CheckHr(qselFound->TextSelInfo(true, &qtssFound, &ichFound, &fAssocPrev,
					&hvoFound, &tagFound, &wsFound));
				unitpp::assert_eq("Right offset fb", 35, ichFound);

				// The other two 'a's in 'paragraph'
				FindAndCheck(qselFound, 31, 32, false);
				FindAndCheck(qselFound, 29, 30, false);
				// Start of 'abc' in second paragraph.
				FindAndCheck(qselFound, 19, 20, false);
				// The next 'A' with two following diacritics near the end of para 1.
				FindAndCheck(qselFound, 9, 12, false);
				// The 'a' at the start of the second 'abc' in para 1
				FindAndCheck(qselFound, 6, 7, false);
				// The first 'A' with diacritics in para 1. This is exactly AT the limit
				// (which is the start, or anchor I forget which, of the limit selection),
				// but not beyond it.
				FindAndCheck(qselFound, 3, 6, false);

				// Now we should hit the limit again...
				CheckHr(m_qpat->FindFrom(qselFound, false, NULL));
				CheckHr(m_qpat->get_Found(&fFound));
				unitpp::assert_true("Hit limit backwards", !fFound);
				CheckHr(m_qpat->get_StoppedAtLimit(&fAtLimit));
				unitpp::assert_true("At limit backwards", fAtLimit);

				// Now remove the limit
				CheckHr(m_qpat->putref_Limit(NULL));

				// Very start of para 1
				FindAndCheck(qselFound, 0, 1, false);
				// And that's it.
				CheckHr(m_qpat->FindFrom(qselFound, false, NULL));
				CheckHr(m_qpat->get_Found(&fFound));
				unitpp::assert_true("Reached start of text backwards", !fFound);

				// Further tests of MatchWhole.
				stuPattern = L"abc";
				CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
				CheckHr(m_qpat->putref_Pattern(qtssPattern));
				CheckHr(m_qpat->put_MatchWholeWord(true));

				CheckHr(m_qpat->Find(qrootb, false, NULL));
				CheckHr(m_qpat->get_Found(&fFound));
				unitpp::assert_true("Found whole word", fFound);
				CheckHr(m_qpat->GetSelection(true, &qselFound));
				unitpp::assert_true("Got a selection ww", qselFound.Ptr() != NULL);
				CheckHr(qselFound->TextSelInfo(false, &qtssFound, &ichFound, &fAssocPrev,
					&hvoFound, &tagFound, &wsFound));
				// Should skip the abc's in the first paragraph because they aren't
				// whole words, and find the one in the second paragraph.
				unitpp::assert_eq("Right offset ww", 19, ichFound);
				CheckHr(qselFound->TextSelInfo(true, &qtssFound, &ichFound, &fAssocPrev,
					&hvoFound, &tagFound, &wsFound));
				unitpp::assert_eq("Right offset ww", 22, ichFound);
				CheckHr(m_qpat->MatchWhole(qselFound, &fMatch));
				unitpp::assert_true("MatchWhole succeeds with whole word specified", fMatch);

			}
			catch(...)
			{
				if (qvg32)
					qvg32->ReleaseDC();
				if (hdc != 0)
					ReleaseTestDC(hdc);
				qrootb->Close();
				throw;
			}

			// Cleanup
			qvg32->ReleaseDC();
			ReleaseTestDC(hdc);
			qrootb->Close();
		}

		void testTrivialTextSource()
		{
			IVwTextSourcePtr qts;
			qts.CreateInstance(CLSID_VwStringTextSource);
			IVwTxtSrcInitPtr qtsi;
			qts->QueryInterface(IID_IVwTxtSrcInit, (void**) &qtsi);
			ITsStringPtr qtss;
			StrUni stuSource(L"This is a text to search in for 'in'");
			m_qtsf->MakeString(stuSource.Bstr(), g_wsEng, &qtss);
			qtsi->SetString(qtss);

			StrUni stuPattern = L"in";
			ITsStringPtr qtssPattern;
			CheckHr(m_qtsf->MakeString(stuPattern.Bstr(), g_wsEng, &qtssPattern));
			CheckHr(m_qpat->putref_Pattern(qtssPattern));

			// OK, test data set up. Now try some tests...
			int ichMin, ichLim;
			CheckHr(m_qpat->FindIn(qts, 0, stuSource.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found first 'in'", (int)wcslen(L"This is a text to search "), ichMin);
			unitpp::assert_eq("Length of 'in'", ichMin + 2, ichLim);

			CheckHr(m_qpat->FindIn(qts, ichMin + 1, stuSource.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found second 'in'", (int)wcslen(L"This is a text to search in for '"), ichMin);
			unitpp::assert_eq("Length of 'in'", ichMin + 2, ichLim);

			// Make sure we can retrieve ws to match it.
			m_qpat->put_MatchOldWritingSystem(true);
			CheckHr(m_qpat->FindIn(qts, 0, stuSource.Length(), true, &ichMin, &ichLim, NULL));
			unitpp::assert_eq("Found first 'in' with ws match", (int)wcslen(L"This is a text to search "), ichMin);
			unitpp::assert_eq("Length of 'in' (search 3)", ichMin + 2, ichLim);
		}

		virtual void Setup()
		{
			CreateTestWritingSystemFactory();
			// Get the WS for English...allows us to set properties on it.
			g_qwsf->get_EngineOrNull(g_wsEng, &m_qwsEng);
			m_qtsf.CreateInstance(CLSID_TsStrFactory);
			// Use this rather than CreateInstance, because for the ORC test the pattern
			// and text source need to be in the same compilation unit.
			m_qpat.Attach(NewObj VwPattern());
			m_qts.Attach(NewObj VwSimpleTxtSrc());
			m_qzvps.Attach(NewObj VwPropertyStore());
			m_qts->SetWritingSystemFactory(g_qwsf);
			m_qzvps->putref_WritingSystemFactory(g_qwsf);
		}
		virtual void Teardown()
		{
			m_qts.Clear();
			m_qpat.Clear();
			m_qwsEng.Clear();
			m_qtsf.Clear();
			m_qzvps.Clear();
			CloseTestWritingSystemFactory();
		}
	};

	// Todo: test with collating rules. -- done but fails.
	// Test MatchWhole (code that deals with enabling Replace button).
}

#endif /*TestVwPattern_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkvw-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

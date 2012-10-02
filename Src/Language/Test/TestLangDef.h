/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLangDef.h
Responsibility:
Last reviewed:

	Unit tests for the LanguageDefinitionFactory and LanguageDefinition classes.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLANGDEF_H_INCLUDED
#define TESTLANGDEF_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	const char * g_rgpszXyzzyXML[] = {
		"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n",
		"<!DOCTYPE LanguageDefinition SYSTEM \"LanguageDefinition.dtd\">\n",
		"<LanguageDefinition>\n",
		"<LgWritingSystem id=\"xyzzy_Latn_GAK_BLORT\">\n",
		"<Abbr24>\n",
		"<AUni ws=\"en\">xyz</AUni>\n",
		"<AUni ws=\"fr\">Xyz</AUni>\n",
		"</Abbr24>\n",
		"<Name24>\n",
		"<AUni ws=\"en\">Xyzzyish</AUni>\n",
		"<AUni ws=\"fr\">Xyzzyais</AUni>\n",
		"</Name24>\n",
		"<Description24>\n",
		"<AStr ws=\"en\">\n",
		"<Run ws=\"en\">The standard alphabetic representation of Xyzzy.</Run>\n",
		"</AStr>\n",
		"<AStr ws=\"fr\">\n",
		"<Run ws=\"fr\">La representation alphabetic standard de Xyzzy.</Run>\n",
		"</AStr>\n",
		"</Description24>\n",
		"<Locale24><Integer val=\"1033\"/></Locale24>\n",
		"<RightToLeft24><Boolean val=\"false\"/></RightToLeft24>\n",
		"<DefaultSerif24><Uni>Times New Roman</Uni></DefaultSerif24>\n",
		"<DefaultSansSerif24><Uni>Arial</Uni></DefaultSansSerif24>\n",
		"<DefaultBodyFont24><Uni>Charis SIL</Uni></DefaultBodyFont24>\n",
		"<DefaultMonospace24><Uni>Courier New</Uni></DefaultMonospace24>\n",
		"<ICULocale24><Uni>xyzzy_Latn_GAK_BLORT</Uni></ICULocale24>\n",
		"<KeyboardType24><Uni>standard</Uni></KeyboardType24>\n",
		"<Collations24>\n",
		"<LgCollation>\n",
		"<Name30>\n",
		"<AUni ws=\"en\">Default Collation</AUni>\n",
		"<AUni ws=\"fr\">Collation Default</AUni>\n",
		"</Name30>\n",
		"<WinLCID30><Integer val=\"1033\"/></WinLCID30>\n",
		"<WinCollation30><Uni>Latin1_General_CI_AI</Uni></WinCollation30>\n",
		"</LgCollation>\n",
		"<LgCollation>\n",
		"<Name30>\n",
		"<AUni ws=\"en\">Case Sensitive</AUni>\n",
		"<AUni ws=\"fr\">Sensitive Case</AUni>\n",
		"</Name30>\n",
		"<WinLCID30><Integer val=\"1033\"/></WinLCID30>\n",
		"<WinCollation30><Uni>Latin1_General_CS_AI</Uni></WinCollation30>\n",
		"</LgCollation>\n",
		"</Collations24>\n",
		"</LgWritingSystem>\n",
		//"<BaseLocale>xyzzy_GAK</BaseLocale>\n",
		"<NewLocale>NEW LOCALE</NewLocale>\n",
		"<EthnoCode>xxyzzy</EthnoCode>\n",
		"<LocaleName>Xyzzyish</LocaleName>\n",
		"<LocaleScript>Latin</LocaleScript>\n",
		"<LocaleCountry>Republic of GAK</LocaleCountry>\n",
		"<LocaleVariant>Who can say what BLORT is?</LocaleVariant>\n",
		"<LocaleWinLCID>1234</LocaleWinLCID>\n",
		"<CollationElements>SOME ELEMENTS;I DON'T KNOW WHAT</CollationElements>\n",
		//"<LocaleResources>SOME RESOURCES</LocaleResources>\n",
		"<PuaDefinitions>\n",
		"<CharDef code=\"123456\" data=\"THIS IS A TEST\"/>\n",
		"<CharDef code=\"123457\" data=\"THIS IS ALSO A TEST\"/>\n",
		"</PuaDefinitions>\n",
		"<Fonts>\n",
		"<Font file=\"filename.ttf\"/>\n",
		"<Font file=\"filename2.ttf\"/>\n",
		"</Fonts>\n",
		"<Keyboard file=\"Some random keyboard\"/>\n",
		"<EncodingConverter install=\"THIS IS BOGUS, I KNOW IT IS!\"/>\n",
		"</LanguageDefinition>\n",
		NULL
	};

	const char * g_rgpszZzzxXML[] = {
		"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n",
		"<!DOCTYPE LanguageDefinition SYSTEM \"LanguageDefinition.dtd\">\n",
		"<LanguageDefinition>\n",
		"<LgWritingSystem id=\"zzzx\">\n",
		"<Abbr24>\n",
		"<AUni ws=\"en\">zz</AUni>\n",
		"<AUni ws=\"fr\">zzz</AUni>\n",
		"<AUni ws=\"xyzzy_Latn_GAK_BLORT\">ZZZ</AUni>\n",
		"</Abbr24>\n",
		"<Name24>\n",
		"<AUni ws=\"en\">Zzzxish</AUni>\n",
		"<AUni ws=\"fr\">Zzzxais</AUni>\n",
		"<AUni ws=\"xyzzy_Latn_GAK_BLORT\">ZZZxxx</AUni>\n",
		"</Name24>\n",
		"<Description24>\n",
		"<AStr ws=\"en\">\n",
		"<Run ws=\"en\">The standard alphabetic representation of Zzzxish.</Run>\n",
		"<Run ws=\"xyzzy_Latn_GAK_BLORT\">  (Not to mention ZZZxx.)</Run>\n",
		"</AStr>\n",
		"<AStr ws=\"fr\">\n",
		"<Run ws=\"fr\">La representation alphabetic standard de Zzzxais.</Run>\n",
		"<Run ws=\"xyzzy_Latn_GAK_BLORT\">  (Not to mention ZZZxx.)</Run>\n",
		"</AStr>\n",
		"<AStr ws=\"xyzzy_Latn_GAK_BLORT\">\n",
		"<Run ws=\"xyzzy_Latn_GAK_BLORT\">The standard alphabetic representation of ZZZxx.</Run>\n",
		"</AStr>\n",
		"</Description24>\n",
		"<Locale24><Integer val=\"1033\"/></Locale24>\n",
		"<RightToLeft24><Boolean val=\"false\"/></RightToLeft24>\n",
		"<DefaultSerif24><Uni>Times New Roman</Uni></DefaultSerif24>\n",
		"<DefaultSansSerif24><Uni>Arial</Uni></DefaultSansSerif24>\n",
		"<DefaultBodyFont24><Uni>Charis SIL</Uni></DefaultBodyFont24>\n",
		"<DefaultMonospace24><Uni>Courier New</Uni></DefaultMonospace24>\n",
		"<ICULocale24><Uni>zzzx</Uni></ICULocale24>\n",
		"<KeyboardType24><Uni>standard</Uni></KeyboardType24>\n",
		"<Collations24>\n",
		"<LgCollation>\n",
		"<Name30>\n",
		"<AUni ws=\"en\">Default Collation</AUni>\n",
		"<AUni ws=\"fr\">Collation Default</AUni>\n",
		"</Name30>\n",
		"<WinLCID30><Integer val=\"1033\"/></WinLCID30>\n",
		"<WinCollation30><Uni>Latin1_General_CI_AI</Uni></WinCollation30>\n",
		"</LgCollation>\n",
		"<LgCollation>\n",
		"<Name30>\n",
		"<AUni ws=\"en\">Case Sensitive</AUni>\n",
		"<AUni ws=\"fr\">Sensitive Case</AUni>\n",
		"</Name30>\n",
		"<WinLCID30><Integer val=\"1033\"/></WinLCID30>\n",
		"<WinCollation30><Uni>Latin1_General_CS_AI</Uni></WinCollation30>\n",
		"</LgCollation>\n",
		"</Collations24>\n",
		"</LgWritingSystem>\n",
		//"<BaseLocale>zzzx</BaseLocale>\n",
		"<EthnoCode>zzzx</EthnoCode>\n",
		"<LocaleName>Zzzxish</LocaleName>\n",
		"<LocaleWinLCID>1234</LocaleWinLCID>\n",
		"<CollationElements>SOME ELEMENTS;I DON'T KNOW WHAT</CollationElements>\n",
		//"<LocaleResources>SOME RESOURCES</LocaleResources>\n",
		"<PuaDefinitions>\n",
		"<CharDef code=\"123456\" data=\"THIS IS A TEST\"/>\n",
		"<CharDef code=\"123457\" data=\"THIS IS ALSO A TEST\"/>\n",
		"</PuaDefinitions>\n",
		"<Fonts>\n",
		"<Font file=\"filename.ttf\"/>\n",
		"<Font file=\"filename2.ttf\"/>\n",
		"</Fonts>\n",
		"<Keyboard file=\"Some random keyboard\"/>\n",
		"<EncodingConverter install=\"THIS IS BOGUS, I KNOW IT IS!\" file=\"ThisIsA.Test\"/>\n",
		"</LanguageDefinition>\n",
		NULL
	};

	const char * g_rgpszZzzxXML2[] = {
		"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n",
		"<!DOCTYPE LanguageDefinition SYSTEM \"LanguageDefinition.dtd\">\n",
		"<LanguageDefinition>\n",
		"<LgWritingSystem id=\"zzzx\">\n",
		"<Abbr24>\n",
		"<AUni ws=\"en\">zz</AUni>\n",
		"<AUni ws=\"fr\">zzz</AUni>\n",
		"\n",
		"</Abbr24>\n",
		"<Name24>\n",
		"<AUni ws=\"en\">Zzzxish</AUni>\n",
		"<AUni ws=\"fr\">Zzzxais</AUni>\n",
		"\n",
		"</Name24>\n",
		"<Description24>\n",
		"<AStr ws=\"en\">\n",
		"<Run ws=\"en\">The standard alphabetic representation of Zzzxish.</Run>\n",
		"\n",
		"</AStr>\n",
		"<AStr ws=\"fr\">\n",
		"<Run ws=\"fr\">La representation alphabetic standard de Zzzxais.</Run>\n",
		"\n",
		"</AStr>\n",
		"\n",
		"</Description24>\n",
		"<Locale24><Integer val=\"1033\"></Integer></Locale24>\n",
		"<RightToLeft24><Boolean val=\"false\"></Boolean></RightToLeft24>\n",
		"<DefaultSerif24><Uni>Times New Roman</Uni></DefaultSerif24>\n",
		"<DefaultSansSerif24><Uni>Arial</Uni></DefaultSansSerif24>\n",
		"<DefaultBodyFont24><Uni>Charis SIL</Uni></DefaultBodyFont24>\n",
		"<DefaultMonospace24><Uni>Courier New</Uni></DefaultMonospace24>\n",
		"<ICULocale24><Uni>zzzx</Uni></ICULocale24>\n",
		"<KeyboardType24><Uni>standard</Uni></KeyboardType24>\n",
		"<Collations24>\n",
		"<LgCollation>\n",
		"<Name30>\n",
		"<AUni ws=\"en\">Default Collation</AUni>\n",
		"<AUni ws=\"fr\">Collation Default</AUni>\n",
		"</Name30>\n",
		"<WinLCID30><Integer val=\"1033\"></Integer></WinLCID30>\n",
		"<WinCollation30><Uni>Latin1_General_CI_AI</Uni></WinCollation30>\n",
		"</LgCollation>\n",
		"<LgCollation>\n",
		"<Name30>\n",
		"<AUni ws=\"en\">Case Sensitive</AUni>\n",
		"<AUni ws=\"fr\">Sensitive Case</AUni>\n",
		"</Name30>\n",
		"<WinLCID30><Integer val=\"1033\"></Integer></WinLCID30>\n",
		"<WinCollation30><Uni>Latin1_General_CS_AI</Uni></WinCollation30>\n",
		"</LgCollation>\n",
		"</Collations24>\n",
		"</LgWritingSystem>\n",
		//"<BaseLocale>zzzx</BaseLocale>\n",
		"<EthnoCode>zzzx</EthnoCode>\n",
		"<LocaleName>Zzzxish</LocaleName>\n",
		"<LocaleWinLCID>1234</LocaleWinLCID>\n",
		"<CollationElements>SOME ELEMENTS;I DON'T KNOW WHAT</CollationElements>\n",
		//"<LocaleResources>SOME RESOURCES</LocaleResources>\n",
		"<PuaDefinitions>\n",
		"<CharDef code=\"123456\" data=\"THIS IS A TEST\"></CharDef>\n",
		"<CharDef code=\"123457\" data=\"THIS IS ALSO A TEST\"></CharDef>\n",
		"</PuaDefinitions>\n",
		"<Fonts>\n",
		"<Font file=\"filename.ttf\"></Font>\n",
		"<Font file=\"filename2.ttf\"></Font>\n",
		"</Fonts>\n",
		"<Keyboard file=\"Some random keyboard\"></Keyboard>\n",
		"<EncodingConverter install=\"THIS IS BOGUS, I KNOW IT IS!\" file=\"ThisIsA.Test\"></EncodingConverter>\n",
		"</LanguageDefinition>\n",
		NULL
	};



	/*******************************************************************************************
		Tests for LanguageDefinitionFactory and LanguageDefinition.
	 ******************************************************************************************/
	class TestLanguageDefinition : public unitpp::suite
	{
		ILgWritingSystemFactoryPtr m_qwsf;
		ITsStrFactoryPtr m_qtsf;

		// Test reading the XML file with only one language (English) defined.
		void testFactoryInitializeFromXml1()
		{
			LanguageDefinitionFactoryPtr qldf;
			qldf.Attach(NewObj LanguageDefinitionFactory);
			LanguageDefinitionPtr qld;
			// Put test data in place.
			StrUni stuIcuLocale(L"xyzzy_Latn_GAK_BLORT");
			HRESULT hr;
			WriteXyzzy();
			hr = qldf->InitializeFromXml(m_qwsf, stuIcuLocale.Bstr(), &qld);
			RemoveXyzzy();
			unitpp::assert_eq("InitializeFromXml({en}, \"xyzzy_Latn_GAK_BLORT\") HRESULT",
				S_OK, hr);
			CheckXyzzyLanguageDefinition(qld,
				"InitializeFromXml({en}, \"xyzzy_Latn_GAK_BLORT\")", 1);
		}

		// Test reading the XML file with two languages (English and French) defined.
		void testFactoryInitializeFromXml2()
		{
			// Add French ("fr") to the set of languages that the lgWs factory knows about.
			CreateTestWritingSystem(m_qwsf, kwsFrn, kszFrn);
			LanguageDefinitionFactoryPtr qldf;
			qldf.Attach(NewObj LanguageDefinitionFactory);
			LanguageDefinitionPtr qld;
			// Put test data in place.
			StrUni stuIcuLocale(L"xyzzy_Latn_GAK_BLORT");
			HRESULT hr;
			WriteXyzzy();
			hr = qldf->InitializeFromXml(m_qwsf, stuIcuLocale.Bstr(), &qld);
			RemoveXyzzy();
			unitpp::assert_eq("InitializeFromXml({en,fr}, \"xyzzy_Latn_GAK_BLORT\") HRESULT",
				S_OK, hr);
			CheckXyzzyLanguageDefinition(qld,
				"InitializeFromXml({en,fr}, \"xyzzy_Latn_GAK_BLORT\")", 2);
		}

		// Test reading an invalid XML file.
		void testFactoryInitializeFromXmlBAD()
		{
			LanguageDefinitionFactoryPtr qldf;
			qldf.Attach(NewObj LanguageDefinitionFactory);
			LanguageDefinitionPtr qld;
			// Put test data in place.
			StrUni stuIcuLocale(L"xyzzy_Latn_GAK_BLORT");
			HRESULT hr;
			WriteBadXyzzy();
			hr = qldf->InitializeFromXml(m_qwsf, stuIcuLocale.Bstr(), &qld);
			RemoveXyzzy();
			unitpp::assert_true("BAD InitializeFromXml({en}, \"xyzzy_Latn_GAK_BLORT\") HRESULT",
				hr != S_OK);
		}

		// Write the contents of g_rgpszXyzzyXML[] to {FwRoot}/Languages/xyzzy_Latn_GAK_BLORT.xml.
		void WriteXyzzy()
		{
			StrUni stuDir(DirectoryFinder::FwRootDataDir());
			StrAnsi staFile;
			staFile.Format("%S\\Languages\\xyzzy_Latn_GAK_BLORT.xml", stuDir.Chars());
			FILE * fp;
			fopen_s(&fp, staFile.Chars(), "w");
			StrAnsi staMsg;
			staMsg.Format("Creating %s", staFile.Chars());
			unitpp::assert_true(staMsg.Chars(), fp != NULL);
			for (int i = 0; g_rgpszXyzzyXML[i]; ++i)
				fputs(g_rgpszXyzzyXML[i], fp);
			fclose(fp);
		}

		// Remove {FwRoot}/Languages/xyzzy_Latn_GAK_BLORT.xml from the filesystem.
		void RemoveXyzzy()
		{
			StrUni stuDir(DirectoryFinder::FwRootDataDir());
			StrAnsi staFile;
			staFile.Format("%S\\Languages\\xyzzy_Latn_GAK_BLORT.xml", stuDir.Chars());
			::DeleteFileA(staFile.Chars());
		}

		// Write the contents of g_rgpszXyzzyXML[] to {FwRoot}/Languages/xyzzy_Latn_GAK_BLORT.xml,
		// but skip every other line.
		void WriteBadXyzzy()
		{
			StrUni stuDir(DirectoryFinder::FwRootDataDir());
			StrAnsi staFile;
			staFile.Format("%S\\Languages\\xyzzy_Latn_GAK_BLORT.xml", stuDir.Chars());
			FILE * fp;
			fopen_s(&fp, staFile.Chars(), "w");
			StrAnsi staMsg;
			staMsg.Format("Creating %s", staFile.Chars());
			unitpp::assert_true(staMsg.Chars(), fp != NULL);
			for (int i = 0; g_rgpszXyzzyXML[i]; ++i)
			{
				if ((i % 2) == 0)
					fputs(g_rgpszXyzzyXML[i], fp);
			}
			fclose(fp);
		}

		// Compare the IWritingSystem stored in the LanguageDefinition object to what we
		// expect.
		void CheckLgWritingSystemContents(LanguageDefinition * pld, const char * pszMsgHeader,
			int cwsFact)
		{
			IWritingSystemPtr qws;
			HRESULT hr;
			hr = pld->get_WritingSystem(&qws);
			StrAnsi staMsg;
			staMsg.Format("%s get_WritingSystem hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_WritingSystem qws", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), qws.Ptr() != NULL);

			int cws;
			int iws;
			Vector<int> vws;
			SmartBstr sbstr;
			ComBool fT;
			int ccoll;
			int icoll;

			hr = qws->get_NameWsCount(&cws);
			staMsg.Format("%s qws->get_NameWsCount hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_NameWsCount count", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), cwsFact, cws);
			vws.Resize(cws);
			hr = qws->get_NameWss(cws, vws.Begin());
			staMsg.Format("%s qws->get_NameWss hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			for (iws = 0; iws < cws; ++iws)
			{
				hr = qws->get_Name(vws[iws], &sbstr);
				staMsg.Format("%s qws->get_Name [%d] hr", pszMsgHeader, iws);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				staMsg.Format("%s qws->get_Name [%d] value", pszMsgHeader, iws);
				unitpp::assert_true(staMsg.Chars(),
					wcscmp(sbstr.Chars(), vws[iws] == kwsEng ? L"Xyzzyish" : L"Xyzzyais") == 0);
				if (vws[iws] != kwsEng)
				{
					staMsg.Format("%s 2 factory languages", pszMsgHeader);
					unitpp::assert_eq(staMsg.Chars(), 2, cwsFact);
					staMsg.Format("%s qws->get_NameWss => en/fr", pszMsgHeader);
					unitpp::assert_eq(staMsg.Chars(), kwsFrn, vws[iws]);
				}
			}

			int nLocale;
			hr = qws->get_Locale(&nLocale);
			staMsg.Format("%s qws->get_Locale hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_Locale value", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), 1033, nLocale);

			hr = qws->get_RightToLeft(&fT);
			staMsg.Format("%s qws->get_RightToLeft hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_RightToLeft value", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), !fT);

			hr = qws->get_FontVariation(&sbstr);
			staMsg.Format("%s qws->get_FontVariation hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_FontVariation value", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());

			hr = qws->get_SansFontVariation(&sbstr);
			staMsg.Format("%s qws->get_SansFontVariation hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_SansFontVariation value", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());

			hr = qws->get_DefaultSerif(&sbstr);
			staMsg.Format("%s qws->get_DefaultSerif hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_DefaultSerif value", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstr.Chars(), L"Times New Roman") == 0);

			hr = qws->get_DefaultSansSerif(&sbstr);
			staMsg.Format("%s qws->get_DefaultSansSerif hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_DefaultSansSerif value", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstr.Chars(), L"Arial") == 0);

			hr = qws->get_DefaultBodyFont(&sbstr);
			staMsg.Format("%s qws->get_DefaultBodyFont hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_DefaultBodyFont value", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstr.Chars(), L"Charis SIL") == 0);

			hr = qws->get_DefaultMonospace(&sbstr);
			staMsg.Format("%s qws->get_DefaultMonospace hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_DefaultMonospace value", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstr.Chars(), L"Courier New") == 0);

			hr = qws->get_KeyMan(&fT);
			staMsg.Format("%s qws->get_KeyMan hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_KeyMan value", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), !fT);

			hr = qws->get_CollationCount(&ccoll);
			staMsg.Format("%s qws->get_CollationCount hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_CollationCount count", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), 2, ccoll);
			for (icoll = 0; icoll < ccoll; ++icoll)
			{
				ICollationPtr qcoll;
				hr = qws->get_Collation(icoll, &qcoll);
				staMsg.Format("%s qws->get_Collation [%d] hr", pszMsgHeader, icoll);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				staMsg.Format("%s qws->get_Collation [%d]", pszMsgHeader, icoll);
				CheckLgCollation(icoll, qcoll, staMsg.Chars(), cwsFact);
			}

			hr = qws->get_AbbrWsCount(&cws);
			staMsg.Format("%s qws->get_AbbrWsCount hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_AbbrWsCount count", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), cwsFact, cws);
			vws.Resize(cws);
			hr = qws->get_AbbrWss(cws, vws.Begin());
			staMsg.Format("%s qws->get_AbbrWss hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			for (iws = 0; iws < cws; ++iws)
			{
				hr = qws->get_Abbr(vws[iws], &sbstr);
				staMsg.Format("%s qws->get_Abbr[%d] hr", pszMsgHeader, iws);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				staMsg.Format("%s qws->get_Abbr [%d] value", pszMsgHeader, iws);
				unitpp::assert_true(staMsg.Chars(),
					wcscmp(sbstr.Chars(), vws[iws] == kwsEng ? L"xyz" : L"Xyz") == 0);
				if (vws[iws] != kwsEng)
				{
					staMsg.Format("%s 2 factory languages", pszMsgHeader);
					unitpp::assert_eq(staMsg.Chars(), 2, cwsFact);
					staMsg.Format("%s qws->get_AbbrWss => en/fr", pszMsgHeader);
					unitpp::assert_eq(staMsg.Chars(), kwsFrn, vws[iws]);
				}
			}

			hr = qws->get_DescriptionWsCount(&cws);
			staMsg.Format("%s qws->get_DescriptionWsCount hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_DescriptionWsCount count", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), cwsFact, cws);
			vws.Resize(cws);
			hr = qws->get_DescriptionWss(cws, vws.Begin());
			staMsg.Format("%s qws->get_DescriptionWss hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			for (iws = 0; iws < cws; ++iws)
			{
				ITsStringPtr qtss;
				hr = qws->get_Description(vws[iws], &qtss);
				staMsg.Format("%s qws->get_Description [%d] hr", pszMsgHeader, iws);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				StrUni stu;
				if (vws[iws] == kwsEng)
					stu.Assign(L"The standard alphabetic representation of Xyzzy.");
				else
					stu.Assign(L"La representation alphabetic standard de Xyzzy.");
				ITsStringPtr qtssExpect;
				hr = m_qtsf->MakeString(stu.Bstr(), vws[iws], &qtssExpect);
				staMsg.Format("%s m_qtsf->MakeString [%d] hr", pszMsgHeader, iws);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				hr = qtssExpect->Equals(qtss, &fT);
				staMsg.Format("%s m_qtssExpect->Equals [%d] hr", pszMsgHeader, iws);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				staMsg.Format("%s qws->get_Description [%d] value", pszMsgHeader, iws);
				unitpp::assert_eq(staMsg.Chars(), ComBool(TRUE), fT);
				if (vws[iws] != kwsEng)
				{
					staMsg.Format("%s 2 factory languages", pszMsgHeader);
					unitpp::assert_eq(staMsg.Chars(), 2, cwsFact);
					staMsg.Format("%s qws->get_DescriptionWss => en/fr", pszMsgHeader);
					unitpp::assert_eq(staMsg.Chars(), kwsFrn, vws[iws]);
				}
			}

			hr = qws->get_IcuLocale(&sbstr);
			staMsg.Format("%s qws->get_IcuLocale hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_IcuLocale value", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(),
				wcscmp(sbstr.Chars(), L"xyzzy_Latn_GAK_BLORT") == 0);

			SmartBstr sbstrLanguage;
			SmartBstr sbstrScript;
			SmartBstr sbstrCountry;
			SmartBstr sbstrVariant;
			hr = qws->GetIcuLocaleParts(&sbstrLanguage,	&sbstrScript, &sbstrCountry, &sbstrVariant);
			staMsg.Format("%s qws->GetIcuLocaleParts hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->GetIcuLocaleParts Language", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstrLanguage.Chars(), L"xyzzy") == 0);
			staMsg.Format("%s qws->GetIcuLocaleParts Script", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstrScript.Chars(), L"Latn") == 0);
			staMsg.Format("%s qws->GetIcuLocaleParts Country", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstrCountry.Chars(), L"GAK") == 0);
			staMsg.Format("%s qws->GetIcuLocaleParts Variant", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstrVariant.Chars(), L"BLORT") == 0);

			hr = qws->get_LegacyMapping(&sbstr);
			staMsg.Format("%s qws->get_LegacyMapping hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_LegacyMapping value", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());

			hr = qws->get_KeymanKbdName(&sbstr);
			staMsg.Format("%s qws->get_KeymanKbdName hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s qws->get_KeymanKbdName value", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());
		}

		// Compare the IWritingSystem info stored in the LanguageDefinition object, and
		// accessed via ICU, to what we expect.
		void CheckMoreLgWritingSystemContents(IWritingSystem * pws, const char * pszMsgHeader)
		{
			HRESULT hr;
			StrAnsi staMsg;
			SmartBstr sbstr;

			// The following require installing the test language into ICU first.
			hr = pws->get_LanguageName(&sbstr);
			staMsg.Format("%s pws->get_LanguageName hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			hr = pws->get_ScriptName(&sbstr);
			staMsg.Format("%s pws->get_ScriptName hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			hr = pws->get_CountryName(&sbstr);
			staMsg.Format("%s pws->get_CountryName hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			hr = pws->get_VariantName(&sbstr);
			staMsg.Format("%s pws->get_VariantName hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			hr = pws->get_LanguageAbbr(&sbstr);
			staMsg.Format("%s pws->get_LanguageAbbr hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			hr = pws->get_ScriptAbbr(&sbstr);
			staMsg.Format("%s pws->get_ScriptAbbr hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			hr = pws->get_CountryAbbr(&sbstr);
			staMsg.Format("%s pws->get_CountryAbbr hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			hr = pws->get_VariantAbbr(&sbstr);
			staMsg.Format("%s pws->get_VariantAbbr hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
		}

		// Check that the LanguageDefinition object loaded from xyzzy_Latn_GAK_BLORT.xml has the
		// expected attribute values.
		void CheckXyzzyLanguageDefinition(LanguageDefinition * pld, const char * pszMsgHeader,
			int cwsFact)
		{
			StrAnsi staMsg;
			staMsg.Format("%s qld", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), pld != NULL);

			SmartBstr sbstr;
			SmartBstr sbstrFile;
			int cPUA;
			int nCode;
			int cColl;
			int cFont;
			int ie;
			HRESULT hr;

			CheckLgWritingSystemContents(pld, pszMsgHeader, cwsFact);

			hr = pld->get_BaseLocale(&sbstr);
			staMsg.Format("%s get_BaseLocale hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s BaseLocale", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());

			hr = pld->get_EthnoCode(&sbstr);
			staMsg.Format("%s get_EthnoCode hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s EthnoCode", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstr.Chars(), L"xxyzzy") == 0);

			hr = pld->get_LocaleName(&sbstr);
			staMsg.Format("%s get_LocaleName hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s LocaleName", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstr.Chars(), L"Xyzzyish") == 0);

			hr = pld->get_LocaleScript(&sbstr);
			staMsg.Format("%s get_LocaleScript hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s LocaleScript", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(),
				wcscmp(sbstr.Chars(), L"Latin") == 0);

			hr = pld->get_LocaleCountry(&sbstr);
			staMsg.Format("%s get_LocaleCountry hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s LocaleCountry", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(),
				wcscmp(sbstr.Chars(), L"Republic of GAK") == 0);

			hr = pld->get_LocaleVariant(&sbstr);
			staMsg.Format("%s get_LocaleVariant hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s LocaleVariant", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(),
				wcscmp(sbstr.Chars(), L"Who can say what BLORT is?") == 0);

			hr = pld->get_CollationElements(&sbstr);
			staMsg.Format("%s get_CollationElements hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s CollationElements", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(),
				wcscmp(sbstr.Chars(), L"SOME ELEMENTS;I DON'T KNOW WHAT") == 0);

			hr = pld->get_LocaleResources(&sbstr);
			staMsg.Format("%s get_LocaleResources hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s LocaleResources", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());

			hr = pld->get_PuaDefinitionCount(&cPUA);
			staMsg.Format("%s get_PuaDefinitionCount hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_PuaDefinitionCount count", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), 2, cPUA);

			hr = pld->GetPuaDefinition(0, &nCode, &sbstr);
			staMsg.Format("%s GetPuaDefinition[0] hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s GetPuaDefinition[0] code", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), 0x123456, nCode);
			staMsg.Format("%s GetPuaDefinition[0] data", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstr.Chars(), L"THIS IS A TEST") == 0);

			hr = pld->GetPuaDefinition(1, &nCode, &sbstr);
			staMsg.Format("%s GetPuaDefinition[1] hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s GetPuaDefinition[1] code", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), 0x123457, nCode);
			staMsg.Format("%s GetPuaDefinition[1] data", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(),
				wcscmp(sbstr.Chars(), L"THIS IS ALSO A TEST") == 0);

			try{
				CheckHr(hr = pld->GetPuaDefinition(2, &nCode, &sbstr));
				staMsg.Format("%s GetPuaDefinition[2] hr", pszMsgHeader);
				unitpp::assert_eq(staMsg.Chars(), E_INVALIDARG, hr);
			}
			catch(Throwable& thr){
				staMsg.Format("%s GetPuaDefinition[2] hr", pszMsgHeader);
				unitpp::assert_eq(staMsg.Chars(), E_INVALIDARG, thr.Result());
			}
			staMsg.Format("%s GetPuaDefinition[2] code", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), 0, nCode);
			staMsg.Format("%s GetPuaDefinition[2] data", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());

			hr = pld->get_FontCount(&cFont);
			staMsg.Format("%s get_FontCount hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_FontCount count", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), 2, cFont);

			hr = pld->GetFont(0, &sbstr);
			staMsg.Format("%s GetFont[0] hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s GetFont[0] file", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstr.Chars(), L"filename.ttf") == 0);

			hr = pld->GetFont(1, &sbstr);
			staMsg.Format("%s GetFont[1] hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s GetFont[1] file", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), wcscmp(sbstr.Chars(), L"filename2.ttf") == 0);


			try{
				CheckHr(hr = pld->GetFont(2, &sbstr));
				staMsg.Format("%s GetFont[2] hr", pszMsgHeader);
				unitpp::assert_eq(staMsg.Chars(), E_INVALIDARG, hr);
			}
			catch(Throwable& thr)
			{
				staMsg.Format("%s GetFont[2] hr", pszMsgHeader);
				unitpp::assert_eq(staMsg.Chars(), E_INVALIDARG, thr.Result());
			}
			staMsg.Format("%s GetFont[2] file", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());

			hr = pld->get_Keyboard(&sbstr);
			staMsg.Format("%s get_Keyboard hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s Keyboard", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(),
				wcscmp(sbstr.Chars(), L"Some random keyboard") == 0);

			try{
				CheckHr(hr = pld->GetEncodingConverter(&sbstr, &sbstrFile));
				staMsg.Format("%s GetEncodingConverter hr", pszMsgHeader);
				unitpp::assert_eq(staMsg.Chars(), E_NOTIMPL, hr);	// !?
			}
			catch(Throwable& thr)
			{
				staMsg.Format("%s GetEncodingConverter hr", pszMsgHeader);
				unitpp::assert_eq(staMsg.Chars(), E_NOTIMPL, thr.Result());	// !?
			}

			hr = pld->get_CollationCount(&cColl);
			staMsg.Format("%s get_CollationCount hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_CollationCount count", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), 2, cColl);
			for (ie = 0; ie < cColl; ++ie)
			{
				ICollationPtr qcoll;
				hr = pld->GetCollation(ie, &qcoll);
				staMsg.Format("%s GetCollation [%d] hr", pszMsgHeader, ie);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				staMsg.Format("%s pld->GetCollation [%d]", pszMsgHeader, ie);
				CheckLgCollation(ie, qcoll, staMsg.Chars(), cwsFact);
			}
		}

		// Check the ICollation object for having the expected attributes.
		void CheckLgCollation(int icoll, ICollation * pcoll, const char * pszMsgHeader,
			int cwsFact)
		{
			StrAnsi staMsg;
			staMsg.Format("%s qcoll", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), pcoll != NULL);

			HRESULT hr;
			int cws;
			int iws;
			Vector<int> vws;
			SmartBstr sbstr;
			StrUni stuName;

			hr = pcoll->get_NameWsCount(&cws);
			staMsg.Format("%s get_NameWsCount hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_NameWsCount count", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), cwsFact, cws);
			vws.Resize(cws);
			hr = pcoll->get_NameWss(cws, vws.Begin());
			staMsg.Format("%s get_NameWss hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			for (iws = 0; iws < cws; ++iws)
			{
				hr = pcoll->get_Name(vws[iws], &sbstr);
				staMsg.Format("%s get_Name [%d] hr", pszMsgHeader, iws);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				staMsg.Format("%s get_Name [%d] value", pszMsgHeader, iws);
				if (icoll == 0)
					stuName = vws[iws] == kwsEng ? L"Default Collation" : L"Collation Default";
				else
					stuName = vws[iws] == kwsEng ? L"Case Sensitive" : L"Sensitive Case";
				unitpp::assert_eq(staMsg.Chars(), stuName, sbstr.Chars());
				if (vws[iws] != kwsEng)
				{
					staMsg.Format("%s 2 factory languages", pszMsgHeader);
					unitpp::assert_eq(staMsg.Chars(), 2, cwsFact);
					staMsg.Format("%s get_NameWss => en/fr", pszMsgHeader);
					unitpp::assert_eq(staMsg.Chars(), kwsFrn, vws[iws]);
				}
			}

			int nCode;
			hr = pcoll->get_WinLCID(&nCode);
			staMsg.Format("%s get_WinLCID hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_WinLCID value", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), 1033, nCode);

			hr = pcoll->get_WinCollation(&sbstr);
			staMsg.Format("%s get_WinCollation hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_WinCollation value", pszMsgHeader);
			if (icoll == 0)
				stuName = L"Latin1_General_CI_AI";
			else
				stuName = L"Latin1_General_CS_AI";
			unitpp::assert_eq(staMsg.Chars(), stuName, sbstr.Chars());

			hr = pcoll->get_IcuResourceName(&sbstr);
			staMsg.Format("%s get_IcuResourceName hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_IcuResourceName value", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());

			hr = pcoll->get_IcuResourceText(&sbstr);
			staMsg.Format("%s get_IcuResourceText hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_IcuResourceText value", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());

			hr = pcoll->get_IcuRules(&sbstr);
			staMsg.Format("%s get_IcuRules hr", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s get_IcuRules value", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (BSTR)NULL, sbstr.Bstr());
		}

		// Test reading the XML file with two languages (English and French) defined, then
		// installing the third language (xyzzy_Latn_GAK_BLORT), and finally reading yet a fourth
		// language from another XML file.
		void testFactoryInitializeFromXml3()
		{
			// Add French ("fr") to the set of languages that the lgWs factory knows about.
			CreateTestWritingSystem(m_qwsf, kwsFrn, kszFrn);
			LanguageDefinitionFactoryPtr qldf;
			qldf.Attach(NewObj LanguageDefinitionFactory);
			LanguageDefinitionPtr qld;
			// Put test data in place.
			StrUni stuIcuLocale(L"xyzzy_Latn_GAK_BLORT");
			HRESULT hr;
			WriteXyzzy();
			// Parse it.
			hr = qldf->InitializeFromXml(m_qwsf, stuIcuLocale.Bstr(), &qld);
			unitpp::assert_eq("InitializeFromXml({en,fr}, \"xyzzy_Latn_GAK_BLORT\") HRESULT [3]",
				S_OK, hr);
			// Install it.
			IWritingSystemPtr qws;
			hr = qld->get_WritingSystem(&qws);
			unitpp::assert_eq("qld->get_WritingSystem([xyzzy_Latn_GAK_BLORT]) HRESULT", S_OK, hr);
			hr = m_qwsf->AddEngine(qws);
			unitpp::assert_eq("m_qwsf->AddEngine([xyzzy_Latn_GAK_BLORT]) HRESULT", S_OK, hr);
			CheckMoreLgWritingSystemContents(qws, "after AddEngine([xyzzy_Latn_GAK_BLORT])");

			LanguageDefinitionFactoryPtr qldf2;
			qldf2.Attach(NewObj LanguageDefinitionFactory);
			LanguageDefinitionPtr qld2;
			// Put more test data in place.
			StrUni stuIcuLocale2(L"zzzx");
			WriteZzzx();
			// Parse it.
			hr = qldf2->InitializeFromXml(m_qwsf, stuIcuLocale2.Bstr(), &qld2);
			unitpp::assert_eq("InitializeFromXml({en,fr,xyzzy_Latn_GAK_BLORT}, [zzzx]) HRESULT",
				S_OK, hr);
			// Check it, and then install it.
			CheckZzzxLanguageDefinition(qld2,
				"InitializeFromXml({en,fr,xyzzy_Latn_GAK_BLORT}, [zzzx])", true);
			IWritingSystemPtr qws2;
			hr = qld2->get_WritingSystem(&qws2);
			unitpp::assert_eq("qld2->get_WritingSystem([zzzx]) HRESULT", S_OK, hr);
			hr = m_qwsf->AddEngine(qws2);
			unitpp::assert_eq("m_qwsf->AddEngine([zzzx]) HRESULT", S_OK, hr);

			// Remove xyzzy_Latn_GAK_BLORT.
			int wsXyzzy;
			hr = qws->get_WritingSystem(&wsXyzzy);
			unitpp::assert_eq("qws->get_WritingSystem([xyzzy_Latn_GAK_BLORT]) HRESULT", S_OK, hr);
			hr = m_qwsf->RemoveEngine(wsXyzzy);
			unitpp::assert_eq("m_qwsf->RemoveEngine([xyzzy_Latn_GAK_BLORT]) HRESULT", S_OK, hr);

			// Check for the effect on zzzx.
			CheckZzzxLanguageDefinition(qld2,
				"InitializeFromXml({en,fr}, [zzzx])", false);
			CheckModifiedZzzxFile("zzzx after RemoveEngine([xyzzy_Latn_GAK_BLORT])");

			// Remove zzzx.
			int wsZzzx;
			hr = qws2->get_WritingSystem(&wsZzzx);
			unitpp::assert_eq("qws->get_WritingSystem([zzzx]) HRESULT", S_OK, hr);
			hr = m_qwsf->RemoveEngine(wsZzzx);
			unitpp::assert_eq("m_qwsf->RemoveEngine([zzzx]) HRESULT", S_OK, hr);

			// Check that the files are really removed.
			StrUni stuDir(DirectoryFinder::FwRootDataDir());
			StrAnsi staFile;

			staFile.Format("%S\\Languages\\xyzzy_Latn_GAK_BLORT.xml", stuDir.Chars());
			FILE * fp;
			fopen_s(&fp, staFile.Chars(), "r");
			if (fp)
				fclose(fp);
			unitpp::assert_eq("fopen(\"xyzzy_Latn_GAK_BLORT.xml\") after removing",
				(FILE *)NULL, fp);

			staFile.Format("%S\\Languages\\zzzx.xml", stuDir.Chars());
			fopen_s(&fp, staFile.Chars(), "r");
			if (fp)
				fclose(fp);
			unitpp::assert_eq("fopen(\"zzzx.xml\") after removing",
				(FILE *)NULL, fp);
		}

		// Check that the LanguageDefinition object loaded from zzzx.xml has the expected
		// attribute values.
		void CheckZzzxLanguageDefinition(LanguageDefinition * pld, const char * pszMsgHeader,
			bool fHasXyzzy)
		{
			const char * pszHasXyzzy = fHasXyzzy ? "T" : "F";
			StrAnsi staMsg;
			staMsg.Format("%s (%s) pld", pszMsgHeader, pszHasXyzzy);
			unitpp::assert_true(staMsg.Chars(), pld != NULL);
			IWritingSystemPtr qws;
			HRESULT hr;
			int cws;
			int iws;
			Vector<int> vws;

			hr = pld->get_WritingSystem(&qws);
			staMsg.Format("%s (%s) get_WritingSystem hr", pszMsgHeader, pszHasXyzzy);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s (%s) get_WritingSystem qws", pszMsgHeader, pszHasXyzzy);
			unitpp::assert_true(staMsg.Chars(), qws.Ptr() != NULL);
			hr = qws->get_NameWsCount(&cws);
			staMsg.Format("%s (%s) qws->get_NameWsCount hr", pszMsgHeader, pszHasXyzzy);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s (%s) qws->get_NameWsCount count", pszMsgHeader, pszHasXyzzy);
			unitpp::assert_eq(staMsg.Chars(), fHasXyzzy ? 3 : 2, cws);
			hr = qws->get_AbbrWsCount(&cws);
			staMsg.Format("%s (%s) qws->get_AbbrWsCount hr", pszMsgHeader, pszHasXyzzy);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s (%s) qws->get_AbbrWsCount count", pszMsgHeader, pszHasXyzzy);
			unitpp::assert_eq(staMsg.Chars(), fHasXyzzy ? 3 : 2, cws);
			hr = qws->get_DescriptionWsCount(&cws);
			staMsg.Format("%s (%s) qws->get_DescriptionWsCount hr", pszMsgHeader, pszHasXyzzy);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			staMsg.Format("%s (%s) qws->get_DescriptionWsCount count",
				pszMsgHeader, pszHasXyzzy);
			unitpp::assert_eq(staMsg.Chars(), fHasXyzzy ? 3 : 2, cws);
			vws.Resize(cws);
			hr = qws->get_DescriptionWss(cws, vws.Begin());
			staMsg.Format("%s (%s) qws->get_DescriptionWss hr", pszMsgHeader, pszHasXyzzy);
			unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
			for (iws = 0; iws < cws; ++iws)
			{
				ITsStringPtr qtss;
				SmartBstr sbstr;
				hr = qws->get_Description(vws[iws], &qtss);
				staMsg.Format("%s (%s) qws->get_Description [%d] hr",
					pszMsgHeader, pszHasXyzzy, iws);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				StrUni stu;
				if (vws[iws] == kwsEng)
				{
					stu.Assign(L"The standard alphabetic representation of Zzzxish.");
					if (fHasXyzzy)
						stu.Append(L"  (Not to mention ZZZxx.)");
				}
				else if (vws[iws] == kwsFrn)
				{
					stu.Assign(L"La representation alphabetic standard de Zzzxais.");
					if (fHasXyzzy)
						stu.Append(L"  (Not to mention ZZZxx.)");
				}
				else
				{
					staMsg.Format("%s (%s) 3rd ws => fHasXyzzy", pszMsgHeader, pszHasXyzzy);
					unitpp::assert_true(staMsg.Chars(), fHasXyzzy);
					stu.Assign(L"The standard alphabetic representation of ZZZxx.");
				}
				hr = qtss->get_Text(&sbstr);
				staMsg.Format("%s (%s) qtss->get_Text [%d] hr", pszMsgHeader, pszHasXyzzy, iws);
				unitpp::assert_eq(staMsg.Chars(), S_OK, hr);
				staMsg.Format("%s (%s) qtss->get_Text [%d] text",
					pszMsgHeader, pszHasXyzzy, iws);
				unitpp::assert_true(staMsg.Chars(), stu == sbstr.Chars());
			}
			// For now, that's good enough for our purposes.
		}

		// Write the contents of g_rgpszZzzxXML[] to {FwRoot}/Languages/zzzx.xml.
		void WriteZzzx()
		{
			StrUni stuDir(DirectoryFinder::FwRootDataDir());
			StrAnsi staFile;
			staFile.Format("%S\\Languages\\zzzx.xml", stuDir.Chars());
			FILE * fp;
			fopen_s(&fp, staFile.Chars(), "w");
			StrAnsi staMsg;
			staMsg.Format("Creating %s", staFile.Chars());
			unitpp::assert_true(staMsg.Chars(), fp != NULL);
			for (int i = 0; g_rgpszZzzxXML[i]; ++i)
				fputs(g_rgpszZzzxXML[i], fp);
			fclose(fp);
		}

		// Check that the modified contents of zzzx.xml matches what we expect.
		void CheckModifiedZzzxFile(const char * pszMsgHeader)
		{
			StrUni stuDir(DirectoryFinder::FwRootDataDir());
			StrAnsi staFile;
			staFile.Format("%S\\Languages\\zzzx.xml", stuDir.Chars());
			FILE * fp;
			fopen_s(&fp, staFile.Chars(), "r");
			StrAnsi staMsg;
			staMsg.Format("%s Opening %s to read", pszMsgHeader, staFile.Chars());
			unitpp::assert_true(staMsg.Chars(), fp != NULL);
			char rgch[128];
			char * psz;
			for (int i = 0; g_rgpszZzzxXML2[i]; ++i)
			{
				psz = fgets(rgch, 128, fp);
				staMsg.Format("%s reading line [%d]", pszMsgHeader, i);
				unitpp::assert_true(staMsg.Chars(), psz != NULL);
				staMsg.Format("%s comparing line [%d]", pszMsgHeader, i);
				rgch[127] = 0;		// may as well be paranoid.
				unitpp::assert_true(staMsg.Chars(), !strcmp(rgch,g_rgpszZzzxXML2[i]));
			}
			psz = fgets(rgch, 128, fp);
			staMsg.Format("%s reading past end", pszMsgHeader);
			unitpp::assert_eq(staMsg.Chars(), (char *)NULL, psz);
			staMsg.Format("%s EOF signal", pszMsgHeader);
			unitpp::assert_true(staMsg.Chars(), feof(fp));
			fclose(fp);
		}

	public:
		TestLanguageDefinition();

		virtual void Setup()
		{
			CreateTestWritingSystemFactory(&m_qwsf);
		}

		virtual void Teardown()
		{
			m_qwsf->Shutdown();
			m_qwsf.Clear();
		}

		virtual void SuiteSetup()
		{
			m_qtsf.CreateInstance(CLSID_TsStrFactory);
		}

		virtual void SuiteTeardown()
		{
			m_qtsf.Clear();
			// Get rid of the leftover log file for the failed import.
			StrUni stuDir(DirectoryFinder::FwRootDataDir());
			StrAnsi staFile;
			staFile.Format("%S\\Languages\\xyzzy_Latn_GAK_BLORT-Import.log", stuDir.Chars());
			::DeleteFileA(staFile.Chars());
		}
	};
}

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

#endif /*TESTLANGDEF_H_INCLUDED*/

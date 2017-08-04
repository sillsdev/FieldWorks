/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (c) 2003-2013 SIL International
This software is licensed under the LGPL, version 2.1 or later
(http://www.gnu.org/licenses/lgpl-2.1.html)

File: TestUtilString.h
Responsibility:
Last reviewed:

	Unit tests for the functions/classes from Generic/UtilXml.cpp
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTUTILSTRING_H_INCLUDED
#define TESTUTILSTRING_H_INCLUDED

#pragma once

#include "testGenericLib.h"

namespace TestGenericLib
{
	class TestHelper{
	public:
		const char * GetConstCharString(const StrAnsi& sta)
		{
			m_sta = sta;
			return m_sta.Chars();
		}

		static const wchar * GetConstWCharString(const StrUni& stu)
		{
			return stu.Chars();
		}
	private:
		StrAnsi m_sta;

	};

	class TestUtilString : public unitpp::suite
	{
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
#ifdef WIN32
#define MUSICAL_SYMBOL_MINIMA L"\xD834\xDDBB" // 1D1BB decomposes to 1D1B9 1D165
#define MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"\xD834\xDDB9" // 1D1B9
#define MUSICAL_SYMBOL_COMBINING_STEM L"\xD834\xDD65" // 1D165
#else
#define MUSICAL_SYMBOL_MINIMA L"\x1D1BB" // 1D1BB decomposes to 1D1B9 1D165
#define MUSICAL_SYMBOL_SEMIBREVIS_WHITE L"\x1D1B9" // 1D1B9
#define MUSICAL_SYMBOL_COMBINING_STEM L"\x1D165" // 1D165
#endif
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

//UNORM_NONE  No decomposition/composition.
//UNORM_NFD  Canonical decomposition.
//UNORM_NFKD  Compatibility decomposition.
//UNORM_NFC  Canonical decomposition followed by canonical composition.
//UNORM_NFKC  Compatibility decomposition followed by canonical composition.

		void testNormalizeStrUni()
		{
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

			StrUni stu = stuInput;
			bool fSuccess;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NONE);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput", stu == stuInput);

			stu = stuInput;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFC);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput", stu == stuNFC);
		}

		void testNormalizeStrUni1()
		{
			StrUni stuInput1 = L"a" COMBINING_DIAERESIS COMBINING_DIAERESIS COMBINING_DIAERESIS
				COMBINING_DOT_BELOW
				a_WITH_DIAERESIS COMBINING_DOT_BELOW;
			// NFD or NFKD: decompose a_WITH_DIAERESIS, reorder both sequences
			StrUni stuNFD1 = L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS
				COMBINING_DIAERESIS L"a" COMBINING_DOT_BELOW COMBINING_DIAERESIS;
			// NFC or NFKC: expand, reorder, recombine
			StrUni stuNFC1 = a_WITH_DOT_BELOW COMBINING_DIAERESIS COMBINING_DIAERESIS
				COMBINING_DIAERESIS a_WITH_DOT_BELOW COMBINING_DIAERESIS;

			StrUni stu = stuInput1;
			bool fSuccess;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NONE);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput1 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput1", stu == stuInput1);

			stu = stuInput1;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFC);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput1 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput1", stu == stuNFC1);

			stu = stuInput1;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFD);
			unitpp::assert_true("NormalizeStrUni UNORM_NFD for stuInput1 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFD for stuInput1", stu == stuNFD1);

			stu = stuInput1;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFKC);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKC for stuInput1 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKC for stuInput1", stu == stuNFC1);

			stu = stuInput1;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFKD);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKD for stuInput1 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKD for stuInput1", stu == stuNFD1);

		}

		void testNormalizeStrUni2()
		{
			// For each normaliztion make a single and multi-run string that requires
			// changes to be normalized. Try to test several aspects, for example, collapsing
			// and expanding, re-ordering diacritics,...
			// Note: as NFC and NFD are the only currently interesting forms, maybe it is
			// enough to test those for now?
			StrUni stuInput2 = L"abc" A_WITH_DIAERESIS_AND_MACRON L"A" COMBINING_DIAERESIS
				COMBINING_MACRON L"C" COMBINING_OVERLINE COMBINING_LEFT_HALF_RING_BELOW L"XYZ"
				BREVE L"GAP" SPACE COMBINING_BREVE L"QED" MUSICAL_SYMBOL_MINIMA;
			// outputs. All reorder overline and half ring.
			// NFD: decompose A_WITH_DIAERESIS_AND_MACRON
			StrUni stuNFD2 = L"abcA" COMBINING_DIAERESIS COMBINING_MACRON L"A"
				COMBINING_DIAERESIS COMBINING_MACRON L"C" COMBINING_LEFT_HALF_RING_BELOW
				COMBINING_OVERLINE L"XYZ" BREVE L"GAP" SPACE COMBINING_BREVE L"QED"
				MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM;
			// NFKD: same plus decompose BREVE
			StrUni stuNFKD2 = L"abcA" COMBINING_DIAERESIS COMBINING_MACRON L"A"
				COMBINING_DIAERESIS COMBINING_MACRON L"C" COMBINING_LEFT_HALF_RING_BELOW
				COMBINING_OVERLINE L"XYZ" SPACE COMBINING_BREVE L"GAP" SPACE COMBINING_BREVE
				L"QED" MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM;
			// NFC: compose to A_WITH_DIAERESIS_AND_MACRON
			// note: the composed surrogate pair gets decomposed due to backwards
			// compatibility with the Unicode 3.2 algorithm.
			StrUni stuNFC2 = L"abc" A_WITH_DIAERESIS_AND_MACRON A_WITH_DIAERESIS_AND_MACRON
				L"C" COMBINING_LEFT_HALF_RING_BELOW COMBINING_OVERLINE L"XYZ"
				BREVE L"GAP" SPACE COMBINING_BREVE L"QED"
				MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM;
			// NFKC : same plus decompose BREVE (This is surprising, but NFKC
			// DEcomposes compatibility equivalents while composing canonical ones.)
			StrUni stuNFKC2 = L"abc" A_WITH_DIAERESIS_AND_MACRON A_WITH_DIAERESIS_AND_MACRON
				L"C" COMBINING_LEFT_HALF_RING_BELOW COMBINING_OVERLINE L"XYZ"
				SPACE COMBINING_BREVE L"GAP" SPACE COMBINING_BREVE L"QED"
				MUSICAL_SYMBOL_SEMIBREVIS_WHITE MUSICAL_SYMBOL_COMBINING_STEM;

			StrUni stu = stuInput2;
			bool fSuccess;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NONE);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput2 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput2", stu == stuInput2);

			stu = stuInput2;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFC);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput2 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput2", stu == stuNFC2);

			stu = stuInput2;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFD);
			unitpp::assert_true("NormalizeStrUni UNORM_NFD for stuInput2 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFD for stuInput2", stu == stuNFD2);

			stu = stuInput2;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFKC);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKC for stuInput2 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKC for stuInput2", stu == stuNFKC2);

			stu = stuInput2;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFKD);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKD for stuInput2 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKD for stuInput2", stu == stuNFKD2);
		}

		void testNormalizeStrUni3()
		{
			StrUni stuInput3 = e_WITH_GRAVE;

			// NFD and NFKD: decompose e_WITH_GRAVE
			StrUni stuNFD3 = L"e" COMBINING_GRAVE_ACCENT;
			// NFC and NFKD: stay composed -- same as original input.

			StrUni stu = stuInput3;
			bool fSuccess;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NONE);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput3 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput3", stu == stuInput3);

			stu = stuInput3;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFC);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput3 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput3", stu == stuInput3);

			stu = stuInput3;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFD);
			unitpp::assert_true("NormalizeStrUni UNORM_NFD for stuInput3 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFD for stuInput3", stu == stuNFD3);

			stu = stuInput3;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFKC);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKC for stuInput3 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKC for stuInput3", stu == stuInput3);

			stu = stuInput3;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFKD);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKD for stuInput3 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKD for stuInput3", stu == stuNFD3);
		}

		void testNormalizeStrUni4()
		{
			StrUni stuInput4 = L"Plain ASCII type string without any accents or diacritics or "
				L"other funny characters.";

			// NFD, NFKD NFC, and NFKD: same as input.

			StrUni stu = stuInput4;
			bool fSuccess;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NONE);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput4 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NONE for stuInput4", stu == stuInput4);

			stu = stuInput4;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFC);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput4 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFC for stuInput4", stu == stuInput4);

			stu = stuInput4;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFD);
			unitpp::assert_true("NormalizeStrUni UNORM_NFD for stuInput4 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFD for stuInput4", stu == stuInput4);

			stu = stuInput4;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFKC);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKC for stuInput4 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKC for stuInput4", stu == stuInput4);

			stu = stuInput4;
			fSuccess = StrUtil::NormalizeStrUni(stu, UNORM_NFKD);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKD for stuInput4 retval", fSuccess);
			unitpp::assert_true("NormalizeStrUni UNORM_NFKD for stuInput4", stu == stuInput4);
		}

		void testSkipLeadingWhiteSpace()
		{
			unsigned cch;

			StrAnsi staNoSpaces = "No leading or trailing spaces";
			const char * pszOut = StrUtil::SkipLeadingWhiteSpace(staNoSpaces.Chars());
			unitpp::assert_true("SkipLeadingWhiteSpace char (no spaces)",
				! strcmp(staNoSpaces.Chars(), pszOut));

			cch = StrUtil::LengthLessTrailingWhiteSpace(pszOut);
			unitpp::assert_eq("LengthLessTrailingWhiteSpace char (no spaces)",
				unsigned(staNoSpaces.Length()), cch);

			StrAnsi staSpace = " Z ";
			pszOut = StrUtil::SkipLeadingWhiteSpace(staSpace.Chars());
			unitpp::assert_true("SkipLeadingWhiteSpace char (one letter, one space)",
				! strcmp(staSpace.Chars() + 1, pszOut));

			cch = StrUtil::LengthLessTrailingWhiteSpace(pszOut);
			unitpp::assert_eq("LengthLessTrailingWhiteSpace char (one letter, one space)",
				unsigned(staSpace.Length()) - 2, cch);

			staSpace = "      ";
			pszOut = StrUtil::SkipLeadingWhiteSpace(staSpace.Chars());
			unitpp::assert_true("SkipLeadingWhiteSpace char (just space)",
				! strcmp("", pszOut));

			cch = StrUtil::LengthLessTrailingWhiteSpace(pszOut);
			unitpp::assert_eq("LengthLessTrailingWhiteSpace char (just space)",
				unsigned(0), cch);

			staSpace = " leading and trailing space ";
			pszOut = StrUtil::SkipLeadingWhiteSpace(staSpace.Chars());
			unitpp::assert_true("SkipLeadingWhiteSpace char (space)",
				! strcmp(staSpace.Chars() + 1, pszOut));

			cch = StrUtil::LengthLessTrailingWhiteSpace(pszOut);
			unitpp::assert_eq("LengthLessTrailingWhiteSpace char (space)",
				unsigned(staSpace.Length()) - 2, cch);

			StrAnsi staOut;
			StrUtil::TrimWhiteSpace(staSpace, staOut);
			unitpp::assert_true("TrimWhiteSpace StrAnsi (space)",
				! strncmp(staOut.Chars(), pszOut, cch));
			unitpp::assert_eq("TrimWhiteSpace StrAnsi (space)",
				unsigned(staOut.Length()), cch);

			// Make sure overlapping input and output succeeds.
			int cchT = staOut.Length();
			StrUtil::TrimWhiteSpace(staOut, staOut);
			unitpp::assert_true("TrimWhiteSpace StrAnsi in=out",
				staOut.Length() == cchT);

			StrUni stuNoSpaces = L"No leading or trailing spaces";
			const wchar *pszwOut = StrUtil::SkipLeadingWhiteSpace(stuNoSpaces.Chars());
			unitpp::assert_true("SkipLeadingWhiteSpace wchar (no spaces)",
				! wcscmp(stuNoSpaces.Chars(), pszwOut));

			StrUni stuSpace = L" leading and trailing space ";
			pszwOut = StrUtil::SkipLeadingWhiteSpace(stuSpace.Chars());
			unitpp::assert_true("SkipLeadingWhiteSpace wchar (space)",
				! wcscmp(stuSpace.Chars() + 1, pszwOut));

			StrUni stuFancySpaces = L"\x2000\x3000\x200A" L"leading and trailing space ";
			pszwOut = StrUtil::SkipLeadingWhiteSpace(stuFancySpaces.Chars());
			unitpp::assert_true("SkipLeadingWhiteSpace wchar (fancy spaces)",
				! wcscmp(stuFancySpaces.Chars() + 3, pszwOut));
		}

		void testCompare()
		{
			int cele; // collation elements that matched during the comparison.
			int dif;
			UErrorCode uerr = U_ZERO_ERROR;
			// Collator * pcolEng = Collator::createInstance(Locale::getEnglish(), uerr);
			// There's got to be an easier way to get a RuleBasedCollator for English, but so
			// far I haven't found out how!
			UnicodeString ust = "x";
			StringSearch * pss = new StringSearch(ust, ust, Locale("en", "US"), NULL, uerr);
			unitpp::assert_true("Created new StringSearch successfully 1", U_SUCCESS(uerr));
			RuleBasedCollator * prbc = pss->getCollator();
			prbc->setStrength(Collator::SECONDARY); // Don't ignore case.
			dif = StrUtil::Compare(StrUni(L"abc"), 3, StrUni(L"ab"), 2, prbc, &cele); // dif > 0, cele = 2
			unitpp::assert_true("StrUtil::Compare test 1", dif > 0 && cele == 2);
			dif = StrUtil::Compare(StrUni(L"ab"), 2, StrUni(L"abc"), 3, prbc, &cele); // dif < 0, cele = 2
			unitpp::assert_true("StrUtil::Compare test 2", dif < 0 && cele == 2);
			dif = StrUtil::Compare(StrUni(L"abc"), 3, StrUni(L"abd"), 3, prbc, &cele); // dif < 0, cele = 2
			unitpp::assert_true("StrUtil::Compare test 3", dif < 0 && cele == 2);
			dif = StrUtil::Compare(StrUni(L"abd"), 3, StrUni(L"abc"), 3, prbc, &cele); // dif > 0, cele = 2
			unitpp::assert_true("StrUtil::Compare test 4", dif > 0 && cele == 2);
			dif = StrUtil::Compare(StrUni(L"abc"), 3, StrUni(L"abc"), 3, prbc, &cele); // dif = 0, cele = 3
			unitpp::assert_true("StrUtil::Compare test 5", dif == 0 && cele == 3);
//			This should work, but acts as though setStrength() does nothing.
//			prbc->setStrength(Collator::PRIMARY); // Ignore case.
//			dif = StrUtil::Compare(OleStringLiteral(L"abc"), 3, OleStringLiteral(L"AbC"), 3, prbc, &cele); // dif = 0, cele = 3
//			unitpp::assert_true("StrUtil::Compare test 6", dif == 0 && cele == 3);
//			prbc->setStrength(Collator::SECONDARY); // Don't ignore case.
			//O WITH combining OGONEK AND combininb MACRON
			dif = StrUtil::Compare(StrUni(L"ab\x006f\x0328\x0304gh"), 7, StrUni(L"ab\x006f\x0328\x0304gi"), 7,
				prbc, &cele);
			unitpp::assert_true("StrUtil::Compare test 7", dif < 0 && cele == 6);
			// O WITH OGONEK AND combining MACRON
			dif = StrUtil::Compare(StrUni(L"ab\x01eb\x0304gh"), 6, StrUni(L"ab\x01eb\x0304gd"), 6, prbc, &cele);
			unitpp::assert_true("StrUtil::Compare test 8", dif > 0 && cele == 6);
			// O WITH OGONEK AND MACRON
			dif = StrUtil::Compare(StrUni(L"ab\x01edgh"), 5, StrUni(L"ab\x01egd"), 5, prbc, &cele);
			unitpp::assert_true("StrUtil::Compare test 9", dif > 0 && cele == 2);
			delete pss;
			//delete prbc; // gets deleted by pss.

			// German
			StringSearch * pssGer = new StringSearch(ust, ust, Locale("de"), NULL, uerr);
			unitpp::assert_true("Created new StringSearch successfully 2", U_SUCCESS(uerr));
			RuleBasedCollator * prbcGer = pssGer->getCollator();
			prbcGer->setStrength(Collator::SECONDARY); // Don't ignore case.
			// These are equal in German
			dif = StrUtil::Compare(StrUni(L"abssg"), 5, StrUni(L"ab\x00dfg"), 4, prbcGer, &cele);
			unitpp::assert_true("StrUtil::Compare test 10", dif < 0 && cele == 2);
			// These are equal in German
			dif = StrUtil::Compare(StrUni(L"ab\x00dfg"), 5, StrUni(L"abssg"), 4, prbcGer, &cele);
			unitpp::assert_true("StrUtil::Compare test 11", dif > 0 && cele == 2);
			delete pssGer;
			//delete prbcGer; // gets deleted by pssGer.
		}
		void testFormat()
		{
			TestHelper th;
			StrUni stuExpected = _T("the beginningthe middlethe end");
			SmartBstr bstrBeginning = ::SysAllocString(StrUni(L"the beginning"));


			StrUni stu;
			StrUni stuFormatSpec(_T("%s%s%S"));

			stu.Format((OLECHAR*)StrUni(L"%s%s%S").Chars(), (OLECHAR*)StrUni(L"the beginning").Chars(),
				(OLECHAR*)StrUni(L"the middle").Chars(), "the end");
			unitpp::assert_true("Format with char format spec %s%s%S with stack strings",
				stu == stuExpected);

				stu.Clear();
				unitpp::assert_true("Clear", stu != stuExpected);

			StrUni stuMiddle(_T("the middle"));
			StrAnsi stuEnd("the end");
			stu.Format((OLECHAR*)StrUni(L"%b%s%S").Chars(), bstrBeginning.Chars(), stuMiddle.Chars(), stuEnd.Chars());
			unitpp::assert_true("Format with char format spec %s%s%S with SmartBstr",
				stu == stuExpected);

				stu.Clear();
				unitpp::assert_true("Clear", stu != stuExpected);

			stu.Format(stuFormatSpec, (OLECHAR*)StrUni(L"the beginning").Chars(), (OLECHAR*)StrUni(L"the middle").Chars(), "the end");
			unitpp::assert_true("Format with StrUni format spec %s%s%S with SmartBstr",
				stu == stuExpected);

				stu.Clear();
				unitpp::assert_true("Clear", stu != stuExpected);

			stuFormatSpec.Replace(1,2,'b');
			stu.Format(stuFormatSpec, bstrBeginning.Chars(), (OLECHAR*)StrUni(L"the middle").Chars(), "the end");
			unitpp::assert_true("Format with StrUni format spec %s%s%S with SmartBstr",
				stu == stuExpected);

				stu.Clear();
				unitpp::assert_true("Clear", stu != stuExpected);

			stu.Format((OLECHAR*)StrUni(L"%<1>s%<0>s%<2>S").Chars(),
				TestHelper::GetConstWCharString(_T("the middle")), bstrBeginning.Chars(),
				th.GetConstCharString("the end"));
			unitpp::assert_true("Format %<1>s%<0>s%S", stu == stuExpected);

			stu.Clear();
			unitpp::assert_true("Clear", stu != stuExpected);

			stu.Format(_T("%c"), 'c');
			unitpp::assert_true("Format %c", stu == StrUni(L"c"));
		}

		void testFormatAppend()
		{
			TestHelper th;
			StrUni stuExpected = _T("the prefixthe beginningthe middlethe end");
			SmartBstr bstrBeginning = ::SysAllocString(StrUni(L"the beginning"));


			StrUni stu(_T("the prefix"));
			StrUni stuFormatSpec(_T("%s%s%S"));

			stu.FormatAppend((OLECHAR*)StrUni(L"%s%s%S").Chars(),
				(OLECHAR*)StrUni(L"the beginning").Chars(), (OLECHAR*)StrUni(L"the middle").Chars(),
				"the end");
			unitpp::assert_true("FormatAppend with char format spec %s%s%S with stack strings",
				stu == stuExpected);

				stu = _T("the prefix");

			StrUni stuMiddle(_T("the middle"));
			StrAnsi stuEnd("the end");
			stu.FormatAppend((OLECHAR*)StrUni(L"%b%s%S").Chars(), bstrBeginning.Chars(),
				stuMiddle.Chars(), stuEnd.Chars());
			unitpp::assert_true("FormatAppend with char format spec %s%s%S with SmartBstr",
				stu == stuExpected);

				stu = _T("the prefix");

			stu.FormatAppend(&stuFormatSpec, (OLECHAR*)StrUni(L"the beginning").Chars(),
				(OLECHAR*)StrUni(L"the middle").Chars(), "the end");
			unitpp::assert_true("FormatAppend with StrUni format spec %s%s%S with SmartBstr",
				stu == stuExpected);

				stu = _T("the prefix");

			stuFormatSpec.Replace(1,2,'b');
			stu.FormatAppend(&stuFormatSpec, bstrBeginning.Chars(), StrUni(L"the middle").Chars(),
				"the end");
			unitpp::assert_true("FormatAppend with StrUni format spec %s%s%S with SmartBstr",
				stu == stuExpected);

				stu = _T("the prefix");

			stu.FormatAppend((OLECHAR*)StrUni(L"%<1>s%<0>s%<2>S").Chars(), TestHelper::GetConstWCharString(_T("the middle")),
				 bstrBeginning.Chars(), th.GetConstCharString("the end"));
			unitpp::assert_true("FormatAppend %<1>s%<0>s%S", stu == stuExpected);

				stu = _T("the prefix");

			stu.FormatAppend((OLECHAR*)StrUni(L"%c").Chars(), 'c');
			unitpp::assert_true("FormatAppend %c", stu == StrUni(L"the prefixc").Chars());
		}

		// This just tests StrUni string formating capablity for integers.
		// Most of the string is arbitary, it was taken from real code.
		void testFormatOfLargeInteger()
		{

			#define kgrfcptReference 352321536

			StrUni stuVerifyUndo;
			stuVerifyUndo.Format(L"declare @cobj int, @res int%n"
			L"set @res = 1%n"
			L"select @cobj = COUNT(id) from CmObject where Owner$ = %<0>d%n"
			L"if @cobj != 0 set @res = 0%n"
			L"if @res = 1 begin%n"
			L"	create table [#ObjInfoTbl$]%n"
			L"	(%n"
			L"		[ObjId]			int not null,%n"
			L"		[ObjClass]		int null,%n"
			L"		[InheritDepth]	int null default(0),%n"
			L"		[OwnerDepth]	int null default(0),%n"
			L"		[RelObjId]		int null,%n"
			L"		[RelObjClass]	int null,%n"
			L"		[RelObjField]	int null,%n"
			L"		[RelOrder]		int null,%n"
			L"		[RelType]		int null,%n"
			L"		[OrdKey]		varbinary(250) null default(0)%n"
			L"	)%n"
		L"	create nonclustered index #ObjInfoTbl$_Ind_ObjId on [#ObjInfoTbl$] (ObjId)%n"
		L"	create nonclustered index #ObjInfoTbl$_Ind_ObjClass on [#ObjInfoTbl$] (ObjClass)%n"
			L"	exec GetLinkedObjs$ %<0>d, null, %<1>d, 0, 0, 0, -1, null%n"
			L"	select @cobj = COUNT(*) from [#ObjInfoTbl$]%n"
			L"	drop table [#ObjInfoTbl$]%n"
			L"	if @cobj != 0 set @res = 0%n"
			L"end%n"
			L"select @res",
			134419, kgrfcptReference);

			BSTR bstrTemp = stuVerifyUndo.Bstr();

			SmartBstr smartBStr;

			smartBStr = bstrTemp;

			unitpp::assert_true("stuVerifyUndo.Length() == smartBStr.Length()", stuVerifyUndo.Length() == smartBStr.Length());

		}

		void testGetBstr_StrUni()
		{
			StrUni stu(L"text");
			SmartBstr expected(stu.Chars());
			BSTR bstr = NULL;
			try
			{
				stu.GetBstr(&bstr);
				unitpp::assert_true("Wrong value for BSTR from StrUni", expected.Equals(bstr));
			}
			catch (...)
			{
				::SysFreeString(bstr);
				throw;
			}
			::SysFreeString(bstr);
		}

		void testGetBstr_StrAnsi()
		{
			StrAnsi sta("text");
			SmartBstr expected(L"text");
			BSTR bstr = NULL;
			try
			{
				sta.GetBstr(&bstr);
				unitpp::assert_true("Wrong value for BSTR from StrAnsi", expected.Equals(bstr));
			}
			catch (...)
			{
				::SysFreeString(bstr);
				throw;
			}
			::SysFreeString(bstr);
		}

	public:
		TestUtilString();
	};

}

#endif /*TESTUTILSTRING_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mkGenLib-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

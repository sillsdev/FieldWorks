/*--------------------------------------------------------------------*//*:Ignore this sentence.
Copyright (C) 2003 SIL International. All rights reserved.

Distributable under the terms of either the Common Public License or the
GNU Lesser General Public License, as specified in the LICENSING.txt file.

File: TestLgIcuCharPropEngine.h
Responsibility:
Last reviewed:

	Unit tests for the LgIcuCharPropEngine class.
-------------------------------------------------------------------------------*//*:End Ignore*/
#ifndef TESTLGICUCHARPROPENGINE_H_INCLUDED
#define TESTLGICUCHARPROPENGINE_H_INCLUDED

#pragma once

#include "testLanguage.h"

namespace TestLanguage
{
	/*******************************************************************************************
		Tests for LgCharacterPropertyEngine (ICU based implementation)
	 ******************************************************************************************/
	class TestLgIcuCharPropEngine : public unitpp::suite
	{
		ILgCharacterPropertyEnginePtr m_qpropeng;

		void testNullArgs()
		{
			unitpp::assert_true("m_qpropeng", m_qpropeng.Ptr());
			HRESULT hr;
			hr = m_qpropeng->get_GeneralCategory(0, NULL);
			unitpp::assert_eq("get_GeneralCategory(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_BidiCategory(0, NULL);
			unitpp::assert_eq("get_BidiCategory(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsLetter(0, NULL);
			unitpp::assert_eq("get_IsLetter(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsWordForming(0, NULL);
			unitpp::assert_eq("get_IsWordForming(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsPunctuation(0, NULL);
			unitpp::assert_eq("get_IsPunctuation(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsNumber(0, NULL);
			unitpp::assert_eq("get_IsNumber(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsSeparator(0, NULL);
			unitpp::assert_eq("get_IsSeparator(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsSymbol(0, NULL);
			unitpp::assert_eq("get_IsSymbol(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsMark(0, NULL);
			unitpp::assert_eq("get_IsMark(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsOther(0, NULL);
			unitpp::assert_eq("get_IsOther(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsUpper(0, NULL);
			unitpp::assert_eq("get_IsUpper(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsLower(0, NULL);
			unitpp::assert_eq("get_IsLower(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsTitle(0, NULL);
			unitpp::assert_eq("get_IsTitle(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsModifier(0, NULL);
			unitpp::assert_eq("get_IsModifier(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsOtherLetter(0, NULL);
			unitpp::assert_eq("get_IsOtherLetter(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsOpen(0, NULL);
			unitpp::assert_eq("get_IsOpen(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsClose(0, NULL);
			unitpp::assert_eq("get_IsClose(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsWordMedial(0, NULL);
			unitpp::assert_eq("get_IsWordMedial(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsControl(0, NULL);
			unitpp::assert_eq("get_IsControl(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_ToLowerCh(0, NULL);
			unitpp::assert_eq("get_ToLowerCh(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_ToUpperCh(0, NULL);
			unitpp::assert_eq("get_ToUpperCh(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_ToTitleCh(0, NULL);
			unitpp::assert_eq("get_ToTitleCh(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->ToLower(NULL, NULL);
			unitpp::assert_eq("ToLower(NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->ToUpper(NULL, NULL);
			unitpp::assert_eq("ToUpper(NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->ToTitle(NULL, NULL);
			unitpp::assert_eq("ToTitle(NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->ToLowerRgch(NULL, 0, NULL, 0, NULL);
			unitpp::assert_eq("ToLowerRgch(NULL, 0, NULL, 0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->ToUpperRgch(NULL, 0, NULL, 0, NULL);
			unitpp::assert_eq("ToUpperRgch(NULL, 0, NULL, 0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->ToTitleRgch(NULL, 0, NULL, 0, NULL);
			unitpp::assert_eq("ToTitleRgch(NULL, 0, NULL, 0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_IsUserDefinedClass(0, 0, NULL);
			unitpp::assert_eq("get_IsUserDefinedClass(0, 0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_SoundAlikeKey(NULL, NULL);
			unitpp::assert_eq("get_SoundAlikeKey(NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_CharacterName(0, NULL);
			unitpp::assert_eq("get_CharacterName(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_Decomposition(0, NULL);
			unitpp::assert_eq("get_Decomposition(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->DecompositionRgch(0, 0, NULL, NULL, NULL);
			unitpp::assert_eq("DecompositionRgch(0, 0, NULL, NULL, NULL) HRESULT",
				E_POINTER, hr);
			hr = m_qpropeng->get_FullDecomp(0, NULL);
			unitpp::assert_eq("get_FullDecomp(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->FullDecompRgch(0, 0, NULL, NULL, NULL);
			unitpp::assert_eq("FullDecompRgch(0, 0, NULL, NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_NumericValue(0, NULL);
			unitpp::assert_eq("get_NumericValue(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_CombiningClass(0, NULL);
			unitpp::assert_eq("get_CombiningClass(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_Comment(0, NULL);
			unitpp::assert_eq("get_Comment(0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->GetLineBreakInfo(NULL, 0, 0, 0, NULL, NULL);
			unitpp::assert_eq("GetLineBreakInfo(NULL, 0, 0, 0, NULL, NULL) HRESULT",
				E_POINTER, hr);
			hr = m_qpropeng->StripDiacritics(NULL, NULL);
			unitpp::assert_eq("StripDiacritics(NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->StripDiacriticsRgch(NULL, 0, NULL, 0, NULL);
			unitpp::assert_eq("StripDiacriticsRgch(NULL, 0, NULL, 0, NULL) HRESULT",
				E_POINTER, hr);
			//hr = m_qpropeng->NormalizeKd(NULL, NULL);
			//unitpp::assert_eq("NormalizeKd(NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->NormalizeKdRgch(NULL, 0, NULL, 0, NULL);
			unitpp::assert_eq("NormalizeKdRgch(NULL, 0, NULL, 0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->NormalizeD(NULL, NULL);
			unitpp::assert_eq("NormalizeD(NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->NormalizeDRgch(NULL, 0, NULL, 0, NULL);
			unitpp::assert_eq("NormalizeDRgch(NULL, 0, NULL, 0, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->get_Locale(NULL);
			unitpp::assert_eq("get_Locale(NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->put_LineBreakText(NULL, 0);
			unitpp::assert_eq("put_LineBreakText(NULL, 0) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->GetLineBreakText(0, NULL, NULL);
			unitpp::assert_eq("GetLineBreakText(0, NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->LineBreakBefore(0, NULL, NULL);
			unitpp::assert_eq("LineBreakBefore(0, NULL, NULL) HRESULT", E_POINTER, hr);
			hr = m_qpropeng->LineBreakAfter(0, NULL, NULL);
			unitpp::assert_eq("LineBreakAfter(0, NULL, NULL) HRESULT", E_POINTER, hr);
		}

		// Ensure that FullDecomp does canonical, rather than compatibility decomposition (TE-8384)
		void testFullDecomp_Canonical()
		{
			unitpp::assert_true("m_qpropeng", m_qpropeng.Ptr());
			HRESULT hr;
			SmartBstr sbstrDecomp;
			hr = m_qpropeng->get_FullDecomp(0x2074, &sbstrDecomp);
			unitpp::assert_eq("get_FullDecomp(0x2074...", S_OK, hr);
			unitpp::assert_eq("Superscript 4 should not decompose at all", 0, sbstrDecomp.Length());
			OLECHAR *prgchDecomp = new OLECHAR[3];
			ComBool fHasDecomp = false;
			int cch;
			hr = m_qpropeng->FullDecompRgch(0x2074, 3, prgchDecomp, &cch, &fHasDecomp);
			unitpp::assert_eq("FullDecompRgch(0x2074...", S_OK, hr);
			unitpp::assert_true("Superscript 4 should not decompose at all", !fHasDecomp);
			unitpp::assert_eq("Superscript 4 should not decompose at all", 0x2074, prgchDecomp[0]);
		}


		void CheckChar(int ch, const char * pszName, LgGeneralCharCategory cc,
			int nCombiningClass, LgBidiCategory bic, HRESULT hrNumericValue,
			int nNumericValue, int chLower, int chUpper, int chTitle)
		{
			StrAnsi staMsg;
			HRESULT hr;
			LgGeneralCharCategory ccT;
			//LgBidiCategory bicT;
			SmartBstr sbstr;
			StrAnsi sta;
			int chT;
			int nT = 0;
			ComBool fRet;
			// The SIL/FieldWorks changes for ICU 5.0 do not support get_CharacterName.
			//hr = m_qpropeng->get_CharacterName(ch, &sbstr);
			//unitpp::assert_eq("get_CharacterName(ch, &sbstr) HRESULT", S_OK, hr);
			//sta.Assign(sbstr.Chars());
			//staMsg.Format("get_CharacterName(%x): expected = \"%s\", actual = \"%s\"", ch, pszName, sta.Chars());
			//unitpp::assert_true(staMsg.Chars(), strcmp(pszName, sta.Chars()) == 0);

			hr = m_qpropeng->get_GeneralCategory(ch, &ccT);
			unitpp::assert_eq("get_GeneralCategory(ch, &ccT) HRESULT", S_OK, hr);
			staMsg.Format("get_GeneralCategory(%x)", ch);
			unitpp::assert_eq(staMsg.Chars(), cc, ccT);

			hr = m_qpropeng->get_CombiningClass(ch, &nT);
			unitpp::assert_eq("get_CombiningClass(ch, &nT) HRESULT", S_OK, hr);
			staMsg.Format("get_CombiningClass(%x)", ch);
			unitpp::assert_eq(staMsg.Chars(), nCombiningClass, nT);

			// The SIL/FieldWorks changes for ICU 5.0 do not support get_BidiCategory.
			//hr = m_qpropeng->get_BidiCategory(ch, &bicT);
			//unitpp::assert_eq("get_BidiCategory(ch, &bicT) HRESULT", S_OK, hr);
			//staMsg.Format("get_BidiCategory(%x)", ch);
			//unitpp::assert_eq(staMsg.Chars(), bic, bicT);

			// The SIL/FieldWorks changes for ICU 5.0 do not support get_NumericValue.
			//hr = m_qpropeng->get_NumericValue(ch, &nT);
			//unitpp::assert_eq("get_NumericValue(ch, &nT) HRESULT", hrNumericValue, hr);
			//if (hr == S_OK)
			//{
			//	staMsg.Format("get_NumericValue(%x)", ch);
			//	unitpp::assert_eq(staMsg.Chars(), nNumericValue, nT);
			//}

			hr = m_qpropeng->get_ToLowerCh(ch, &chT);
			unitpp::assert_eq("get_ToLowerCh(ch, &chT) HRESULT", S_OK, hr);
			staMsg.Format("get_ToLowerCh(%x)", ch);
			unitpp::assert_eq(staMsg.Chars(), chLower, chT);

			hr = m_qpropeng->get_ToUpperCh(ch, &chT);
			unitpp::assert_eq("get_ToUpperCh(ch, &chT) HRESULT", S_OK, hr);
			staMsg.Format("get_ToUpperCh(%x)", ch);
			unitpp::assert_eq(staMsg.Chars(), chUpper, chT);

			hr = m_qpropeng->get_ToTitleCh(ch, &chT);
			unitpp::assert_eq("get_ToTitleCh(ch, &chT) HRESULT", S_OK, hr);
			staMsg.Format("get_ToTitleCh(%x)", ch);
			unitpp::assert_eq(staMsg.Chars(), chTitle, chT);
		}

		void testSILPUAChars()
		{
			unitpp::assert_true("m_qpropeng", m_qpropeng.Ptr());

// F171;COMBINING MACRON-ACUTE;Mn;230;NSM;;;;;N;;;;;   # [SIL-Corp] Added Sep 2005
// F21A;LATIN SMALL LETTER W WITH HOOK;Ll;0;L;;;;;N;;;F21B;;F21B   # [SIL-Corp] Added Sep 2005
// F21B;LATIN CAPITAL LETTER W WITH HOOK;Lu;0;L;;;;;N;;;;F21A;   # [SIL-Corp] Added Sep 2005

			CheckChar(0xF171, "COMBINING MACRON-ACUTE", kccMn, 230, kbicNSM, E_FAIL, 0,
				0xF171, 0xF171, 0xF171);

			CheckChar(0xF21A, "LATIN SMALL LETTER W WITH HOOK", kccLl, 0, kbicL, E_FAIL, 0,
				0xF21A, 0xF21B, 0xF21B);

			CheckChar(0xF21B, "LATIN CAPITAL LETTER W WITH HOOK", kccLu, 0, kbicL, E_FAIL, 0,
				0xF21A, 0xF21B, 0xF21B);
		}

		// TODO (LT-9311) Unignore this test
		void ignore_testIsWordForming()
		{
			HRESULT hr;
			ComBool fRet;
			hr = m_qpropeng->get_IsWordForming(0x0041, &fRet); // capital A
			unitpp::assert_eq("get_IsWordForming(0x0041, &fRet) HRESULT", S_OK, hr);
			unitpp::assert_true("'A' should be word-forming", fRet);

			hr = m_qpropeng->get_IsWordForming(0x0021, &fRet); // exclamation point
			unitpp::assert_eq("get_IsWordForming(0x0021, &fRet) HRESULT", S_OK, hr);
			unitpp::assert_true("Exclamation point should not be word-forming", !fRet);

			hr = m_qpropeng->get_IsWordForming(0x0027, &fRet); // apostrophe
			unitpp::assert_eq("get_IsWordForming(0x0027, &fRet) HRESULT", S_OK, hr);
			unitpp::assert_true("Apostrophe should not be word-forming", !fRet);

			hr = m_qpropeng->get_IsWordForming(0x002D, &fRet); // Hyphen-minus
			unitpp::assert_eq("get_IsWordForming(0x002D, &fRet) HRESULT", S_OK, hr);
			unitpp::assert_true("Hyphen-minus should not be word-forming", !fRet);
		}

	public:
		TestLgIcuCharPropEngine();
		virtual void SuiteSetup()
		{
			LgIcuCharPropEngine::CreateCom(NULL,
				IID_ILgCharacterPropertyEngine, (void **)&m_qpropeng);
		}
		virtual void SuiteTeardown()
		{
			m_qpropeng.Clear();
		}
	};
}

#endif /*TESTLGICUCHARPROPENGINE_H_INCLUDED*/

// Local Variables:
// mode:C++
// compile-command:"cmd.exe /e:4096 /c c:\\FW\\Bin\\mklg-tst.bat DONTRUN"
// End: (These 4 lines are useful to Steve McConnel.)

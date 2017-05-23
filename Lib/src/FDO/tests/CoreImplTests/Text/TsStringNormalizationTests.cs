// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using SIL.CoreImpl.KernelInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl.Text
{
	class TsStringNormalizationTests
	{
		private const int EnglishWS = 1;
		private const int SpanishWS = 2;

		#region Constants for diacritics and combined characters

		private const string COMBINING_LEFT_HALF_RING_BELOW = "\u031C"; // not involved in any compositions; cc 220.
		private const string BREVE = "\u02D8"; // compatibility decomposition to 0020 0306
		private const string SPACE = "\u0020";

		private const string COMBINING_GRAVE_ACCENT = "\u0300";        // cc 230
		private const string COMBINING_CIRCUMFLEX_ACCENT = "\u0302";   // cc 230
		private const string COMBINING_TILDE = "\u0303";               // cc 230
		private const string COMBINING_MACRON = "\u0304";              // cc 230
		private const string COMBINING_BREVE = "\u0306";               // cc 230
		private const string COMBINING_DOT_ABOVE = "\u0307";           // cc 230
		private const string COMBINING_DIAERESIS = "\u0308";           // cc 230
		private const string COMBINING_DOUBLE_ACUTE_ACCENT = "\u030B"; // cc 230
		private const string COMBINING_INVERTED_BREVE = "\u0311";      // cc 230
		private const string COMBINING_OVERLINE = "\u0305";            // not involved in any compositions with characters; cc 230
		private const string COMBINING_GRAVE_ACCENT_BELOW = "\u0316";  // cc 220
		private const string COMBINING_ACUTE_ACCENT_BELOW = "\u0317";  // cc 220
		private const string COMBINING_LEFT_TACK_BELOW = "\u0318";     // cc 220
		private const string COMBINING_DOWN_TACK_BELOW = "\u031E";     // cc 220
		private const string COMBINING_MINUS_SIGN_BELOW = "\u0320";    // cc 220
		private const string COMBINING_DOT_BELOW = "\u0323";           // cc 220
		private const string COMBINING_RING_BELOW = "\u0325";          // cc 220
		private const string COMBINING_TILDE_BELOW = "\u0330";         // cc 220
		private const string COMBINING_SQUARE_BELOW = "\u033B";        // cc 220
		private const string COMBINING_SEAGULL_BELOW = "\u033C";       // cc 220
		private const string A_WITH_DIAERESIS_AND_MACRON = "\u01DE";   // decomposes to 00C4 0304 and then to 0041 0308 0304
		private const string A_WITH_DIAERESIS  = "\u00C4";             // decomposes to 0041 0308
		private const string a_WITH_DIAERESIS  = "\u00E4";             // decomposes to 0061 0308.
		private const string a_WITH_DOT_BELOW  = "\u1EA1";             // decomposes to 0061 0323
		private const string a_WITH_DOT_ABOVE  = "\u0227";             // composition of a COMBINING_DOT_ABOVE
		private const string o_WITH_DOT_ABOVE  = "\u022F";             // composition of o COMBINING_DOT_ABOVE
		private const string o_WITH_CIRCUMFLEX = "\u00F4";             // composition of o COMBINING_CIRCUMFLEX_ACCENT
		private const string o_WITH_DIAERESIS  = "\u00F6";             // composition of o COMBINING_DIAERESIS
		private const string e_WITH_GRAVE = "\u00E8";                  // composition of e COMBINING_GRAVE_ACCENT

		private const string MUSICAL_SYMBOL_MINIMA = "\uD834\uDDBB"; // 1D1BB decomposes to 1D1B9 1D165
		private const string MUSICAL_SYMBOL_SEMIBREVIS_WHITE = "\uD834\uDDB9"; // 1D1B9 is a base character (cc 0)
		private const string MUSICAL_SYMBOL_COMBINING_STEM = "\uD834\uDD65"; // 1D165 has cc 216
		private const string MUSICAL_SYMBOL_QUARTER_NOTE = "\uD834\uDD5F"; // 1D15F decomposes to 1D158 1D165
		private const string MUSICAL_SYMBOL_EIGHTH_NOTE = "\uD834\uDD60"; // 1D160 decomposes to 1D15F 1D16E and then to 1D158 1D165 1D16E
		private const string MUSICAL_SYMBOL_NOTEHEAD_BLACK = "\uD834\uDD58"; // 1D158 is a base character (cc 0)
		private const string MUSICAL_SYMBOL_COMBINING_STACCATO = "\uD834\uDD7C"; // 1D17C has cc 220
		private const string MUSICAL_SYMBOL_COMBINING_FLAG_1 = "\uD834\uDD6E"; // 1D16E has cc 216

		#endregion

		private const string inputForBasicNormalizationTests =
			"abc" +
			A_WITH_DIAERESIS_AND_MACRON +
			"A" + COMBINING_DIAERESIS + COMBINING_MACRON +
			"C" + COMBINING_OVERLINE + COMBINING_LEFT_HALF_RING_BELOW +
			"XYZ" + BREVE +
			"GAP" + SPACE + COMBINING_BREVE +
			"QED" + MUSICAL_SYMBOL_MINIMA;

		// NFC should compose the "A" + COMBINING_DIAERESIS + COMBINING_MACRON sequence
		private const string expectedBasicNormalizationResultNFC =
			"abc" +
			A_WITH_DIAERESIS_AND_MACRON +
			A_WITH_DIAERESIS_AND_MACRON +
			"C" + COMBINING_LEFT_HALF_RING_BELOW + COMBINING_OVERLINE +
			"XYZ" + BREVE +
			"GAP" + SPACE + COMBINING_BREVE +
			"QED" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + MUSICAL_SYMBOL_COMBINING_STEM;

		// NFKC should be same as NFC plus decompose BREVE (This is surprising, but NFKC
		// will DEcompose compatibility equivalents while composing canonical ones.)
		private const string expectedBasicNormalizationResultNFKC =
			"abc" +
			A_WITH_DIAERESIS_AND_MACRON +
			A_WITH_DIAERESIS_AND_MACRON +
			"C" + COMBINING_LEFT_HALF_RING_BELOW + COMBINING_OVERLINE +
			"XYZ" + SPACE + COMBINING_BREVE +
			"GAP" + SPACE + COMBINING_BREVE +
			"QED" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + MUSICAL_SYMBOL_COMBINING_STEM;

		// NFSC should be same as NFC
		private const string expectedBasicNormalizationResultNFSC = expectedBasicNormalizationResultNFC;

		// NFD should decompose A_WITH_DIAERESIS_AND_MACRON into A plus two combining diacritics
		private const string expectedBasicNormalizationResultNFD =
			"abc" +
			"A" + COMBINING_DIAERESIS + COMBINING_MACRON +
			"A" + COMBINING_DIAERESIS + COMBINING_MACRON +
			"C" + COMBINING_LEFT_HALF_RING_BELOW + COMBINING_OVERLINE +
			"XYZ" + BREVE +
			"GAP" + SPACE + COMBINING_BREVE +
			"QED" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + MUSICAL_SYMBOL_COMBINING_STEM;

		// NFKD should be NFD plus one extra decomposition: BREVE into space plus COMBINING_BREVE
		private const string expectedBasicNormalizationResultNFKD =
			"abc" +
			"A" + COMBINING_DIAERESIS + COMBINING_MACRON +
			"A" + COMBINING_DIAERESIS + COMBINING_MACRON +
			"C" + COMBINING_LEFT_HALF_RING_BELOW + COMBINING_OVERLINE +
			"XYZ" + SPACE + COMBINING_BREVE +
			"GAP" + SPACE + COMBINING_BREVE +
			"QED" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + MUSICAL_SYMBOL_COMBINING_STEM;

		[TestCase(FwNormalizationMode.knmNFC,  expectedBasicNormalizationResultNFC,  5)]
		[TestCase(FwNormalizationMode.knmNFKC, expectedBasicNormalizationResultNFKC, 5)]
		[TestCase(FwNormalizationMode.knmNFSC, expectedBasicNormalizationResultNFSC, 5)]
		[TestCase(FwNormalizationMode.knmNFD,  expectedBasicNormalizationResultNFD,  9)]
		[TestCase(FwNormalizationMode.knmNFKD, expectedBasicNormalizationResultNFKD, 9)]
		public void get_NormalizedForm_SimpleCases_WorkAsExpected(FwNormalizationMode nm, string expectedNormalizedText, int expectedStartIndexOfSecondRun)
		{
			TsString oneRunInput = new TsString(inputForBasicNormalizationTests, EnglishWS);
			ITsString actual = oneRunInput.get_NormalizedForm(nm);
			Assert.That(actual.Text, Is.EqualTo(expectedNormalizedText));
			Assert.That(((TsString)actual).IsAlreadyNormalized(nm), Is.True);
			Assert.That(actual.get_IsNormalizedForm(nm), Is.True);

			// Make second run starting at the capital C
			ITsStrBldr builder = oneRunInput.GetBldr();
			builder.SetIntPropValues(7, oneRunInput.Length, (int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			ITsString twoRunInput = builder.GetString();
			actual = twoRunInput.get_NormalizedForm(nm);
			Assert.That(actual.get_MinOfRun(1), Is.EqualTo(expectedStartIndexOfSecondRun));
			// This run does not split any diacritic clusters, so it should not change any normalization results (even NFSC)
			Assert.That(actual.Text, Is.EqualTo(expectedNormalizedText));
		}

		[TestCase(FwNormalizationMode.knmNFC)]
		[TestCase(FwNormalizationMode.knmNFKC)]
		[TestCase(FwNormalizationMode.knmNFSC)]
		[TestCase(FwNormalizationMode.knmNFD)]
		[TestCase(FwNormalizationMode.knmNFKD)]
		public void get_NormalizedForm_EmptyStrings_AreNormalizedInAnyForm(FwNormalizationMode nm)
		{
			TsString emptyString = new TsString("", EnglishWS);
			Assert.That(emptyString.get_IsNormalizedForm(nm), Is.True);
		}

		[TestCase(FwNormalizationMode.knmNFC)]
		[TestCase(FwNormalizationMode.knmNFKC)]
		[TestCase(FwNormalizationMode.knmNFSC)]
		[TestCase(FwNormalizationMode.knmNFD)]
		[TestCase(FwNormalizationMode.knmNFKD)]
		public void get_NormalizedForm_EmptyStrings_AfterCheckingNormalizationHaveAlreadyNormalizedFlagSet(FwNormalizationMode nm)
		{
			TsString emptyString = new TsString("", EnglishWS);
			Assert.That(emptyString.get_IsNormalizedForm(nm), Is.True);
			Assert.That(emptyString.IsAlreadyNormalized(nm), Is.True);
		}

		[TestCase(FwNormalizationMode.knmNFC)]
		[TestCase(FwNormalizationMode.knmNFKC)]
		[TestCase(FwNormalizationMode.knmNFSC)]
		[TestCase(FwNormalizationMode.knmNFD)]
		[TestCase(FwNormalizationMode.knmNFKD)]
		public void get_NormalizedForm_EmptyStrings_NormalizedFormIsSameObjectAsInput(FwNormalizationMode nm)
		{
			TsString emptyString = new TsString("", EnglishWS);
			TsString actual = (TsString)emptyString.get_NormalizedForm(nm);
			Assert.That(actual, Is.SameAs(emptyString));
		}

		[TestCase(FwNormalizationMode.knmNFC)]
		[TestCase(FwNormalizationMode.knmNFKC)]
		[TestCase(FwNormalizationMode.knmNFSC)]
		[TestCase(FwNormalizationMode.knmNFD)]
		[TestCase(FwNormalizationMode.knmNFKD)]
		public void get_NormalizedForm_EmptyStrings_AfterNormalizationHaveAlreadyNormalizedFlagSet(FwNormalizationMode nm)
		{
			TsString emptyString = new TsString("", EnglishWS);
			emptyString.get_NormalizedForm(nm); // Don't need result for this test
			Assert.That(emptyString.IsAlreadyNormalized(nm), Is.True);
		}

		[TestCase(FwNormalizationMode.knmNFC)]
		[TestCase(FwNormalizationMode.knmNFKC)]
		[TestCase(FwNormalizationMode.knmNFSC)]
		[TestCase(FwNormalizationMode.knmNFD)]
		[TestCase(FwNormalizationMode.knmNFKD)]
		public void get_NormalizedForm_NullStrings_AreNormalizedInAnyForm(FwNormalizationMode nm)
		{
			TsString nullString = new TsString(null, EnglishWS);
			Assert.That(nullString.get_IsNormalizedForm(nm), Is.True);
		}

		[TestCase(FwNormalizationMode.knmNFC)]
		[TestCase(FwNormalizationMode.knmNFKC)]
		[TestCase(FwNormalizationMode.knmNFSC)]
		[TestCase(FwNormalizationMode.knmNFD)]
		[TestCase(FwNormalizationMode.knmNFKD)]
		public void get_NormalizedForm_NullStrings_AfterCheckingNormalizationHaveAlreadyNormalizedFlagSet(FwNormalizationMode nm)
		{
			TsString nullString = new TsString("", EnglishWS);
			Assert.That(nullString.get_IsNormalizedForm(nm), Is.True);
			Assert.That(nullString.IsAlreadyNormalized(nm), Is.True);
		}

		[TestCase(FwNormalizationMode.knmNFC)]
		[TestCase(FwNormalizationMode.knmNFKC)]
		[TestCase(FwNormalizationMode.knmNFSC)]
		[TestCase(FwNormalizationMode.knmNFD)]
		[TestCase(FwNormalizationMode.knmNFKD)]
		public void get_NormalizedForm_NullStrings_NormalizedFormIsSameObjectAsInput(FwNormalizationMode nm)
		{
			TsString nullString = new TsString(null, EnglishWS);
			TsString actual = (TsString)nullString.get_NormalizedForm(nm);
			Assert.That(actual, Is.SameAs(nullString));
		}

		[TestCase(FwNormalizationMode.knmNFC)]
		[TestCase(FwNormalizationMode.knmNFKC)]
		[TestCase(FwNormalizationMode.knmNFSC)]
		[TestCase(FwNormalizationMode.knmNFD)]
		[TestCase(FwNormalizationMode.knmNFKD)]
		public void get_NormalizedForm_NullStrings_AfterNormalizationHaveAlreadyNormalizedFlagSet(FwNormalizationMode nm)
		{
			TsString nullString = new TsString(null, EnglishWS);
			nullString.get_NormalizedForm(nm); // Don't need result for this test
			Assert.That(nullString.IsAlreadyNormalized(nm), Is.True);
		}

		[TestCase(FwNormalizationMode.knmNFC,  A_WITH_DIAERESIS)]
		[TestCase(FwNormalizationMode.knmNFKC, A_WITH_DIAERESIS)]
		[TestCase(FwNormalizationMode.knmNFSC, "A" + COMBINING_DIAERESIS)]
		[TestCase(FwNormalizationMode.knmNFD,  "A" + COMBINING_DIAERESIS)]
		[TestCase(FwNormalizationMode.knmNFKD, "A" + COMBINING_DIAERESIS)]
		public void get_NormalizedForm_SplitRuns_ShouldVaryBehaviorBetweenNFCAndNFSC(FwNormalizationMode nm, string expected)
		{
			// Setup
			string input = "A" + COMBINING_DIAERESIS;
			var intProps1 = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, EnglishWS)},
				{(int) FwTextPropType.ktptBold, new TsIntPropValue((int) FwTextPropVar.ktpvEnum, (int) FwTextToggleVal.kttvForceOn)}
			};
			var intProps2 = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, SpanishWS)},
				{(int) FwTextPropType.ktptUnderline, new TsIntPropValue((int) FwTextPropVar.ktpvEnum, (int) FwTextToggleVal.kttvForceOn)}
			};
			var runs = new List<TsRun>();
			runs.Add(new TsRun(1, new TsTextProps(intProps1, null)));
			runs.Add(new TsRun(input.Length, new TsTextProps(intProps2, null)));
			var tsInput = new TsString(input, runs);

			// Exercise
			ITsString actual = tsInput.get_NormalizedForm(nm);
			Assert.That(actual.Text, Is.EqualTo(expected));
			Assert.That(actual.get_IsNormalizedForm(nm), Is.True);
		}

		[Test]
		public void get_IsNormalizedForm_SplitRuns_NFSCIsNotNFC()
		{
			// Setup
			string input = "A" + COMBINING_DIAERESIS;
			var intProps1 = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, EnglishWS)},
				{(int) FwTextPropType.ktptBold, new TsIntPropValue((int) FwTextPropVar.ktpvEnum, (int) FwTextToggleVal.kttvForceOn)}
			};
			var intProps2 = new Dictionary<int, TsIntPropValue>
			{
				{(int) FwTextPropType.ktptWs, new TsIntPropValue((int) FwTextPropVar.ktpvDefault, SpanishWS)},
				{(int) FwTextPropType.ktptUnderline, new TsIntPropValue((int) FwTextPropVar.ktpvEnum, (int) FwTextToggleVal.kttvForceOn)}
			};
			var runs = new List<TsRun>();
			runs.Add(new TsRun(1, new TsTextProps(intProps1, null)));
			runs.Add(new TsRun(input.Length, new TsTextProps(intProps2, null)));
			var tsInput = new TsString(input, runs);

			// Before calling get_IsNormalizedForm once, we did NOT know that this string was valid NFSC
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFSC), Is.False);

			// Exercise
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFSC), Is.True);

			// We've set the NFSC flag, but NOT the NFKC or NFC flags -- because in some cases (like this one!),
			// a string can be NFSC but not NFC or NFKC.
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFSC), Is.True);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFKC), Is.False);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFC), Is.False);
			// And indeed, this string is NOT normalized under either NFC or NFKC.
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFC), Is.False);
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFKC), Is.False);
		}

		[Test]
		public void get_IsNormalizedForm_SingleRun_NFSCIsAlsoNFCButNotNecessarilyNKFC()
		{
			TsString tsInput = new TsString(A_WITH_DIAERESIS, EnglishWS);
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFSC), Is.True);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFSC), Is.True);
			// We have not set the NFKC flag automatically...
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFKC), Is.False);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFC), Is.True);
			// ... even though it turns out that the string was indeed NFKC as well.
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFKC), Is.True);
			// And now the IsAlreadyNormalized flag is set, so we will go through the shortcut path next time
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFKC), Is.True);
		}

		[Test]
		public void get_IsNormalizedForm_SingleRun_NFCIsAlsoNFSCButNotNecessarilyNFKC()
		{
			TsString tsInput = new TsString(A_WITH_DIAERESIS, EnglishWS);
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFC), Is.True);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFSC), Is.True);
			// We have not set the NFKC flag automatically...
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFKC), Is.False);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFC), Is.True);
			// ... even though it turns out that the string was indeed NFKC as well.
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFKC), Is.True);
			// And now the IsAlreadyNormalized flag is set, so we will go through the shortcut path next time
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFKC), Is.True);
		}

		[Test]
		public void get_IsNormalizedForm_SingleRun_NFKCIsAlsoNFSCAndNFC()
		{
			TsString tsInput = new TsString(A_WITH_DIAERESIS, EnglishWS);
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFKC), Is.True);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFSC), Is.True);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFKC), Is.True);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFC), Is.True);
		}

		[Test]
		public void get_IsNormalizedForm_SingleRun_NFKDIsAlsoNFD()
		{
			TsString tsInput = new TsString("A" + COMBINING_DIAERESIS, EnglishWS);
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFKD), Is.True);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFKD), Is.True);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFD), Is.True);
		}

		[Test]
		public void get_IsNormalizedForm_SingleRun_NFDIsNotNecessarilyNFKD()
		{
			TsString tsInput = new TsString("A" + COMBINING_DIAERESIS, EnglishWS);
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFD), Is.True);
			// We have not set the NFKD flag automatically...
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFKD), Is.False);
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFD), Is.True);
			// ... even though it turns out that the string was indeed NFKD as well.
			Assert.That(tsInput.get_IsNormalizedForm(FwNormalizationMode.knmNFKD), Is.True);
			// And now the IsAlreadyNormalized flag is set, so we will go through the shortcut path next time
			Assert.That(tsInput.IsAlreadyNormalized(FwNormalizationMode.knmNFKD), Is.True);
		}

		//--------------------------------------------------------------------------------------
		//    Test the tricky normalization case where normalization re-orders diacritics,
		//    and the re-ordered diacritics were in different runs.
		//--------------------------------------------------------------------------------------

		private const string expectedComposedTextForReorderingTests =
			a_WITH_DOT_BELOW + COMBINING_DIAERESIS + a_WITH_DOT_BELOW + COMBINING_DIAERESIS;

		private const string expectedDecomposedTextForReorderingTests =
			"a" + COMBINING_DOT_BELOW + COMBINING_DIAERESIS + "a" + COMBINING_DOT_BELOW + COMBINING_DIAERESIS;

		[TestCase(FwNormalizationMode.knmNFC,  2, 2, 4, expectedComposedTextForReorderingTests)] // NFC: expand, reorder, recombine
		[TestCase(FwNormalizationMode.knmNFKC, 2, 2, 4, expectedComposedTextForReorderingTests)] // NFKC: same as NFC
		[TestCase(FwNormalizationMode.knmNFSC, 6, 1, 2, expectedDecomposedTextForReorderingTests)] // NFSC: expand, reorder, compose is blocked by run boundaries
		[TestCase(FwNormalizationMode.knmNFD,  6, 1, 2, expectedDecomposedTextForReorderingTests)] // NFD: decompose a_WITH_DIAERESIS, reorder both sequences
		[TestCase(FwNormalizationMode.knmNFKD, 6, 1, 2, expectedDecomposedTextForReorderingTests)] // NFKD: same as NFD
		public void get_NormalizedForm_ReorderingDiacriticsAcrossRuns_ShouldKeepCharacterPropertiesIntact(
			FwNormalizationMode nm, int expectedRunCount, int expectedSecondRunMin, int expectedSecondRunLim, string expectedText)
		{
			string input = "a" + COMBINING_DIAERESIS + COMBINING_DOT_BELOW + a_WITH_DIAERESIS + COMBINING_DOT_BELOW;
			var temp = new TsString(input, EnglishWS);
			var builder = temp.GetBldr();
			// Make three runs, with the underdot having different props from the a and the diaeresis in each case.
			builder.SetIntPropValues(2, 4, (int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			var tsInput = builder.GetString();

			Assert.That(tsInput.get_IsNormalizedForm(nm), Is.False);
			var actual = tsInput.get_NormalizedForm(nm);
			Assert.That(actual.Text, Is.EqualTo(expectedText));
			Assert.That(actual.RunCount, Is.EqualTo(expectedRunCount));
			// Second run is a good way to test that all the expected runs are in the right place
			Assert.That(actual.get_MinOfRun(1), Is.EqualTo(expectedSecondRunMin));
			Assert.That(actual.get_LimOfRun(1), Is.EqualTo(expectedSecondRunLim));
			// For all cases, first run of output should be normal, just like first run of input was
			// For all cases, second run of output should be bold, just like second run of input was
			var normalProps = tsInput.get_Properties(0);
			var boldProps = tsInput.get_Properties(1);
			Assert.That(actual.get_Properties(0), Is.EqualTo(normalProps));
			Assert.That(actual.get_Properties(1), Is.EqualTo(boldProps));
			Assert.That(actual.get_PropertiesAt(0), Is.EqualTo(normalProps));
			Assert.That(actual.get_PropertiesAt(expectedSecondRunMin), Is.EqualTo(boldProps));
			Assert.That(actual.get_PropertiesAt(expectedSecondRunLim - 1), Is.EqualTo(boldProps));
			if (expectedRunCount > 2)
			{
				Assert.That(actual.get_PropertiesAt(expectedSecondRunLim), Is.EqualTo(normalProps));
			}
		}

		//--------------------------------------------------------------------------------------
		//    Test for the case where an underdot re-orders around several different runs.
		//--------------------------------------------------------------------------------------

		private const string expectedComposedTextForUnderdotTests =
			a_WITH_DOT_BELOW + COMBINING_DIAERESIS + COMBINING_DIAERESIS + COMBINING_DIAERESIS +
			a_WITH_DOT_BELOW + COMBINING_DIAERESIS;

		private const string expectedDecomposedTextForUnderdotTests =
			"a" + COMBINING_DOT_BELOW + COMBINING_DIAERESIS + COMBINING_DIAERESIS + COMBINING_DIAERESIS +
			"a" + COMBINING_DOT_BELOW + COMBINING_DIAERESIS;

		[TestCase(FwNormalizationMode.knmNFC,  2, 4, 6, expectedComposedTextForUnderdotTests)] // NFC: expand, reorder, recombine
		[TestCase(FwNormalizationMode.knmNFKC, 2, 4, 6, expectedComposedTextForUnderdotTests)] // NFKC: same as NFC
		[TestCase(FwNormalizationMode.knmNFSC, 8, 1, 2, expectedDecomposedTextForUnderdotTests)] // NFSC: expand, reorder, compose is blocked by run boundaries
		[TestCase(FwNormalizationMode.knmNFD,  8, 1, 2, expectedDecomposedTextForUnderdotTests)] // NFD: decompose a_WITH_DIAERESIS, reorder both sequences
		[TestCase(FwNormalizationMode.knmNFKD, 8, 1, 2, expectedDecomposedTextForUnderdotTests)] // NFKD: same as NFD
		public void get_NormalizedForm_MultipleInterveningRuns_CombinesOrReordersCorrectly(
			FwNormalizationMode nm, int expectedRunCount, int expectedSecondRunMin, int expectedSecondRunLim, string expectedText)
		{
			string input = "a" + COMBINING_DIAERESIS + COMBINING_DIAERESIS + COMBINING_DIAERESIS + COMBINING_DOT_BELOW + a_WITH_DIAERESIS + COMBINING_DOT_BELOW;
			var temp = new TsString(input, EnglishWS);
			var builder = temp.GetBldr();
			// Make five total runs in the builder, so that the first underdot will have to reorder "past" multiple intervening runs
			builder.SetIntPropValues(1, 2, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrRed);
			builder.SetIntPropValues(2, 3, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrGreen);
			builder.SetIntPropValues(3, 4, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlue);
			builder.SetIntPropValues(4, 6, (int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			var tsInput = builder.GetString();

			Assert.That(tsInput.get_IsNormalizedForm(nm), Is.False);
			var actual = tsInput.get_NormalizedForm(nm);
			Assert.That(actual.Text, Is.EqualTo(expectedText));
			Assert.That(actual.RunCount, Is.EqualTo(expectedRunCount));
			// Second run is a good way to test that all the expected runs are in the right place
			Assert.That(actual.get_MinOfRun(1), Is.EqualTo(expectedSecondRunMin));
			Assert.That(actual.get_LimOfRun(1), Is.EqualTo(expectedSecondRunLim));
			// For all cases, output should have runs in order normal, bold
			// For all cases except NFC and NFKC, the red, green and blue runs should be preserved
			var normalProps = tsInput.get_Properties(0);
			var redProps = tsInput.get_Properties(1);
			var greenProps = tsInput.get_Properties(2);
			var blueProps = tsInput.get_Properties(3);
			var boldProps = tsInput.get_Properties(4);
			Assert.That(actual.get_PropertiesAt(0), Is.EqualTo(normalProps));
			Assert.That(actual.get_Properties(0), Is.EqualTo(normalProps));
			Assert.That(actual.get_Properties(1), Is.EqualTo(boldProps));
			if (expectedRunCount > 2)  // NFC and NFKC will remove the red/green/blue runs
			{
				Assert.That(actual.get_Properties(2), Is.EqualTo(redProps));
				Assert.That(actual.get_Properties(3), Is.EqualTo(greenProps));
				Assert.That(actual.get_Properties(4), Is.EqualTo(blueProps));
			}
		}

		//--------------------------------------------------------------------------------------
		//    A further special case for NFSC normalization is where we can compress part of
		//    a character sequence, but because of run boundaries can't compress all of it.
		//--------------------------------------------------------------------------------------

		[TestCase(FwNormalizationMode.knmNFC,  1, 1, A_WITH_DIAERESIS_AND_MACRON)] // NFC: all the way to one character
		[TestCase(FwNormalizationMode.knmNFKC, 1, 1, A_WITH_DIAERESIS_AND_MACRON)] // NFKC: same as NFC
		[TestCase(FwNormalizationMode.knmNFSC, 2, 1, A_WITH_DIAERESIS + COMBINING_MACRON)] // NFSC: expand, reorder, compose is blocked by run boundaries
		[TestCase(FwNormalizationMode.knmNFD,  2, 2, "A" + COMBINING_DIAERESIS + COMBINING_MACRON)] // NFD: no change
		[TestCase(FwNormalizationMode.knmNFKD, 2, 2, "A" + COMBINING_DIAERESIS + COMBINING_MACRON)] // NFKD: no change
		public void get_NormalizedForm_NFSC_IsBlockedByRunBoundaries(FwNormalizationMode nm, int expectedRunCount, int expectedFirstRunLim, string expectedText)
		{
			// The macron will be in a different run; so while the first two compression
			// schemes produce a single character, NFSC should produce 2.
			string input = "A" + COMBINING_DIAERESIS + COMBINING_MACRON;
			var temp = new TsString(input, EnglishWS);
			var builder = temp.GetBldr();
			// Make two runs, changing the last character
			builder.SetIntPropValues(2, 3, (int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			var tsInput = builder.GetString();

			var actual = tsInput.get_NormalizedForm(nm);
			Assert.That(actual.Text, Is.EqualTo(expectedText));
			Assert.That(actual.RunCount, Is.EqualTo(expectedRunCount));
			var normalProps = tsInput.get_Properties(0);
			var boldProps = tsInput.get_Properties(1);
			Assert.That(actual.get_Properties(0), Is.EqualTo(normalProps));
			if (expectedRunCount > 1)
			{
				Assert.That(actual.get_LimOfRun(0), Is.EqualTo(expectedFirstRunLim));
				Assert.That(actual.get_Properties(1), Is.EqualTo(boldProps));
			}
		}

		//--------------------------------------------------------------------------------------
		//    Test that strings with multiple stacked diacritics work properly under both
		//    styled and unstyled composition, and that NfdAndFixOffsets produces the right
		//    offsets for composed and decomposed characters.
		//--------------------------------------------------------------------------------------

		private const string stackedDiacriticsInput = "Stacked diacritics: W" +
			"e" + COMBINING_DOUBLE_ACUTE_ACCENT + COMBINING_RING_BELOW + COMBINING_GRAVE_ACCENT_BELOW +
			"lc" + COMBINING_LEFT_TACK_BELOW + COMBINING_MINUS_SIGN_BELOW +
			"o" + COMBINING_CIRCUMFLEX_ACCENT +
			"m" + COMBINING_SEAGULL_BELOW + COMBINING_GRAVE_ACCENT + COMBINING_DIAERESIS + COMBINING_MACRON +
			"e" + COMBINING_GRAVE_ACCENT +
			" to" + COMBINING_DIAERESIS + COMBINING_CIRCUMFLEX_ACCENT +
			" Wo" + COMBINING_DOT_ABOVE + COMBINING_INVERTED_BREVE +
			"r" + COMBINING_SQUARE_BELOW +
			"l" + COMBINING_TILDE +
			"d" + COMBINING_DOWN_TACK_BELOW + COMBINING_TILDE_BELOW +
			"Pa" + COMBINING_DOT_ABOVE + COMBINING_OVERLINE + COMBINING_DOUBLE_ACUTE_ACCENT +
			"d" + COMBINING_ACUTE_ACCENT_BELOW +
			"!";

		private const string stackedDiacriticsExpectedNFSCSingleRun = "Stacked diacritics: W" +
			"e" + COMBINING_RING_BELOW + COMBINING_GRAVE_ACCENT_BELOW + COMBINING_DOUBLE_ACUTE_ACCENT +
			"l" +
			"c" + COMBINING_LEFT_TACK_BELOW + COMBINING_MINUS_SIGN_BELOW +
			o_WITH_CIRCUMFLEX +
			"m" + COMBINING_SEAGULL_BELOW + COMBINING_GRAVE_ACCENT + COMBINING_DIAERESIS + COMBINING_MACRON +
			e_WITH_GRAVE +
			" t" + o_WITH_DIAERESIS + COMBINING_CIRCUMFLEX_ACCENT +
			" W" + o_WITH_DOT_ABOVE + COMBINING_INVERTED_BREVE +
			"r" + COMBINING_SQUARE_BELOW +
			"l" + COMBINING_TILDE +
			"d" + COMBINING_DOWN_TACK_BELOW + COMBINING_TILDE_BELOW +
			"P" +
			a_WITH_DOT_ABOVE + COMBINING_OVERLINE + COMBINING_DOUBLE_ACUTE_ACCENT +
			"d" + COMBINING_ACUTE_ACCENT_BELOW +
			"!";

		private const string stackedDiacriticsExpectedNFSCMultipleRuns = "Stacked diacritics: W" +
			"e" + COMBINING_RING_BELOW + COMBINING_GRAVE_ACCENT_BELOW + COMBINING_DOUBLE_ACUTE_ACCENT +
			"l" +
			"c" + COMBINING_LEFT_TACK_BELOW + COMBINING_MINUS_SIGN_BELOW +
			"o" + COMBINING_CIRCUMFLEX_ACCENT +  // This differs from the single-run expected output
			"m" + COMBINING_SEAGULL_BELOW + COMBINING_GRAVE_ACCENT + COMBINING_DIAERESIS + COMBINING_MACRON +
			e_WITH_GRAVE +
			" t" + o_WITH_DIAERESIS + COMBINING_CIRCUMFLEX_ACCENT +
			" W" + o_WITH_DOT_ABOVE + COMBINING_INVERTED_BREVE +
			"r" + COMBINING_SQUARE_BELOW +
			"l" + COMBINING_TILDE +
			"d" + COMBINING_DOWN_TACK_BELOW + COMBINING_TILDE_BELOW +
			"P" +
			a_WITH_DOT_ABOVE + COMBINING_OVERLINE + COMBINING_DOUBLE_ACUTE_ACCENT +
			"d" + COMBINING_ACUTE_ACCENT_BELOW +
			"!";

		private TsString CreateStackedDiacriticsInput(bool singleRun)
		{
			var temp = new TsString(stackedDiacriticsInput, EnglishWS);
			var builder = new TsStrBldr();
			builder.ReplaceTsString(0, 0, temp);
			builder.SetIntPropValues(0, stackedDiacriticsInput.Length, (int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 20000);
			builder.SetIntPropValues(0, stackedDiacriticsInput.Length, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrGreen);
			if (singleRun)
				return (TsString)builder.GetString();

			// green from 0-22
			builder.SetIntPropValues(22, 23, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrRed);
			builder.SetIntPropValues(23, 24, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, 0x00ff602f);
			// green from 24-30
			builder.SetIntPropValues(30, 31, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlue);
			// green from 31-33
			builder.SetIntPropValues(33, 34, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrRed);
			// green from 34-42
			builder.SetIntPropValues(42, 43, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlack);
			// green from 43-47
			builder.SetIntPropValues(47, 48, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlack);
			// green from 48-51
			builder.SetIntPropValues(51, 52, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlue);
			// green from 52-53
			builder.SetIntPropValues(53, 54, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrRed);
			// green from 54-58
			builder.SetIntPropValues(58, 59, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrRed);
			builder.SetIntPropValues(59, 60, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlack);
			// green from 60-61
			builder.SetIntPropValues(61, 62, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrBlack);
			// green from 62-63

			return (TsString)builder.GetString();
		}

		[Test]
		public void get_NormalizedForm_StackedDiacriticsSingleRun_NFCAndNFSCAreEqual()
		{
			TsString input = CreateStackedDiacriticsInput(singleRun: true);

			var actualNFC = (TsString)input.get_NormalizedForm(FwNormalizationMode.knmNFC);
			var actualNFSC = (TsString)input.get_NormalizedForm(FwNormalizationMode.knmNFSC);
			Assert.That(actualNFC.Text, Is.EqualTo(stackedDiacriticsExpectedNFSCSingleRun));
			Assert.That(actualNFSC.Text, Is.EqualTo(stackedDiacriticsExpectedNFSCSingleRun));
			Assert.That(actualNFC.RunCount, Is.EqualTo(1));
			Assert.That(actualNFSC.RunCount, Is.EqualTo(1));
			// Also check TsString equality, since that compares properties as well as text
			Assert.That(actualNFSC, Is.EqualTo(actualNFC));
		}

		[Test]
		public void get_NormalizedForm_StackedDiacriticsMultipleRuns_NFCAndNFSCDiffer()
		{
			TsString input = CreateStackedDiacriticsInput(singleRun: false);

			var actualNFC = (TsString)input.get_NormalizedForm(FwNormalizationMode.knmNFC);
			var actualNFSC = (TsString)input.get_NormalizedForm(FwNormalizationMode.knmNFSC);
			Assert.That(actualNFC.Text, Is.EqualTo(stackedDiacriticsExpectedNFSCSingleRun));
			Assert.That(actualNFSC.Text, Is.EqualTo(stackedDiacriticsExpectedNFSCMultipleRuns));
			Assert.That(actualNFC.RunCount, Is.EqualTo(1));
			Assert.That(actualNFSC.RunCount, Is.EqualTo(22));
		}

		[Test]
		public void NfdAndFixOffsets_WithStackedDiacriticsThatDoNotOverlapRunBoundaries_FixesOffsetsCorrectly()
		{
			// This test is copied over verbatim from the old C++ unit tests
			string input = "Stacked diacritics: We" + // 0..21
				COMBINING_DOUBLE_ACUTE_ACCENT + COMBINING_RING_BELOW + COMBINING_GRAVE_ACCENT_BELOW + // 22..24
				"lc" + COMBINING_LEFT_TACK_BELOW + COMBINING_MINUS_SIGN_BELOW + // 25..28
				o_WITH_CIRCUMFLEX + "o" + // 29..30 (+1)
				"m" + COMBINING_SEAGULL_BELOW + COMBINING_GRAVE_ACCENT + COMBINING_DIAERESIS + //31..34
				COMBINING_MACRON + //35
				e_WITH_GRAVE + //36 (+1)
				" to" + COMBINING_DIAERESIS + COMBINING_CIRCUMFLEX_ACCENT + //37..41
				" Wo" + COMBINING_DOT_ABOVE + COMBINING_INVERTED_BREVE + //42..46
				"r" + COMBINING_SQUARE_BELOW + //47..48
				"l" + COMBINING_TILDE + //49..50
				"d" + COMBINING_DOWN_TACK_BELOW + COMBINING_TILDE_BELOW + //51..53
				"a" + COMBINING_DIAERESIS + COMBINING_DOT_BELOW + //54..56
				a_WITH_DIAERESIS + COMBINING_DOT_BELOW + //57..58 (+1)
				"d" + COMBINING_ACUTE_ACCENT_BELOW + //59..60
				"!"; //61

			var temp = new TsString(input, EnglishWS);
			var builder = temp.GetBldr();
			// Make two runs, to make things interesting
			builder.SetIntPropValues(0, 30, (int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 20000);
			builder.SetIntPropValues(30, input.Length, (int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrGreen);
			var tsInput = (TsString)builder.GetString();

			int[] origOffsets = { 30, 33, 49, 57, 0, 21, 29, input.Length - 1, 36, 37, 54, 55, 56, 58, 62 };
			int[] expectedOffsets = { 31, 34, 51, 59, 0, 21, 29, input.Length - 1 + 3, 37, 39, 56, 58, 57, 60, 65 };

			ITsString nfd;
			int[] actualOffsets;
			using (ArrayPtr nativeOffsets = ConvertToNativeArray(origOffsets))
			{
				tsInput.NfdAndFixOffsets(out nfd, nativeOffsets, origOffsets.Length);
				actualOffsets = ConvertToManagedArray(nativeOffsets, origOffsets.Length);
			}

			CollectionAssert.AreEqual(expectedOffsets, actualOffsets);

			// Verify that the characters at the new offsets are still the "same" characters that were at the old offsets
			// "Same" because some offsets pointed to composed characters, and we expect to find the base character at the new offset
			for (int i = 0; i < origOffsets.Length; i++)
			{
				if (origOffsets[i] == input.Length)
					continue;

				int oldOffset = origOffsets[i];
				int newOffset = actualOffsets[i];
				char oldCh = input[oldOffset];
				char newCh = nfd.Text[newOffset];
				char expectedCh = oldCh;
				// Three offsets pointed to composed characters, which should be matched to the base character in the output
				if (oldCh == '\u00e4') // 'ä'
					expectedCh = 'a';
				if (oldCh == '\u00f4') // 'ô'
					expectedCh = 'o';
				if (oldCh == '\u00e8') // 'è'
					expectedCh = 'e';
				Assert.That(newCh, Is.EqualTo(expectedCh),
					String.Format("Old char '{0}' (U+{1:X4}) at {2} should match new char '{3}' (U+{4:X4}) at {5}, but didn't match.",
						oldCh, (int) oldCh, oldOffset, newCh, newOffset, (int) newCh));
			}
		}

		[Test]
		public void NfdAndFixOffsets_WithStackedDiacriticsInMultipleRuns_FixesOffsetsCorrectly()
		{
			TsString input = CreateStackedDiacriticsInput(singleRun: false);

			int[] origOffsets =     { 21, 22, 23, 24, 25 }; // Offsets 26 through end of string are unchanged by this test
			int[] expectedOffsets = { 21, 24, 22, 23, 25 };

			ITsString nfd;
			int[] actualOffsets;
			using (ArrayPtr nativeOffsets = ConvertToNativeArray(origOffsets))
			{
				input.NfdAndFixOffsets(out nfd, nativeOffsets, origOffsets.Length);
				actualOffsets = ConvertToManagedArray(nativeOffsets, origOffsets.Length);
			}
			CollectionAssert.AreEqual(expectedOffsets, actualOffsets);

			// All the new offsets should still point to the same characters that they did before
			for (int i = 0; i < origOffsets.Length; i++)
			{
				int oldOffset = origOffsets[i];
				int newOffset = actualOffsets[i];
				char oldCh = input.Text[oldOffset];
				char newCh =   nfd.Text[newOffset];
				Assert.That(newCh, Is.EqualTo(oldCh),
					String.Format("Old char '{0}' (U+{1:X4}) at {2} should match new char '{3}' (U+{4:X4}) at {5}, but didn't match.",
					oldCh, (int)oldCh, oldOffset, newCh, newOffset, (int)newCh));
			}
		}

		[TestCase(
			MUSICAL_SYMBOL_EIGHTH_NOTE,
			MUSICAL_SYMBOL_NOTEHEAD_BLACK + MUSICAL_SYMBOL_COMBINING_STEM + MUSICAL_SYMBOL_COMBINING_FLAG_1,
			new[] { 0, 1 },
			new[] { 0, 0 })]
		[TestCase(
			MUSICAL_SYMBOL_NOTEHEAD_BLACK + MUSICAL_SYMBOL_COMBINING_STEM + MUSICAL_SYMBOL_COMBINING_FLAG_1,
			MUSICAL_SYMBOL_NOTEHEAD_BLACK + MUSICAL_SYMBOL_COMBINING_STEM + MUSICAL_SYMBOL_COMBINING_FLAG_1,
			new[] { 0, 1, 2, 3, 4, 5 },
			new[] { 0, 0, 2, 2, 4, 4 })]
		[TestCase(
			MUSICAL_SYMBOL_EIGHTH_NOTE + MUSICAL_SYMBOL_COMBINING_STACCATO,
			MUSICAL_SYMBOL_NOTEHEAD_BLACK + MUSICAL_SYMBOL_COMBINING_STEM + MUSICAL_SYMBOL_COMBINING_FLAG_1 + MUSICAL_SYMBOL_COMBINING_STACCATO,
			new[] { 0, 1, 2, 3 },
			new[] { 0, 0, 6, 6 })]
		[TestCase(
			MUSICAL_SYMBOL_NOTEHEAD_BLACK + MUSICAL_SYMBOL_COMBINING_STACCATO + MUSICAL_SYMBOL_COMBINING_STEM + MUSICAL_SYMBOL_COMBINING_FLAG_1,
			MUSICAL_SYMBOL_NOTEHEAD_BLACK + MUSICAL_SYMBOL_COMBINING_STEM + MUSICAL_SYMBOL_COMBINING_FLAG_1 + MUSICAL_SYMBOL_COMBINING_STACCATO,
			new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
			new[] { 0, 0, 6, 6, 2, 2, 4, 4 })]
		public void NfdAndFixOffsets_OffsetsPointingToSecondHalfOfSurrogatePair_AreFixedUpToPointToFirstHalf(
			string input, string expectedNfd, int[] origOffsets, int[] expectedOffsets)
		{
			var tsInput = new TsString(input, EnglishWS);
			ITsString nfd;
			int[] actualOffsets;
			using (ArrayPtr nativeOffsets = ConvertToNativeArray(origOffsets))
			{
				tsInput.NfdAndFixOffsets(out nfd, nativeOffsets, origOffsets.Length);
				actualOffsets = ConvertToManagedArray(nativeOffsets, origOffsets.Length);
			}
			CollectionAssert.AreEqual(expectedOffsets, actualOffsets);
			Assert.That(nfd.Text, Is.EqualTo(expectedNfd));
		}

		private static ArrayPtr ConvertToNativeArray(int[] offsets)
		{
			int ptrSize = Marshal.SizeOf(typeof(IntPtr));
			int intSize = Marshal.SizeOf(typeof(int));
			var nativeArray = new ArrayPtr(offsets.Length * ptrSize);
			for (int i = 0; i < offsets.Length; i++)
			{
				IntPtr offsetPtr = Marshal.AllocCoTaskMem(intSize);
				Marshal.WriteInt32(offsetPtr, offsets[i]);
				Marshal.WriteIntPtr(nativeArray.IntPtr, i * ptrSize, offsetPtr);
			}
			return nativeArray;
		}

		private static int[] ConvertToManagedArray(ArrayPtr nativeOffsets, int len)
		{
			int ptrSize = Marshal.SizeOf(typeof(IntPtr));
			var offsets = new int[len];
			for (int i = 0; i < len; i++)
			{
				IntPtr offsetPtr = Marshal.ReadIntPtr(nativeOffsets.IntPtr, i * ptrSize);
				offsets[i] = Marshal.ReadInt32(offsetPtr);
				Marshal.FreeCoTaskMem(offsetPtr);
			}
			return offsets;
		}
	}
}

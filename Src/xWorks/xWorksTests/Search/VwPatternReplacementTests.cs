// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.XWorks.Search
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	[Category("BulkReplacement")]
	public class VwPatternReplacementTests : MemoryOnlyBackendProviderTestBase
	{
		private int Ws => Cache.DefaultVernWs;

		private IVwPattern MakePattern(string patternText, bool matchCase = false,
			bool matchDiacritics = false, bool matchWholeWord = false)
		{
			var pattern = VwPatternClass.Create();
			pattern.Pattern = TsStringUtils.MakeString(patternText, Ws);
			pattern.MatchCase = matchCase;
			pattern.MatchDiacritics = matchDiacritics;
			pattern.MatchWholeWord = matchWholeWord;
			return pattern;
		}

		private IVwTextSource MakeTextSource(string text, out int length)
		{
			return MakeTextSource(TsStringUtils.MakeString(text, Ws), out length);
		}

		private static IVwTextSource MakeTextSource(ITsString tss, out int length)
		{
			length = tss.Length;
			var textSourceInit = VwStringTextSourceClass.Create();
			textSourceInit.SetString(tss);
			return (IVwTextSource)textSourceInit;
		}

		private static void Find(IVwPattern pattern, IVwTextSource ts, int ichStart, int ichEnd,
			bool forward, out int ichMin, out int ichLim)
		{
			pattern.FindIn(ts, ichStart, ichEnd, forward, out ichMin, out ichLim, null);
		}

		private static void AssertFound(int ichMin, int ichLim, int expectedMin, int expectedLim, string why)
		{
			Assert.That(ichMin, Is.EqualTo(expectedMin), why + " (ichMin)");
			Assert.That(ichLim, Is.EqualTo(expectedLim), why + " (ichLim)");
		}

		private static void AssertNotFound(int ichMin, string why)
		{
			Assert.That(ichMin, Is.LessThan(0), why);
		}

		private sealed class ReplacementMatch
		{
			public ReplacementMatch(int ichMin, int ichLim, ITsString replacement)
			{
				IchMin = ichMin;
				IchLim = ichLim;
				Replacement = replacement;
			}

			public int IchMin { get; }
			public int IchLim { get; }
			public ITsString Replacement { get; }
		}

		/// <summary>Describes one expected replacement match.</summary>
		public sealed class ExpectedReplacementMatch
		{
			internal ExpectedReplacementMatch(int ichMin, int ichLim, string replacementText)
			{
				IchMin = ichMin;
				IchLim = ichLim;
				ReplacementText = replacementText;
			}

			internal int IchMin { get; }
			internal int IchLim { get; }
			internal string ReplacementText { get; }
		}

		private static ExpectedReplacementMatch ExpectedMatch(int ichMin, int ichLim, string replacementText)
		{
			return new ExpectedReplacementMatch(ichMin, ichLim, replacementText);
		}

		private static void AssertReplacementMatches(IList<ReplacementMatch> actual,
			params ExpectedReplacementMatch[] expected)
		{
			Assert.That(actual, Has.Count.EqualTo(expected.Length));
			for (var index = 0; index < expected.Length; index++)
			{
				Assert.That(actual[index].IchMin, Is.EqualTo(expected[index].IchMin), "ichMin at match " + index);
				Assert.That(actual[index].IchLim, Is.EqualTo(expected[index].IchLim), "ichLim at match " + index);
				Assert.That(actual[index].Replacement.Text, Is.EqualTo(expected[index].ReplacementText),
					"replacement text at match " + index);
			}
		}

		private static List<ReplacementMatch> CollectReplacementMatches(IVwPattern pattern,
			IVwTextSource ts, int cch, int ichStartSearch = 0)
		{
			var results = new List<ReplacementMatch>();
			var ichLimLastMatch = -1;
			while (ichStartSearch <= cch)
			{
				Find(pattern, ts, ichStartSearch, cch, true, out var ichMin, out var ichLim);
				if (ichMin < 0)
					break;
				if (ichLim == ichLimLastMatch)
				{
					ichStartSearch = ichLim + 1;
					continue;
				}
				ichLimLastMatch = ichLim;
				results.Add(new ReplacementMatch(ichMin, ichLim, pattern.ReplacementText));
				ichStartSearch = ichLim;
			}
			return results;
		}

		private static void AssertReplaceAllMatchesRepeatedFindIn(IVwPattern pattern,
			IVwTextSource oracleSource, int ichStart, int ichEnd, int? expectedCount = null,
			string expectedText = null)
		{
			var source = oracleSource.GetSubString(0, oracleSource.Length);
			var matches = CollectReplacementMatches(pattern, oracleSource, ichEnd, ichStart);
			var expectedBuilder = source.GetBldr();
			var delta = 0;
			foreach (var match in matches)
			{
				expectedBuilder.ReplaceTsString(match.IchMin + delta, match.IchLim + delta,
					match.Replacement);
				delta += match.Replacement.Length - (match.IchLim - match.IchMin);
			}

			var actual = ((IVwPattern2)pattern).ReplaceAllIn(source, ichStart, ichEnd, null,
				out var actualCount);
			Assert.That(actualCount, Is.EqualTo(matches.Count), "bulk match count");
			if (expectedCount.HasValue)
				Assert.That(actualCount, Is.EqualTo(expectedCount.Value), "characterized match count");
			if (expectedText != null)
				Assert.That(actual.Text, Is.EqualTo(expectedText), "characterized result text");
			Assert.That(TsStringHelper.TsStringsAreEqual(expectedBuilder.GetString(), actual,
				out var differences), Is.True, differences);
		}

		private static IEnumerable<TestCaseData> PlainReplacementCases()
		{
			yield return new TestCaseData("old", "none", "new", new ExpectedReplacementMatch[0])
				.SetName("ReplacementIteration_NoMatches_ReturnsEmptySequence");
			yield return new TestCaseData("old", "old", "new", new[] { ExpectedMatch(0, 3, "new") })
				.SetName("ReplacementIteration_OneMatch_ReturnsOneReplacement");
			yield return new TestCaseData("old", "oldold", "new", new[] { ExpectedMatch(0, 3, "new"), ExpectedMatch(3, 6, "new") })
				.SetName("ReplacementIteration_AdjacentMatches_ReturnsBothReplacements");
			yield return new TestCaseData("old", "old old old", "new", new[]
			{
				ExpectedMatch(0, 3, "new"), ExpectedMatch(4, 7, "new"), ExpectedMatch(8, 11, "new")
			}).SetName("ReplacementIteration_MultipleMatches_ReturnsOrderedReplacements");
			yield return new TestCaseData("aa", "aaa", "x", new[] { ExpectedMatch(0, 2, "x") })
				.SetName("ReplacementIteration_OverlappingCandidates_ReturnsNonOverlappingMatches");
		}

		[TestCaseSource(nameof(PlainReplacementCases))]
		public void ReplacementIteration_PlainTextCases_ReturnExpectedSequence(string patternText, string text,
			string replacementText, ExpectedReplacementMatch[] expected)
		{
			var pattern = MakePattern(patternText);
			pattern.ReplaceWith = TsStringUtils.MakeString(replacementText, Ws);
			var source = MakeTextSource(text, out var length);

			AssertReplacementMatches(CollectReplacementMatches(pattern, source, length), expected);
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		[TestCase("^", "abc", 0, 0)]
		[TestCase("$", "abc", 3, 3)]
		public void ReplacementIteration_ZeroLengthRegularExpression_ReturnsOneMatch(string expression, string text,
			int ichMin, int ichLim)
		{
			var pattern = MakePattern(expression);
			pattern.UseRegularExpressions = true;
			pattern.ReplaceWith = TsStringUtils.MakeString("#", Ws);
			var source = MakeTextSource(text, out var length);

			AssertReplacementMatches(CollectReplacementMatches(pattern, source, length), ExpectedMatch(ichMin, ichLim, "#"));
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		[Test]
		public void ReplacementIteration_ZeroWidthLookaheadAcrossAstralCharacter_ReturnsCurrentSequence()
		{
			var pattern = MakePattern("(?=.)");
			pattern.UseRegularExpressions = true;
			pattern.ReplaceWith = TsStringUtils.MakeString("#", Ws);
			var source = MakeTextSource("\uD83D\uDE00a", out var length);

			AssertReplacementMatches(CollectReplacementMatches(pattern, source, length),
				ExpectedMatch(0, 1, "#"), ExpectedMatch(1, 2, "#"), ExpectedMatch(2, 3, "#"));
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		[Test]
		public void ReplacementIteration_RegularExpressionCapture_ReturnsExpandedReplacementText()
		{
			var pattern = MakePattern("(o|e)(ld)");
			pattern.UseRegularExpressions = true;
			pattern.ReplaceWith = TsStringUtils.MakeString("$2-$1", Ws);
			var source = MakeTextSource("old eld", out var length);

			AssertReplacementMatches(CollectReplacementMatches(pattern, source, length),
				ExpectedMatch(0, 3, "ld-o"), ExpectedMatch(4, 7, "ld-e"));
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		/// <summary>Describes one locale-sensitive replacement case.</summary>
		public sealed class CollationReplacementCase
		{
			internal CollationReplacementCase(string patternText, string text, string locale, string rules,
				bool matchDiacritics, ExpectedReplacementMatch[] expected, bool matchCase = true)
			{
				PatternText = patternText;
				Text = text;
				Locale = locale;
				Rules = rules;
				MatchDiacritics = matchDiacritics;
				Expected = expected;
				MatchCase = matchCase;
			}

			internal string PatternText { get; }
			internal string Text { get; }
			internal string Locale { get; }
			internal string Rules { get; }
			internal bool MatchDiacritics { get; }
			internal ExpectedReplacementMatch[] Expected { get; }
			internal bool MatchCase { get; }
		}

		private static IEnumerable<TestCaseData> CollationReplacementCases()
		{
			yield return new TestCaseData(new CollationReplacementCase("h", "ch", "cs", null, true,
				new ExpectedReplacementMatch[0])).SetName("ReplacementIteration_CzechContraction_DoesNotExposeSecondCharacter");
			yield return new TestCaseData(new CollationReplacementCase("a\u0308", " AE ", "de__PHONEBOOK", null, false,
				new[] { ExpectedMatch(1, 3, "x") }, matchCase: false)).SetName("ReplacementIteration_GermanPhonebookExpansion_ReturnsOneReplacement");
			yield return new TestCaseData(new CollationReplacementCase("ab", "a-b", "de-u-co-phonebk-ka-shifted", null, true,
				new[] { ExpectedMatch(0, 3, "x") })).SetName("ReplacementIteration_ShiftedPunctuation_ReturnsExpandedSpan");
			yield return new TestCaseData(new CollationReplacementCase("a", "b", "root", "&a=b", true,
				new[] { ExpectedMatch(0, 1, "x") })).SetName("ReplacementIteration_CustomTailoring_ReturnsTailoredMatch");
			yield return new TestCaseData(new CollationReplacementCase("caf\u00e9", "cafe\u0301", "root", null, true,
				new[] { ExpectedMatch(0, 5, "x") })).SetName("ReplacementIteration_NfcPatternAgainstNfdText_ReturnsDecomposedSpan");
			yield return new TestCaseData(new CollationReplacementCase("\u1e09", "c\u0301\u0327", "root", null, true,
				new ExpectedReplacementMatch[0])).SetName("ReplacementIteration_NonCanonicalCombiningOrder_ReturnsNoMatch");
			yield return new TestCaseData(new CollationReplacementCase("a", "a\u00ad", "root", null, true,
				new[] { ExpectedMatch(0, 2, "x") })).SetName("ReplacementIteration_SoftHyphen_ReturnsExtendedSpan");
			yield return new TestCaseData(new CollationReplacementCase("a", "a\u034f", "root", null, true,
				new[] { ExpectedMatch(0, 2, "x") })).SetName("ReplacementIteration_CombiningGraphemeJoiner_ReturnsExtendedSpan");
			yield return new TestCaseData(new CollationReplacementCase("a", "a\u200d", "root", null, true,
				new[] { ExpectedMatch(0, 2, "x") })).SetName("ReplacementIteration_ZeroWidthJoiner_ReturnsExtendedSpan");
			yield return new TestCaseData(new CollationReplacementCase("\u0628", "\u0628\u0640", "ar", null, true,
				new ExpectedReplacementMatch[0])).SetName("ReplacementIteration_ArabicTatweel_ReturnsNoMatch");
			yield return new TestCaseData(new CollationReplacementCase("\u1820", "\u1820\u180b", "mn", null, true,
				new[] { ExpectedMatch(0, 2, "x") })).SetName("ReplacementIteration_MongolianFvs1_ReturnsExtendedSpan");
			yield return new TestCaseData(new CollationReplacementCase("\u1100", "\u1100\u1160", "ko", null, true,
				new[] { ExpectedMatch(0, 1, "x") })).SetName("ReplacementIteration_HangulFiller_DoesNotExtendSpan");
			yield return new TestCaseData(new CollationReplacementCase("\u0e01", "\u0e01\u0e4d", "th", null, true,
				new[] { ExpectedMatch(0, 1, "x") })).SetName("ReplacementIteration_ThaiNikhahit_DoesNotExtendSpan");
		}

		[TestCaseSource(nameof(CollationReplacementCases))]
		public void ReplacementIteration_CollationCases_ReturnExpectedSequence(CollationReplacementCase testCase)
		{
			var pattern = MakePattern(testCase.PatternText, matchCase: testCase.MatchCase,
				matchDiacritics: testCase.MatchDiacritics);
			pattern.IcuLocale = testCase.Locale;
			if (testCase.Rules != null)
				pattern.IcuCollatingRules = testCase.Rules;
			pattern.ReplaceWith = TsStringUtils.MakeString("x", Ws);
			var source = MakeTextSource(testCase.Text, out var length);

			AssertReplacementMatches(CollectReplacementMatches(pattern, source, length), testCase.Expected);
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		[Test]
		public void ReplacementIteration_WholeWord_ReturnsOnlyStandaloneOccurrence()
		{
			var pattern = MakePattern("cafe", matchWholeWord: true);
			pattern.ReplaceWith = TsStringUtils.MakeString("x", Ws);
			var source = MakeTextSource("cafeteria cafe", out var length);

			AssertReplacementMatches(CollectReplacementMatches(pattern, source, length), ExpectedMatch(10, 14, "x"));
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		[Test]
		public void ReplacementIteration_WritingSystem_ReturnsOnlyMatchingRun()
		{
			var sourceBuilder = TsStringUtils.MakeString("old old", Ws).GetBldr();
			sourceBuilder.SetIntPropValues(4, 7, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
			var source = MakeTextSource(sourceBuilder.GetString(), out var length);

			var propertyPatternBuilder = TsStringUtils.MakeString("old", Cache.DefaultAnalWs).GetBldr();
			var pattern = MakePattern("old");
			pattern.Pattern = propertyPatternBuilder.GetString();
			pattern.MatchOldWritingSystem = true;
			pattern.ReplaceWith = TsStringUtils.MakeString("x", Ws);

			AssertReplacementMatches(CollectReplacementMatches(pattern, source, length), ExpectedMatch(4, 7, "x"));
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		[Test]
		public void ReplacementIteration_Style_ReturnsOnlyMatchingRun()
		{
			var sourceBuilder = TsStringUtils.MakeString("old old", Ws).GetBldr();
			sourceBuilder.SetStrPropValue(4, 7, (int)FwTextPropType.ktptNamedStyle, "Emphasis");
			var source = MakeTextSource(sourceBuilder.GetString(), out var length);

			var patternBuilder = TsStringUtils.MakeString("old", Ws).GetBldr();
			patternBuilder.SetStrPropValue(0, 3, (int)FwTextPropType.ktptNamedStyle, "Emphasis");
			var pattern = MakePattern("old");
			pattern.Pattern = patternBuilder.GetString();
			pattern.ReplaceWith = TsStringUtils.MakeString("x", Ws);

			AssertReplacementMatches(CollectReplacementMatches(pattern, source, length), ExpectedMatch(4, 7, "x"));
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		[Test]
		public void ReplacementIteration_Tag_ReturnsOnlyMatchingRun()
		{
			var sourceBuilder = TsStringUtils.MakeString("old old", Ws).GetBldr();
			sourceBuilder.SetStrPropValue(4, 7, (int)FwTextPropType.ktptTags, "tag\0value");
			var source = MakeTextSource(sourceBuilder.GetString(), out var length);

			var patternBuilder = TsStringUtils.MakeString("old", Ws).GetBldr();
			patternBuilder.SetStrPropValue(0, 3, (int)FwTextPropType.ktptTags, "tag\0value");
			var pattern = MakePattern("old");
			pattern.Pattern = patternBuilder.GetString();
			pattern.ReplaceWith = TsStringUtils.MakeString("x", Ws);

			AssertReplacementMatches(CollectReplacementMatches(pattern, source, length), ExpectedMatch(4, 7, "x"));
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		[Test]
		public void ReplacementIteration_CaptureReplacement_PreservesReplacementRunProperties()
		{
			var pattern = MakePattern("(old)");
			pattern.UseRegularExpressions = true;
			pattern.MatchOldWritingSystem = true;
			var replacementBuilder = TsStringUtils.MakeString("$1", Cache.DefaultAnalWs).GetBldr();
			replacementBuilder.SetStrPropValue(0, 2, (int)FwTextPropType.ktptNamedStyle, "Replacement Style");
			pattern.ReplaceWith = replacementBuilder.GetString();
			var source = MakeTextSource("old", out var length);

			var matches = CollectReplacementMatches(pattern, source, length);
			AssertReplacementMatches(matches, ExpectedMatch(0, 3, "old"));
			Assert.That(TsStringUtils.GetWsOfRun(matches[0].Replacement, 0), Is.EqualTo(Cache.DefaultAnalWs));
			Assert.That(matches[0].Replacement.get_Properties(0)
				.GetStrPropValue((int)FwTextPropType.ktptNamedStyle), Is.EqualTo("Replacement Style"));
			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length);
		}

		[Test]
		public void ReplaceAllIn_PartialRange_EqualsRepeatedFindIn()
		{
			var pattern = MakePattern("old");
			pattern.ReplaceWith = TsStringUtils.MakeString("new", Ws);
			var source = MakeTextSource("old old old", out var length);

			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 4, length - 1);
		}

		[TestCase(false, 2, "\u0645 \u0645")]
		[TestCase(true, 2, "\u0645 \u0645")]
		public void ReplaceAllIn_ArabicHarakat_EqualsRepeatedFindIn(bool matchDiacritics,
			int expectedCount, string expectedText)
		{
			var pattern = MakePattern("\u0628\u064e", matchDiacritics: matchDiacritics);
			pattern.IcuLocale = "ar";
			var replacementBuilder = TsStringUtils.MakeString("\u0645", Ws).GetBldr();
			replacementBuilder.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle,
				"Arabic Replacement");
			pattern.ReplaceWith = replacementBuilder.GetString();
			var sourceBuilder = TsStringUtils.MakeString("\u0628 \u0628\u064e", Ws).GetBldr();
			sourceBuilder.SetStrPropValue(2, 4, (int)FwTextPropType.ktptNamedStyle,
				"Arabic Source");
			var source = MakeTextSource(sourceBuilder.GetString(), out var length);

			AssertReplaceAllMatchesRepeatedFindIn(pattern, source, 0, length, expectedCount,
				expectedText);
		}

		[Test]
		public void ReplaceAllIn_EmptyTextPattern_ReplacesRunMatchingAllProperties()
		{
			const string style = "Target Style";
			const string tags = "target\0tag";
			var sourceBuilder = TsStringUtils.MakeString("one two three", Ws).GetBldr();
			sourceBuilder.SetIntPropValues(4, 7, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
			sourceBuilder.SetStrPropValue(4, 7, (int)FwTextPropType.ktptNamedStyle, style);
			sourceBuilder.SetStrPropValue(4, 7, (int)FwTextPropType.ktptTags, tags);
			var source = sourceBuilder.GetString();

			var patternBuilder = TsStringUtils.MakeString(string.Empty, Cache.DefaultAnalWs).GetBldr();
			patternBuilder.SetStrPropValue(0, 0, (int)FwTextPropType.ktptNamedStyle, style);
			patternBuilder.SetStrPropValue(0, 0, (int)FwTextPropType.ktptTags, tags);
			var pattern = MakePattern(string.Empty);
			pattern.Pattern = patternBuilder.GetString();
			pattern.MatchOldWritingSystem = true;
			var replacementBuilder = TsStringUtils.MakeString("X", Ws).GetBldr();
			replacementBuilder.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle,
				"Replacement Style");
			pattern.ReplaceWith = replacementBuilder.GetString();

			var actual = ((IVwPattern2)pattern).ReplaceAllIn(source, 0, source.Length, null,
				out var count);

			Assert.That(count, Is.EqualTo(1));
			Assert.That(actual.Text, Is.EqualTo("one X three"));
			Assert.That(actual.RunCount, Is.EqualTo(3));
			Assert.That(actual.get_Properties(1).GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle), Is.EqualTo("Replacement Style"));
			Assert.That(TsStringUtils.GetWsOfRun(actual, 1), Is.EqualTo(Ws));
		}

		[Test]
		public void ReplaceAllIn_PreservesObjectReplacementCharacterAndObjectData()
		{
			const string objectData = "\u0001object-data";
			var sourceBuilder = TsStringUtils.MakeString("old \uFFFC keep", Ws).GetBldr();
			sourceBuilder.SetStrPropValue(4, 5, (int)FwTextPropType.ktptObjData, objectData);
			var source = sourceBuilder.GetString();
			var pattern = MakePattern("old");
			pattern.ReplaceWith = TsStringUtils.MakeString("new", Ws);

			var actual = ((IVwPattern2)pattern).ReplaceAllIn(source, 0, source.Length, null,
				out var count);

			Assert.That(count, Is.EqualTo(1));
			Assert.That(actual.Text, Is.EqualTo("new \uFFFC keep"));
			var objectRun = actual.get_RunAt(4);
			Assert.That(actual.get_RunText(objectRun), Is.EqualTo("\uFFFC"));
			Assert.That(actual.get_Properties(objectRun).GetStrPropValue(
				(int)FwTextPropType.ktptObjData), Is.EqualTo(objectData));
		}

		[Test]
		public void ReplaceAllIn_LeavesTerminalNoMatchState()
		{
			var pattern = MakePattern("old");
			pattern.ReplaceWith = TsStringUtils.MakeString("new", Ws);
			var source = TsStringUtils.MakeString("old old", Ws);

			var actual = ((IVwPattern2)pattern).ReplaceAllIn(source, 0, source.Length, null,
				out var count);

			Assert.That(actual.Text, Is.EqualTo("new new"));
			Assert.That(count, Is.EqualTo(2));
			Assert.Throws<COMException>(() => { var unused = pattern.ReplacementText; });
		}

		[Test]
		public void ReusedPatternAfterLocaleChange_ExtendsShiftedPunctuation()
		{
			var pattern = MakePattern("a", matchCase: true, matchDiacritics: true);
			var source = MakeTextSource("a-", out var length);
			Find(pattern, source, 0, length, true, out var rootMin, out var rootLim);
			AssertFound(rootMin, rootLim, 0, 1, "root collation span");

			pattern.IcuLocale = "de-u-co-phonebk-ka-shifted";
			Find(pattern, source, 0, length, true, out var shiftedMin, out var shiftedLim);

			AssertFound(shiftedMin, shiftedLim, 0, 2, "shifted collation span");
		}
	}
}

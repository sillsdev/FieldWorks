// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class VwPatternLiteralSearchCharacterizationTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void CanonicalEquivalentPatternsUseMatchedSourceRange()
		{
			Assert.That(Find("caf\u00e9", "cafe\u0301", matchDiacritics: true), Is.EqualTo((0, 5)));
			Assert.That(Find("cafe\u0301", "caf\u00e9", matchDiacritics: true), Is.EqualTo((0, 4)));
		}

		[Test]
		public void CanonicallyOrderedTwoMarkSequenceMatchesPrecomposedCharacter()
		{
			Assert.That(Find("\u1ebf", "e\u0302\u0301", matchDiacritics: true), Is.EqualTo((0, 3)));
		}

		[Test]
		public void NonCanonicalCombiningMarkOrderDoesNotMatchPrecomposedCharacter()
		{
			Assert.That(Find("\u1e09", "c\u0301\u0327", matchDiacritics: true), Is.EqualTo((-1, -1)));
		}

		[Test]
		public void CaseAndDiacriticsOptionsSelectExpectedOccurrence()
		{
			Assert.That(Find("cafe", "caf\u00e9 CAFE cafe", matchCase: false, matchDiacritics: false), Is.EqualTo((0, 4)));
			Assert.That(Find("cafe", "caf\u00e9 CAFE cafe", matchCase: false, matchDiacritics: true), Is.EqualTo((5, 9)));
			Assert.That(Find("cafe", "caf\u00e9 CAFE cafe", matchCase: true, matchDiacritics: false), Is.EqualTo((10, 14)));
		}

		[Test]
		public void WholeWordOptionControlsEmbeddedMatches()
		{
			Assert.That(Find("cafe", "cafeteria cafe", matchCase: false, matchDiacritics: false,
				matchWholeWord: false), Is.EqualTo((0, 4)));
			Assert.That(Find("cafe", "cafeteria cafe", matchCase: false, matchDiacritics: false,
				matchWholeWord: true), Is.EqualTo((10, 14)));
		}

		[Test]
		public void SearchLimitUsesUtf16Range()
		{
			Assert.That(Find("cafe", "cafe\u0301", matchCase: false, matchDiacritics: false,
				matchWholeWord: false, start: 0, limit: 4), Is.EqualTo((0, 4)));
			Assert.That(Find("cafe", "cafe\u0301", matchCase: false, matchDiacritics: true,
				matchWholeWord: false, start: 0, limit: 4), Is.EqualTo((-1, -1)));
		}

		[Test]
		public void SupplementaryCharactersUseUtf16Offsets()
		{
			Assert.That(Find("\U0001f600", "ab\U0001f600cd"), Is.EqualTo((2, 4)));
		}

		[Test]
		public void ReverseSearchStartsAtEndOfText()
		{
			Assert.That(Find("cafe", "cafe x cafe", matchCase: false, matchDiacritics: false,
				matchWholeWord: false, start: 11, limit: 0, forward: false), Is.EqualTo((7, 11)));
		}

		[Test]
		public void ForwardSearchHonorsRestartOffset()
		{
			Assert.That(Find("cafe", "cafe cafe", matchCase: false, matchDiacritics: false,
				matchWholeWord: false, start: 1, limit: 9), Is.EqualTo((5, 9)));
		}

		private IVwPattern CreatePattern(string pattern, bool matchCase = false,
			bool matchDiacritics = false, bool matchWholeWord = false)
		{
			var result = VwPatternClass.Create();
			result.Pattern = TsStringUtils.MakeString(pattern, Cache.DefaultVernWs);
			result.MatchCase = matchCase;
			result.MatchDiacritics = matchDiacritics;
			result.MatchWholeWord = matchWholeWord;
			result.IcuLocale = "root";
			return result;
		}

		private (int Min, int Lim) Find(string pattern, string text,
			bool matchCase = false, bool matchDiacritics = false, bool matchWholeWord = false,
			int start = 0, int limit = -1, bool forward = true)
		{
			var textSourceInit = VwStringTextSourceClass.Create();
			var textSource = (IVwTextSource)textSourceInit;
			var textString = TsStringUtils.MakeString(text, Cache.DefaultVernWs);
			textSourceInit.SetString(textString);
			if (limit < 0)
				limit = textString.Length;
			var vwPattern = CreatePattern(pattern, matchCase, matchDiacritics, matchWholeWord);
			vwPattern.FindIn(textSource, start, limit, forward, out var min, out var lim, null);
			return (min, lim);
		}
	}

	[TestFixture]
	public class VwPatternRegexSearchCharacterizationTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void NfcPatternAgainstNfcSourceDoesNotMatch()
		{
			Assert.That(Find("\u00e9", "\u00e9"), Is.EqualTo((-1, -1)));
		}

		[Test]
		public void NfcPatternAgainstNfdSourceUsesSourceRange()
		{
			Assert.That(Find("\u00e9", "e\u0301"), Is.EqualTo((0, 2)));
		}

		[Test]
		public void LiteralRegexAgainstNfdSourceMatchesBaseCharacter()
		{
			Assert.That(Find("e", "e\u0301"), Is.EqualTo((0, 1)));
		}

		[Test]
		public void RegexMatchDiacriticsOptionDoesNotChangeBaseCharacterMatch()
		{
			Assert.That(Find("e", "e\u0301", matchDiacritics: false), Is.EqualTo((0, 1)));
			Assert.That(Find("e", "e\u0301", matchDiacritics: true), Is.EqualTo((0, 1)));
		}

		[Test]
		public void RegexMatchWholeWordOptionDoesNotChangeEmbeddedResult()
		{
			Assert.That(Find("cafe", "cafeteria", matchWholeWord: false), Is.EqualTo((0, 4)));
			Assert.That(Find("cafe", "cafeteria", matchWholeWord: true), Is.EqualTo((0, 4)));
		}

		[Test]
		public void RegexMatchCaseSelectsCaseSensitiveOccurrence()
		{
			Assert.That(Find("cafe", "Cafe cafe", matchCase: false), Is.EqualTo((0, 4)));
			Assert.That(Find("cafe", "Cafe cafe", matchCase: true), Is.EqualTo((5, 9)));
		}

		[Test]
		public void RegexAnchorsMatchZeroWidthRanges()
		{
			Assert.That(Find("^", "cafe"), Is.EqualTo((0, 0)));
			Assert.That(Find("$", "cafe"), Is.EqualTo((4, 4)));
		}

		[Test]
		public void RegexGroupsAndReplacementTextExposeCaptures()
		{
			var pattern = CreatePattern("(c)(afe)");
			pattern.ReplaceWith = TsStringUtils.MakeString("$2$1", Cache.DefaultVernWs);
			var source = CreateTextSource(TsStringUtils.MakeString("cafe", Cache.DefaultVernWs));
			pattern.FindIn(source, ichStartLog: 0, ichEndLog: 4, fForward: true,
				_ichMinFoundLog: out var min, _ichLimFoundLog: out var lim, _xserkl: null);

			Assert.That((min, lim), Is.EqualTo((0, 4)));
			Assert.That(pattern.get_Group(0).Text, Is.EqualTo("cafe"));
			Assert.That(pattern.get_Group(1).Text, Is.EqualTo("c"));
			Assert.That(pattern.get_Group(2).Text, Is.EqualTo("afe"));
			Assert.That(pattern.ReplacementText.Text, Is.EqualTo("afec"));
		}

		[Test]
		public void RegexForwardAndReverseSearchSelectFirstAndLastOccurrence()
		{
			Assert.That(Find("cafe", "cafe cafe", start: 0, limit: 9, forward: true), Is.EqualTo((0, 4)));
			Assert.That(Find("cafe", "cafe cafe", start: 9, limit: 0, forward: false), Is.EqualTo((5, 9)));
		}

		[Test]
		public void RegexMatchIgnoresWritingSystemWhenMatchOldWritingSystemIsEnabled()
		{
			Assert.That(Cache.DefaultAnalWs, Is.Not.EqualTo(Cache.DefaultVernWs));
			var sourceString = TsStringUtils.MakeString("cafe", Cache.DefaultVernWs);
			var builder = sourceString.GetBldr();
			builder.SetIntPropValues(0, builder.Length, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.DefaultAnalWs);
			var pattern = CreatePattern("cafe");
			pattern.MatchOldWritingSystem = true;
			var source = CreateTextSource(builder.GetString());
			pattern.FindIn(source, ichStartLog: 0, ichEndLog: 4, fForward: true,
				_ichMinFoundLog: out var min, _ichLimFoundLog: out var lim, _xserkl: null);

			Assert.That((min, lim), Is.EqualTo((0, 4)));
		}

		private IVwPattern CreatePattern(string pattern, bool matchCase = true,
			bool matchDiacritics = true, bool matchWholeWord = false)
		{
			var result = VwPatternClass.Create();
			result.Pattern = TsStringUtils.MakeString(pattern, Cache.DefaultVernWs);
			result.MatchCase = matchCase;
			result.MatchDiacritics = matchDiacritics;
			result.MatchWholeWord = matchWholeWord;
			result.IcuLocale = "root";
			result.UseRegularExpressions = true;
			return result;
		}

		private (int Min, int Lim) Find(string pattern, string text,
			bool matchCase = false, bool matchDiacritics = false, bool matchWholeWord = false,
			int start = 0, int limit = -1, bool forward = true)
		{
			var textString = TsStringUtils.MakeString(text, Cache.DefaultVernWs);
			if (limit < 0)
				limit = textString.Length;
			var patternObject = CreatePattern(pattern, matchCase, matchDiacritics, matchWholeWord);
			var textSource = CreateTextSource(textString);
			patternObject.FindIn(textSource, ichStartLog: start, ichEndLog: limit, fForward: forward,
				_ichMinFoundLog: out var min, _ichLimFoundLog: out var lim, _xserkl: null);
			return (min, lim);
		}

		private IVwTextSource CreateTextSource(ITsString text)
		{
			var textSource = VwStringTextSourceClass.Create();
			textSource.SetString(text);
			return textSource;
		}
	}
}

// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
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
		public void CanonicallyOrderedCombiningMarksDoNotMatchPrecomposedCharacter()
		{
			Assert.That(Find("\u1e17", "e\u0302\u0301", matchDiacritics: true), Is.EqualTo((-1, -1)));
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
}

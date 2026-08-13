// Copyright (c) 2026 SIL Global
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Characterizes current filter matcher behavior for LT-22696.
	/// </summary>
	[TestFixture]
	public class FilterStringMatcherCharacterizationTests : MemoryOnlyBackendProviderTestBase
	{
		[TestCase("+", "+")]
		[TestCase("-", "-")]
		[TestCase("<ipa>", "<ipa>")]
		[TestCase("Labial", "labial")]
		public void AnywhereMatcher_TreatsPhonologyTableSearchTextAsLiteral(string patternText,
			string sourceText)
		{
			var matcher = new AnywhereMatcher(MakePattern(patternText));

			Assert.That(matcher.Matches(MakeString(sourceText)), Is.True,
				$"normal filter pattern '{patternText}' should match '{sourceText}' literally");
		}

		[Test]
		public void RegExpMatcher_BarePlusIsInvalid()
		{
			var matcher = new RegExpMatcher(MakePattern("+"));

			Assert.That(matcher.IsValid(), Is.False,
				"a bare plus is a regex quantifier and must not be accepted as a literal search");
			Assert.That(matcher.ErrorMessage(), Is.Not.Empty,
				"an invalid table-filter regex should provide an actionable error");
		}

		[TestCase(@"\+", "+")]
		[TestCase("-", "-")]
		[TestCase("<ipa>", "<ipa>")]
		[TestCase("Labial", "labial")]
		public void RegExpMatcher_UsesIcuSyntaxForPhonologyTableSearchText(string patternText,
			string sourceText)
		{
			var matcher = new RegExpMatcher(MakePattern(patternText));

			Assert.That(matcher.IsValid(), Is.True,
				$"regex filter pattern '{patternText}' should be valid ICU syntax");
			Assert.That(matcher.Matches(MakeString(sourceText)), Is.True,
				$"regex filter pattern '{patternText}' should match '{sourceText}'");
		}

		private IVwPattern MakePattern(string text)
		{
			var pattern = VwPatternClass.Create();
			pattern.Pattern = MakeString(text);
			pattern.MatchCase = false;
			pattern.MatchDiacritics = true;
			pattern.MatchWholeWord = false;
			pattern.MatchOldWritingSystem = false;
			pattern.UseRegularExpressions = false;
			pattern.IcuLocale = "root";
			return pattern;
		}

		private SIL.LCModel.Core.KernelInterfaces.ITsString MakeString(string text)
		{
			return TsStringUtils.MakeString(text, Cache.DefaultAnalWs);
		}
	}
}

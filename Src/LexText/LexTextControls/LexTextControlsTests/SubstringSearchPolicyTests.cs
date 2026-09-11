// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;

namespace LexTextControlsTests
{
	/// <summary>
	/// Verifies the query-length gate that decides when Find Lexical Entry uses substring
	/// (match-anywhere) matching.
	/// </summary>
	[TestFixture]
	public class SubstringSearchPolicyTests
	{
		// A base letter plus combining acute (U+0301): 2 UTF-16 units that DO compose to one
		// precomposed character under FormC.
		private static readonly string ComposedAcuteE = "e" + (char)0x0301;

		// A base letter plus combining tilde (U+0303): one grapheme (2 UTF-16 units) with NO
		// precomposed form, so FormC leaves it decomposed. This is the case a code-unit count
		// gets wrong.
		private static readonly string NonComposableGrapheme = "b" + (char)0x0303;

		// A non-BMP character (U+10480): one grapheme encoded as a surrogate pair, so 2 UTF-16
		// units.
		private static readonly string SurrogatePairChar = char.ConvertFromUtf32(0x10480);

		[TestCase("", ExpectedResult = false)]
		[TestCase("l", ExpectedResult = false)]
		[TestCase("la", ExpectedResult = false)]      // below MinQueryLength
		[TestCase("lan", ExpectedResult = true)]      // exactly MinQueryLength
		[TestCase("language", ExpectedResult = true)]
		public bool UseSubstring_gatesOnLength(string key)
		{
			return SubstringSearchPolicy.UseSubstring(key);
		}

		[Test]
		public void UseSubstring_nullKey_isFalse()
		{
			Assert.That(SubstringSearchPolicy.UseSubstring(null), Is.False);
		}

		[Test]
		public void UseSubstring_countsGraphemes_notUtf16Units()
		{
			// Each grapheme below is 2 UTF-16 units, so a code-unit count would over-count.
			// All three kinds (composable, non-composable, non-BMP) must gate on text
			// elements, not raw code units.
			var graphemes = new[] { ComposedAcuteE, NonComposableGrapheme, SurrogatePairChar };
			foreach (var grapheme in graphemes)
			{
				Assert.That(SubstringSearchPolicy.UseSubstring(grapheme + grapheme), Is.False,
					"two graphemes should count as length 2, below the threshold");
				Assert.That(SubstringSearchPolicy.UseSubstring(grapheme + grapheme + grapheme), Is.True,
					"three graphemes should count as length 3, at the threshold");
			}
		}

		[Test]
		public void MinQueryLength_hasExpectedDefault()
		{
			Assert.That(SubstringSearchPolicy.MinQueryLength, Is.EqualTo(3));
		}
	}
}

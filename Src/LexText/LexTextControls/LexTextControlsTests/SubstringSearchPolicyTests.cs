// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;

namespace LexTextControlsTests
{
	/// <summary>
	/// Tests for the pure Find-Lexical-Entry substring-search decision logic that was extracted from
	/// EntryGoDlg so it could be tested without driving the dialog.
	/// </summary>
	[TestFixture]
	public class SubstringSearchPolicyTests
	{
		// A base letter followed by a combining acute accent (U+0301): 2 UTF-16 units that compose to
		// a single character under FormC normalization.
		private static readonly string ComposedAcuteE = "e" + (char)0x0301;

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
		public void UseSubstring_countsComposedCharacters_notUtf16Units()
		{
			// Each ComposedAcuteE is 2 UTF-16 units but 1 character after FormC. If the policy counted
			// raw Length it would see 4 and 6 (both >= 3) and wrongly enable substring; counting composed
			// characters it sees 2 and 3.
			string twoComposed = ComposedAcuteE + ComposedAcuteE;                   // raw Length 4 -> 2
			string threeComposed = ComposedAcuteE + ComposedAcuteE + ComposedAcuteE; // raw Length 6 -> 3

			Assert.That(SubstringSearchPolicy.UseSubstring(twoComposed), Is.False,
				"two composed characters should count as length 2, below the threshold");
			Assert.That(SubstringSearchPolicy.UseSubstring(threeComposed), Is.True,
				"three composed characters should count as length 3, at the threshold");
		}

		[Test]
		public void MinQueryLength_hasExpectedDefault()
		{
			Assert.That(SubstringSearchPolicy.MinQueryLength, Is.EqualTo(3));
		}
	}
}

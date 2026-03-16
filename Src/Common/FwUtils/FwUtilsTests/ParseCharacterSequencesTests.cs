// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// A categorizer where diacritics precede their base characters (for hacked fonts).
	/// </summary>
	internal class DiacriticsPrecedeCategorizer : CharacterCategorizer
	{
		public override bool DiacriticsFollowBaseCharacters()
		{
			return false;
		}
	}

	[TestFixture]
	public class ParseCharacterSequencesTests
	{
		private CharacterCategorizer m_defaultCategorizer;
		private DiacriticsPrecedeCategorizer m_precedeCategorizer;

		[SetUp]
		public void SetUp()
		{
			m_defaultCategorizer = new CharacterCategorizer();
			m_precedeCategorizer = new DiacriticsPrecedeCategorizer();
		}

		private static List<string> Parse(string text, CharacterCategorizer categorizer)
		{
			return TextFileDataSource.ParseCharacterSequences(text, categorizer).ToList();
		}

		#region Boundary cases
		[Test]
		public void EmptyString_ReturnsEmpty()
		{
			var result = Parse("", m_defaultCategorizer);
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void NullInput_ReturnsEmpty()
		{
			var result = Parse(null, m_defaultCategorizer);
			Assert.That(result, Is.Empty);
		}
		#endregion

		#region Basic character parsing
		[Test]
		public void BasicAscii_EachCharSeparate()
		{
			var result = Parse("abc", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
		}

		[Test]
		public void SingleCharacter_ReturnsSingleElement()
		{
			var result = Parse("x", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "x" }));
		}

		[Test]
		public void Whitespace_EachSeparate()
		{
			var result = Parse(" \n", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { " ", "\n" }));
		}

		[Test]
		public void PunctuationMixedWithText_EachSeparate()
		{
			var result = Parse("a.b", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "a", ".", "b" }));
		}

		[Test]
		public void Digits_EachSeparate()
		{
			var result = Parse("123", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "1", "2", "3" }));
		}
		#endregion

		#region Diacritics follow base (default Unicode ordering)
		[Test]
		public void CombiningDiacriticFollowsBase_GroupedWithBase()
		{
			// a + combining acute accent + b
			var result = Parse("a\u0301b", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "a\u0301", "b" }));
		}

		[Test]
		public void MultipleDiacriticsOnOneBase_AllGrouped()
		{
			// a + combining acute + combining circumflex
			var result = Parse("a\u0301\u0302", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "a\u0301\u0302" }));
		}

		[Test]
		public void TwoBasesEachWithDiacritics_SeparateGroups()
		{
			// a + combining acute + b + combining tilde
			var result = Parse("a\u0301b\u0303", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "a\u0301", "b\u0303" }));
		}

		[Test]
		public void OnlyDiacriticsFollowMode_GroupedTogether()
		{
			// combining acute + combining circumflex (no base)
			// First diacritic becomes the key, subsequent diacritics append
			var result = Parse("\u0301\u0302", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "\u0301\u0302" }));
		}

		[Test]
		public void SpacingCombiningMark_TreatedAsDiacritic()
		{
			// Devanagari vowel sign AA (U+093E) is a SpacingCombiningMark
			var result = Parse("\u0915\u093E", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "\u0915\u093E" }));
		}
		#endregion

		#region Diacritics precede base (hacked font ordering)
		[Test]
		public void DiacriticsPrecedeBase_DiacriticSeparateFromFollowingBase()
		{
			// combining acute + a (with diacritics-precede mode)
			var result = Parse("\u0301a", m_precedeCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "\u0301", "a" }));
		}

		[Test]
		public void DiacriticsPrecedeBase_MultipleDiacritics_EachSeparate()
		{
			// combining acute + combining circumflex (with diacritics-precede mode)
			var result = Parse("\u0301\u0302", m_precedeCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "\u0301", "\u0302" }));
		}

		[Test]
		public void DiacriticsPrecedeBase_DiacriticBeforeBase_ThenAnotherBase()
		{
			// combining acute + a + b
			var result = Parse("\u0301ab", m_precedeCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "\u0301", "a", "b" }));
		}
		#endregion

		#region Supplementary plane characters (surrogate pairs)
		[Test]
		public void SupplementaryPlaneCharacter_NotSplit()
		{
			// U+10400 DESERET CAPITAL LETTER LONG I (surrogate pair in UTF-16)
			var result = Parse("\U00010400", m_defaultCategorizer);
			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result[0], Is.EqualTo("\U00010400"));
			Assert.That(result[0].Length, Is.EqualTo(2)); // surrogate pair = 2 chars
		}

		[Test]
		public void SupplementaryPlaneCharacterFollowedByCombiningMark_Grouped()
		{
			// U+10000 LINEAR B SYLLABLE B008 A + combining acute
			var result = Parse("\U00010000\u0301", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "\U00010000\u0301" }));
		}

		[Test]
		public void MultipleSupplementaryPlaneCharacters_EachSeparate()
		{
			// Two supplementary characters
			var result = Parse("\U00010400\U00010401", m_defaultCategorizer);
			Assert.That(result.Count, Is.EqualTo(2));
			Assert.That(result[0], Is.EqualTo("\U00010400"));
			Assert.That(result[1], Is.EqualTo("\U00010401"));
		}
		#endregion

		#region Malformed surrogate input
		[Test]
		public void LoneHighSurrogate_DoesNotCrash()
		{
			// A lone high surrogate - malformed but should not throw
			var result = Parse("\uD800", m_defaultCategorizer);
			Assert.That(result.Count, Is.EqualTo(1));
		}

		[Test]
		public void LoneLowSurrogate_DoesNotCrash()
		{
			// A lone low surrogate - malformed but should not throw
			var result = Parse("\uDC00", m_defaultCategorizer);
			Assert.That(result.Count, Is.EqualTo(1));
		}
		#endregion

		#region Real-world minority language text
		[Test]
		public void VietnameseToneMarks_GroupedCorrectly()
		{
			// Vietnamese: a + combining horn + combining acute (NFD form)
			var result = Parse("a\u031B\u0301", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "a\u031B\u0301" }));
		}

		[Test]
		public void HebrewWithMultipleDiacritics_GroupedCorrectly()
		{
			// Shin + shin dot + hiriq + tipeha
			var result = Parse("\u05E9\u05C1\u05B4\u0596", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "\u05E9\u05C1\u05B4\u0596" }));
		}

		[Test]
		public void ThaiWithCombiningMarks_GroupedCorrectly()
		{
			// Thai KO KAI + MAI EK (tone mark, combining)
			var result = Parse("\u0E01\u0E48", m_defaultCategorizer);
			Assert.That(result, Is.EqualTo(new[] { "\u0E01\u0E48" }));
		}
		#endregion

		#region TextFileDataSource.GetReferences integration
		[Test]
		public void GetReferences_ReturnsCorrectOffsetsAndLengths()
		{
			var categorizer = new CharacterCategorizer();
			var data = new TextFileDataSource(
				new[] { "a\u0301b", "cd" },
				"Line {0}",
				categorizer);

			var refs = data.GetReferences();
			Assert.That(refs, Is.Not.Null);
			// Line 1: "a\u0301" (offset 0, length 2) + "b" (offset 2, length 1)
			// Line 2: "c" (offset 0, length 1) + "d" (offset 1, length 1)
			Assert.That(refs.Count, Is.EqualTo(4));

			Assert.That(refs[0].Offset, Is.EqualTo(0));
			Assert.That(refs[0].Length, Is.EqualTo(2));
			Assert.That(refs[0].Text, Is.EqualTo("a\u0301"));

			Assert.That(refs[1].Offset, Is.EqualTo(2));
			Assert.That(refs[1].Length, Is.EqualTo(1));
			Assert.That(refs[1].Text, Is.EqualTo("b"));

			Assert.That(refs[2].Offset, Is.EqualTo(0));
			Assert.That(refs[2].Length, Is.EqualTo(1));
			Assert.That(refs[2].Text, Is.EqualTo("c"));

			Assert.That(refs[3].Offset, Is.EqualTo(1));
			Assert.That(refs[3].Length, Is.EqualTo(1));
			Assert.That(refs[3].Text, Is.EqualTo("d"));
		}

		[Test]
		public void GetReferences_EmptyLines_Skipped()
		{
			var categorizer = new CharacterCategorizer();
			var data = new TextFileDataSource(
				new[] { "", "a", "" },
				"Line {0}",
				categorizer);

			var refs = data.GetReferences();
			Assert.That(refs.Count, Is.EqualTo(1));
			Assert.That(refs[0].Text, Is.EqualTo("a"));
		}

		[Test]
		public void GetReferences_SupplementaryCharacters_CorrectOffsets()
		{
			var categorizer = new CharacterCategorizer();
			// U+10400 is a surrogate pair (2 chars in UTF-16)
			var data = new TextFileDataSource(
				new[] { "\U00010400a" },
				"Line {0}",
				categorizer);

			var refs = data.GetReferences();
			Assert.That(refs.Count, Is.EqualTo(2));
			Assert.That(refs[0].Offset, Is.EqualTo(0));
			Assert.That(refs[0].Length, Is.EqualTo(2)); // surrogate pair
			Assert.That(refs[1].Offset, Is.EqualTo(2));
			Assert.That(refs[1].Length, Is.EqualTo(1));
			Assert.That(refs[1].Text, Is.EqualTo("a"));
		}
		#endregion
	}
}

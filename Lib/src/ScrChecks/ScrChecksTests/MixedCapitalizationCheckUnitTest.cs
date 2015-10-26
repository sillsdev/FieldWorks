// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for the MixedCapitalizationCheck class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MixedCapitalizationCheckUnitTest
	{
		UnitTestChecksDataSource m_source = new UnitTestChecksDataSource();

		[SetUp]
		public void RunBeforeEachTest()
		{
			m_source.SetParameterValue("UncapitalizedPrefixes", "");
			m_source.SetParameterValue("CapitalizedSuffixes", "");
			m_source.SetParameterValue("CapitalizedPrefixes", "");
		}

		void Test(string[] result, string text)
		{
			Test(result, text, "");
		}

		void Test(string[] result, string text, string desiredKey)
		{
			m_source.Text = text;

			MixedCapitalizationCheck check = new MixedCapitalizationCheck(m_source);
			List<TextTokenSubstring> tts =
				check.GetReferences(m_source.TextTokens(), desiredKey);

			Assert.AreEqual(result.GetUpperBound(0)+1, tts.Count,
				"A different number of results was returned than what was expected." );

			for (int i = 0; i <= result.GetUpperBound(0); ++i)
				Assert.AreEqual(result[i], tts[i].InventoryText, "Result number: " + i.ToString());
		}

		[Test]
		public void WordNoPrefixLower()
		{
			AWord word = new AWord("bat", m_source.CharacterCategorizer);
			Assert.AreEqual("", word.Prefix);
		}
		[Test]
		public void WordNoSuffixLower()
		{
			AWord word = new AWord("bat", m_source.CharacterCategorizer);
			Assert.AreEqual("", word.Suffix);
		}

		[Test]
		public void WordNoPrefixUpper()
		{
			AWord word = new AWord("BAT", m_source.CharacterCategorizer);
			Assert.AreEqual("", word.Prefix);
		}
		[Test]
		public void WordNoSuffixUpper()
		{
			AWord word = new AWord("BAT", m_source.CharacterCategorizer);
			Assert.AreEqual("", word.Suffix);
		}

		[Test]
		public void WordPrefixLower()
		{
			AWord word = new AWord("caBat", m_source.CharacterCategorizer);
			Assert.AreEqual("ca", word.Prefix);
		}

		[Test]
		public void WordPrefixLowerWithTitle()
		{
			AWord word = new AWord("ca\u01C5at", m_source.CharacterCategorizer);
			Assert.AreEqual("ca", word.Prefix);
		}

		[Test]
		public void WordPrefixUpper()
		{
			AWord word = new AWord("CaBat", m_source.CharacterCategorizer);
			Assert.AreEqual("Ca", word.Prefix);
		}

		[Test]
		public void WordSuffix()
		{
			AWord word = new AWord("DavidBen", m_source.CharacterCategorizer);
			Assert.AreEqual("Ben", word.Suffix);
		}

		[Test]
		public void WordWithNumberNoPrefix()
		{
			AWord word = new AWord("1Co", m_source.CharacterCategorizer);
			Assert.AreEqual("", word.Prefix);
		}
		[Test]
		public void WordWithNumberNoSuffix()
		{
			AWord word = new AWord("1Co", m_source.CharacterCategorizer);
			Assert.AreEqual("", word.Suffix);
		}

		[Test]
		public void Regular()
		{
			Test(new string[] { }, @"\p \v 1 Bat");
		}

		[Test]
		public void TwoCapitalLetter()
		{
			Test(new string[] { "BaT" }, @"\p \v 1 BaT");
		}

		[Test]
		public void TwoCapitalLetterDiacritic_MustBeDeComposed()
		{
			Test(new string[] { "B\u030BaT\u030B" }, "\\p \\v 1 B\u030BaT\u030B");
		}

		[Test]
		public void TwoCapitalLetterDiacritic_CanBeComposed()
		{
			Test(new string[] { "\u0210a\u0210" }, "\\p \\v 1 \u0210a\u0210");
		}

		[Test]
		public void TwoCapitalLetterDiacritic_CanBeDeComposed()
		{
			Test(new string[] { "R\u030FaR\u030F" }, "\\p \\v 1 R\u030FaR\u030F");
		}

		[Test]
		public void AllCaps()
		{
			Test(new string[] { }, @"\p \v 1 BAT");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that diacritics within a lowercase word does not return a mixed capitalization
		/// problem.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AllLowerDiacriticNFC()
		{
			Test(new string[] { }, "\\p \\v 1 Phile\u0301mon");
		}

		[Test]
		public void AllCapsDiacritic_MustBeDeComposed()
		{
			Test(new string[] { }, "\\p \\v 1 B\030BAT");
		}

		[Test]
		public void AllCapsDiacritic_CanBeComposed()
		{
			Test(new string[] { }, "\\p \\v 1 \0210AT");
		}

		[Test]
		public void AllCapsDiacritic_CanBeDeComposed()
		{
			Test(new string[] { }, "\\p \\v 1 R\u030FAT");
		}

		[Test]
		public void UncapitalizedPrefix()
		{
			Test(new string[] { "aBat" }, @"\p \v 1 aBat");
		}

		[Test]
		public void UncapitalizedPrefixDiacritic_MustBeDeComposed()
		{
			Test(new string[] { "aB\u030Bat" }, "\\p \\v 1 aB\u030Bat");
		}

		[Test]
		public void UncapitalizedPrefixDiacritic_CanBeComposed()
		{
			Test(new string[] { "a\u0210at" }, "\\p \\v 1 a\u0210at");
		}

		[Test]
		public void UncapitalizedPrefixDiacritic_CanBeDeComposed()
		{
			Test(new string[] { "aR\u030Fat" }, "\\p \\v 1 aR\u030Fat");
		}

		[Test]
		[Ignore("Text needs to be normalized to NFC (or maybe NFD) before check is run.")]
		public void FindingDifferentNormalization()
		{
			Test(new string[] { "a\u0210at", "a\u0210at" },
				"\\p \\v 1 aR\u030Fat aNd a\u0210at", "a\u0210at");
		}

		[Test]
		public void UncapitalizedPrefixTitleCase()
		{
			Test(new string[] { "a\u01C5at" }, "\\p \\v 1 a\u01C5at");
		}

		[Test]
		public void UncapitalizedPrefixSpecificOK()
		{
			m_source.SetParameterValue("UncapitalizedPrefixes", "a");
			Test(new string[] { }, @"\p \v 1 aBat");
		}

		[Test]
		public void UncapitalizedPrefixPatternOK1()
		{
			m_source.SetParameterValue("UncapitalizedPrefixes", "*a");
			Test(new string[] { }, @"\p \v 1 baBat");
		}
		[Test]
		public void UncapitalizedPrefixPatternOK2()
		{
			m_source.SetParameterValue("UncapitalizedPrefixes", "*a");
			Test(new string[] { }, @"\p \v 1 caBat");
		}

		[Test]
		public void UncapitalizedPrefixAllOK1()
		{
			m_source.SetParameterValue("UncapitalizedPrefixes", "*");
			Test(new string[] { }, @"\p \v 1 baBat");
		}
		[Test]
		public void UncapitalizedPrefixAllOK2()
		{
			m_source.SetParameterValue("UncapitalizedPrefixes", "*");
			Test(new string[] { }, @"\p \v 1 caBat");
		}

		[Test]
		public void CapitalizedSuffixOK()
		{
			m_source.SetParameterValue("CapitalizedSuffixes", "Bat");
			Test(new string[] { }, @"\p \v 1 CaBat");
		}

		[Test]
		public void CapitalizedPrefixOK()
		{
			m_source.SetParameterValue("CapitalizedPrefixes", "Ca");
			Test(new string[] { }, @"\p \v 1 CaBat");
		}

		[Test]
		public void WithNumbers()
		{
			Test(new string[] { }, @"\p \v 1 1Co");
		}
		[Test]
		public void WithNumbersPrefix()
		{
			Test(new string[] { "1CoR" }, @"\p \v 1 1CoR");
		}

		[Test]
		public void NonLettersOneCapitalLetter()
		{
			Test(new string[] { }, @"\p \v 1 Foo-bar");
		}

		[Test]
		public void NonLettersTwoCapitalLetter()
		{
			Test(new string[] { "Foo-Bar" }, @"\p \v 1 Foo-Bar");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the character categorizer after the check is instantiated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeCharacterCategorizerAfterInstantiationOfCheck()
		{
			MixedCapitalizationCheck check = new MixedCapitalizationCheck(m_source);

			m_source.Text = @"\p \v 1 w!Forming";

			List<TextTokenSubstring> tts = check.GetReferences(m_source.TextTokens(), null);

			Assert.AreEqual(0, tts.Count);

			m_source.m_extraWordFormingCharacters = "!";

			tts = check.GetReferences(m_source.TextTokens(), null);

			Assert.AreEqual(1, tts.Count);
			Assert.AreEqual("w!Forming", tts[0].Text);
		}
	}
}

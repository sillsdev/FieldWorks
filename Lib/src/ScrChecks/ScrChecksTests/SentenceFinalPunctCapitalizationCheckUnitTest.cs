using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
#if DEBUG
	[TestFixture]
	public class SentenceFinalPunctCapitalizationCheckUnitTest
	{
		UnitTestChecksDataSource source = new UnitTestChecksDataSource();

		[SetUp]
		public void RunBeforeEachTest()
		{
			source.SetParameterValue("ValidPunctuation", "._ !_ ?_");
		}

		void Test(string[] result, string text)
		{
			source.Text = text;

			SentenceFinalPunctCapitalizationCheck check = new SentenceFinalPunctCapitalizationCheck(source);
			List<TextTokenSubstring> tts =
				check.GetReferences(source.TextTokens(), "");

			Assert.AreEqual(result.GetUpperBound(0)+1, tts.Count,
				"A different number of results was returned than what was expected." );

			for (int i = 0; i <= result.GetUpperBound(0); ++i)
				Assert.AreEqual(result[i], tts[i].InventoryText, "Result number: " + i.ToString());
		}

		[Test]
		public void UpperCase()
		{
			Test(new string[] { }, @"\p \v 1 Foo. Bar");
		}

		[Test]
		public void LowerCase()
		{
			Test(new string[] { "." }, @"\p \v 1 Foo. bar");
		}

		[Test]
		public void NoCaseNonRoman()
		{
			Test(new string[] { }, "\\p \\v 1 Foo. \u0E01");
		}

		[Test]
		public void NoCasePUA()
		{
			Test(new string[] { }, "\\p \\v 1 Foo. \uEE00");
		}

		[Test]
		public void TitleCase()
		{
			Test(new string[] { }, "\\p \\v 1 Foo. \u01C5");
		}

		[Test]
		public void MultipleUpperCase()
		{
			Test(new string[] { }, @"\p \v 1 Foo. Bar! Baz");
		}

		[Test]
		public void MultipleLowerCase()
		{
			Test(new string[] { ".", "!" }, @"\p \v 1 Foo. bar! baz");
		}

		[Test]
		public void MultipleMixedCase()
		{
			Test(new string[] { "!" }, @"\p \v 1 Foo. Bar! baz");
		}

		[Test]
		public void MultiplePunctUpperCase()
		{
			Test(new string[] { }, @"\p \v 1 Foo!? Bar");
		}

		[Test]
		public void MultiplePunctLowerCase()
		{
			Test(new string[] { "!", "?" }, @"\p \v 1 Foo!? bar");
		}

		[Test]
		public void Quotes()
		{
			Test(new string[] { "!" }, "\\p \\v 1 \u201CFoo!\u201D bar");
		}

		[Test]
		public void Digits()
		{
			Test(new string[] { }, @"\p \v 1 Foo 1.2 bar");
		}

		[Test]
		public void AbbreviationError()
		{
			Test(new string[] { "." }, @"\p \v 1 The E.U. headquarters.");
		}

		[Test]
		public void AbbreviationOK()
		{
			source.SetParameterValue("Abbreviations", "E.U.");
			Test(new string[] { }, @"\p \v 1 The E.U. headquarters.");
		}
	}
#endif
}

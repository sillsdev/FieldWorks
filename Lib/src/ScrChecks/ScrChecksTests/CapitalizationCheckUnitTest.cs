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
	/// USFM-style unit tests for the CapitalizationCheck class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CapitalizationCheckUnitTest
	{
		UnitTestChecksDataSource source = new UnitTestChecksDataSource();

		// A subset of serialized style information for seven different classes of styles
		// that require capitalization:
		//  sentence intial styles, proper nouns, tables, lists, special, headings and titles.
		string stylesInfo =
			"<?xml version=\"1.0\" encoding=\"utf-16\"?><StylePropsInfo>" +
			"<SentenceInitial>" +
				"<StyleInfo StyleName=\"ft\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"iex\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"q\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"f\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"p\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"qt\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"qr\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"v\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"wj\" StyleType=\"character\" /></SentenceInitial>" +
			"<ProperNouns>" +
				"<StyleInfo StyleName=\"nd\" StyleType=\"character\" /></ProperNouns>" +
			"<Table>" +
				"<StyleInfo StyleName=\"th1\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"thr1\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"tcr1\" StyleType=\"paragraph\" /></Table>" +
			"<List>" +
				"<StyleInfo StyleName=\"ph1\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"ph2\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"ph3\" StyleType=\"paragraph\" /></List>" +
			"<Special>" +
				"<StyleInfo StyleName=\"pmo\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"pc\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"qs\" StyleType=\"paragraph\" /></Special>" +
			"<Heading>" +
				"<StyleInfo StyleName=\"imte\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"cs\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"div\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"nc\" StyleType=\"paragraph\" /></Heading>" +
			"<Title>" +
				"<StyleInfo StyleName=\"imt\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"imt2\" StyleType=\"character\" />" +
				"<StyleInfo StyleName=\"imt3\" StyleType=\"character\" />" +
				"</Title></StylePropsInfo>";

		void Test(string[] result, string text)
		{
			source.Text = text;

			source.SetParameterValue("StylesInfo", stylesInfo);
			source.SetParameterValue("SentenceFinalPunctuation", ".!?");
			CapitalizationCheck check = new CapitalizationCheck(source);
			List<TextTokenSubstring> tts =
				check.GetReferences(source.TextTokens());

			Assert.AreEqual(result.Length, tts.Count,
				"A different number of results was returned from what was expected." );

			for (int i = 0; i < result.Length; i++)
				Assert.AreEqual(result[i], tts[i].InventoryText, "Result number: " + i);
		}

		#region Test capitalization of styles
		[Test]
		public void ParagraphCapitalized()
		{
			Test(new string[] { }, @"\p \v 1 The earth");
		}

		[Test]
		public void ParagraphNoCaseNonRoman()
		{
			Test(new string[] { }, "\\p \\v 1 \u0E01");
		}

		[Test]
		public void ParagraphNoCasePUA()
		{
			Test(new string[] { }, "\\p \\v 1 \uEE00");
		}

		[Test]
		public void ParagraphTitleCase()
		{
			Test(new string[] { }, "\\p \\v 1 \u01C5");
		}

		[Test]
		public void ParagraphUnCapitalized()
		{
			Test(new string[] { "p" }, @"\p \v 1 the earth");
		}

		[Test]
		public void ParagraphUnCapitalizedWithQuotes()
		{
			Test(new string[] { "p" }, "\\p \\v 1 \u201C \u2018the earth");
		}

		[Test]
		public void ParagraphCapitalizedWithQuotes()
		{
			Test(new string[] { }, "\\p \\v 1 \u201C \u2018The earth");
		}

		[Test]
		public void Capitalized()
		{
			Test(new string[] { }, @"\p \v 1 \nd Lord\nd* in");
		}

		[Test]
		public void UnCapitalized()
		{
			// test used to be { "p", "nd" } - was changed to reflect that we didn't want duplicated
			// results.
			Test(new string[] { "p" }, @"\p \v 1 \nd lord\nd* in");
		}

		[Test]
		public void AllCapitalized()
		{
			Test(new string[] { }, @"\p \v 1 The \nd Lord\nd* in");
		}

		[Test]
		public void ParagraphCapitalizedCharacterUnCapitalized()
		{
			Test(new string[] { "nd" }, @"\p \v 1 The \nd lord\nd* in");
		}

		[Test]
		public void ParagraphUnCapitalizedCharacterCapitalized()
		{
			Test(new string[] { "p" }, @"\p \v 1 the \nd Lord\nd* in");
		}

		[Test]
		public void AllUnCapitalized()
		{
			Test(new string[] { "p", "nd" }, @"\p \v 1 the \nd lord\nd* in");
		}
		#endregion

		#region Test capitalization after sentence-final punctuation
		[Test]
		public void UpperCase()
		{
			Test(new string[] { }, @"\p \v 1 Foo. Bar");
		}

		[Test]
		public void LowerCase()
		{
			Test(new string[] { "b" }, @"\p \v 1 Foo. bar");
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
			Test(new string[] { "b", "b" }, @"\p \v 1 Foo. bar! baz");
		}

		[Test]
		public void MultipleMixedCase()
		{
			Test(new string[] { "b" }, @"\p \v 1 Foo. Bar! baz");
		}

		[Test]
		public void MultiplePunctUpperCase()
		{
			Test(new string[] { }, @"\p \v 1 Foo!? Bar");
		}

		[Test]
		public void MultiplePunctLowerCase()
		{
			Test(new string[] { "b" }, @"\p \v 1 Foo!? bar");
		}

		[Test]
		public void Quotes()
		{
			Test(new string[] { "b" }, "\\p \\v 1 \u201CFoo!\u201D bar");
		}

		[Test]
		public void Digits()
		{
			Test(new string[] { }, @"\p \v 1 Foo 1.2 bar");
		}

		[Test]
		public void AbbreviationError()
		{
			Test(new string[] { "h" }, @"\p \v 1 The E.U. headquarters.");
		}

		[Test]
		public void AbbreviationOK()
		{
			source.SetParameterValue("Abbreviations", "E.U.");
			Test(new string[] { }, @"\p \v 1 The E.U. headquarters.");
		}
		#endregion
	}
}

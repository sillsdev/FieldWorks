// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Diagnostics;
using SILUBS.ScriptureChecks;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
#if DEBUG
	[TestFixture]
	public class RepeatedWordsCheckUnitTest
	{
		UnitTestChecksDataSource source = new UnitTestChecksDataSource();

		[SetUp]
		public void RunBeforeEachTest()
		{
		}

		void Test(string[] result, string text)
		{
			Test(result, text, "");
		}

		void Test(string[] result, string text, string desiredKey)
		{
			source.Text = text;

			RepeatedWordsCheck check = new RepeatedWordsCheck(source);
			List<TextTokenSubstring> tts =
				check.GetReferences(source.TextTokens(), desiredKey);

			Assert.AreEqual(result.GetUpperBound(0)+1, tts.Count,
				"A different number of results was returned than what was expected." );

			for (int i = 0; i <= result.GetUpperBound(0); ++i)
				Assert.AreEqual(result[i], tts[i].InventoryText, "Result number: " + i.ToString());
		}

		[Test]
		public void Repeated()
		{
			Test(new string[] { "Bar" }, @"\p \v 1 Bar Bar");
		}

		[Test]
		public void Quotes()
		{
			Test(new string[] { }, "\\p \\v 1 Bar\u201D \u201CBar");
		}

		[Test]
		public void DifferentCase()
		{
			Test(new string[] { "bar" }, @"\p \v 1 Bar bar");
		}

		[Test]
		[Ignore("Text needs to be normalized to NFC (or maybe NFD) before check is run.")]
		public void DifferentNormalization()
		{
			Test(new string[] { "B\u00E3r", "B\u00E3r" },
				"\\p \\v 1 B\u00E3r Ba\u0303r and Ba\u0303r B\u00E3r ");
		}

		[Test]
		[Ignore("Text needs to be normalized to NFC (or maybe NFD) before check is run.")]
		public void FindingDifferentNormalization()
		{
			Test(new string[] { "B\u00E3r", "B\u00E3r" },
				"\\p \\v 1 B\u00E3r Ba\u0303r and and Ba\u0303r B\u00E3r ", "B\u00E3r");
		}

		[Test]
		public void CharacterStyle()
		{
			Test(new string[] { "Bar" }, @"\p \v 1 Bar \nd Bar\nd*");
		}

		[Test]
		public void Footnote()
		{
			Test(new string[] { "foo", "Bar" }, @"\p \v 1 Bar \f + foo foo\f* Bar");
		}

		[Test]
		public void Footnotes()
		{
			Test(new string[] { "Bar" }, @"\p \v 1 Bar \f + foo\f* Bar \f + foo\f*");
		}

		[Test]
		public void NewParagraph()
		{
			Test(new string[] { }, @"\p \v 1 Bar \p Bar");
		}

		[Test]
		public void NewVerse()
		{
			Test(new string[] { "Bar" }, @"\p \v 1 Bar \v 2 Bar");
		}

		[Test]
		public void NewChapter()
		{
			Test(new string[] { }, @"\c 1 \p \v 1 Bar \c 2 \nd Bar\nd*");
		}

		[Test]
		public void EndOfLine()
		{
			Test(new string[] { "bar" }, "\\p \\v 1 bar\\f + foo\\f* \r\nbar");
		}
	}
#endif
}

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the Matched Pairs check using the USFM-style data source
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class MatchedPairsCheckUnitTest_Usfm : ScrChecksTestBase
	{
		internal const string kMatchedPairXml1 =
				"<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
				"<MatchedPairs>" +
					"<pair open=\"[\" close=\"]\" permitParaSpanning=\"false\" />" +
					"<pair open=\"{\" close=\"}\" permitParaSpanning=\"true\" />" +
					"<pair open=\"(\" close=\")\" permitParaSpanning=\"false\" />" +
				"</MatchedPairs>";

		internal const string kMatchedPairXml2 =
				"<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
				"<MatchedPairs>" +
					"<pair open=\"[\" close=\"]\" closedByPara=\"true\" />" +
					"<pair open=\"{\" close=\"}\" closedByPara=\"false\" />" +
					"<pair open=\"(\" close=\")\" closedByPara=\"true\" />" +
					"<pair open=\"\u00A1\" close=\"!\" closedByPara=\"true\" />" +
				"</MatchedPairs>";

		UnitTestChecksDataSource m_dataSource = new UnitTestChecksDataSource();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_dataSource.SetParameterValue("MatchedPairs", kMatchedPairXml1);
			m_dataSource.SetParameterValue("IntroductionOutlineStyles", "io");
			m_dataSource.SetParameterValue("PoeticStyles",
				"q1" + CheckUtils.kStyleNamesDelimiter.ToString() + "q2");
			m_check = new MatchedPairsCheck(m_dataSource);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Test(string[,] result, string text)
		{
			m_dataSource.Text = text;

			List<TextTokenSubstring> tts =
				CheckInventory.GetReferences(m_dataSource.TextTokens(), string.Empty);

			Assert.AreEqual(result.GetUpperBound(0) + 1, tts.Count,
				"A different number of results was returned than what was expected.");

			for (int i = 0; i <= result.GetUpperBound(0); ++i)
			{
				Assert.AreEqual(result[i, 0], tts[i].InventoryText, "InventoryText number: " + i);
				Assert.AreEqual(result[i, 1], tts[i].Message, "Message number: " + i);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Matched()
		{
			Test(new string[0, 0], @"\p \v 1 [foo]");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnMatched()
		{
			string[,] result = new string[,]
			{
				{ "]", "Unmatched punctuation" },
				{ "[", "Unmatched punctuation" },
			};

			Test(result, @"\p \v 1 ]foo[");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NestedDifferentMatched()
		{
			Test(new string[0, 0], @"\p \v 1 (foo [bar] baz)");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NestedSameMatched()
		{
			Test(new string[0, 0], @"\p \v 1 (foo (bar) baz)");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MutipleUnMatched()
		{
			string[,] result = new string[,]
			{
				{ "(", "Unmatched punctuation" },
				{ "]", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" }
			};

			Test(result, @"\p \v 1 (foo] bar)");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Overlapping()
		{
			string[,] result = new string[,]
			{
				{ "(", "Overlapping pair" },
				{ "[", "Overlapping pair" },
				{ ")", "Overlapping pair" },
				{ "]", "Overlapping pair" }
			};

			Test(result, @"\p \v 1 (foo [bar) baz]");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BodyFootnote()
		{
			string[,] result = new string[,]
			{
				{ ")", "Unmatched punctuation" },
				{ "(", "Unmatched punctuation" },
			};

			Test(result, @"\p \v 1 (foo \f + bar)\f* baz");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteFootnote()
		{
			string[,] result = new string[,]
			{
				{ "(", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" },
			};

			Test(result, @"\p \v 1 \f + (foo\f* bar \f + baz)\f*");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ZMoreMatchedPairs()
		{
			string[,] result = new string[,]
			{
				{ "\u00A1", "Unmatched punctuation" }
			};

			m_dataSource.SetParameterValue("MatchedPairs", kMatchedPairXml2);
			Test(result, "\\p \\v 1 \u00A1hola");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IntroOutlineValue()
		{
			string[,] result = new string[,]
			{
				{ ")", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" },
			};

			Test(result, @"\io A.] foo 1) bar 2) baz");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IntroOutlineValueWrongDirection()
		{
			string[,] result = new string[,]
			{
				{ "[", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" },
			};

			Test(result, @"\io A.[ foo 1) bar 2) baz");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IntroOutlineValueDuplicate()
		{
			string[,] result = new string[,]
			{
				{ ")", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" },
			};

			Test(result, @"\io A.)] foo 1) bar 2) baz");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IntroOutline()
		{
			string[,] result = new string[,]
			{
				{ ")", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" },
			};

			Test(result, @"\io A. B.) foo 1) bar 2) baz");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestClosedByParagraphForNormalPara()
		{
			string[,] result = new string[,]
			{
				{ "(", "Unmatched punctuation" },
				{ ")", "Unmatched punctuation" },
			};

			Test(result, @"\p \v 1 (foo \p \v 2 bar)");
			Test(new string[0, 0], @"\p \v 1 {foo \p \v 2 bar}");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestClosedByParagraphForPoetryPara()
		{
			Test(new string[0, 0], @"\q1 \v 1 [foo \q2 \v 2 bar]");
			Test(new string[0, 0], @"\q1 \v 1 {foo \q2 \v 2 bar}");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHead()
		{
			Test(new string[,] {{ ")", "Unmatched punctuation" },}, @"\s text)");
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the Matched Pairs check using a data source that passes tokens similar
	/// to those produced by TE.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class MatchedPairsCheckUnitTest_Fw : ScrChecksTestBase
	{
		private TestChecksDataSource m_dataSource;

		#region Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up that happens before every test runs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_dataSource = new TestChecksDataSource();
			m_check = new MatchedPairsCheck(m_dataSource);
			m_dataSource.SetParameterValue("PoeticStyles", "Citation Line1" +
				CheckUtils.kStyleNamesDelimiter.ToString() + "Citation Line2");
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we correctly detect a paragraph start when it begins with a verse number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OpenParenFollowedByParaStartingWithVerseNum()
		{
			m_dataSource.SetParameterValue("MatchedPairs", MatchedPairsCheckUnitTest_Usfm.kMatchedPairXml1);

			m_dataSource.m_tokens.Add(new DummyTextToken("This is nice (and by nice, I mean",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("17",
				TextType.VerseNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken(" really, super nice). Amen?",
				TextType.Verse, false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 13, "(", "Unmatched punctuation");
			CheckError(1, m_dataSource.m_tokens[2].Text, 19, ")", "Unmatched punctuation");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote doesn't mess up processing of surrounding body text when a
		/// matched pair spans paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OpenFollowedByFootnoteFollowedByParaWithClosing()
		{
			m_dataSource.SetParameterValue("MatchedPairs", MatchedPairsCheckUnitTest_Usfm.kMatchedPairXml1);

			m_dataSource.m_tokens.Add(new DummyTextToken("This is nice (and by nice, I mean",
				TextType.Verse, true, false, "Citation Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Mean <> cruel",
				TextType.Note, true, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken(" text following footnote.",
				TextType.Verse, false, false, "Citation Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("really, super nice). Amen?",
				TextType.Verse, true, false, "Citation Line1"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(0, m_errors.Count);
		}
		#endregion
	}
}

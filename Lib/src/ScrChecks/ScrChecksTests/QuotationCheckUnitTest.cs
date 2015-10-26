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
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for the QuotationCheck class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class QuotationCheckUnitTest
	{
		public const string kUnmatchedOpeningMark = "Unmatched opening mark: level {0}";
		public const string kUnmatchedClosingMark = "Unmatched closing mark: level {0}";
		public const string kMissingContinuationMark = "Missing continuation mark: level {0}";
		public const string kMissingContinuationMarks = "Missing continuation marks: levels 1-{0}";
		public const string kUnexpectedOpeningMark = "Unexpected opening mark: level {0}";

		public const string kVerboseQuoteOpened = "Level {0} quote opened";
		public const string kVerboseQuoteClosed = "Level {0} quote closed";
		public const string kVerboseQuoteContinuer = "Level {0} quote continuer";

		UnitTestChecksDataSource m_source;

		private QuotationMarksList m_qmarks;

		#region Test setup
		[TestFixtureSetUp]
		public void TestSetup()
		{
			m_source = new UnitTestChecksDataSource();

			string stylesInfo =
				"<?xml version=\"1.0\" encoding=\"utf-16\"?><StylePropsInfo>" +
				"<SentenceInitial>" +
					"<StyleInfo StyleName=\"q1\" StyleType=\"paragraph\" UseType=\"line\"/>" +
					"<StyleInfo StyleName=\"q2\" StyleType=\"paragraph\" UseType=\"line\"/>" +
					"<StyleInfo StyleName=\"p\" StyleType=\"paragraph\" UseType=\"prose\"/>" +
					"<StyleInfo StyleName=\"d\" StyleType=\"paragraph\" UseType=\"prose\"/>" +
				"</SentenceInitial><ProperNouns/><Table/><List/>" +
				"<Special>" +
					"<StyleInfo StyleName=\"b\" StyleType=\"paragraph\" UseType=\"stanzabreak\"/>" +
				"</Special>" +
				"<Heading>" +
					"<StyleInfo StyleName=\"s\" StyleType=\"paragraph\" UseType=\"other\"/>" +
				"</Heading>" +
				"<Title/>" +
				"</StylePropsInfo>";

			m_source.SetParameterValue("StylesInfo", stylesInfo);
		}

		[SetUp]
		public void RunBeforeEachTest()
		{
			m_qmarks = QuotationMarksList.NewList();
			m_qmarks.QMarksList[0].Opening = "<<";
			m_qmarks.QMarksList[0].Closing = ">>";
			m_qmarks.QMarksList[1].Opening = "<";
			m_qmarks.QMarksList[1].Closing = ">";
			m_qmarks.EnsureLevelExists(5);
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}
		#endregion

		#region Helper methods
		public static string FormatMessage(string format, int level)
		{
			return string.Format(format, level);
		}

		void Test(string[,] result, string text)
		{
			m_source.Text = text;

			QuotationCheck check = new QuotationCheck(m_source);
			List<TextTokenSubstring> tts =
				check.GetReferences(m_source.TextTokens(), "");

			for (int i = 0; i < tts.Count; i++)
			{
				Console.WriteLine(tts[i].Text);
				Console.WriteLine(tts[i].Message);
				Debug.WriteLine(tts[i].Text);
				Debug.WriteLine(tts[i].Message);
			}

			Assert.AreEqual(result.GetUpperBound(0) + 1, tts.Count,
				"A different number of results was returned than what was expected." );

			for (int i = 0; i <= result.GetUpperBound(0); ++i)
			{
				// Verify the Reference, Message, and Details columns of the results pane.
				// Verifies empty string, but not null, for the reference (for original tests).
				if (result.GetUpperBound(1) == 2)
					Assert.AreEqual(result[i, 2], tts[i].FirstToken.ScrRefString, "Reference number: " + i);

				Assert.AreEqual(result[i, 0], tts[i].Text, "Text number: " + i.ToString());
				Assert.AreEqual(result[i, 1], tts[i].Message, "Message number: " + i.ToString());
			}
		}

		#endregion

		#region Top-level quotes
		[Test]
		public void TopLevelNoCloser()
		{
			m_qmarks.QMarksList[0].Closing = string.Empty;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
			Test(new string[0,0], @"\p \v 1 <<foo \v 2 <<foo");
		}

		[Test]
		public void TopLevel()
		{
			Test(new string[,] {
				{ ">>", FormatMessage(kUnmatchedClosingMark, 1) },
				{ "<<", FormatMessage(kUnmatchedOpeningMark, 1) },
				}, @"\p \v 1 <<foo bar>> qux>> <<quux");
		}

		[Test]
		public void TopLevelMissingClosing()
		{
			Test(new string[,] {
				{ "<<", FormatMessage(kUnmatchedOpeningMark, 1) },
				}, @"\p \v 1 <<foo");
		}

		[Test]
		public void TopLevelMissingOpening()
		{
			Test(new string[,] {
				{ ">>", FormatMessage(kUnmatchedClosingMark, 1) },
			}, @"\p \v 1 foo>>");
		}

		[Test]
		public void OtherQuotesAndParameters()
		{
			m_qmarks.QMarksList[0].Opening = "\u201C";
			m_qmarks.QMarksList[0].Closing = "\u201D";
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
			Test(new string[,] {
				{ "\u201C", FormatMessage(kUnmatchedOpeningMark, 1) },
			}, "\\p \\v 1 \u201Cfoo");
		}
		#endregion

		#region Inner level quotes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test this scenario:
		/// When {< >} are inner quotes and not followed by anything... The inner open is used
		/// and not followed by other quotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InnerLevel_ClosingAndTopLevelMissing()
		{
			Test(new string[,] {
				{ "<", FormatMessage(kUnexpectedOpeningMark, 2) },
				{ "<", FormatMessage(kUnmatchedOpeningMark, 2) },
				}, @"\p \v 1 <foo");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test this scenario:
		/// When {< >} are inner quotes... The inner open is used and followed by valid outer quotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InnerLevel_ClosingAndTopLevelAfter()
		{
			Test(new string[,] {
				{ "<", FormatMessage(kUnexpectedOpeningMark, 2) },
				{ "<", FormatMessage(kUnmatchedOpeningMark, 2) },
				}, @"\p \v 1 <foo <<foofoo>>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test this scenario:
		/// When {< >} are inner quotes... The inner open is used and followed by valid outer
		/// left and right double turned quotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InnerLevel_ClosingAndTopLevelAfter2()
		{
			m_qmarks.QMarksList[0].Opening = "\u201C";
			m_qmarks.QMarksList[0].Closing = "\u201D";
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
			Test(new string[,] {
				{ "<", FormatMessage(kUnexpectedOpeningMark, 2) },
				{ "<", FormatMessage(kUnmatchedOpeningMark, 2) }
				}, @"\p \v 1 <foo \u201Cfoofoo\u201D");
		}

		[Test]
		public void Inner()
		{
			Test(new string[0,0],
				@"\p \v 1 <<foo <bar> baz>>");
		}

		[Test]
		public void InnerMissingClosing()
		{
			Test(new string[,] {
				{ "<", FormatMessage(kUnmatchedOpeningMark, 2) },
				}, @"\p \v 1 <<foo <bar baz>>");
		}

		[Test]
		public void InnerMissingOpening()
		{
			Test(new string[,] {
				{ ">", FormatMessage(kUnmatchedClosingMark, 2) },
				}, @"\p \v 1 <<foo bar> baz>>");
		}
		#endregion

		#region Third level quotes - repeated level 1 as level 3
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Thirds the level embedding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ThirdLevelEmbedding()
		{
			Test(new string[0,0],
				@"\p \v 1 <<foo <bar <<baz>> qux> quux>>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inners the inner missing closing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InnerInnerMissingClosing()
		{
			Test(new string[,] {
				{ "<<", FormatMessage(kUnmatchedOpeningMark, 3) },
				}, @"\p \v 1 <<foo <bar <<baz qux> quux>>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inners the inner missing opening.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InnerInnerMissingOpening()
		{
			m_qmarks.Clear();
			m_qmarks.EnsureLevelExists(3);
			m_qmarks[0].Opening = "<<<";
			m_qmarks[0].Closing = ">>>";
			m_qmarks[1].Opening = "<<";
			m_qmarks[1].Closing = ">>";
			m_qmarks[2].Opening = "<";
			m_qmarks[2].Closing = ">";
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[,] {
				{ ">", FormatMessage(kUnmatchedClosingMark, 3) },
				}, @"\p \v 1 <<<foo <<bar baz> qux>> quux>>>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Others the quotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OtherQuotes()
		{
			Test(new string[0, 0], @"<<foo <bar <<baz>> qux> quux>>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Threes the distinct levels.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ThreeDistinctLevels()
		{
			m_qmarks.AddLevel();
			m_qmarks.QMarksList[2].Opening = "[";
			m_qmarks.QMarksList[2].Closing = "]";
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
			Test(new string[0, 0], @"<<foo <bar [baz] qux> quux>>");
		}

		#endregion

		#region Quote continuation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueNoMarkAllLevels()
		{
			m_qmarks.ContinuationType = ParagraphContinuationType.None;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.None;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[0, 0],
				@"\p <<foo foos <bar bars \q1 baz bazs\p qux quxs> >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatInnerMostOpening()
		{
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireInnermost;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[0, 0],
				@"\p <<foo foos <bar bars \q1 <baz bazs qux quxs> >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatAllOpening()
		{
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireAll;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[0, 0],
				@"\p <<foo foos <bar bars \q1 << <baz bazs qux quxs> >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatInnerMostClosing()
		{
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireInnermost;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Closing;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[0, 0],
				@"\p <<foo foos <bar bars \q1 >baz bazs qux quxs> >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatAllClosing()
		{
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireAll;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Closing;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[0, 0],
				@"\p <<foo foos <bar bars \q1 >> >baz bazs qux quxs> >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatAllClosing_3Levels()
		{
			m_qmarks.EnsureLevelExists(3);
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireAll;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Closing;
			m_qmarks[2].Opening = "[";
			m_qmarks[2].Closing = "]";
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[0, 0],
				@"\p <<foo foos <bar [bars \q1 >> > ]baz bazs] qux quxs> >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatInnerMostOpening_3Levels()
		{
			m_qmarks.EnsureLevelExists(3);
			m_qmarks[2].Opening = "[";
			m_qmarks[2].Closing = "]";
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireInnermost;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[0, 0],
				@"\p <<foo foos <bar bars \q1 <baz [bazs qux] quxs> >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatInnerMostOpeningMissing()
		{
			m_qmarks.EnsureLevelExists(3);
			m_qmarks[2].Opening = "[";
			m_qmarks[2].Closing = "]";
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireInnermost;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[,] {
				{ "baz", FormatMessage(kMissingContinuationMark, 2) },
			}, @"\p <<foo foos <bar bars \q1 baz [bazs qux] quxs> >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatInnerMostClosingMissing()
		{
			m_qmarks.EnsureLevelExists(3);
			m_qmarks[2].Opening = "[";
			m_qmarks[2].Closing = "]";
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireInnermost;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Closing;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[,] {
				{ "baz", FormatMessage(kMissingContinuationMark, 2) },
			}, @"\p <<foo foos <bar bars \q1 baz [bazs qux] quxs> >>");
		}

		#endregion

		#region Deeply Nested Quotes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deeplies the nested quotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeeplyNestedQuotes()
		{
			m_qmarks.EnsureLevelExists(5);
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[0, 0], @"\p \v 1 << < <<foo < <<bar baz qux>> > quux>> > >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deeplies the nested quotes missing closer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeeplyNestedQuotesMissingCloser()
		{
			m_qmarks.EnsureLevelExists(5);
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireAll;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[,] {
				{ "<<", FormatMessage(kUnmatchedOpeningMark, 5) },
			}, @"\p \v 1 << < <<foo < <<bar baz qux > quux>> > >>");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deeplies the nested quotes no nesting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ToManyLevels()
		{
			m_qmarks = QuotationMarksList.NewList();
			m_qmarks.QMarksList[0].Opening = "<<";
			m_qmarks.QMarksList[0].Closing = ">>";
			m_qmarks.QMarksList[1].Opening = "<";
			m_qmarks.QMarksList[1].Closing = ">";
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);

			Test(new string[,] {
				{ "<", FormatMessage(kUnmatchedOpeningMark, 2) },
				{ "<<", FormatMessage(kUnmatchedOpeningMark, 1) },
				{ ">", FormatMessage(kUnmatchedClosingMark, 2) },
				{ ">>", FormatMessage(kUnmatchedClosingMark, 1) },
				}, @"\p \v 1 << <foo <<bar baz>> qux> quux>>");

			Test(new string[,] {
				{ "<", FormatMessage(kUnmatchedOpeningMark, 2) },
				{ ">", FormatMessage(kUnmatchedClosingMark, 2) },
				}, @"\p \v 1 << <foo <bar baz> qux> quux>>");
		}

		#endregion

		#region Tests for verbose option
		[Test]
		public void VerboseOptionNormal()
		{
			m_source.SetParameterValue("VerboseQuotes", "Yes");
			Test(new string[,] {
				{ "<<", FormatMessage(kVerboseQuoteOpened, 1) },
				{ "<", FormatMessage(kVerboseQuoteOpened, 2) },
				{ ">", FormatMessage(kVerboseQuoteClosed, 2) },
				{ ">>", FormatMessage(kVerboseQuoteClosed, 1) },
				}, @"\p \v 1 <<foo <foo> >>");
		}
		#endregion

		// The tests verify the inconsistencies found in various combinations of:
		// Correct or incorrect quotation marks
		// Appropriate or inappropriate writing system properties

		#region New test setup of quotation marks for levels

		void SetupEnglish1()
		{
			m_qmarks = QuotationMarksList.NewList();
			m_qmarks.RemoveLastLevel();
			m_qmarks.QMarksList[0].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[0].Closing = "\u201D"; // Right double quotation mark
			m_qmarks.ContinuationMark = ParagraphContinuationMark.None;
			m_qmarks.ContinuationType = ParagraphContinuationType.None;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupEnglish2()
		{
			m_qmarks = QuotationMarksList.NewList();
			m_qmarks.EnsureLevelExists(2);
			m_qmarks.QMarksList[0].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[0].Closing = "\u201D"; // Right double quotation mark
			m_qmarks.QMarksList[1].Opening = "\u2018"; // Left single quotation mark
			m_qmarks.QMarksList[1].Closing = "\u2019"; // Right single quotation mark
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupEnglish3() // TO DO: Is there a way to limit it?
		{
			m_qmarks = QuotationMarksList.NewList();
			m_qmarks.EnsureLevelExists(3);
			m_qmarks.QMarksList[0].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[0].Closing = "\u201D"; // Right double quotation mark
			m_qmarks.QMarksList[1].Opening = "\u2018"; // Left single quotation mark
			m_qmarks.QMarksList[1].Closing = "\u2019"; // Right single quotation mark
			m_qmarks.QMarksList[2].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[2].Closing = "\u201D"; // Right double quotation mark
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupEnglish4()
		{
			m_qmarks = QuotationMarksList.NewList();
			m_qmarks.EnsureLevelExists(4);
			m_qmarks.QMarksList[0].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[0].Closing = "\u201D"; // Right double quotation mark
			m_qmarks.QMarksList[1].Opening = "\u2018"; // Left single quotation mark
			m_qmarks.QMarksList[1].Closing = "\u2019"; // Right single quotation mark
			m_qmarks.QMarksList[2].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[2].Closing = "\u201D"; // Right double quotation mark
			m_qmarks.QMarksList[3].Opening = "\u2018"; // Left single quotation mark
			m_qmarks.QMarksList[3].Closing = "\u2019"; // Right single quotation mark
			m_qmarks.ContinuationType = ParagraphContinuationType.None;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.None;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupEuropean1()
		{
			m_qmarks.RemoveLastLevel();
			m_qmarks.QMarksList[0].Opening = "«"; // Left-pointing double angle quotation mark
			m_qmarks.QMarksList[0].Closing = "»"; // Right-pointing double angle quotation mark
			m_qmarks.ContinuationType = ParagraphContinuationType.None;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.None;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupPortuguese2()
		{
			m_qmarks.EnsureLevelExists(2);
			m_qmarks.QMarksList[0].Opening = "«"; // Left-pointing double angle quotation mark
			m_qmarks.QMarksList[0].Closing = "»"; // Right-pointing double angle quotation mark
			m_qmarks.QMarksList[1].Opening = "«"; // Left-pointing double angle quotation mark
			m_qmarks.QMarksList[1].Closing = "»"; // Right-pointing double angle quotation mark
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupPortuguese3()
		{
			// A Boa Nova, Good News for Modern Man in Portuguese:
			// * Uses the same marks for levels one, two, and three (which is rare).
			// * Instead of continuing marks, closes, and then reopens quotations
			//   at section heads (for example, in MAT 5-7).
			m_qmarks.EnsureLevelExists(3);
			m_qmarks.QMarksList[0].Opening = "«"; // Left-pointing double angle quotation mark
			m_qmarks.QMarksList[0].Closing = "»"; // Right-pointing double angle quotation mark
			m_qmarks.QMarksList[1].Opening = "«"; // Left-pointing double angle quotation mark
			m_qmarks.QMarksList[1].Closing = "»"; // Right-pointing double angle quotation mark
			m_qmarks.QMarksList[2].Opening = "«"; // Left-pointing double angle quotation mark
			m_qmarks.QMarksList[2].Closing = "»"; // Right-pointing double angle quotation mark
			m_qmarks.ContinuationType = ParagraphContinuationType.None;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.None;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupSpanish2()
		{
			// Spanish has a distinct mark for level 3.
			m_qmarks.EnsureLevelExists(2);
			m_qmarks.QMarksList[0].Opening = "«"; // Left-pointing double angle quotation mark
			m_qmarks.QMarksList[0].Closing = "»"; // Right-pointing double angle quotation mark
			m_qmarks.QMarksList[1].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[1].Closing = "\u201D"; // Right double quotation mark
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireInnermost;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
			//m_source.SetParameterValue("ContinueQuotes", "Yes");
			//m_source.SetParameterValue("ContinueInnerQuotes", "Yes");
			//m_source.SetParameterValue("NestingAlternates", "No");
		}

		void SetupSpanish2_QuotationDash()
		{
			// Spanish has a distinct mark for level 3.
			m_qmarks.EnsureLevelExists(2);
			m_qmarks.QMarksList[0].Opening = "\u2014"; // Em dash
			m_qmarks.QMarksList[0].Closing = string.Empty;
			m_qmarks.QMarksList[1].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[1].Closing = "\u201D"; // Right double quotation mark
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupSpanish3()
		{
			// Spanish has a distinct mark for level 3.
			m_qmarks.EnsureLevelExists(3);
			m_qmarks.QMarksList[0].Opening = "«"; // Left-pointing double angle quotation mark
			m_qmarks.QMarksList[0].Closing = "»"; // Right-pointing double angle quotation mark
			m_qmarks.QMarksList[1].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[1].Closing = "\u201D"; // Right double quotation mark
			m_qmarks.QMarksList[2].Opening = "\u2018"; // Left single quotation mark
			m_qmarks.QMarksList[2].Closing = "\u2019"; // Right single quotation mark
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupSpanish3_QuotationDash()
		{
			// Spanish has a distinct mark for level 3.
			m_qmarks.EnsureLevelExists(3);
			m_qmarks.QMarksList[0].Opening = "\u2014"; // Em dash
			m_qmarks.QMarksList[0].Closing = string.Empty;
			m_qmarks.QMarksList[1].Opening = "\u201C"; // Left double quotation mark
			m_qmarks.QMarksList[1].Closing = "\u201D"; // Right double quotation mark
			m_qmarks.QMarksList[2].Opening = "\u2018"; // Left single quotation mark
			m_qmarks.QMarksList[2].Closing = "\u2019"; // Right single quotation mark
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupSwiss2() // for French, German, or Italian
		{
			m_qmarks.EnsureLevelExists(2);
			m_qmarks.QMarksList[0].Opening = "«"; // Left-pointing double angle quotation mark
			m_qmarks.QMarksList[0].Closing = "»"; // Right-pointing double angle quotation mark
			m_qmarks.QMarksList[1].Opening = "\u2039"; // Single left-pointing angle quotation mark
			m_qmarks.QMarksList[1].Closing = "\u203A"; // Single right-pointing angle quotation mark
			m_qmarks.ContinuationType = ParagraphContinuationType.None;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.None;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}
		#endregion

		#region New test setup for quotation continuation across paragraphs

		void SetupContinuationAllOpening()
		{
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireAll;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}

		void SetupContinuationInnermost()
		{
			m_qmarks.ContinuationType = ParagraphContinuationType.RequireInnermost;
			m_qmarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_source.SetParameterValue("QuotationMarkInfo", m_qmarks.XmlString);
		}
		#endregion

		#region Level 1 quotation marks

		// Level1_OnePair is based on MAT 2:2 (NIV and especially NLT) but the marks differ

		[Test]
		public void Level1_OnePair_European_Correct()
		{
			SetupEuropean1();
			Test(new string[0, 0], "\\id MAT \\c 2 \\p \\v 1 asking, \\v 2 «Level one.»");
		}

		[Test]
		public void Level1_OnePair_European_UnmatchedOpeningMark()
		{
			SetupEuropean1();
			Test(new string[,] {
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "2:2" }
			}, "\\id MAT \\c 2 \\p \\v 1 asking, \\v 2 «Level one.");
		}

		[Test]
		public void Level1_OnePair_European_UnmatchedClosingMark()
		{
			SetupEuropean1();
			Test(new string[,] {
				{ "»", FormatMessage(kUnmatchedClosingMark, 1), "2:2" }
			}, "\\id MAT \\c 2 \\p \\v 1 asking, \\v 2 Level one.»");
		}

		// The check might not find inconsistencies if checking properties are inappropriate
		// or if the character is not a quotation mark for the writing system.

		[Test]
		public void Level1_OnePair_Inappropriate_Correct()
		{
			SetupEnglish1();
			Test(new string[0, 0], "\\id MAT \\c 2 \\p \\v 1 asking, \\v 2 «Level one.»");
		}

		[Test]
		public void Level1_OnePair_Inappropriate_UnmatchedOpeningMark()
		{
			SetupEnglish1();
			Test(new string[0, 0], "\\id MAT \\c 2 \\p \\v 1 asking, \\v 2 «Level one.");
		}

		[Test]
		public void Level1_OnePair_Inappropriate_UnmatchedClosingMark()
		{
			SetupEnglish1();
			Test(new string[0, 0], "\\id MAT \\c 2 \\p \\v 1 asking, \\v 2 Level one.»");
		}

		// Same marks as NIV and NLT

		[Test]
		public void Level1_OnePair_English_Correct()
		{
			SetupEnglish1();
			Test(new string[0, 0], "\\id MAT \\c 2 \\p \\v 1 asking, \\v 2 \u201CLevel one.\u201D");
		}

		[Test]
		public void Level1_OnePair_English_UnmatchedOpeningMark()
		{
			SetupEnglish1();
			Test(new string[,] {
				{ "\u201C", FormatMessage(kUnmatchedOpeningMark, 1), "2:2" }
			}, "\\id MAT \\c 2 \\p \\v 1 asking, \\v 2 \u201CLevel one.");
		}

		[Test]
		public void Level1_OnePair_English_UnmatchedClosingMark()
		{
			SetupEnglish1();
			Test(new string[,] {
				{ "\u201D", FormatMessage(kUnmatchedClosingMark, 1), "2:2" }
			}, "\\id MAT \\c 2 \\p \\v 1 asking, \\v 2 Level one.\u201D");
		}

		// Level1_TwoPairs is based on MAT 2:13 (NIV and especially NLT) but the marks differ

		[Test]
		public void Level1_TwoPairs_European_Correct()
		{
			SetupEuropean1();
			Test(new string[0, 0], "\\id MAT \\c 2 \\p \\v 13 in a dream. «Level one,» the angel said, «level one.»");
		}

		[Test]
		public void Level1_TwoPairs_European_UnmatchedOpeningMark()
		{
			SetupEuropean1();
			Test(new string[,] {
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "2:13" }
			}, "\\id MAT \\c 2 \\p \\v 13 in a dream. «Level one, the angel said, «level one.»");
		}

		[Test]
		public void Level1_TwoPairs_European_UnmatchedClosingMark()
		{
			SetupEuropean1();
			Test(new string[,] {
				{ "»", FormatMessage(kUnmatchedClosingMark, 1), "2:13" }
			}, "\\id MAT \\c 2 \\p \\v 13 in a dream. «Level one,» the angel said, level one.»");
		}

		// Level1_ThreePairs is based on MAT 8:3-4 (NIV and NLT) but the marks differ

		[Test]
		public void Level1_ThreePairs_European_Correct()
		{
			SetupEuropean1();
			Test(new string[0, 0], "\\id MAT \\c 8 \\p \\v 3 «Level one,» he said. «level one.» \\v 4 Then said to him, «level one.»");
		}

		[Test]
		public void Level1_ThreePairs_European_UnmatchedClosingAndOpeningMarks()
		{
			SetupEuropean1();
			Test(new string[,] {
				{ "»", FormatMessage(kUnmatchedClosingMark, 1), "8:3" },
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "8:4" }
			}, "\\id MAT \\c 8 \\p \\v 3 «Level one,» he said. level one.» \\v 4 Then said to him, «level one.");
		}
		#endregion

		#region Level 1 quotation marks in section headings

		// Level1_HebrewTitle is based on PSA 9:1 (NIV and NLT) but the marks differ
		// Does the Test code recognize \d Hebrew Title?

		[Test]
		public void Level1_HebrewTitle_European_Correct()
		{
			SetupEuropean1();
			Test(new string[0, 0], "\\id PSA \\c 9 \\d to the tune of «Level one.» \\q1 \\v 1 Line one; \\q2 Line two.");
		}

		[Test]
		public void Level1_HebrewTitle_European_UnmatchedOpeningMark()
		{
			SetupEuropean1();
			Test(new string[,] {
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "9:0" } // Does the check know that the reference is verse 1?
			}, "\\id PSA \\c 9 \\d to the tune of «Level one. \\q1 \\v 1 Line one; \\q2 Line two.");
		}

		[Test]
		public void Level1_HebrewTitle_European_UnmatchedClosingMark()
		{
			SetupEuropean1();
			Test(new string[,] {
				{ "»", FormatMessage(kUnmatchedClosingMark, 1), "9:0" } // Does the check know that the reference is verse 1?
			}, "\\id PSA \\c 9 \\d to the tune of Level one.» \\q1 \\v 1 Line one; \\q2 Line two.");
		}

		// Level1_SectionHead is based on 1CO 1:10 (J.B. Phillips) but the marks differ
		// Does the Test code recognize \s Section Head?

		[Test]
		public void Level1_SectionHead_European_Correct()
		{
			SetupEuropean1();
			Test(new string[0, 0], "\\id 1CO \\c 1 \\v 9 \\s But I am anxious over your «divisions» \\p \\v 10 Now I do beg you.");
		}

		[Test]
		public void Level1_SectionHead_European_UnmatchedOpeningMark()
		{
			SetupEuropean1();
			Test(new string[,] {
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "1:9" } // Does the check know that the reference is verse 10?
			}, "\\id 1CO \\c 1 \\v 9 \\s But I am anxious over your «divisions \\p \\v 10 Now I do beg you.");
		}

		[Test]
		public void Level1_SectionHead_European_UnmatchedClosingMark()
		{
			SetupEuropean1();
			Test(new string[,] {
				{ "»", FormatMessage(kUnmatchedClosingMark, 1), "1:9" } // Does the check know that the reference is verse 10?
			}, "\\id 1CO \\c 1 \\v 9 \\s But I am anxious over your divisions» \\p \\v 10 Now I do beg you.");
		}
		#endregion

		#region Level 2 quotation marks

		// Level2_OnePair is based on MAT 5:38-39 (NIV) but the marks differ

		[Test]
		public void Level2_OnePair_Correct()
		{
			// In the actual context, the level 1 quotation continues preceding and following.
			SetupSwiss2();
			Test(new string[0, 0], "\\id MAT \\c 5 \\p \\v 38 «Level one says, \u2039Level two, and level two.\u203A \\v 39 But, Level one.»");
		}

		[Test]
		public void Level2_OnePair_UnmatchedOpeningMark()
		{
			SetupSwiss2();
			Test(new string[,] {
				{ "\u2039", FormatMessage(kUnmatchedOpeningMark, 2), "5:38" }
			}, "\\id MAT \\c 5 \\p \\v 38 «Level one says, \u2039Level two, and level two. \\v 39 But, Level one.»");
		}

		[Test]
		public void Level2_OnePair_UnmatchedClosingMark()
		{
			SetupSwiss2();
			Test(new string[,] {
				{ "\u203A", FormatMessage(kUnmatchedClosingMark, 2), "5:38" }
			}, "\\id MAT \\c 5 \\p \\v 38 «Level one says, Level two, and level two.\u203A \\v 39 But, Level one.»");
		}

		[Test]
		public void Level2_OnePair_UnexpectedAndUnmatchedOpeningMark()
		{
			SetupSwiss2();
			Test(new string[,] {
				{ "\u2039", FormatMessage(kUnexpectedOpeningMark, 2), "5:38" },
				{ "\u2039", FormatMessage(kUnmatchedOpeningMark, 2), "5:38" }
			}, "\\id MAT \\c 5 \\p \\v 38 Level one says, \u2039Level two, and level two. \\v 39 But, Level one.");
		}

		// Level2_OnePairFollowedByLevel1 is based on MAT 9:12-14 (NLT) but the marks differ

		[Test]
		public void Level2_OnePairFollowedByLevel1_UnexpectedOpeningMark()
		{
			// The first first-level quotation is marked with characters that are not quotation marks for the writing system.
			SetupSwiss2();
			Test(new string[,] {
				{ "\u2039", FormatMessage(kUnexpectedOpeningMark, 2), "9:13" }
			}, "\\id MAT \\c 9 \\p \\v 12 \\v 13 He added, \u201CLevel one \u2039Level two.\u203A Level one.\u201D \\p \\v 14 Asked him, «Level one.»");
		}

		// Level2_ThreePairs is based on MAT 8:8-9 (NLT) but the marks differ

		[Test]
		public void Level2_ThreePairs_UnexpectedOpeningMark()
		{
			// Might happen if you do not notice that first-level marks were missing
			// when you inserted second-level marks.
			SetupSwiss2();
			Test(new string[,] {
				{ "\u2039", FormatMessage(kUnexpectedOpeningMark, 2), "8:9" },
			}, "\\id MAT \\c 8 \\p \\v 8 Said, Level one. \\v 9 I say \u2039Two,\u203A and they go, or \u2039Two,\u203A and they come. If I say, \u2039Two,\u203A they do it.»");
		}

		[Test]
		public void Level2_ThreePairs_UnexpectedOpeningMark_InappropriateProperties()
		{
			// Might happen after you imported from source with different first-level marks,
			// which the check does not notice until you insert second-level marks
			// where they did not occur in the source.
			SetupSwiss2();
			Test(new string[,] {
				{ "\u2039", FormatMessage(kUnexpectedOpeningMark, 2), "8:9" }
			}, "\\id MAT \\c 8 \\p \\v 8 Said, \u201CLevel one. \\v 9 I say \u2039Two,\u203A and they go, or \u2039Two,\u203A and they come. If I say, \u2039Two,\u203A they do it.\u201D");
		}

		// Level2_TwoPairs is based on MAT 5:33-37 (A Boa Nova, Portuguese Good News)

		[Test]
		public void Level2_TwoPairs_Correct()
		{
			// In the actual context, the level 1 quotation continues preceding and following.
			SetupPortuguese2();
			Test(new string[0, 0], "\\id MAT \\c 5 \\p \\v 33 «Level one says: In italics not quotation marks. \\v 37 Let your «Yes» and «No» level one.»");
		}

		// Level2_FivePairs is based on MAT 5:33-37 (NIV) but the marks differ

		[Test]
		public void Level2_FivePairs_Correct()
		{
			// In the actual context, the level 1 quotation continues preceding and following.
			SetupSwiss2();
			Test(new string[0, 0], "\\id MAT \\c 5 \\p \\v 33 «Level one says, \u2039Level two.\u203A \\v 37 Let your \u2039Yes\u203A be \u2039Yes\u203A and your \u2039No,\u203A \u2039No\u203A; level one.»");
		}

		[Test]
		public void Level2_FivePairs_UnmatchedOpeningMark()
		{
			SetupSwiss2();
			Test(new string[,] {
				{ "\u2039", FormatMessage(kUnmatchedOpeningMark, 2), "5:37" }
			}, "\\id MAT \\c 5 \\p \\v 33 «Level one says, \u2039Level two.\u203A \\v 37 Let your \u2039Yes be \u2039Yes\u203A and your \u2039No,\u203A \u2039No\u203A; level one.»");
		}

		[Test]
		public void Level2_FivePairs_UnmatchedClosingMark()
		{
			SetupSwiss2();
			Test(new string[,] {
				{ "\u203A", FormatMessage(kUnmatchedClosingMark, 2), "5:37" }
			}, "\\id MAT \\c 5 \\p \\v 33 «Level one says, \u2039Level two.\u203A \\v 37 Let your \u2039Yes\u203A be Yes\u203A and your \u2039No,\u203A \u2039No\u203A; level one.»");
		}

		[Test]
		public void Level2_FivePairs_UnmatchedOpeningAndClosingMarks()
		{
			SetupSwiss2();
			Test(new string[,] {
				{ "\u2039", FormatMessage(kUnmatchedOpeningMark, 2), "5:37" },
				{ "\u203A", FormatMessage(kUnmatchedClosingMark, 2), "5:37" }
			}, "\\id MAT \\c 5 \\p \\v 33 «Level one says, \u2039Level two.\u203A \\v 37 Let your \u2039Yes be \u2039Yes\u203A and your \u2039No,\u203A No\u203A; level one.»");
		}

		[Test]
		public void Level2_FivePairs_UnmatchedClosingAndOpeningMarks()
		{
			SetupSwiss2();
			Test(new string[,] {
				{ "\u203A", FormatMessage(kUnmatchedClosingMark, 2), "5:37" },
				{ "\u2039", FormatMessage(kUnmatchedOpeningMark, 2), "5:37" }
			}, "\\id MAT \\c 5 \\p \\v 33 «Level one says, \u2039Level two.\u203A \\v 37 Let your \u2039Yes\u203A be Yes\u203A and your \u2039No, \u2039No\u203A; level one.»");
		}
		#endregion

		#region Same marks for multiple levels

		// Level2_QuotedText is based on MAT 5:33-37 (A Boa Nova)

		[Test]
		public void Level2_QuotedText_Correct()
		{
			SetupPortuguese3();
			Test(new string[0, 0], "\\id MAT \\c 5 \\p \\v 33 «Level one was said: \\qt Quoted text instead of level two. \\qt* But I say: What I say. \\v 37 Let your «yes» be yes, and your «no» no.»");
		}

		// Level2_SameMarks is based on MAT 7:1-6 (A Boa Nova)

		[Test]
		public void Level2_SameMarks_Correct()
		{
			SetupPortuguese3();
			Test(new string[0, 0], "\\id MAT \\c 7 \\p \\v 1 «Level one. \\v 4 How can you say: «Level two», level \\v 5 one. \\p \\v 6 No continuation mark.»");
		}

		// Level2_MissingClosingMark is based on MAT 6:25-7:6 (A Boa Nova)

		[Test]
		public void Level2_MissingClosingMark_UnmatchedOpeningMark()
		{
			// In MAT 5-7, each section consists of a separate quotation.
			// If you omit a closing mark at the end of a section,
			// and if the following section contains a level 2 quotation, it seems to be a level 3 quotation.
			// Can the check limit the number of levels?
			SetupPortuguese3();
			Test(new string[,] {
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "6:25" }
			}, "\\id MAT \\c 6 \\v 25 \\p «Section with a missing closing mark. \\c 7 \\p \\v 1 «Level one. \\v 4 How can you say: «Level two», level \\v 5 one. \\p \\v 6 No continuation mark.»");
		}

		// Level2_OnlyOneClosingMark is based on MAT 7:21-23 (A Boa Nova)

		[Test]
		public void Level2_OnlyOneClosingMark_UnmatchedOpeningMark()
		{
			// If levels one and two have the same marks, and if both levels close at the same place,
			// the check displays an inconsistency that translators need to ignore.
			SetupPortuguese3();
			Test(new string[,] {
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "7:21" }
			}, "\\id MAT \\c 7 \\p \\v 21 «Not everyone who says: «Level two!», level one. \\v 22 Many will say: «Level two?» \\v 23 Then I will tell them: «Levels two and one end here!»");
		}

		// Level3_OnlyOneClosingMark is based on LUK 12:16-21 (A Boa Nova)

		[Test]
		public void Level3_OnlyOneClosingMark_UnmatchedOpeningMark()
		{
			SetupPortuguese3();
			Test(new string[,] {
				{ "«", FormatMessage(kUnmatchedOpeningMark, 2), "12:17" },
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "12:16" } // Does the check return this message first?
			}, "\\id LUK \\c 12 \\p \\v 16 He told them: «Level one. \\v 17 So he said: «Level two \\v 18 \\v 19 and say to myself: «Level three and level two ends here too.», \\v 20 But God said to him: «Level two?» \\p \\v 21 Jesus said: «Level two and level one ends here too.»");
		}
		#endregion

		#region Level 3 with same marks as level 1

		// For level 3 with same marks as level 1,
		// the check displays misleading inconsistencies instead of unmatched closing mark: level 3.

		// The following tests omit paragraph marks so that continuation does not matter.

		[Test]
		public void Level3_Recycled_Correct()
		{
			SetupEnglish3();
			Test(new string[0, 0], "\\id LUK \\c 12 \\p \\v 16 He told: \u201CLevel one. \\v 17 He thought, \u2018Level two.\u2019 \\v 18 \u2018Two. \\v 19 To myself, \u201Clevel three.\u201D \u2019 \\v 20 \u2018Two.\u2019 \\v 21 Close level one.\u201D");
		}
		#endregion

		#region Level 3 with distinct marks

		// The following tests omit paragraph marks so that continuation does not matter.

		[Test]
		public void Level3_Distinct_Correct()
		{
			SetupSpanish3();
			Test(new string[0, 0], "\\id LUK \\c 12 \\p \\v 16 He told: «Level one. \\v 17 He thought, \u201Clevel two.\u201D \\v 18 \u201CTwo. \\v 19 To myself, \u2018Level three.\u2019 \u201D \\v 20 \u201CTwo.\u201D \\v 21 Close level one.»");
		}

		[Test]
		public void Level3_Distinct_UnmatchedOpeningMark()
		{
			SetupSpanish3();
			Test(new string[,] {
				{ "\u2018", FormatMessage(kUnmatchedOpeningMark, 3), "12:19" }
				}, "\\id LUK \\c 12 \\p \\v 16 He told: «Level one. \\v 17 He thought, \u201Clevel two.\u201D \\v 18 \u201CTwo. \\v 19 To myself, \u2018Level three.\u201D \\v 20 \u201CTwo.\u201D \\v 21 Close level one.»");
		}

		[Test]
		public void Level3_Distinct_UnmatchedClosingMark()
		{
			SetupSpanish3();
			Test(new string[,] {
				{ "\u2019", FormatMessage(kUnmatchedClosingMark, 3), "12:19" }
				}, "\\id LUK \\c 12 \\p \\v 16 He told: «Level one. \\v 17 He thought, \u201Clevel two.\u201D \\v 18 \u201CTwo. \\v 19 To myself, Level three.\u2019 \u201D \\v 20 \u201CTwo.\u201D \\v 21 Close level one.»");
		}
		#endregion

		#region Levels 3-4 with same marks as levels 1-2
		// We have not yet found four levels in a translation that has paragraph structure.

		// Level4_Recycled is based on JER 29:24-28 (NASB)
		// Inserted \p in verse 28 to account for continuation, but that does not seem quite right,
		// because NASB has verse structure, instead of paragraph structure.
		// The main paragraphs are at verses 24 and 29.

		public void Level4_Recycled_Continuation_InappropriateProperties_UnmatchedOpeningMark3()
		{
			SetupEnglish4();
			// If there is a continuation mark, but the properties indicate no mark needed,
			// the recycling makes it seem to be an unmatched opening mark for level 3
			// instead of a missing continuation mark for level 1.
			Test(new string[,] {
				{ "\u201C", FormatMessage(kUnmatchedOpeningMark, 3), "29:26" }
			}, "\\id JER \\c 29 \\p \\v 24 To Shemiah speak, saying, \\v 25 \u201Clevel one, \u2018Level two, \\v 26 \u201Clevel three \\v 27 \\p \\v 28 \u201CHe has sent saying, \u2018Level four.\u2019\u201D\u2019\u201D");
		}

		[Test]
		public void Level4_Recycled_Correct() // Omit continuation in verse 28.
		{
			SetupEnglish4();
			Test(new string[0, 0], "\\id JER \\c 29 \\p \\v 24 To Shemiah speak, saying, \\v 25 \u201Clevel one, \u2018Level two, \\v 26 \u201Clevel three \\v 27 \\v 28 He has sent saying, \u2018Level four.\u2019\u201D\u2019\u201D");
		}
		#endregion

		#region Level 1 to continue in prose

		[Test]
		public void Level1_ContinuationFromLinesIntoProse_InappropriateProperties_UnmatchedOpeningMark()
		{
			SetupSwiss2();
			// If there is a continuation mark, but the properties indicate no mark needed:
			// The opening mark is unmatched.
			// The continuation mark immediately preceding the closing mark is matched.
			// Any other continuation marks are unmatched.
			Test(new string[,] {
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "5:3" },
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "5:11" },
				{ "«", FormatMessage(kUnmatchedOpeningMark, 1), "5:13" }
			}, "\\id MAT \\c 5 \\p \\v 1 \\v 2 He taught them: \\q1 \\v 3 «Level one, \\q2 line two. \\q1 \\v 10 Line one, \\q2 line two. \\p \\v 11 «Continue \\v 12 \\p \\v 13 «Continue \\p \\v 14 «Continue and close level one.»");
		}
		#endregion

		#region Level 1 to continue into lines

		[Test]
		public void Level1_ContinuationIntoLines2_MissingContinuationMark()
		{
			SetupSwiss2();
			SetupContinuationAllOpening();
			Test(new string[,] {
				{ "\u2039", FormatMessage(kMissingContinuationMark, 1), "2:6" },
			}, "\\id MAT \\c 2 \\p \\v 5 «Level one,» they said, «level one: \\q1 \\v 6 \u2039Level two, \\q2 level two, \\q1 level two \\q2 level two.\u203A » \\p \\v 7 Following.");
		}

		// Level1_ContinuationIntoLines23 is based on MRK 12:35-37 (NIV)

		[Test]
		public void Level1_ContinuationIntoLines23_Correct()
		{
			SetupEnglish3();
			SetupContinuationAllOpening();
			Test(new string[0, 0], "\\id MRK \\c 12 \\p \\v 35 He asked, \u201CLevel one? \\v 36 declared, \\q1 \u201C \u2018Level two, \\q2 \u201CLevel three, \\q1 line one \\q2 line two.\u201D \u2019 \\m \\v 37 No continuation \u2018Two.\u2019 Close level one.\u201D");
		}
		#endregion

		#region Level 1 not to continue into lines

		// Level1_NoContinuationIntoLines2 is based on MAT 2:5-7 (NLT) but the marks differ

		[Test]
		public void Level1_NoContinuationIntoLines2_Correct()
		{
			SetupSwiss2();
			Test(new string[0, 0], "\\id MAT \\c 2 \\p \\v 5 «Level one,» they said, «level one: \\q1 \\v 6 \u2039Level two, \\q2 line two, \\q1 line one \\q2 end of levels two and one.\u203A » \\p \\v 7 Following.");
		}
		#endregion

		#region Level 1 to continue within lines
		// Continuation at PSA 81:8 and 11 in NIV and NLT; also at 13 in NIV but not NLT.
		// Continuation occurs in PSA 82 in NIV and NLT (not at same verses).
		// Continuation occurs in PSA 39:4,12; 50:22; 89:30; 132:17 in NIV, but not NLT.
		// No continuation following interlude without stanza break at PSA 39:6 in NIV.
		// Interlude withoug stanza break might not occur in NLT.

		// Level1_LinesContinuationFollowingStanzaBreak is based on PSA 81:6-16 (NLT and especially NIV) but the marks differ

		[Test]
		[Ignore("This is better tested (because of the other used styles) in QuotationCheckSilUnitTest.cs")]
		public void Level1_LinesContinuationFollowingStanzaBreak_Correct()
		{
			SetupEuropean1();
			SetupContinuationAllOpening();
			Test(new string[0, 0], "\\id PSA \\c 81 \\q1 \\v 6 He says, «Level one. \\q2 line two \\q1 \\v 7 line 1 \\qs \\b \\q1 \\v 8 «Continuation \\v 9 \\v 10 \\b \\q1 \\v 11 «Continuation \\v 12 to \\v 13 the \\v 14 end \\v 15 of \\v 16 the quotation and psalm.»");
		}
		#endregion

		#region Not to continue within lines

		// Level12_NoContinuationWithinLines_Correct is based on MAT 3:3-4 (NIV) but the marks differ

		[Test]
		public void Level12_NoContinuationWithinLines_Correct()
		{
			SetupSwiss2();
			Test(new string[0, 0], "\\id MAT \\c 3 \\p \\v 3 He said, \\q1 «Level one: \\q1 \\v6 \u2039Level two. \\q2 Both levels end.\u203A » \\p \\v 4 Following.");
		}
		#endregion

		#region Level 1 quotation dash has no closing mark

		// QuotationDash_Level2 is based on JDG 11:13-19 (RVE95 for the first level, but NIV the second level)

		[Test]
		[Ignore("We currently don't correctly support this functionality (quotation dashes)")]
		public void QuotationDash_Level2_Correct()
		{
			SetupSpanish2_QuotationDash();
			Test(new string[0, 0], "\\id JDG \\c 11 \\p \\v 13 Ammon answered. \\p \u2014Answer. \\p \\v 14 Jepthah sent messages \\v 15 saying: \\p \u2014Jepthah says: \\v 17 to Edom: \u201Clevel two.\u201D \\v 19 To Amorites: \u201Clevel two.\u201D End of message.");
		}

		[Test]
		[Ignore("We currently don't correctly support this functionality (quotation dashes)")]
		public void QuotationDash_Level2_InappropriateProperties_UnexpectedOpeningMark()
		{
			SetupSpanish2();
			Test(new string[,] {
				{ "\u201C", FormatMessage(kUnexpectedOpeningMark, 2), "11:15" }
				// The check does not find a second inconsistency for 11:17, should it?
			}, "\\id JDG \\c 11 \\p \\v 13 Ammon answered. \\p \u2014Answer. \\p \\v 14 Jepthah sent messages \\v 15 saying: \\p \u2014Jepthah says: \\v 17 to Edom: \u201Clevel two.\u201D \\v 19 To Amorites: \u201Clevel two.\u201D End of message.");
		}

		// QuotationDash_Level3 is based on JDG 11:13-19 (RVE95)

		[Test]
		[Ignore("We currently don't correctly support this functionality (quotation dashes)")]
		public void QuotationDash_Level3_Correct()
		{
			SetupSpanish3_QuotationDash();
			Test(new string[0, 0], "\\id JDG \\c 11 \\p \\v 13 Ammon answered. \\p \u2014Answer. \\p \\v 14 Jepthah sent messages \\v 15 saying: \\p \u2014Jepthah says: \u201Clevel two \\v 17 to Edom: \u2018Level three.\u2019 \\v 19 To Amorites: \u2018Level three.\u2019 End of message.\u201D");
		}
		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TE-style Unit tests for the QuotationCheck class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class QuotationCheckSilUnitTest : ScrChecksTestBase
	{
		private TestChecksDataSource m_dataSource;

		// A subset of serialized style information for seven different classes of styles
		// that require capitalization:
		//  sentence intial styles, proper nouns, tables, lists, special, headings and titles.
		//  Only "Paragraph" style has the UseType set. If the UseType is not set, it will
		//  be set to Other by default.
		string stylesInfo =
			"<?xml version=\"1.0\" encoding=\"utf-16\"?><StylePropsInfo>" +
			"<SentenceInitial>" +
				"<StyleInfo StyleName=\"Line1\" StyleType=\"paragraph\" UseType=\"line\" />" +
				"<StyleInfo StyleName=\"Line2\" StyleType=\"paragraph\" UseType=\"line\" />" +
				"<StyleInfo StyleName=\"Paragraph\" StyleType=\"paragraph\" UseType=\"prose\" />" +
			"</SentenceInitial>" +
			"<ProperNouns>" +
			"</ProperNouns>" +
			"<Table>" +
			"</Table>" +
			"<List>" +
				"<StyleInfo StyleName=\"List Item1\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"List Item2\" StyleType=\"paragraph\" />" +
				"<StyleInfo StyleName=\"List Item3\" StyleType=\"paragraph\" /></List>" +
			"<Special>" +
				"<StyleInfo StyleName=\"Stanza Break\" StyleType=\"paragraph\" UseType=\"stanzabreak\"/>" +
			"</Special>" +
			"<Heading>" +
				"<StyleInfo StyleName=\"Intro Section Head\" StyleType=\"paragraph\" UseType=\"other\" />" +
				"<StyleInfo StyleName=\"Section Head\" StyleType=\"paragraph\" UseType=\"other\" />" +
			"</Heading>" +
			"<Title>" +
				"<StyleInfo StyleName=\"Title Main\" StyleType=\"paragraph\" />" +
			"</Title></StylePropsInfo>";

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
			m_dataSource.SetParameterValue("StylesInfo", stylesInfo);
			m_check = new QuotationCheck(m_dataSource);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Quotation check when continuing through an empty paragraph followed by a
		/// paragraph that only has a verse number in it and then some text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueEmptyParaAfterEmptyVerse()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.RemoveLastLevel();
			qMarks[0].Opening = "<<";
			qMarks[0].Closing = ">>";
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse one <<text", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("<<this is my verse two text>>", TextType.Verse,
				true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			Assert.AreEqual(m_dataSource.m_tokens[2], m_errors[0].Tts.FirstToken);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Quotation check when the continuation quote occurs after a verse number,
		/// but at the start of a paragraph. TE-8092.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueAfterVerseNumber()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.RemoveLastLevel();
			qMarks[0].Opening = "<<";
			qMarks[0].Closing = ">>";
			qMarks.ContinuationMark = ParagraphContinuationMark.Closing;
			qMarks.ContinuationType = ParagraphContinuationType.RequireOutermost;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse <<one", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken(">>verse two>>", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Quotation check when the continuation quote occurs after a verse number
		/// and a footnote ORC, but at the start of a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueAfterVerseNumberAndFootnote()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.RemoveLastLevel();
			qMarks[0].Opening = "<<";
			qMarks[0].Closing = ">>";
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("verse <<one", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("This is my note", TextType.Note,
				true, true, "Note General Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("<<verse two>>", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Quotation check when we have one level and have repeat closing quotation
		/// with a present continuer. TE-8093
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatClosingWhenContinuerPresent()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.RemoveLastLevel();
			qMarks[0].Opening = "<";
			qMarks[0].Closing = ">";
			qMarks.ContinuationMark = ParagraphContinuationMark.Closing;
			qMarks.ContinuationType = ParagraphContinuationType.RequireOutermost;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Para1 <one", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken(">Para2 two>", TextType.Verse,
				true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Para3 three", TextType.Verse,
				true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Quotation check when we have one level and have repeat closing quotation
		/// with a missing continuation. TE-8093
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueRepeatClosingWhenContinuerMissing()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.RemoveLastLevel();
			qMarks[0].Opening = "<";
			qMarks[0].Closing = ">";
			qMarks.ContinuationMark = ParagraphContinuationMark.Closing;
			qMarks.ContinuationType = ParagraphContinuationType.RequireOutermost;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Para1 <one", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Para2 two>", TextType.Verse,
				true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Para3 three", TextType.Verse,
				true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			Assert.AreEqual(m_dataSource.m_tokens[2], m_errors[0].Tts.FirstToken);
			Assert.AreEqual("Para2", m_errors[0].Tts.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Continues at quotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueAtQuotation()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "<<";
			qMarks[0].Closing = ">>";
			qMarks[1].Opening = "<";
			qMarks[1].Closing = ">";
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("<<foo foos <bar bars", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("<< <baz bazs", TextType.Verse,
				false, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("qux quxs> >>", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			Assert.AreEqual(m_dataSource.m_tokens[5], m_errors[0].Tts.FirstToken);
			Assert.AreEqual("qux", m_errors[0].Tts.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Continues at quotation after paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContinueAtQuotationAfterParagraph()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "<<";
			qMarks[0].Closing = ">>";
			qMarks[1].Opening = "<";
			qMarks[1].Closing = ">";
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("<<foo <bar", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("<< <baz", TextType.Verse,
				false, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("qux> >>", TextType.Verse,
				false, false, "Line1"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests when the first level quotes have identical opening and closing marks
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level1OpenAndClosingAreSameChar()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "!";
			qMarks[0].Closing = "!";
			qMarks[1].Opening = "<";
			qMarks[1].Closing = ">";
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("Intro !Paragraph <is> very! short.",
				TextType.Other, true, false, "Intro Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken(string.Empty, TextType.Other,
				true, false, "Section Head"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level1_ContinuationFromLinesIntoProse is based on MAT 5:1-14 (NIV) but the marks differ
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level1_ContinuationFromLinesIntoProse_Correct()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "«"; // Left-pointing double angle quotation mark
			qMarks[0].Closing = "»"; // Right-pointing double angle quotation mark
			qMarks[1].Opening = "\u2039"; // Single left-pointing angle quotation mark
			qMarks[1].Closing = "\u203A"; // Single right-pointing angle quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("5", TextType.ChapterNumber,
				true, false, "Line1", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3", TextType.VerseNumber,
				false, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Level one, ", TextType.Verse,
				false, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("line two. ", TextType.Verse,
				true, false, "Line2"));
			m_dataSource.m_tokens.Add(new DummyTextToken("11", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continue ", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("14", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continue and close level one.»", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level1_ContinuationInProseEvenSpanningSectionHead is based on MAT 5:11-13 (NIV) but
		/// the marks differ
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level1_ContinuationInProseEvenSpanningSectionHead_Correct()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "«"; // Left-pointing double angle quotation mark
			qMarks[0].Closing = "»"; // Right-pointing double angle quotation mark
			qMarks[1].Opening = "\u2039"; // Single left-pointing angle quotation mark
			qMarks[1].Closing = "\u203A"; // Single right-pointing angle quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("5", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("11", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Level one begins", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("12", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("really it continues. ", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("", TextType.Other,
				true, false, "Section Head"));
			m_dataSource.m_tokens.Add(new DummyTextToken("13", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continue level one.»", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level1_ContinuationIntoFourLines2 is based on MAT 2:5-7 (NIV) but the marks differ
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level1_ContinuationIntoLines2_Correct()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "«"; // Left-pointing double angle quotation mark
			qMarks[0].Closing = "»"; // Right-pointing double angle quotation mark
			qMarks[1].Opening = "\u2039"; // Single left-pointing angle quotation mark
			qMarks[1].Closing = "\u203A"; // Single right-pointing angle quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("5", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("5", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Level one,» they replied, «level one:", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("6", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("« \u2039Level two,", TextType.Verse,
				false, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("line two, ", TextType.Verse,
				true, false, "Line2"));
			m_dataSource.m_tokens.Add(new DummyTextToken("line one", TextType.Verse,
				true, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("both levels end.\u203A »", TextType.Verse,
				true, false, "Line2"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the case when there is only one continuer when there should be two (i.e. one
		/// for each open level) and that continuer is closing when it should be opening.
		/// See TE-8173.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level2_IncorrectContinuation()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "«"; // Left-pointing double angle quotation mark
			qMarks[0].Closing = "»"; // Right-pointing double angle quotation mark
			qMarks[1].Opening = "\u2039"; // Single left-pointing angle quotation mark
			qMarks[1].Closing = "\u203A"; // Single right-pointing angle quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("«Level one, they replied, \u2039level two",
				TextType.Verse,	false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("»Paragraph two\u203A, the end»",
				TextType.Verse, true, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(3, m_errors.Count);
			Assert.AreEqual("»", m_errors[0].Tts.Text);
			Assert.AreEqual("\u203A", m_errors[1].Tts.Text);
			Assert.AreEqual("»", m_errors[2].Tts.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level2_ContinuationContainsLevel3_Recycled is based on EZK 27:1-12 (NIV)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level2_ContinuationContainsLevel3_Recycled_Correct()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "«"; // Left-pointing double angle quotation mark
			qMarks[0].Closing = "»"; // Right-pointing double angle quotation mark
			qMarks[1].Opening = "\u2039"; // Single left-pointing angle quotation mark
			qMarks[1].Closing = "\u203A"; // Single right-pointing angle quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("27", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201CLevel one.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Say, \u2018Level two says: ", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("4", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201C \u2018Continuation says,", TextType.Verse,
				false, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201CLevel three.\u201D ", TextType.Verse,
				true, false, "Line2"));
			m_dataSource.m_tokens.Add(new DummyTextToken(string.Empty, TextType.Other,
				true, false, "Stanza Break"));
			m_dataSource.m_tokens.Add(new DummyTextToken("10", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201C \u2018Continuation.", TextType.Verse,
				false, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("12", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201C \u2018Continuation into prose, and then into lines again.\u2019 \u201D", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level2_ContinuationContainsLevel3_Distinct is based on EZK 27:1-12 (NIV) but
		/// the marks differ
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level2_ContinuationContainsLevel3_Distinct()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.EnsureLevelExists(3);
			qMarks[0].Opening = "«"; // Left-pointing double angle quotation mark
			qMarks[0].Closing = "»"; // Right-pointing double angle quotation mark
			qMarks[1].Opening = "\u201C"; // Left double quotation mark
			qMarks[1].Closing = "\u201D"; // Right double quotation mark
			qMarks[2].Opening = "\u2018"; // Left single quotation mark
			qMarks[2].Closing = "\u2019"; // Right single quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("27", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Level one.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Say, \u201CLevel two says:", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("4", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("« \u201CContinuation says,", TextType.Verse,
				false, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u2018Level three.\u2019", TextType.Verse,
				true, false, "Line2"));
			m_dataSource.m_tokens.Add(new DummyTextToken(string.Empty, TextType.Other,
				true, false, "Stanza Break"));
			m_dataSource.m_tokens.Add(new DummyTextToken("10", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("« \u201CContinuation.", TextType.Verse,
				false, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("12", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("« \u201CContinuation into prose, and then into lines again.\u201D »", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test when the 2nd level quotes have identical opening and closing marks
		/// (TE-8104)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level2OpenAndClosingAreSameChar()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "<";
			qMarks[0].Closing = ">";
			qMarks[1].Opening = "!";
			qMarks[1].Closing = "!";
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("Intro <Paragraph is !very! short.",
				TextType.Other,	true, false, "Intro Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken(string.Empty, TextType.Other,
				true, false, "Section Head"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(2, m_errors.Count);
			Assert.AreEqual(m_dataSource.m_tokens[2], m_errors[0].Tts.FirstToken);
			Assert.AreEqual("1", m_errors[0].Tts.Text);
			Assert.AreEqual(0, m_errors[0].Tts.Offset);
			Assert.AreEqual(m_dataSource.m_tokens[0], m_errors[1].Tts.FirstToken);
			Assert.AreEqual("<", m_errors[1].Tts.Text);
			Assert.AreEqual(6, m_errors[1].Tts.Offset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level3_Distinct is based on LUK 12:16-21 (NIV) but the marks differ
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level3_Distinct_Continuation_Correct()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.EnsureLevelExists(3);
			qMarks[0].Opening = "«"; // Left-pointing double angle quotation mark
			qMarks[0].Closing = "»"; // Right-pointing double angle quotation mark
			qMarks[1].Opening = "\u201C"; // Left double quotation mark
			qMarks[1].Closing = "\u201D"; // Right double quotation mark
			qMarks[2].Opening = "\u2018"; // Left single quotation mark
			qMarks[2].Closing = "\u2019"; // Right single quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("12", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("16", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He told: «Level one.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("17", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He thought, \u201Clevel two.\u201D", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("18", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continuation \u201CTwo.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("19", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("To myself, \u2018Level three.\u2019 \u201D", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("20", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continuation \u201CTwo.\u201D", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("21", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continuation, and then close level one.»", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level3_Distinct_Continuation_UnmatchedOpeningMark()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.EnsureLevelExists(3);
			qMarks[0].Opening = "«"; // Left-pointing double angle quotation mark
			qMarks[0].Closing = "»"; // Right-pointing double angle quotation mark
			qMarks[1].Opening = "\u201C"; // Left double quotation mark
			qMarks[1].Closing = "\u201D"; // Right double quotation mark
			qMarks[2].Opening = "\u2018"; // Left single quotation mark
			qMarks[2].Closing = "\u2019"; // Right single quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("12", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("16", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He told: «Level one.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("17", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He thought, \u201Clevel two.\u201D", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("18", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continuation \u201CTwo.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("19", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("To myself, \u2018Level three.\u201D", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("20", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continuation \u201CTwo.\u201D", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("21", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continuation, and then close level one.»", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			Assert.AreEqual(m_dataSource.m_tokens[8], m_errors[0].Tts.FirstToken);
			Assert.AreEqual("\u2018", m_errors[0].Tts.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level3_Distinct_Continuation_UnmatchedClosingMark()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.EnsureLevelExists(3);
			qMarks[0].Opening = "«"; // Left-pointing double angle quotation mark
			qMarks[0].Closing = "»"; // Right-pointing double angle quotation mark
			qMarks[1].Opening = "\u201C"; // Left double quotation mark
			qMarks[1].Closing = "\u201D"; // Right double quotation mark
			qMarks[2].Opening = "\u2018"; // Left single quotation mark
			qMarks[2].Closing = "\u2019"; // Right single quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("12", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("16", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He told: «Level one.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("17", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He thought, \u201Clevel two.\u201D", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("18", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continuation \u201CTwo.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("19", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("To myself, Level three.\u2019 \u201D", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("20", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continuation \u201CTwo.\u201D", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("21", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("«Continuation, and then close level one.»", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			Assert.AreEqual(m_dataSource.m_tokens[8], m_errors[0].Tts.FirstToken);
			Assert.AreEqual("\u2019", m_errors[0].Tts.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level3_Recycled is based on LUK 12:16-21 (NIV)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level3_Recycled_Continuation_Correct()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.EnsureLevelExists(3);
			qMarks[0].Opening = "\u201C"; // Left double quotation mark
			qMarks[0].Closing = "\u201D"; // Right double quotation mark
			qMarks[1].Opening = "\u2018"; // Left single quotation mark
			qMarks[1].Closing = "\u2019"; // Right single quotation mark
			qMarks[2].Opening = "\u201C"; // Left double quotation mark
			qMarks[2].Closing = "\u201D"; // Right double quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("12", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("16", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He told: \u201CLevel one.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("17", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He thought, \u2018Level two.\u2019", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("18", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201CContinuation \u2018Two.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("19", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("To myself, \u201Clevel three.\u201D \u2019", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("20", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201CContinuation \u2018Two.\u2019", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("21", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201CContinuation, and then close level one.\u201D", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level3_Recycled_Continuation_UnmatchedOpeningMark()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.EnsureLevelExists(3);
			qMarks[0].Opening = "\u201C"; // Left double quotation mark
			qMarks[0].Closing = "\u201D"; // Right double quotation mark
			qMarks[1].Opening = "\u2018"; // Left single quotation mark
			qMarks[1].Closing = "\u2019"; // Right single quotation mark
			qMarks[2].Opening = "\u201C"; // Left double quotation mark
			qMarks[2].Closing = "\u201D"; // Right double quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("12", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("16", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He told: \u201CLevel one.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("17", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He thought, \u2018Level two.\u2019", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("18", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201CContinuation \u2018Two.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("19", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("To myself, \u201Clevel three.\u2019", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("20", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201CContinuation \u2018Two.\u2019", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("21", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201CContinuation, and then close level one.\u201D", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			Assert.AreEqual(m_dataSource.m_tokens[8], m_errors[0].Tts.FirstToken);
			Assert.AreEqual("\u201C", m_errors[0].Tts.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level3_Recycled_UnmatchedOpeningMark()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.EnsureLevelExists(3);
			qMarks[0].Opening = "\u201C"; // Left double quotation mark
			qMarks[0].Closing = "\u201D"; // Right double quotation mark
			qMarks[1].Opening = "\u2018"; // Left single quotation mark
			qMarks[1].Closing = "\u2019"; // Right single quotation mark
			qMarks[2].Opening = "\u201C"; // Left double quotation mark
			qMarks[2].Closing = "\u201D"; // Right double quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.None;
			qMarks.ContinuationMark = ParagraphContinuationMark.None;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("12", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("16", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He told: \u201CLevel one.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("17", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("He thought, \u2018Level two.\u2019", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("18", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u2018Two.", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("19", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("To myself, \u201Clevel three.\u2019", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("20", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u2018Two.\u2019", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("21", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Close level one.\u201D", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			Assert.AreEqual(m_dataSource.m_tokens[8], m_errors[0].Tts.FirstToken);
			Assert.AreEqual("\u201C", m_errors[0].Tts.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Level4_Recycled is based on JER 29:24-28 (NASB)
		/// Inserted \p in verse 28 to account for continuation, but that does not seem
		/// quite right, because NASB has verse structure, instead of paragraph structure.
		/// The main paragraphs are at verses 24 and 29.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Level4_Recycled_Continuation_Correct()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks.EnsureLevelExists(4);
			qMarks[0].Opening = "\u201C"; // Left double quotation mark
			qMarks[0].Closing = "\u201D"; // Right double quotation mark
			qMarks[1].Opening = "\u2018"; // Left single quotation mark
			qMarks[1].Closing = "\u2019"; // Right single quotation mark
			qMarks[2].Opening = "\u201C"; // Left double quotation mark
			qMarks[2].Closing = "\u201D"; // Right double quotation mark
			qMarks[3].Opening = "\u2018"; // Left single quotation mark
			qMarks[3].Closing = "\u2019"; // Right single quotation mark
			qMarks.ContinuationType = ParagraphContinuationType.RequireInnermost;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);

			m_dataSource.m_tokens.Add(new DummyTextToken("29", TextType.ChapterNumber,
				true, false, "Paragraph", "Chapter Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("24", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("To Shemiah speak, saying,", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("25", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201Clevel one, \u2018Level two,", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("26", TextType.VerseNumber,
				false, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201Clevel three", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("28", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("\u201CHe has sent saying, \u2018Level four.\u2019\u201D\u2019\u201D", TextType.Verse,
				false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerboseOptionContinuers()
		{
			QuotationMarksList qMarks = QuotationMarksList.NewList();
			qMarks[0].Opening = "<<";
			qMarks[0].Closing = ">>";
			qMarks[1].Opening = "<";
			qMarks[1].Closing = ">";
			qMarks.ContinuationType = ParagraphContinuationType.RequireAll;
			qMarks.ContinuationMark = ParagraphContinuationMark.Opening;
			m_dataSource.SetParameterValue("QuotationMarkInfo", qMarks.XmlString);
			m_dataSource.SetParameterValue("VerboseQuotes", "Yes");

			m_dataSource.m_tokens.Add(new DummyTextToken("1", TextType.VerseNumber,
				true, false, "Paragraph", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("<<foo <bar", TextType.Verse,
				false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("2", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("<< <baz", TextType.Verse,
				false, false, "Line1"));
			m_dataSource.m_tokens.Add(new DummyTextToken("3", TextType.VerseNumber,
				true, false, "Line1", "Verse Number"));
			m_dataSource.m_tokens.Add(new DummyTextToken("qux> >>", TextType.Verse,
				false, false, "Line1"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(6, m_errors.Count);
			Assert.AreEqual(m_dataSource.m_tokens[1], m_errors[0].Tts.FirstToken);
			Assert.AreEqual("<<", m_errors[0].Tts.Text);
			Assert.AreEqual(m_dataSource.m_tokens[1], m_errors[1].Tts.FirstToken);
			Assert.AreEqual("<", m_errors[1].Tts.Text);
			Assert.AreEqual(m_dataSource.m_tokens[3], m_errors[2].Tts.FirstToken);
			Assert.AreEqual("<<", m_errors[2].Tts.Text);
			Assert.AreEqual(m_dataSource.m_tokens[3], m_errors[3].Tts.FirstToken);
			Assert.AreEqual("<", m_errors[3].Tts.Text);
			Assert.AreEqual(m_dataSource.m_tokens[5], m_errors[4].Tts.FirstToken);
			Assert.AreEqual(">", m_errors[4].Tts.Text);
			Assert.AreEqual(m_dataSource.m_tokens[5], m_errors[5].Tts.FirstToken);
			Assert.AreEqual(">>", m_errors[5].Tts.Text);
		}
	}
}

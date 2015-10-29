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
	/// Unit tests for the PunctuationCheck class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PunctuationCheckUnitTest : ScrChecksTestBase
	{
		private UnitTestChecksDataSource m_dataSource = new UnitTestChecksDataSource();

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test fixture setup (runs once for the whole fixture).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			QuotationMarksList qmarks = QuotationMarksList.NewList();
			qmarks.QMarksList[0].Opening = "\u201C";
			qmarks.QMarksList[0].Closing = "\u201D";
			qmarks.QMarksList[1].Opening = "\u2018";
			qmarks.QMarksList[1].Closing = "\u2019";
			qmarks.EnsureLevelExists(5);
			m_dataSource.SetParameterValue("QuotationMarkInfo", qmarks.XmlString);
			m_dataSource.SetParameterValue("PunctWhitespaceChar", "_");
			m_check = new PunctuationCheck(m_dataSource);
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that processing the specified text produces the expected punctuation pattern.
		/// Use this version for tests that expect a single pattern.
		/// </summary>
		/// <param name="expectedPunctPattern">The expected punct pattern.</param>
		/// <param name="expectedOffset">The expected offset.</param>
		/// <param name="text">A string marked up with SF codes representing a text to be
		/// processed.</param>
		/// ------------------------------------------------------------------------------------
		void TestGetReferences(string expectedPunctPattern, int expectedOffset, string text)
		{
			TestGetReferences(new string[] { expectedPunctPattern }, new int[] { expectedOffset }, text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that processing the specified text using the GetReferences method produces the
		/// expected punctuation patterns.
		/// </summary>
		/// <param name="expectedPunctPatterns">The expected punct patterns.</param>
		/// <param name="expectedOffsets">The expected offsets.</param>
		/// <param name="text">A string marked up with SF codes representing a text to be
		/// processed.</param>
		/// ------------------------------------------------------------------------------------
		void TestGetReferences(string[] expectedPunctPatterns, int[] expectedOffsets, string text)
		{
			Assert.AreEqual(expectedPunctPatterns.Length, expectedOffsets.Length, "Poorly defined expected test results.");
			m_dataSource.Text = text;

			PunctuationCheck check = new PunctuationCheck(m_dataSource);
			List<TextTokenSubstring> tts =
				check.GetReferences(m_dataSource.TextTokens(), String.Empty);

			Assert.AreEqual(expectedPunctPatterns.Length, tts.Count, "Unexpected number of punctuation patterns." );

			for (int i = 0; i < expectedPunctPatterns.Length; i++ )
			{
				Assert.AreEqual(expectedPunctPatterns[i], tts[i].InventoryText, "Result number: " + i);
				Assert.AreEqual(expectedOffsets[i], tts[i].Offset, "Result number: " + i);
			}
		}
		#endregion

		#region GetReferences tests
		[Test]
		public void GetReferences_BasicMedial()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences("-", 3, "\\p \\v 1 pre-word");
		}

		[Test]
		public void GetReferences_IntermediateMedial()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("-", 3, "\\p \\v 1 pre-word");
		}

		[Test]
		public void GetReferences_AdvancedMedial()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("-", 3, "\\p \\v 1 pre-word");
		}

		[Test]
		public void GetReferences_BasicIsolated()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences("_\u2014_", 5, "\\p \\v 1 word \u2014 word");
		}

		[Test]
		public void GetReferences_IntermediateIsolated()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("_\u2014_", 5, "\\p \\v 1 word \u2014 word");
		}

		[Test]
		public void GetReferences_AdvancedIsolated()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("_\u2014_", 5, "\\p \\v 1 word \u2014 word");
		}

		[Test]
		public void GetReferences_BasicDoubleStraightQuoteAfterVerseNum()
		{
			TestChecksDataSource dataSource = new TestChecksDataSource();
			dataSource.SetParameterValue("PunctCheckLevel", "Basic");

			PunctuationCheck check = new PunctuationCheck(dataSource);
			dataSource.m_tokens.Add(new DummyTextToken("Wow.",
				TextType.Verse, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("17",
				TextType.VerseNumber, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("\"Word",
				TextType.Verse, false, false, "Paragraph"));
			List<TextTokenSubstring> tokens =
				check.GetReferences(dataSource.TextTokens(), string.Empty);
			Assert.AreEqual(2, tokens.Count);

			Assert.AreEqual("._", tokens[0].InventoryText);
			Assert.AreEqual(3, tokens[0].Offset);
			Assert.AreEqual("Wow.", tokens[0].FirstToken.Text);

			Assert.AreEqual("_\"", tokens[1].InventoryText);
			Assert.AreEqual(0, tokens[1].Offset);
			Assert.AreEqual("\"Word", tokens[1].FirstToken.Text);
		}

		[Test]
		public void GetReferences_IntermediateDoubleStraightQuoteAfterVerseNum()
		{
			TestChecksDataSource dataSource = new TestChecksDataSource();
			dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");

			PunctuationCheck check = new PunctuationCheck(dataSource);
			dataSource.m_tokens.Add(new DummyTextToken("Wow.",
				TextType.Verse, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("17",
				TextType.VerseNumber, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("\"Word",
				TextType.Verse, false, false, "Paragraph"));
			List<TextTokenSubstring> tokens =
				check.GetReferences(dataSource.TextTokens(), string.Empty);
			Assert.AreEqual(2, tokens.Count);

			Assert.AreEqual("._", tokens[0].InventoryText);
			Assert.AreEqual(3, tokens[0].Offset);
			Assert.AreEqual("Wow.", tokens[0].FirstToken.Text);

			Assert.AreEqual("_\"", tokens[1].InventoryText);
			Assert.AreEqual(0, tokens[1].Offset);
			Assert.AreEqual("\"Word", tokens[1].FirstToken.Text);
		}

		[Test]
		public void GetReferences_AdvancedDoubleStraightQuoteAfterVerseNum()
		{
			TestChecksDataSource dataSource = new TestChecksDataSource();
			dataSource.SetParameterValue("PunctCheckLevel", "Advanced");

			PunctuationCheck check = new PunctuationCheck(dataSource);
			dataSource.m_tokens.Add(new DummyTextToken("Wow.",
				TextType.Verse, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("17",
				TextType.VerseNumber, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("\"Word",
				TextType.Verse, false, false, "Paragraph"));
			List<TextTokenSubstring> tokens =
				check.GetReferences(dataSource.TextTokens(), string.Empty);
			Assert.AreEqual(2, tokens.Count);

			Assert.AreEqual("._", tokens[0].InventoryText);
			Assert.AreEqual(3, tokens[0].Offset);
			Assert.AreEqual("Wow.", tokens[0].FirstToken.Text);

			Assert.AreEqual("_\"", tokens[1].InventoryText);
			Assert.AreEqual(0, tokens[1].Offset);
			Assert.AreEqual("\"Word", tokens[1].FirstToken.Text);
		}

		[Test]
		public void GetReferences_BasicVerseNumBetweenNotes()
		{
			TestChecksDataSource dataSource = new TestChecksDataSource();
			dataSource.SetParameterValue("PunctCheckLevel", "Basic");

			PunctuationCheck check = new PunctuationCheck(dataSource);
			dataSource.m_tokens.Add(new DummyTextToken("Wow",
				TextType.Verse, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("I am a note.",
				TextType.Note, true, true, "Note General Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("17",
				TextType.VerseNumber, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("\"I am a quote note!\"",
				TextType.Note, true, true, "Note General Paragraph"));
			List<TextTokenSubstring> tokens =
				check.GetReferences(dataSource.TextTokens(), string.Empty);
			Assert.AreEqual(4, tokens.Count);

			Assert.AreEqual("._", tokens[0].InventoryText);
			Assert.AreEqual(11, tokens[0].Offset);
			Assert.AreEqual("I am a note.", tokens[0].FirstToken.Text);

			Assert.AreEqual("_\"", tokens[1].InventoryText);
			Assert.AreEqual(0, tokens[1].Offset);
			Assert.AreEqual("\"I am a quote note!\"", tokens[1].FirstToken.Text);

			Assert.AreEqual("!_", tokens[2].InventoryText);
			Assert.AreEqual(18, tokens[2].Offset);
			Assert.AreEqual("\"I am a quote note!\"", tokens[2].FirstToken.Text);

			Assert.AreEqual("\"_", tokens[3].InventoryText);
			Assert.AreEqual(19, tokens[3].Offset);
			Assert.AreEqual("\"I am a quote note!\"", tokens[3].FirstToken.Text);
		}

		[Test]
		public void GetReferences_IntermediateVerseNumBetweenNotes()
		{
			TestChecksDataSource dataSource = new TestChecksDataSource();
			dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");

			PunctuationCheck check = new PunctuationCheck(dataSource);
			dataSource.m_tokens.Add(new DummyTextToken("Wow",
				TextType.Verse, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("I am a note.",
				TextType.Note, true, true, "Note General Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("17",
				TextType.VerseNumber, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("\"I am a quote note!\"",
				TextType.Note, true, true, "Note General Paragraph"));
			List<TextTokenSubstring> tokens =
				check.GetReferences(dataSource.TextTokens(), string.Empty);
			Assert.AreEqual(3, tokens.Count);

			Assert.AreEqual("._", tokens[0].InventoryText);
			Assert.AreEqual(11, tokens[0].Offset);
			Assert.AreEqual("I am a note.", tokens[0].FirstToken.Text);

			Assert.AreEqual("_\"", tokens[1].InventoryText);
			Assert.AreEqual(0, tokens[1].Offset);
			Assert.AreEqual("\"I am a quote note!\"", tokens[1].FirstToken.Text);

			Assert.AreEqual("!\"_", tokens[2].InventoryText);
			Assert.AreEqual(18, tokens[2].Offset);
			Assert.AreEqual("\"I am a quote note!\"", tokens[2].FirstToken.Text);
		}

		[Test]
		public void GetReferences_AdvancedVerseNumBetweenNotes()
		{
			TestChecksDataSource dataSource = new TestChecksDataSource();
			dataSource.SetParameterValue("PunctCheckLevel", "Advanced");

			PunctuationCheck check = new PunctuationCheck(dataSource);
			dataSource.m_tokens.Add(new DummyTextToken("Wow",
				TextType.Verse, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("I am a note.",
				TextType.Note, true, true, "Note General Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("17",
				TextType.VerseNumber, true, false, "Paragraph"));
			dataSource.m_tokens.Add(new DummyTextToken("\"I am a quote note!\"",
				TextType.Note, true, true, "Note General Paragraph"));
			List<TextTokenSubstring> tokens =
				check.GetReferences(dataSource.TextTokens(), string.Empty);
			Assert.AreEqual(3, tokens.Count);

			Assert.AreEqual("._", tokens[0].InventoryText);
			Assert.AreEqual(11, tokens[0].Offset);
			Assert.AreEqual("I am a note.", tokens[0].FirstToken.Text);

			Assert.AreEqual("_\"", tokens[1].InventoryText);
			Assert.AreEqual(0, tokens[1].Offset);
			Assert.AreEqual("\"I am a quote note!\"", tokens[1].FirstToken.Text);

			Assert.AreEqual("!\"_", tokens[2].InventoryText);
			Assert.AreEqual(18, tokens[2].Offset);
			Assert.AreEqual("\"I am a quote note!\"", tokens[2].FirstToken.Text);
		}

		[Test]
		public void GetReferences_BasicInitialSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences("_\u201C", 5, "\\p \\v 1 word \u201Cword");
		}

		[Test]
		public void GetReferences_IntermediateInitialSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("_\u201C", 5, "\\p \\v 1 word \u201Cword");
		}

		[Test]
		public void GetReferences_AdvancedInitialSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("_\u201C", 5, "\\p \\v 1 word \u201Cword");
		}

		[Test]
		public void GetReferences_BasicParagraphInitialSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences("_\u201C", 0, "\\p \\v 1 \u201Cword");
		}

		[Test]
		public void GetReferences_IntermediateParagraphInitialSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("_\u201C", 0, "\\p \\v 1 \u201Cword");
		}

		[Test]
		public void GetReferences_AdvancedParagraphInitialSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("_\u201C", 0, "\\p \\v 1 \u201Cword");
		}

		[Test]
		public void GetReferences_BasicInitialMultiple()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences(new string[] { "_\u201C", "_\u2018" },
				new int[] { 5, 7 },
				"\\p \\v 1 word \u201C \u2018word");
		}

		[Test]
		public void GetReferences_IntermediateInitialMultiple()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("_\u201C_\u2018", 5, "\\p \\v 1 word \u201C \u2018word");
		}

		[Test]
		public void GetReferences_AdvancedInitialMultiple()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("_\u201C_\u2018", 5, "\\p \\v 1 word \u201C \u2018word");
			try
			{
				m_dataSource.SetParameterValue("PunctWhitespaceChar", " ");
				TestGetReferences(" \u201C \u2018", 5, "\\p \\v 1 word \u201C \u2018word");
			}
			finally
			{
				m_dataSource.SetParameterValue("PunctWhitespaceChar", "_");
			}
		}

		[Test]
		public void GetReferences_BasicFinalSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences("\u201D_", 4, "\\p \\v 1 word\u201D word");
		}

		[Test]
		public void GetReferences_IntermediateFinalSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("\u201D_", 4, "\\p \\v 1 word\u201D word");
		}

		[Test]
		public void GetReferences_AdvancedFinalSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("\u201D_", 4, "\\p \\v 1 word\u201D word");
		}

		[Test]
		public void GetReferences_BasicParagraphFinalMultiple()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			// REVIEW: This appears to be the intended design, but it doesn't seem to be particularly
			// useful since a period followed by a space and a comma followed by a space are both valid,
			// but a period followed by a space, followed by a dollar sign is probably not valid. The
			// user who runs this check in "basic" mode will never be able to catch this kind of error.
			TestGetReferences(new string[] { "._", ",_", "$_" }, new int[] { 4, 5, 6 }, "\\p \\v 1 word.,$");
		}

		[Test]
		public void GetReferences_IntermediateParagraphFinalMultiple()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences(".,$_", 4, "\\p \\v 1 word.,$");
		}

		[Test]
		public void GetReferences_AdvancedParagraphFinalMultiple()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences(".,$_", 4, "\\p \\v 1 word.,$");
		}

		[Test]
		public void GetReferences_BasicParagraphFinalSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences("\u201D_", 4, "\\p \\v 1 word\u201D");
		}

		[Test]
		public void GetReferences_IntermediateParagraphFinalSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("\u201D_", 4, "\\p \\v 1 word\u201D");
		}

		[Test]
		public void GetReferences_AdvancedParagraphFinalSingle()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("\u201D_", 4, "\\p \\v 1 word\u201D");
		}

		[Test]
		public void GetReferences_BasicFinalMutiplePeriodBefore()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences(new string[] { "._", "\u2019_", "\u201D_" },
				new int[] {4, 5, 7},
				"\\p \\v 1 word.\u2019 \u201D word");
		}

		[Test]
		public void GetReferences_IntermediateFinalMutiplePeriodBefore()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences(".\u2019_\u201D_", 4, "\\p \\v 1 word.\u2019 \u201D word");
		}

		[Test]
		public void GetReferences_AdvancedFinalMutiplePeriodBefore()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences(".\u2019_\u201D_", 4,  "\\p \\v 1 word.\u2019 \u201D word");
		}

		[Test]
		public void GetReferences_BasicFinalMutiplePeriodAfter()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences(new string[] { "\u2019_", "\u201D_", "._" },
				new int[] { 4, 6, 7 },
				"\\p \\v 1 word\u2019 \u201D. word");
		}

		[Test]
		public void GetReferences_IntermediateFinalMutiplePeriodAfter()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("\u2019_\u201D._", 4, "\\p \\v 1 word\u2019 \u201D. word");
		}

		[Test]
		public void GetReferences_AdvancedFinalMutiplePeriodAfter()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("\u2019_\u201D._", 4, "\\p \\v 1 word\u2019 \u201D. word");
		}

		[Test]
		public void GetReferences_BasicFinalMutipleNoSpace()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences(new string[] { "\u2019_", "\u201D_" },
				new int[] { 4, 5 },
				"\\p \\v 1 word\u2019\u201D word");
		}

		[Test]
		public void GetReferences_IntermediateFinalMutipleNoSpace()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("\u2019\u201D_", 4, "\\p \\v 1 word\u2019\u201D word");
		}

		[Test]
		public void GetReferences_AdvancedFinalMutipleNoSpace()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("\u2019\u201D_", 4, "\\p \\v 1 word\u2019\u201D word");
		}

		[Test]
		public void GetReferences_BasicFinalInitialAllQuotes()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences(new string[] { "\u2019_", "_\u201C" },
				new int[] { 4, 6 },
				"\\p \\v 1 word\u2019 \u201Cword");
		}

		[Test]
		public void GetReferences_IntermediateFinalInitialAllQuotes()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences(new string[] { "\u2019_", "_\u201C" },
				new int[] { 4, 6 },
				"\\p \\v 1 word\u2019 \u201Cword");
		}

		[Test]
		public void GetReferences_AdvancedFinalInitialAllQuotes()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("\u2019_\u201C", 4, "\\p \\v 1 word\u2019 \u201Cword");
		}

		[Test]
		public void GetReferences_IntermediateFinalInitialSomeQuotes()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences(new string[] { "\u2019!_", "_\u201C" },
				new int[] { 4, 7 },
				"\\p \\v 1 word\u2019! \u201Cword");
		}

		[Test]
		public void GetReferences_IntermediateFinalExclamationBetweenClosingQuotes()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences(new string[] { "\u2019!_", "_\u201D" },
				new int[] { 4, 7 },
				"\\p \\v 1 word\u2019! \u201Dword");
		}

		[Test]
		public void GetReferences_BasicFinalInitialSomeQuotes()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences(new string[] { "\u2019_", "!_", "_\u201C" },
				new int[] { 4, 5, 7 },
				"\\p \\v 1 word\u2019! \u201Cword");
		}

		[Test]
		public void GetReferences_AdvancedFinalInitialSomeQuotes()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("\u2019!_\u201C", 4, "\\p \\v 1 word\u2019! \u201Cword");
		}

		[Test]
		public void GetReferences_BasicEllipsis()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences("...", 4, "\\p \\v 1 word...word");
		}

		[Test]
		public void GetReferences_IntermediateEllipsis()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("...", 4, "\\p \\v 1 word...word");
		}

		[Test]
		public void GetReferences_AdvancedEllipsis()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("...", 4, "\\p \\v 1 word...word");
		}

		[Test]
		public void GetReferences_BasicNumbersIgnore()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences(new string[] { "._", "._" },
				new int[] { 4, 14 },
				"\\p \\v 1 word. 3:4 word.");
		}

		[Test]
		public void GetReferences_IntermediateNumbersIgnore()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences(new string[] { "._", "._" },
				new int[] { 4, 14 },
				"\\p \\v 1 word. 3:4 word.");
		}

		[Test]
		public void GetReferences_AdvancedNumbersIgnore()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences(new string[] { "._", "._" },
				new int[] { 4, 14 },
				"\\p \\v 1 word. 3:4 word.");
		}

		[Test]
		public void GetReferences_BasicNumbersIgnoreNoPunctBeforeNumbers()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences("._", 13, "\\p \\v 1 word 3:4 word.");
		}

		[Test]
		public void GetReferences_IntermediateNumbersIgnoreNoPunctBeforeNumbers()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences("._", 13, "\\p \\v 1 word 3:4 word.");
		}

		[Test]
		public void GetReferences_AdvancedNumbersIgnoreNoPunctNumbers()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences("._", 13, "\\p \\v 1 word 3:4 word.");
		}

		[Test]
		public void GetReferences_BasicFootnoteSpanning()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences(new string[] { "._", "!_", "\u2019_", "\u201D_" },
				new int[] { 8, 4, 0, 2},
				"\\p \\v 1 text!\\f + \\fr 1:1 Note.\\f*\u2019 \u201D");
		}

		[Test]
		public void GetReferences_IntermediateFootnoteSpanning()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences(new string[] { "._", "!\u2019_\u201D_" },
				new int[] { 8, 4 },
				"\\p \\v 1 text!\\f + \\fr 1:1 Note.\\f*\u2019 \u201D");
		}

		[Test]
		public void GetReferences_AdvancedFootnoteSpanning()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences(new string[] { "._", "!\u2019_\u201D_" },
				new int[] { 8, 4 },
				"\\p \\v 1 text!\\f + \\fr 1:1 Note.\\f*\u2019 \u201D");
		}

		[Test]
		public void GetReferences_BasicFootnoteText()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences(new string[] { "_\u2018", "._", "\u2019_" },
				new int[] { 4, 9, 10 },
				"\\p \\v 1 text\\f + \\fr 1:1 \u2018Note.\u2019\\f* text");
		}

		[Test]
		public void GetReferences_IntermediateFootnoteText()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");
			TestGetReferences(new string[] { "_\u2018", ".\u2019_" },
				new int[] { 4, 9 },
				"\\p \\v 1 text\\f + \\fr 1:1 \u2018Note.\u2019\\f* text");
		}

		[Test]
		public void GetReferences_AdvancedFootnoteText()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Advanced");
			TestGetReferences(new string[] { "_\u2018", ".\u2019_" },
				new int[] { 4, 9 },
				"\\p \\v 1 text\\f + \\fr 1:1 \u2018Note.\u2019\\f* text");
		}

		[Test]
		public void GetReferences_BasicSectionHead()
		{
			m_dataSource.SetParameterValue("PunctCheckLevel", "Basic");
			TestGetReferences("._", 4, @"\s text.");
		}
		#endregion

		#region Check tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Check method reports inconsistencies for invalid and unknown
		/// patterns, but not for patterns which are valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Check_ValidPatternsAreNotReported()
		{
			PuncPatternsList puncPatterns = new PuncPatternsList();
			PuncPattern pattern = new PuncPattern();
			pattern.Pattern = "._";
			pattern.ContextPos = ContextPosition.WordFinal;
			pattern.Status = PuncPatternStatus.Valid;
			puncPatterns.Add(pattern);
			pattern = new PuncPattern();
			pattern.Pattern = ",";
			pattern.ContextPos = ContextPosition.WordBreaking;
			pattern.Status = PuncPatternStatus.Invalid;
			puncPatterns.Add(pattern);
			m_dataSource.SetParameterValue("PunctuationPatterns", puncPatterns.XmlString);
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");

			PunctuationCheck check = new PunctuationCheck(m_dataSource);

			m_dataSource.Text = "\\p This is nice. By nice,I mean really nice!";

			check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, "This is nice. By nice,I mean really nice!", 21, ",", "Invalid punctuation pattern");
			CheckError(1, "This is nice. By nice,I mean really nice!", 40, "!", "Unspecified use of punctuation pattern");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Check method reports the whole pattern for a punctuation pattern that
		/// consists of more than one character (not counting spaces).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Check_MultiCharPatterns()
		{
			m_dataSource.SetParameterValue("PunctuationPatterns", String.Empty);
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");

			PunctuationCheck check = new PunctuationCheck(m_dataSource);

			m_dataSource.Text = "\\p This _> is!?.";

			check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, "This _> is!?.", 5, "_>", "Unspecified use of punctuation pattern");
			CheckError(1, "This _> is!?.", 10, "!?.", "Unspecified use of punctuation pattern");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Check method reports the whole pattern for a punctuation pattern that
		/// consists of nested quotation marks, where the opening and closing marks are
		/// separated by a (thin no-break) space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Check_PatternsWithSpaceSeparatedQuoteMarks()
		{
			PuncPatternsList puncPatterns = new PuncPatternsList();
			PuncPattern pattern = new PuncPattern();
			pattern.Pattern = ",_";
			pattern.ContextPos = ContextPosition.WordFinal;
			pattern.Status = PuncPatternStatus.Valid;
			puncPatterns.Add(pattern);
			pattern = new PuncPattern();
			pattern.Pattern = "_\u201C";
			pattern.ContextPos = ContextPosition.WordInitial;
			pattern.Status = PuncPatternStatus.Valid;
			puncPatterns.Add(pattern);
			pattern = new PuncPattern();
			pattern.Pattern = "_\u2018";
			pattern.ContextPos = ContextPosition.WordInitial;
			pattern.Status = PuncPatternStatus.Valid;
			puncPatterns.Add(pattern);
			m_dataSource.SetParameterValue("PunctuationPatterns", puncPatterns.XmlString);
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");

			PunctuationCheck check = new PunctuationCheck(m_dataSource);

			m_dataSource.Text = "\\p Tom replied, \u201CBill said, \u2018Yes!\u2019\u202F\u201D";

			check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, "Tom replied, \u201CBill said, \u2018Yes!\u2019\u202F\u201D", 29, "!\u2019\u202F\u201D", "Unspecified use of punctuation pattern");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Check method reports an error for a paragraph whose only character
		/// is a quotation mark.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Check_ParaWithSingleQuotationMark()
		{
			PuncPatternsList puncPatterns = new PuncPatternsList();
			PuncPattern pattern = new PuncPattern();
			pattern.Pattern = "._";
			pattern.ContextPos = ContextPosition.WordFinal;
			pattern.Status = PuncPatternStatus.Valid;
			puncPatterns.Add(pattern);
			m_dataSource.SetParameterValue("PunctuationPatterns", puncPatterns.XmlString);
			m_dataSource.SetParameterValue("PunctCheckLevel", "Intermediate");

			PunctuationCheck check = new PunctuationCheck(m_dataSource);
			m_dataSource.Text = "\\p wow\u201D\\p \u2019";

			check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, "wow\u201D", 3, "\u201D", "Unspecified use of punctuation pattern");
			CheckError(1, "\u2019", 0, "\u2019", "Unspecified use of punctuation pattern");
		}
		#endregion
	}
}

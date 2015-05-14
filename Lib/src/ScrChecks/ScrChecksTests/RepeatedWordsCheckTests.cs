// ---------------------------------------------------------------------------------------------
// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RepeatedWordsTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class RepeatedWordsCheckTests : ScrChecksTestBase
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
			m_check = new RepeatedWordsCheck(m_dataSource);
		}
		#endregion

		#region Tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Repeated Words check for some simple cases that don't freak your mind out.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Basic()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("This this is is nice, my friend! ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("friend monkey ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("monkey friend.",
				TextType.Verse, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(3, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[0].Text, 5, "this", "Repeated word");
			CheckError(1, m_dataSource.m_tokens[0].Text, 13, "is", "Repeated word");
			CheckError(2, m_dataSource.m_tokens[2].Text, 0, "monkey", "Repeated word");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Repeated Words check when the repeated words differ in case (TE-6311).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DiffCapitalization()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("This thiS is IS nice, my friend! ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("friend monkey ",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("moNkEY friend.",
				TextType.Verse, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(3, m_errors.Count);

			CheckError(0, m_dataSource.m_tokens[0].Text, 5, "thiS", "Repeated word");
			CheckError(1, m_dataSource.m_tokens[0].Text, 13, "IS", "Repeated word");
			CheckError(2, m_dataSource.m_tokens[2].Text, 0, "moNkEY", "Repeated word");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Repeated Words check when chapter 1 is followed  by verse 1.
		/// Jira issue is TE-6255
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Chapter1Verse1()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Some verse text.",
				TextType.Verse, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Repeated Words check when the last word in a section head is the same
		/// as the first word in the following content. A repeated word should not be reported.
		/// Jira issue is TE-6634.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SameWordAfterSectionHead()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("Same Words", TextType.Other, true,
				false, "Section Head"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Words that begin the paragraph",
				TextType.Verse, false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count,
				"Word at para start is not a repeated word when the section head ends with the same word.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Repeated Words check when the same word occurs after a verse break. We
		/// do want this to be reported as a repeated word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SameWordAfterVerseNumber()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("In love", TextType.Verse, true,
				false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("5",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("love he predestined us",
				TextType.Verse, false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[2].Text, 0, "love", "Repeated word");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Repeated Words check when the repeated words have punctuation between them.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SameWordAfterPunctuation()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("I am, am I not?", TextType.Verse, true,
				false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Repeated Words check when the last word in a picture is the same
		/// as the word following the picture ORC. A repeated word should not be reported.
		/// Jira issue is TE-6402.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SameWordAfterPictureCaption()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("words ending a caption",
				TextType.PictureCaption, true, false, "Caption"));
			m_dataSource.m_tokens.Add(new DummyTextToken("Caption also begins this paragraph",
				TextType.Verse, false, false, "Paragraph"));

			m_check.Check(m_dataSource.TextTokens(), RecordError);
			Assert.AreEqual(0, m_errors.Count,
				"Word after caption marker is not a repeated word when the caption ends with the same word.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Repeated Words check when there are a bunch of ones mingled together, some
		/// of which are chapter numbers, some verse numbers, and some plain text.
		/// Jira issue is TE-6255, sort of.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Mixed1s()
		{
			// \c 1
			// \v 1
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));

			// \p 1 1
			m_dataSource.m_tokens.Add(new DummyTextToken(" 1 1 ",
				TextType.Verse, false, false, "Paragraph"));

			// \c 1
			// \p 1
			// \v 1
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.ChapterNumber, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken(" 1 ",
				TextType.Verse, false, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("1",
				TextType.VerseNumber, false, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens(), RecordError);

			Assert.AreEqual(1, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[2].Text, 3, "1", "Repeated word");
		}
		#endregion
	}
}

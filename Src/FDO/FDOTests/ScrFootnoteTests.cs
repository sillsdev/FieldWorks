// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2004' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrFootnoteTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ScrFootnote class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrFootnoteTests : ScrInMemoryFdoTestBase
	{
		private IScrBook m_genesis;
		private IScrSection m_introSection;
		private IScrSection m_section;
		private IScrSection m_secondSection;

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We have to reset the footnote cache!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			// Create book of Genesis with 3 sections. First section contains one content para,
			// second section contains head and two content paragraphs (with one footnote each),
			// third section contains head and one content paragraph (with one footnote).
			m_genesis = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(m_genesis, "Genesis");

			// Intro section
			m_introSection = AddSectionToMockedBook(m_genesis, true);
			IStTxtPara para = AddParaToMockedSectionContent(m_introSection,
				ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "Introduction para", null);
			AddFootnote(m_genesis, para, 10);

			// First section
			m_section = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(m_section, "Heading",
				ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(m_section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is the first paragraph.", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse two.", null);
			AddFootnote(m_genesis, para, 5);

			para = AddParaToMockedSectionContent(m_section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is the last paragraph.", null);
			AddFootnote(m_genesis, para, 10);

			// Second section
			m_secondSection = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(m_secondSection, "Heading of last section",
				ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(m_secondSection,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "This is the paragraph of the last section", null);
			AddFootnote(m_genesis, para, 20);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_introSection = null;
			m_genesis = null;
			m_section = null;
			m_secondSection = null;

			base.TestTearDown();
		}
		#endregion

		#region FindNextFootnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the title and
		/// the footnote is in the intro section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfTitleFootnoteInIntro()
		{
			FootnoteLocationInfo info = m_genesis.FindNextFootnote(0, 0, 0, ScrBookTags.kflidTitle);

			IScrFootnote footnote = info.m_footnote;
			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[0], footnote, "Should find a footnote somewhere");
			Assert.AreEqual(0, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the title and
		/// the footnote is in the title
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfTitleFootnoteInTitle()
		{
			IStFootnote expectedFootnote = AddFootnote(m_genesis,
				(IStTxtPara)m_genesis.TitleOA.ParagraphsOS[0], 5);
			FootnoteLocationInfo info = m_genesis.FindNextFootnote(0, 0, 0, ScrBookTags.kflidTitle);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in title");
			Assert.AreEqual(0, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(6, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrBookTags.kflidTitle, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is after the only footnote in a title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPAfterOnlyFootnoteInTitle()
		{
			// Adding a footnote puts it at the end of the sequence
			AddFootnote(m_genesis,
				(IStTxtPara)m_genesis.TitleOA.ParagraphsOS[0], 2);
			FootnoteLocationInfo info = m_genesis.FindNextFootnote(0, 0, 3, ScrBookTags.kflidTitle);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[0], footnote, "Should find a footnote in title");
			Assert.AreEqual(0, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the in the last paragraph of the
		/// book and the footnote is in the title. This should not find a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPEndOfBookFootnoteInTitle()
		{
			AddFootnote(m_genesis, (IStTxtPara)m_genesis.TitleOA.ParagraphsOS[0], 5);
			FootnoteLocationInfo info = m_genesis.FindNextFootnote(2, 0, 25, ScrSectionTags.kflidContent);
			Assert.IsNull(info, "Should not find a footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the title and
		/// the footnote is in the intro section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfTitleFootnoteInHeading()
		{
			IStTxtPara para = AddSectionHeadParaToSection(m_introSection,
				"Intro heading", ScrStyleNames.IntroSectionHead);
			IStFootnote expectedFootnote = AddFootnote(m_genesis, para, 5);

			FootnoteLocationInfo info = m_genesis.FindNextFootnote(0, 0, 0, ScrBookTags.kflidTitle);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in heading");
			Assert.AreEqual(0, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(6, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidHeading, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the title and
		/// the footnote is in the title of the next book. This should not find a footnote
		/// in the current book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPInTitleNoFootnoteInBook()
		{

			IScrBook exodus = AddBookWithTwoSections(2, "Exodus");
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "Some text", null);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "Some more text", null);

			IScrBook leviticus = AddBookWithTwoSections(3, "Leviticus");
			AddFootnote(leviticus, (IStTxtPara)leviticus.TitleOA.ParagraphsOS[0], 5);

			FootnoteLocationInfo info = exodus.FindNextFootnote(0, 0, 0, ScrBookTags.kflidTitle);
			Assert.IsNull(info, "Should not find a footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the end of the title and
		/// the footnote is before the IP. This should not find a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPInTitleFootnoteBeforeIP()
		{
			IScrBook exodus = AddBookWithTwoSections(2, "Exodus");
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "Some text", null);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "Some more text", null);
			AddFootnote(exodus, (IStTxtPara)exodus.TitleOA.ParagraphsOS[0], 0);

			FootnoteLocationInfo info = exodus.FindNextFootnote(0, 0, 5, ScrBookTags.kflidTitle);
			Assert.IsNull(info, "Should not find a footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the heading and
		/// there are no footnotes in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPInHeadingNoFootnoteInBook()
		{
			IScrBook exodus = AddBookWithTwoSections(2, "Exodus");
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "Some text", null);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "Some more text", null);

			FootnoteLocationInfo info = exodus.FindNextFootnote(0, 0, 1, ScrSectionTags.kflidHeading);
			Assert.IsNull(info, "Should not find a footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the content and
		/// there are no footnotes in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPInContentNoFootnoteInBook()
		{
			IScrBook exodus = AddBookWithTwoSections(2, "Exodus");
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "Some text", null);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "Some more text", null);

			FootnoteLocationInfo info = exodus.FindNextFootnote(0, 0, 2, ScrSectionTags.kflidContent);
			Assert.IsNull(info, "Should not find a footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the heading and
		/// the footnote is in the same heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfHeadingFootnoteInHeading()
		{
			IStTxtPara para = AddSectionHeadParaToSection(m_introSection,
				"Intro heading", ScrStyleNames.IntroSectionHead);
			IStFootnote expectedFootnote = AddFootnote(m_genesis, para, 5);

			FootnoteLocationInfo info = m_genesis.FindNextFootnote(0, 0, 2, ScrSectionTags.kflidHeading);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in heading");
			Assert.AreEqual(0, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(6, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidHeading, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the heading and
		/// the footnote is in the contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfHeadingFootnoteInContents()
		{
			IStTxtPara para = AddSectionHeadParaToSection(m_introSection,
				"Intro heading", ScrStyleNames.IntroSectionHead);

			FootnoteLocationInfo info = m_genesis.FindNextFootnote(0, 0, 0, ScrSectionTags.kflidHeading);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[0].Hvo, footnote.Hvo, "Should find a footnote in content");
			Assert.AreEqual(0, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is somewhere in a heading
		/// that consists of multiple paragraphs and the footnote is in the contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPInMultiParaHeadingFootnoteInContents()
		{
			AddSectionHeadParaToSection(m_introSection, "Intro heading", ScrStyleNames.IntroSectionHead);
			AddSectionHeadParaToSection(m_introSection, "Another intro heading", ScrStyleNames.IntroSectionHead);

			FootnoteLocationInfo info = m_genesis.FindNextFootnote(0, 1, 4, ScrSectionTags.kflidHeading);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[0].Hvo, footnote.Hvo, "Should find a footnote in content");
			Assert.AreEqual(0, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the heading and
		/// the footnote is in the next heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfHeadingFootnoteInNextHeading()
		{
			IScrSection section = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(section, "Heading",
				ScrStyleNames.SectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "This is the paragraph of the section", null);

			section = AddSectionToMockedBook(m_genesis);
			para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "This is the paragraph of the other section",
				null);
			para = AddSectionHeadParaToSection(section, "Another Heading",
				ScrStyleNames.SectionHead);
			IStFootnote expectedFootnote = AddFootnote(m_genesis, para, 7);

			FootnoteLocationInfo info = m_genesis.FindNextFootnote(3, 0, 0, ScrSectionTags.kflidHeading);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in next heading");
			Assert.AreEqual(4, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidHeading, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the heading and
		/// the footnote is in the content of the next section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfHeadingFootnoteInContentOfNextSection()
		{
			IScrSection section = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(section, "Heading",
				ScrStyleNames.SectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "This is the paragraph of the section", null);

			section = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(section, "Another Heading",
				ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "This is the paragraph of the other section",
				null);
			IStFootnote expectedFootnote = AddFootnote(m_genesis, para, 7);

			FootnoteLocationInfo info = m_genesis.FindNextFootnote(3, 0, 0, ScrSectionTags.kflidHeading);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in content of next section");
			Assert.AreEqual(4, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the content and
		/// the footnote is in the same content paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfContentFootnoteInSamePara()
		{
			FootnoteLocationInfo info = m_genesis.FindNextFootnote(0, 0, 0, ScrSectionTags.kflidContent);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[0].Hvo, footnote.Hvo,
				"Should find a footnote in same paragraph in content");
			Assert.AreEqual(0, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the content and
		/// the footnote is in the next paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfContentFootnoteInNextPara()
		{
			FootnoteLocationInfo info = m_genesis.FindNextFootnote(1, 0, 10, ScrSectionTags.kflidContent);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[2].Hvo, footnote.Hvo, "Should find a footnote in next para");
			Assert.AreEqual(1, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(1, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the content and
		/// the footnote is in the heading of the next section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfContentFootnoteInNextHeading()
		{
			IScrSection section = AddSectionToMockedBook(m_genesis);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "This is the paragraph of the other section",
				null);
			para = AddSectionHeadParaToSection(section, "Another Heading",
				ScrStyleNames.SectionHead);
			IStFootnote expectedFootnote = AddFootnote(m_genesis, para, 7);

			FootnoteLocationInfo info = m_genesis.FindNextFootnote(2, 0, 25, ScrSectionTags.kflidContent);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in next heading");
			Assert.AreEqual(3, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidHeading, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the content and
		/// the footnote is in the content of the next section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfContentFootnoteInContentOfNextSection()
		{
			IScrSection section = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(section, "Another Heading",
				ScrStyleNames.SectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "This is the paragraph of the other section",
				null);
			IStFootnote expectedFootnote = AddFootnote(m_genesis, para, 7);

			FootnoteLocationInfo info = m_genesis.FindNextFootnote(2, 0, 25, ScrSectionTags.kflidContent);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in content of next section");
			Assert.AreEqual(3, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(0, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, info.m_tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is at the beginning of the content and
		/// the footnote is at the beginning of the next paragraph. (TE-4024)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPBeginningOfContentFootnoteBeginOfNextPara()
		{
			IStFootnote expectedFootnote = AddFootnote(m_genesis,
				(IStTxtPara)m_section.ContentOA.ParagraphsOS[1], 0);

			FootnoteLocationInfo info = m_genesis.FindNextFootnote(1, 0, 10, ScrSectionTags.kflidContent);
			IScrFootnote footnote = info.m_footnote;

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in next para");
			Assert.AreEqual(1, info.m_iSection, "Should have the correct section set");
			Assert.AreEqual(1, info.m_iPara, "Should have the correct paragraph set");
			Assert.AreEqual(1, info.m_ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, info.m_tag, "Wrong tag");
		}
		#endregion

		#region FindPreviousFootnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is at the end of the book and
		/// the footnote is in the same section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPEndOfBookFootnoteInLastSection()
		{
			IStTxtPara para = AddParaToMockedSectionContent(m_secondSection,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "This is another paragraph", null);

			int iSection = 2;
			int iPara = 1;
			int ich = para.Contents.Length;
			int tag = ScrSectionTags.kflidContent;
			IScrFootnote footnote = m_genesis.FindPrevFootnote(ref iSection, ref iPara, ref ich,
				ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[3], footnote, "Should find a footnote somewhere");
			Assert.AreEqual(2, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(20, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is at the end of the book and
		/// the footnote is in the section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPEndOfBookFootnoteInLastSectionHead()
		{
			IStTxtPara para = (IStTxtPara)m_secondSection.HeadingOA.ParagraphsOS[0];
			IStFootnote expectedFootnote = AddFootnote(m_genesis, para, 8);
			int iSection = 2;
			int iPara = 0;
			int ich = 5;
			int tag = ScrSectionTags.kflidContent;
			IScrFootnote footnote = m_genesis.FindPrevFootnote(ref iSection, ref iPara, ref ich,
				ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(2, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidHeading, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is at the end of the book and
		/// the footnote is at the end of the section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPEndOfBookFootnoteAtEndOfLastSectionHead()
		{
			IStTxtPara para = (IStTxtPara)m_secondSection.HeadingOA.ParagraphsOS[0];
			IStFootnote expectedFootnote = AddFootnote(m_genesis, para,
				para.Contents.Length);
			int iSection = 2;
			int iPara = 0;
			int ich = 5;
			int tag = ScrSectionTags.kflidContent;
			IScrFootnote footnote = m_genesis.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(2, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(para.Contents.Length-1, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidHeading, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is at the end of the book and
		/// the footnote is in the title of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPEndOfBookFootnoteInBookTitle()
		{
			IScrBook book = AddBookWithTwoSections(2, "Exodus");
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "This is text", null);
			para = (IStTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "This is more text", null);
			IStFootnote expectedFootnote = AddFootnote(book,
				(IStTxtPara)book.TitleOA.ParagraphsOS[0], 3);
			int iSection = 1;
			int iPara = 0;
			int ich = 5;
			int tag = ScrSectionTags.kflidContent;
			IScrFootnote footnote = book.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(3, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrBookTags.kflidTitle, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is in a section head and the
		/// footnote is in the previous section contents
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPLastSectionHeadFootnoteInPrevSectionContent()
		{
			int iSection = 2;
			int iPara = 0;
			int ich = 5;
			int tag = ScrSectionTags.kflidHeading;
			IScrFootnote footnote = m_genesis.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[2], footnote, "Should find a footnote somewhere");
			Assert.AreEqual(1, iSection, "Should have the correct section set");
			Assert.AreEqual(1, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(10, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is in a section head and the
		/// footnote is in the previous section heading
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPLastSectionHeadFootnoteInPrevSectionHeading()
		{
			IScrBook book = AddBookWithTwoSections(2, "Exodus");
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			IStFootnote expectedFootnote = AddFootnote(book, para, 3);
			int iSection = 1;
			int iPara = 0;
			int ich = 2;
			int tag = ScrSectionTags.kflidHeading;
			IScrFootnote footnote = book.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(3, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidHeading, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is in a section head and the
		/// footnote is in the book title
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPLastSectionHeadFootnoteInBookTitle()
		{
			IScrBook book = AddBookWithTwoSections(2, "Exodus");
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "This is text", null);
			IStFootnote expectedFootnote = AddFootnote(book,
				(IStTxtPara)book.TitleOA.ParagraphsOS[0], 3);
			int iSection = 1;
			int iPara = 0;
			int ich = 2;
			int tag = ScrSectionTags.kflidHeading;
			IScrFootnote footnote = book.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(3, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrBookTags.kflidTitle, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is in a section head and the
		/// footnote is at the end of the book title
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPLastSectionHeadFootnoteAtEndOfBookTitle()
		{
			IScrBook book = AddBookWithTwoSections(2, "Exodus");
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "This is text", null);
			para = (IStTxtPara)book.TitleOA.ParagraphsOS[0];
			IStFootnote expectedFootnote = AddFootnote(book, para,
				para.Contents.Length);
			int iSection = 1;
			int iPara = 0;
			int ich = 2;
			int tag = ScrSectionTags.kflidHeading;
			IScrFootnote footnote = book.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(para.Contents.Length-1, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrBookTags.kflidTitle, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is in the book title and the
		/// footnote is in the book title
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPInBookTitleFootnoteInBookTitle()
		{
			IStFootnote expectedFootnote = AddFootnote(m_genesis,
				(IStTxtPara)m_genesis.TitleOA.ParagraphsOS[0], 3);
			int iSection = 0;
			int iPara = 0;
			int ich = 5;
			int tag = ScrBookTags.kflidTitle;
			IScrFootnote footnote = m_genesis.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(3, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrBookTags.kflidTitle, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is in the book title before the
		/// footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPInBookTitleFootnoteAfterIP()
		{
			IStFootnote expectedFootnote = AddFootnote(m_genesis,
				(IStTxtPara)m_genesis.TitleOA.ParagraphsOS[0], 3);
			int iSection = 0;
			int iPara = 0;
			int ich = 1;
			int tag = ScrBookTags.kflidTitle;
			IScrFootnote footnote = m_genesis.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNull(footnote, "Should not find a footnote");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(1, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrBookTags.kflidTitle, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is at the end of the book and
		/// there is no footnote in the book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPEndOfBookNoFootnoteInBook()
		{
			IScrBook book = AddBookWithTwoSections(2, "Exodus");
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "This is text", null);
			para = (IStTxtPara)book.SectionsOS[1].ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "This is more text", null);
			int iSection = 1;
			int iPara = 0;
			int ich = 5;
			int tag = ScrSectionTags.kflidContent;
			IScrFootnote footnote = book.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNull(footnote, "Should not find a footnote");
			Assert.AreEqual(1, iSection, "Should have the same section");
			Assert.AreEqual(0, iPara, "Should have the same paragraph");
			Assert.AreEqual(5, ich, "Should have the same IP position");
			Assert.AreEqual(ScrSectionTags.kflidContent, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPreviousFootnote method when the IP is in a section head that has
		/// multiple paragraphs and the footnote is in the previous section contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPreviousFootnote_IPInMultiParaHeadingFootnoteInPrevSectionContents()
		{
			AddSectionHeadParaToSection(m_section, "More heading",
				ScrStyleNames.IntroSectionHead);
			int iSection = 1;
			int iPara = 1;
			int ich = 5;
			int tag = ScrSectionTags.kflidHeading;
			IScrFootnote footnote = m_genesis.FindPrevFootnote(ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[0], footnote, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(10, ich, "Should have the correct IP position set");
			Assert.AreEqual(ScrSectionTags.kflidContent, tag, "Wrong tag");
		}
		#endregion

		#region GetRefForFootnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding a reference for a footnote in a Scripture paragraph when the
		/// Scripture.DisplayFootnoteReference is true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRefForFootnote_Scripture()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			m_scr.DisplayFootnoteReference = true;
			m_scr.CrossRefsCombinedWithFootnotes = false;
			m_scr.DisplayCrossRefReference = false; // Just to make sure it's not using this by mistake
			Assert.AreEqual("1:1 ", footnote.RefAsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding a reference for a footnote in a Scripture paragraph when the
		/// Scripture.DisplayFootnoteReference is false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRefForFootnote_Scripture_DontDisplay()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			m_scr.DisplayFootnoteReference = false;
			m_scr.CrossRefsCombinedWithFootnotes = false;
			m_scr.DisplayCrossRefReference = true; // Just to make sure it's not using this by mistake
			Assert.AreEqual(string.Empty, footnote.RefAsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding a reference for a cross-reference in a Scripture paragraph when the
		/// Scripture.DisplayCrossRefReference is true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRefForCrossRef_Scripture()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.CrossRefFootnoteParagraph);
			m_scr.DisplayFootnoteReference = false; // Just to make sure it's not using this by mistake
			m_scr.CrossRefsCombinedWithFootnotes = false;
			m_scr.DisplayCrossRefReference = true;
			Assert.AreEqual("1:1 ", footnote.RefAsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding a reference for a footnote in a Scripture paragraph when the
		/// Scripture.DisplayCrossRefReference is false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRefForCrossRef_Scripture_DontDisplay()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.CrossRefFootnoteParagraph);
			m_scr.DisplayFootnoteReference = true; // Just to make sure it's not using this by mistake
			m_scr.CrossRefsCombinedWithFootnotes = false;
			m_scr.DisplayCrossRefReference = false;
			Assert.AreEqual(string.Empty, footnote.RefAsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding a reference for a footnote for intro paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRefForFootnote_IntroPara()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[0];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			m_scr.DisplayFootnoteReference = true;
			Assert.AreEqual("", footnote.RefAsString, "Intro footnotes don't have Scripture references.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test updating a reference for a footnote - reference should change when a verse
		/// number is inserted before the footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateRefForFootnote_InsertVerse()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[2];
			IStTxtPara footnotePara = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			m_scr.DisplayFootnoteReference = true;
			Assert.AreEqual("1:5 ", footnote.RefAsString);

			// Add a new verse number before the footnote marker.
			IScrSection section = m_genesis.SectionsOS[1];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(5, 5, "21", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			para.Contents = bldr.GetString();

			// Verify that reference for footnote is now correct
			Assert.AreEqual("1:21 ", footnote.RefAsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test updating a reference for a footnote - reference should change when a verse
		/// number is removed before the footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateRefForFootnote_DeleteVerse()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[2];
			IStTxtPara footnotePara = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			m_scr.DisplayFootnoteReference = true;
			Assert.AreEqual("1:5 ", footnote.RefAsString);

			// Add a new verse number before the footnote marker.
			IScrSection section = m_genesis.SectionsOS[1];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(0, 1, null, null); // Delete verse 5
			para.Contents = bldr.GetString();

			// Verify that reference for footnote is now correct
			Assert.AreEqual("1:2 ", footnote.RefAsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test footnote reference when footnote is in a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteRefVerseBridge()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[2];
			IStTxtPara footnotePara = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			m_scr.DisplayFootnoteReference = true;
			Assert.AreEqual("1:5 ", footnote.RefAsString);

			// Add a new verse bridge
			IScrSection section = m_genesis.SectionsOS[1];
			IStTxtPara para = (IStTxtPara) section.ContentOA.ParagraphsOS[1];
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(1, 1, "-9", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			para.Contents = bldr.GetString();

			// Verify that reference for footnote is now correct
			Assert.AreEqual("1:5-9 ", footnote.RefAsString);
		}
		#endregion

		#region UpdateFootnoteRef tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpdateFootnoteRef method when there is a chapter reference in the middle
		/// of a paragraph before a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateFootnoteRef_ChapterMidSection()
		{
			m_scr.DisplayFootnoteReference = true;
			IScrTxtPara newPara = AddParaToMockedSectionContent(m_secondSection, ScrStyleNames.NormalParagraph);
			AddVerse(newPara, 2, 0, "Start of chapter two.");
			IScrFootnote newFootnote = AddFootnote(m_genesis, newPara, newPara.Contents.Length);

			Assert.AreEqual("2:1 ", newFootnote.RefAsString); // calls UpdateFootnoteRef
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpdateFootnoteRef method when there is no chapter or verse reference in a
		/// paragraph with a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateFootnoteRef_NoChapterVerseInPara()
		{
			m_scr.DisplayFootnoteReference = true;
			IScrFootnote footnote = m_genesis.FootnotesOS[3];

			Assert.AreEqual("1:5 ", footnote.RefAsString); // calls UpdateFootnoteRef
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpdateFootnoteRef method when there is a previous footnote that has a
		/// valid Scripture reference in the same paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateFootnoteRef_PrevFootnoteInSamePara()
		{
			m_scr.DisplayFootnoteReference = true;
			IScrFootnote footnotePrev = m_genesis.FootnotesOS[1];
			string dummyRef = footnotePrev.RefAsString; // set all footnote references

			// We have to insert a new footnote to use previously set footnote references.
			IScrTxtPara para = (IScrTxtPara)m_section.ContentOA[0];
			IScrFootnote newFootnote = AddFootnote(m_genesis, para, para.Contents.Length);

			Assert.AreEqual("1:2 ", newFootnote.RefAsString); // calls UpdateFootnoteRef
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the UpdateFootnoteRef method when there is a previous footnote that has a
		/// valid Scripture reference in a previous paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateFootnoteRef_PrevFootnoteInPrevPara()
		{
			m_scr.DisplayFootnoteReference = true;
			IScrFootnote footnotePrev = m_genesis.FootnotesOS[1];
			string dummyRef = footnotePrev.RefAsString; // set all footnote references

			IScrTxtPara newPara = AddParaToMockedSectionContent(m_section, ScrStyleNames.NormalParagraph);
			// We have to insert a new footnote in to use previously set footnote references.
			IScrFootnote newFootnote = AddFootnote(m_genesis, newPara, 0);

			Assert.AreEqual("1:5 ", newFootnote.RefAsString); // calls UpdateFootnoteRef
		}

		#endregion

		#region GetMarkerForFootnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding the marker for a footnote in a Scripture paragraph when the
		/// Scripture.FootnoteMarker is "a".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMarkerForFootnote_Scripture_AutoNumbered()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[0];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);

			footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			para = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);

			footnote = (IScrFootnote)m_genesis.FootnotesOS[2];
			para = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);

			ITsString expectedMarker = MakeMarker("c");
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;  // Just to make sure it's not using this by mistake
			AssertEx.AreTsStringsEqual(expectedMarker, footnote.FootnoteMarker);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding the marker for a footnote in a Scripture paragraph when the
		/// Scripture.FootnoteMarker is null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMarkerForFootnote_Scripture_Nothing()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;  // Just to make sure it's not using this by mistake
			AssertEx.AreTsStringsEqual(MakeMarker(""), footnote.FootnoteMarker);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding the marker for a footnote in a Scripture paragraph when the
		/// Scripture.FootnoteMarker is "$".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMarkerForFootnote_Scripture_LiteralSymbol()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			m_scr.FootnoteMarkerSymbol = "$";
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;  // Just to make sure it's not using this by mistake
			AssertEx.AreTsStringsEqual(MakeMarker("$"), footnote.FootnoteMarker);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding the marker for a cross-reference in a Scripture paragraph for a sequence
		/// cross-reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMarkerForCrossRef_Scripture_AutoNumbered()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[0];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.CrossRefFootnoteParagraph);

			footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			para = footnote.AddNewTextPara(ScrStyleNames.CrossRefFootnoteParagraph);

			footnote = (IScrFootnote)m_genesis.FootnotesOS[2];
			para = footnote.AddNewTextPara(ScrStyleNames.CrossRefFootnoteParagraph);

			ITsString expectedMarker = MakeMarker("c");
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;  // Just to make sure it's not using this by mistake
			AssertEx.AreTsStringsEqual(expectedMarker, footnote.FootnoteMarker);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding the marker for a cross-reference in a Scripture paragraph for no marker
		/// in text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMarkerForCrossRef_Scripture_Nothing()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.CrossRefFootnoteParagraph);
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker; // Just to make sure it's not using this by mistake
			AssertEx.AreTsStringsEqual(MakeMarker(""), footnote.FootnoteMarker);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test finding the marker for a cross-reference in a Scripture paragraph when
		/// CrossRefMarkerSymbol is "$".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetMarkerForCrossRef_Scripture_LiteralSymbol()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			IStTxtPara para = footnote.AddNewTextPara(ScrStyleNames.CrossRefFootnoteParagraph);
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			m_scr.CrossRefMarkerSymbol = "$";
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker; // Just to make sure it's not using this by mistake
			AssertEx.AreTsStringsEqual(MakeMarker("$"), footnote.FootnoteMarker);
		}
		#endregion

		#region FindCurrentFootnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting current footnote if we are directly in front of a footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindCurrentFootnote_BeforeMarker()
		{
			IScrFootnote footnote = m_genesis.FindCurrentFootnote(1, 0, 5, ScrSectionTags.kflidContent);

			Assert.AreEqual(m_genesis.FootnotesOS[1], footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting current footnote if we are directly after a footnote marker. This is
		/// already the next run, so doesn't find a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindCurrentFootnote_AfterMarker()
		{
			IScrFootnote footnote = m_genesis.FindCurrentFootnote(1, 0, 6, ScrSectionTags.kflidContent);

			Assert.IsNull(footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting current footnote if we are not on a footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindCurrentFootnote_NotOnMarker()
		{
			IScrFootnote footnote = m_genesis.FindCurrentFootnote(1, 0, 3, ScrSectionTags.kflidContent);

			Assert.IsNull(footnote);
		}
		#endregion

		#region Footnote integrity constraints tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to make sure that ScrFootnote prevents the addition of a second footnote
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void PreventMultipleFootnoteParagraphs_AddToEnd()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to make sure that ScrFootnote prevents the insertion of a new footnote
		/// paragraph at the beginning.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void PreventMultipleFootnoteParagraphs_InsertAtBeginning()
		{
			IScrFootnote footnote = (IScrFootnote)m_genesis.FootnotesOS[1];
			footnote.AddNewTextPara(ScrStyleNames.NormalFootnoteParagraph);
			footnote.InsertNewTextPara(0, ScrStyleNames.NormalFootnoteParagraph);
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the marker.
		/// </summary>
		/// <param name="marker">The marker text.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString MakeMarker(string marker)
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			ITsPropsBldr propBldr = TsPropsBldrClass.Create();
			propBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.FootnoteMarker);
			propBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
				Cache.DefaultVernWs);
			bldr.Replace(0, 0, marker, propBldr.GetTextProps());
			return bldr.GetString();
		}
		#endregion
	}
}

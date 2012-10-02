// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrFootnoteTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.FDO.Scripture
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
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			ScrBook.ClearAllFootnoteCaches(Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_introSection = null;
			m_genesis = null;
			m_section = null;
			m_secondSection = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create book of Genesis with 3 sections. First section contains one content para,
		/// second section contains head and two content paragraphs (with one footnote each),
		/// third section contains head and one content paragraph (with one footnote).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(m_genesis.Hvo, "Genesis");

			// Intro section
			m_introSection = m_scrInMemoryCache.AddIntroSectionToMockedBook(m_genesis.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_introSection.Hvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Introduction para", null);
			m_scrInMemoryCache.AddFootnote(m_genesis, para, 10);
			m_introSection.AdjustReferences();

			// First section
			m_section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(m_section.Hvo, "Heading",
				ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the first paragraph.", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Verse two.", null);
			m_scrInMemoryCache.AddFootnote(m_genesis, para, 5);

			para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the last paragraph.", null);
			m_scrInMemoryCache.AddFootnote(m_genesis, para, 10);
			m_section.AdjustReferences();

			// Second section
			m_secondSection = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(m_secondSection.Hvo, "Heading of last section",
				ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_secondSection.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph of the last section", null);
			m_scrInMemoryCache.AddFootnote(m_genesis, para, 20);
			m_secondSection.AdjustReferences();
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
			CheckDisposed();

			int iSection = 0;
			int iPara = 0;
			int ich = 0;
			int tag = (int)ScrBook.ScrBookTags.kflidTitle;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS.HvoArray[0], footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis,
				(StTxtPara)m_genesis.TitleOA.ParagraphsOS.FirstItem, 5);
			int iSection = 0;
			int iPara = 0;
			int ich = 0;
			int tag = (int)ScrBook.ScrBookTags.kflidTitle;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in title");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(6, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag, "Wrong tag");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNextFootnote method when the IP is after the only footnote in a title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextFootnote_IPAfterOnlyFootnoteInTitle()
		{
			CheckDisposed();

			// Adding a footnote puts it at the end of the sequence
			m_scrInMemoryCache.AddFootnote(m_genesis,
				(StTxtPara)m_genesis.TitleOA.ParagraphsOS.FirstItem, 2);
			int iSection = 0;
			int iPara = 0;
			int ich = 3;
			int tag = (int)ScrBook.ScrBookTags.kflidTitle;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS.HvoArray[0], footnote.Hvo, "Should find a footnote in title");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			m_scrInMemoryCache.AddFootnote(m_genesis,
				(StTxtPara)m_genesis.TitleOA.ParagraphsOS.FirstItem, 5);
			int iSection = 2;
			int iPara = 0;
			int ich = 25;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNull(footnote, "Should not find a footnote");
			Assert.AreEqual(2, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(25, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddSectionHeadParaToSection(m_introSection.Hvo,
				"Intro heading", ScrStyleNames.IntroSectionHead);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis, para, 5);

			int iSection = 0;
			int iPara = 0;
			int ich = 0;
			int tag = (int)ScrBook.ScrBookTags.kflidTitle;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in heading");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(6, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, tag, "Wrong tag");
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
			CheckDisposed();

			IScrBook exodus = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			IScrSection section = exodus.SectionsOS.FirstItem;
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS.FirstItem;
			m_scrInMemoryCache.AddRunToMockedPara(para, "Some text", null);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Some more text", null);

			IScrBook leviticus = m_scrInMemoryCache.AddBookWithTwoSections(3, "Leviticus");
			m_scrInMemoryCache.AddFootnote(leviticus, (StTxtPara)leviticus.TitleOA.ParagraphsOS.FirstItem, 5);

			int iSection = 0;
			int iPara = 0;
			int ich = 0;
			int tag = (int)ScrBook.ScrBookTags.kflidTitle;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, exodus, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNull(footnote, "Should not find a footnote");
			Assert.AreEqual(0, iSection, "Should not change the section");
			Assert.AreEqual(0, iPara, "Should not change the paragraph");
			Assert.AreEqual(0, ich, "Should not change the position");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag, "Should not change the tag");
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
			CheckDisposed();

			IScrBook exodus = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			IScrSection section = exodus.SectionsOS.FirstItem;
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS.FirstItem;
			m_scrInMemoryCache.AddRunToMockedPara(para, "Some text", null);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Some more text", null);
			m_scrInMemoryCache.AddFootnote(exodus, (StTxtPara)exodus.TitleOA.ParagraphsOS[0], 0);

			int iSection = 0;
			int iPara = 0;
			int ich = 5;
			int tag = (int)ScrBook.ScrBookTags.kflidTitle;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, exodus, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNull(footnote, "Should not find a footnote");
			Assert.AreEqual(0, iSection, "Should not change the section");
			Assert.AreEqual(0, iPara, "Should not change the paragraph");
			Assert.AreEqual(5, ich, "Should not change the position");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag, "Should not change the tag");
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
			CheckDisposed();

			IScrBook exodus = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			IScrSection section = exodus.SectionsOS.FirstItem;
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS.FirstItem;
			m_scrInMemoryCache.AddRunToMockedPara(para, "Some text", null);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Some more text", null);

			int iSection = 0;
			int iPara = 0;
			int ich = 1;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, exodus, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNull(footnote, "Should not find a footnote");
			Assert.AreEqual(0, iSection, "Should not change the section");
			Assert.AreEqual(0, iPara, "Should not change the paragraph");
			Assert.AreEqual(1, ich, "Should not change the position");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, tag, "Should not change the tag");
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
			CheckDisposed();

			IScrBook exodus = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			IScrSection section = exodus.SectionsOS.FirstItem;
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS.FirstItem;
			m_scrInMemoryCache.AddRunToMockedPara(para, "Some text", null);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Some more text", null);

			int iSection = 0;
			int iPara = 0;
			int ich = 2;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, exodus, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNull(footnote, "Should not find a footnote");
			Assert.AreEqual(0, iSection, "Should not change the section");
			Assert.AreEqual(0, iPara, "Should not change the paragraph");
			Assert.AreEqual(2, ich, "Should not change the position");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Should not change the tag");
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
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddSectionHeadParaToSection(m_introSection.Hvo,
				"Intro heading", ScrStyleNames.IntroSectionHead);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis, para, 5);

			int iSection = 0;
			int iPara = 0;
			int ich = 0;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in heading");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(6, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, tag, "Wrong tag");
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
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddSectionHeadParaToSection(m_introSection.Hvo,
				"Intro heading", ScrStyleNames.IntroSectionHead);

			int iSection = 0;
			int iPara = 0;
			int ich = 0;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[0].Hvo, footnote.Hvo, "Should find a footnote in content");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddSectionHeadParaToSection(m_introSection.Hvo,
				"Intro heading", ScrStyleNames.IntroSectionHead);
			para = m_scrInMemoryCache.AddSectionHeadParaToSection(m_introSection.Hvo,
				"Another intro heading", ScrStyleNames.IntroSectionHead);

			int iSection = 0;
			int iPara = 1;
			int ich = 4;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[0].Hvo, footnote.Hvo, "Should find a footnote in content");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Heading",
				ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph of the section", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph of the other section",
				null);
			para = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Another Heading",
				ScrStyleNames.SectionHead);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis, para, 7);
			section.AdjustReferences();

			int iSection = 3;
			int iPara = 0;
			int ich = 0;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in next heading");
			Assert.AreEqual(4, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, tag, "Wrong tag");
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
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Heading",
				ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph of the section", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Another Heading",
				ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph of the other section",
				null);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis, para, 7);
			section.AdjustReferences();

			int iSection = 3;
			int iPara = 0;
			int ich = 0;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in content of next section");
			Assert.AreEqual(4, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			int iSection = 0;
			int iPara = 0;
			int ich = 0;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[0].Hvo, footnote.Hvo,
				"Should find a footnote in same paragraph in content");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			int iSection = 1;
			int iPara = 0;
			int ich = 10;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS[2].Hvo, footnote.Hvo, "Should find a footnote in next para");
			Assert.AreEqual(1, iSection, "Should have the correct section set");
			Assert.AreEqual(1, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(11, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph of the other section",
				null);
			para = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Another Heading",
				ScrStyleNames.SectionHead);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis, para, 7);
			section.AdjustReferences();

			int iSection = 2;
			int iPara = 0;
			int ich = 25;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in next heading");
			Assert.AreEqual(3, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, tag, "Wrong tag");
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
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Another Heading",
				ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph of the other section",
				null);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis, para, 7);
			section.AdjustReferences();

			int iSection = 2;
			int iPara = 0;
			int ich = 25;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in content of next section");
			Assert.AreEqual(3, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis,
				(StTxtPara)m_section.ContentOA.ParagraphsOS[1], 0);

			int iSection = 1;
			int iPara = 0;
			int ich = 10;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindNextFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote in next para");
			Assert.AreEqual(1, iSection, "Should have the correct section set");
			Assert.AreEqual(1, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(1, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_secondSection.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is another paragraph", null);

			int iSection = 2;
			int iPara = 1;
			int ich = para.Contents.Length;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS.HvoArray[3], footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(2, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(20, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_secondSection.HeadingOA.ParagraphsOS.FirstItem;
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis, para, 8);
			int iSection = 2;
			int iPara = 0;
			int ich = 5;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(2, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(8, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, tag, "Wrong tag");
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
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_secondSection.HeadingOA.ParagraphsOS.FirstItem;
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis, para,
				para.Contents.Length);
			int iSection = 2;
			int iPara = 0;
			int ich = 5;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(2, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(para.Contents.Length-1, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, tag, "Wrong tag");
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			StTxtPara para = (StTxtPara)((ScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is text", null);
			para = (StTxtPara)((ScrSection)book.SectionsOS[1]).ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is more text", null);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(book,
				(StTxtPara)book.TitleOA.ParagraphsOS[0], 3);
			int iSection = 1;
			int iPara = 0;
			int ich = 5;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, book, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(3, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag, "Wrong tag");
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
			CheckDisposed();

			int iSection = 2;
			int iPara = 0;
			int ich = 5;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS.HvoArray[2], footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(1, iSection, "Should have the correct section set");
			Assert.AreEqual(1, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(10, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			StTxtPara para = (StTxtPara)((ScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(book, para, 3);
			int iSection = 1;
			int iPara = 0;
			int ich = 2;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, book, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(3, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, tag, "Wrong tag");
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			StTxtPara para = (StTxtPara)((ScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is text", null);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(book,
				(StTxtPara)book.TitleOA.ParagraphsOS[0], 3);
			int iSection = 1;
			int iPara = 0;
			int ich = 2;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, book, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(3, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag, "Wrong tag");
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			StTxtPara para = (StTxtPara)((ScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is text", null);
			para = (StTxtPara)book.TitleOA.ParagraphsOS[0];
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(book, para,
				para.Contents.Length);
			int iSection = 1;
			int iPara = 0;
			int ich = 2;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, book, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(para.Contents.Length-1, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag, "Wrong tag");
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
			CheckDisposed();

			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis,
				(StTxtPara)m_genesis.TitleOA.ParagraphsOS[0], 3);
			int iSection = 0;
			int iPara = 0;
			int ich = 5;
			int tag = (int)ScrBook.ScrBookTags.kflidTitle;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(3, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag, "Wrong tag");
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
			CheckDisposed();

			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_genesis,
				(StTxtPara)m_genesis.TitleOA.ParagraphsOS[0], 3);
			int iSection = 0;
			int iPara = 0;
			int ich = 1;
			int tag = (int)ScrBook.ScrBookTags.kflidTitle;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNull(footnote, "Should not find a footnote");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(1, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag, "Wrong tag");
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			StTxtPara para = (StTxtPara)((ScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is text", null);
			para = (StTxtPara)((ScrSection)book.SectionsOS[1]).ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is more text", null);
			int iSection = 1;
			int iPara = 0;
			int ich = 5;
			int tag = (int)ScrSection.ScrSectionTags.kflidContent;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, book, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNull(footnote, "Should not find a footnote");
			Assert.AreEqual(1, iSection, "Should have the same section");
			Assert.AreEqual(0, iPara, "Should have the same paragraph");
			Assert.AreEqual(5, ich, "Should have the same IP position");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			m_scrInMemoryCache.AddSectionHeadParaToSection(m_section.Hvo, "More heading",
				ScrStyleNames.IntroSectionHead);
			int iSection = 1;
			int iPara = 1;
			int ich = 5;
			int tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			ScrFootnote footnote = ScrFootnote.FindPreviousFootnote(Cache, m_genesis, ref iSection,
				ref iPara, ref ich, ref tag);

			Assert.IsNotNull(footnote, "Should find a footnote");
			Assert.AreEqual(m_genesis.FootnotesOS.HvoArray[0], footnote.Hvo, "Should find a footnote somewhere");
			Assert.AreEqual(0, iSection, "Should have the correct section set");
			Assert.AreEqual(0, iPara, "Should have the correct paragraph set");
			Assert.AreEqual(10, ich, "Should have the correct IP position set");
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag, "Wrong tag");
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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);
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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);
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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[0]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
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
		public void UpdateRefForFootnote()
		{
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[2]);
			StTxtPara footnotePara = new StTxtPara();
			footnote.ParagraphsOS.Append(footnotePara);
			footnotePara.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
			m_scr.DisplayFootnoteReference = true;

			// Add a new verse number before the footnote marker.
			IScrSection section = m_genesis.SectionsOS[1];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[1];
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			bldr.Replace(5, 5, "21", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			para.Contents.UnderlyingTsString = bldr.GetString();

			// Give property change event related to verse insert.
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll,
				para.Hvo, StTxtPara.ktagVerseNumbers, 0, 1, 1); // char positions and counts are ignored

			// Verify that reference for footnote is now correct
			Assert.AreEqual("1:21 ", footnote.RefAsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test footnote reference when footnote is in a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteRefVerseBridge()
		{
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[2]);
			StTxtPara footnotePara = new StTxtPara();
			footnote.ParagraphsOS.Append(footnotePara);
			footnotePara.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
			m_scr.DisplayFootnoteReference = true;

			// Add a new verse bridge
			IScrSection section = m_genesis.SectionsOS[1];
			StTxtPara para = (StTxtPara) section.ContentOA.ParagraphsOS[1];
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			bldr.Replace(1, 1, "-9", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			para.Contents.UnderlyingTsString = bldr.GetString();

			// Give property change event related to verse insert.
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll,
				para.Hvo, StTxtPara.ktagVerseNumbers, 0, 1, 1); // char positions and counts are ignored

			// Verify that reference for footnote is now correct
			Assert.AreEqual("1:5-9 ", footnote.RefAsString);
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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[0]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);

			footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);

			footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[2]);
			para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);

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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[0]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);

			footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);

			footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[2]);
			para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);

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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);
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
			CheckDisposed();

			ScrFootnote footnote = new ScrFootnote(Cache, m_genesis.FootnotesOS.HvoArray[1]);
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);
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
			CheckDisposed();

			ScrFootnote footnote = ScrFootnote.FindCurrentFootnote(Cache, m_genesis,
				1, 0, 5, (int)ScrSection.ScrSectionTags.kflidContent);

			Assert.AreEqual(m_genesis.FootnotesOS.HvoArray[1], footnote.Hvo);
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
			CheckDisposed();

			ScrFootnote footnote = ScrFootnote.FindCurrentFootnote(Cache, m_genesis,
				1, 0, 6, (int)ScrSection.ScrSectionTags.kflidContent);

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
			CheckDisposed();

			ScrFootnote footnote = ScrFootnote.FindCurrentFootnote(Cache, m_genesis,
				1, 0, 3, (int)ScrSection.ScrSectionTags.kflidContent);

			Assert.IsNull(footnote);
		}
		#endregion

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
	}
}

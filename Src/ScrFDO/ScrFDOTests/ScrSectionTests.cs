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
// File: ScrSectionTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using System.Collections.Generic;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Summary description for TeSectionOpsTests.
	/// </summary>
	[TestFixture]
	public class ScrSectionTests: ScrInMemoryFdoTestBase
	{
		#region member data
		IScrBook m_matthew;
		IScrBook m_philemon;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_matthew = m_scrInMemoryCache.AddBookToMockedScripture(40, "Matthews");
			m_philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddTitleToMockedBook(m_philemon.Hvo, "Philemon");
			m_inMemoryCache.InitializeWritingSystemEncodings();

			// Initialize Philemon with intro and scripture sections

			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(m_philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Intro1", ScrStyleNames.IntroSectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some intro text", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddIntroSectionToMockedBook(m_philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Intro2", ScrStyleNames.IntroSectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some more intro text", null);
			section.AdjustReferences();

			// intro ends

			// normal scripture
			section = m_scrInMemoryCache.AddSectionToMockedBook(m_philemon.Hvo);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Scripture1", ScrStyleNames.SectionHead);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3-5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Scripture2", ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			section.AdjustReferences();
		}

		#endregion

		#region Test version of CreateScrSection that takes a string and run props
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a section at end of an intro
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScrSection_AtEndOfIntro()
		{
			CheckDisposed();

			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);

			ScrSection.CreateScrSection(m_philemon, 2, "New content of the new section", textProps, true);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			VerifyInsertedBookSection((ScrSection)m_philemon.SectionsOS[2], true,
				"New content of the new section",
				null, Cache.DefaultVernWs, 57001000); // intro section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a section at middle of an intro.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScrSection_AtMiddleOfIntro()
		{
			CheckDisposed();

			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);

			ScrSection.CreateScrSection(m_philemon, 1, "New of the new section", textProps, true);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			ScrSection section = (ScrSection)m_philemon.SectionsOS[1];
			VerifyInsertedBookSection(section, true, "New of the new section",
				null, Cache.DefaultVernWs, 57001000); // intro section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a section at end of book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScrSection_AtEndOfBook()
		{
			CheckDisposed();

			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);

			ScrSection.CreateScrSection(m_philemon, 4, "New content of section", textProps, false);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			ScrSection section = (ScrSection)m_philemon.SectionsOS[4];
			VerifyInsertedBookSection(section, false, "New content of section",
				null, Cache.DefaultVernWs, 57001007); // scripture section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a section at start of book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScrSection_AtStartOfBook()
		{
			CheckDisposed();

			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);

			ScrSection.CreateScrSection(m_philemon, 0, "New section", textProps, true);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			ScrSection section = (ScrSection)m_philemon.SectionsOS[0];
			VerifyInsertedBookSection(section, true, "New section", null,
				Cache.DefaultVernWs, 57001000); // intro section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creation of a section at start of first scripture section (following an
		/// intro section). Jira # for this is TE-2780.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScrSection_AtStartOfFirstScriptureSection()
		{
			CheckDisposed();

			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);

			ScrSection.CreateScrSection(m_philemon, 2, "New section", textProps, false);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			ScrSection section = (ScrSection)m_philemon.SectionsOS[2];
			VerifyInsertedBookSection(section, false, "New section", null,
				Cache.DefaultVernWs, 57001001); // new first Scripture section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetSectionFromParagraph method to make sure it gets the correct section
		/// and returns null when a book title para is given
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetSectionFromParagraph()
		{
			CheckDisposed();

			// Get the paragraph of the title and make sure the section is null
			StTxtPara paraTitle = (StTxtPara)m_philemon.TitleOA.ParagraphsOS[0];
			Assert.IsNull(ScrSection.GetSectionFromParagraph(paraTitle),
				"Expected no section for a title para");

			// Get the paragraph for an intro content para
			StTxtPara paraIntroContent = (StTxtPara)((ScrSection)m_philemon.SectionsOS[0]).ContentOA.ParagraphsOS[0];
			Assert.AreEqual(m_philemon.SectionsOS[0], ScrSection.GetSectionFromParagraph(paraIntroContent));

			// Get the paragraph for an intro section heading para
			StTxtPara paraIntroHeading = (StTxtPara)((ScrSection)m_philemon.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(m_philemon.SectionsOS[0], ScrSection.GetSectionFromParagraph(paraIntroHeading));

			// Get the paragraph for a scripture content para
			StTxtPara paraScrContent = (StTxtPara)((ScrSection)m_philemon.SectionsOS[2]).ContentOA.ParagraphsOS[0];
			Assert.AreEqual(m_philemon.SectionsOS[2], ScrSection.GetSectionFromParagraph(paraScrContent));

			// Get the paragraph for a scripture section heading para
			StTxtPara paraScrHeading = (StTxtPara)((ScrSection)m_philemon.SectionsOS[2]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(m_philemon.SectionsOS[2], ScrSection.GetSectionFromParagraph(paraScrHeading));
		}
		#endregion

		#region ChangeParagraphToSectionHead tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ChangeParagraphToSectionHead function when the paragraph is the last
		/// paragraph in a section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChangeParagraphToSectionHead_EndOfSection()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "This is the heading",
				ScrStyleNames.SectionHead);
			m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Content of paragraph 2", null);
			section.AdjustReferences();

			ScrSection secHelp = new ScrSection(Cache, section.Hvo);
			IScrSection newSecHelp = secHelp.ChangeParagraphToSectionHead(2, 1);

			// verify that the new section has one content paragraph and one heading paragraph.
			Assert.AreEqual(1, newSecHelp.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(1, newSecHelp.HeadingOA.ParagraphsOS.Count);

			// verify that the text of the old paragraph is now in the new section head
			// and that the new section content paragraph is empty and has normal paragraph
			// style.
			StTxtPara newSecPara = (StTxtPara)newSecHelp.ContentOA.ParagraphsOS[0];
			Assert.IsTrue(StStyle.IsStyle(newSecPara.StyleRules, ScrStyleNames.NormalParagraph));
			Assert.IsFalse(StStyle.IsStyle(newSecPara.Contents.UnderlyingTsString.get_Properties(0),
				ScrStyleNames.NormalParagraph));

			Assert.IsNull(newSecPara.Contents.Text);
			StTxtPara newSecHeadPara = (StTxtPara)newSecHelp.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(para.Contents.Text, newSecHeadPara.Contents.Text);
		}
		#endregion

		#region AdjustReferences tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AdjustReferences method when there is a normal range of verse numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_NormalRange()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section.AdjustReferences();

			// Check the section references and squawk if wrong
			VerifySectionRefs(section, 40001001, 40001003, 40001001, 40001003);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AdjustReferences method when the last verse is less than the first
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_LastVerseBeforeFirst()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "some text", null);

			section1.AdjustReferences();

			// need 2 sections because we need a section without a chapter number

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "some text", null);

			section2.AdjustReferences();

			// Check the section references and squawk if wrong
			VerifySectionRefs(section2, 40001004, 40001003, 40001003, 40001005);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AdjustReferences method when the last verse number is less than the
		/// first verse
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_LastVerseLess()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section.AdjustReferences();

			// Check the section references and squawk if wrong
			VerifySectionRefs(section, 40001001, 40001002, 40001001, 40001004);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AdjustReferences method when the chapter numbers are out of order
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_ChapterOutOfOrder()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section.AdjustReferences();

			// Check the section references and squawk if wrong
			VerifySectionRefs(section, 40002001, 40001003, 40001001, 40002003);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_InvalidVerseNumberAtStart()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "155", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section.AdjustReferences();

			// Check the section references and squawk if wrong
			VerifySectionRefs(section, 40001001, 40001003, 40001001, 40001003);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_InvalidChapterNumberInMiddle()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "155", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			// chapter number 40 is invalid in our book of Matthew
			m_scrInMemoryCache.AddRunToMockedPara(para, "40", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section.AdjustReferences();

			// Check the section references and squawk if wrong
			VerifySectionRefs(section, 40001001, 40001003, 40001001, 40001003);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_InvalidVerseNumberAtEnd()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "155", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section.AdjustReferences();

			// Check the section references and squawk if wrong
			VerifySectionRefs(section, 40001001, 40001003, 40001001, 40001003);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_InvalidVerseNumberInMiddle()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "155", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section.AdjustReferences();

			// Check the section references and squawk if wrong
			VerifySectionRefs(section, 40001001, 40001003, 40001001, 40001003);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AdjustReferences method when a chapter number is followed by text, implying
		/// verse one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_ChapterWithImpliedVerseOne()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.ChapterNumber);
			// add text of implied verse one
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			section1.AdjustReferences();

			// We expect that the refs begin with the implied verse 1.
			VerifySectionRefs(section1, 40004001, 40004005, 40004001, 40004005);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AdjustReferences method when a chapter number is immediately followed by
		/// a verse number OTHER THAN ONE.  TE-4686
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_ChapterWithVerseOtherThanOne()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			section1.AdjustReferences();

			// We expect the the refs begin with verse 3, not an implied verse 1.
			VerifySectionRefs(section1, 40004003, 40004005, 40004003, 40004005);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AdjustReferences method simulating what happens during import
		/// first verse
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustReferences_ImportSimulation()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section1.AdjustReferences();

			// make a second section without a chapter number
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_matthew.Hvo);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section2.AdjustReferences();

			// Check the section references and squawk if wrong
			VerifySectionRefs(section1, 40004003, 40004005, 40004003, 40004005);

			// Check the section references and squawk if wrong
			VerifySectionRefs(section2, 40004006, 40004008, 40004006, 40004008);
		}
		#endregion

		#region GetFootnotes tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ScrSection.GetFootnotes"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFootnotes_InHeadingAndContents()
		{
			CheckDisposed();

			ScrSection section1 = (ScrSection)m_philemon.SectionsOS[0];
			StTxtPara para = (StTxtPara)section1.HeadingOA.ParagraphsOS[0];
			StFootnote footnote = InsertTestFootnote(m_philemon, para, 0, 0);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "heading footnote");

			para = (StTxtPara)section1.ContentOA.ParagraphsOS[0];
			footnote = InsertTestFootnote(m_philemon, para, 1, 0);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "Anything");
			footnote = InsertTestFootnote(m_philemon, para, 2, 5);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "Bla");
			footnote = InsertTestFootnote(m_philemon, para, 3, 10);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "hing");

			ScrSection section2 = (ScrSection)m_philemon.SectionsOS[1];
			para = (StTxtPara)section2.ContentOA.ParagraphsOS[0];
			footnote = InsertTestFootnote(m_philemon, para, 4, 0);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "This one shouldn't be included");

			List<FootnoteInfo> footnotes = section1.GetFootnotes();
			Assert.AreEqual(4, footnotes.Count);
			Assert.AreEqual(m_philemon.FootnotesOS[0].Hvo, ((FootnoteInfo)footnotes[0]).footnote.Hvo);
			Assert.AreEqual("heading footnote", ((FootnoteInfo)footnotes[0]).paraStylename);
			Assert.AreEqual(m_philemon.FootnotesOS[1].Hvo, ((FootnoteInfo)footnotes[1]).footnote.Hvo);
			Assert.AreEqual("Anything", ((FootnoteInfo)footnotes[1]).paraStylename);
			Assert.AreEqual(m_philemon.FootnotesOS[2].Hvo, ((FootnoteInfo)footnotes[2]).footnote.Hvo);
			Assert.AreEqual("Bla", ((FootnoteInfo)footnotes[2]).paraStylename);
			Assert.AreEqual(m_philemon.FootnotesOS[3].Hvo, ((FootnoteInfo)footnotes[3]).footnote.Hvo);
			Assert.AreEqual("hing", ((FootnoteInfo)footnotes[3]).paraStylename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="ScrSection.GetFootnotes"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFootnotes_NoFootnotes()
		{
			CheckDisposed();
			ScrSection section1 = (ScrSection)m_philemon.SectionsOS[0];
			StTxtPara para = (StTxtPara)section1.ContentOA.ParagraphsOS[0];
			StFootnote footnote = InsertTestFootnote(m_philemon, para, 1, 0);
			m_inMemoryCache.AddParaToMockedText(footnote.Hvo, "Anything");

			ScrSection section2 = (ScrSection)m_philemon.SectionsOS[1];
			para = (StTxtPara)section2.ContentOA.ParagraphsOS[0];

			List<FootnoteInfo> footnotes = section2.GetFootnotes();
			Assert.AreEqual(0, footnotes.Count);
		}
		#endregion

		#region Verify methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify misc. stuff about the specified section, just inserted into a book. We expect
		/// a section with 1) section head with one empty para, 2) contents with one para with
		/// one run, having the specified details.
		/// </summary>
		/// <param name="section">Section to verify</param>
		/// <param name="isIntro">true if an intro paragraph, false if a scripture para</param>
		/// <param name="contentRunText"></param>
		/// <param name="contentRunStyle"></param>
		/// <param name="contentRunWS"></param>
		/// <param name="verseRef"></param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyInsertedBookSection(IScrSection section, bool isIntro,
			string contentRunText, string contentRunStyle, int contentRunWS, int verseRef)
		{
			// Verify the new section heading paragraph count and the first para's style.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count,
				"Incorrect number of paras in section heading.");
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(
				isIntro ? ScrStyleNames.IntroSectionHead : ScrStyleNames.SectionHead),
				para.StyleRules);

			// Verify there's one content paragraph in new section, and its para style.
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(
				isIntro ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph),
				para.StyleRules);

			// Verify the paragraph has one run, with specified text, style, & ws.
			Assert.AreEqual(1, para.Contents.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(para.Contents.UnderlyingTsString, 0, contentRunText,
				contentRunStyle, contentRunWS);

			// Check the section verse refs
			VerifySectionRefs(section, verseRef, verseRef, verseRef, verseRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify misc. stuff about the specified section, just inserted into a book. We expect
		/// a section with 1) section head with one empty para, 2) contents mathing the given
		/// TsString, having the specified details.
		/// </summary>
		/// <param name="section">Section to verify</param>
		/// <param name="headingStyleRules"></param>
		/// <param name="tssContent"></param>
		/// <param name="contentStyleRules"></param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyInsertedBookSection(IScrSection section,
			ITsTextProps headingStyleRules, ITsString tssContent, ITsTextProps contentStyleRules)
		{
			// Verify the new section heading paragraph count and the first para's style.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count,
				"Incorrect number of paras in section heading.");
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(headingStyleRules, para.StyleRules);

			// Verify there's one content paragraph in new section, and its para style.
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(contentStyleRules, para.StyleRules);

			// Verify the paragraph contents
			AssertEx.AreTsStringsEqual(tssContent, para.Contents.UnderlyingTsString);
		}

		private static void VerifySectionRefs(IScrSection section, int refStart, int refEnd, int refMin, int refMax)
		{
			Assert.AreEqual(refStart, section.VerseRefStart);
			Assert.AreEqual(refEnd, section.VerseRefEnd);
			Assert.AreEqual(refMin, section.VerseRefMin);
			Assert.AreEqual(refMax, section.VerseRefMax);
		}
		#endregion
	}
}

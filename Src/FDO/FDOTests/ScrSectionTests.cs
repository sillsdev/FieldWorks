// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2004' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrSectionTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using System;
using System.Collections.Generic;

using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.CoreImpl;

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
		IScrSectionFactory m_ScrSectionFactory;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_ScrSectionFactory = Cache.ServiceLocator.GetInstance<IScrSectionFactory>();
			m_matthew =AddBookToMockedScripture(40, "Matthews");
			m_philemon = AddBookToMockedScripture(57, "Philemon");
			AddTitleToMockedBook(m_philemon, "Philemon");

			// Initialize Philemon with intro and scripture sections

			IScrSection section = AddSectionToMockedBook(m_philemon, true);
			AddSectionHeadParaToSection(section, "Intro1", ScrStyleNames.IntroSectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "some intro text", null);

			section = AddSectionToMockedBook(m_philemon, true);
			AddSectionHeadParaToSection(section, "Intro2", ScrStyleNames.IntroSectionHead);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "some more intro text", null);

			// intro ends

			// normal scripture
			section = AddSectionToMockedBook(m_philemon);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddSectionHeadParaToSection(section, "Scripture1", ScrStyleNames.SectionHead);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3-5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);

			section = AddSectionToMockedBook(m_philemon);
			AddSectionHeadParaToSection(section, "Scripture2", ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
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
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);
			m_ScrSectionFactory.CreateScrSection(
				m_philemon, 2, "New content of the new section", textProps, true);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			VerifyInsertedBookSection(m_philemon.SectionsOS[2], true,
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
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);

			m_ScrSectionFactory.CreateScrSection(
				m_philemon, 1, "New of the new section", textProps, true);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			VerifyInsertedBookSection(m_philemon.SectionsOS[1], true, "New of the new section",
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
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);

			m_ScrSectionFactory.CreateScrSection(
				m_philemon, 4, "New content of section", textProps, false);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			VerifyInsertedBookSection(m_philemon.SectionsOS[4], false, "New content of section",
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
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);

			m_ScrSectionFactory.CreateScrSection(
				m_philemon, 0, "New section", textProps, true);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			VerifyInsertedBookSection(m_philemon.SectionsOS[0], true, "New section", null,
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
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);

			m_ScrSectionFactory.CreateScrSection(
				m_philemon, 2, "New section", textProps, false);
			Assert.AreEqual(5, m_philemon.SectionsOS.Count);
			VerifyInsertedBookSection(m_philemon.SectionsOS[2], false, "New section", null,
				Cache.DefaultVernWs, 57001001); // new first Scripture section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the getting the owning IScrSection for a paragraph to make sure it gets the
		/// correct section and returns null when a book title para is given
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetSectionFromParagraph()
		{
			// Get the paragraph of the title and make sure the section is null
			IStPara paraTitle = m_philemon.TitleOA.ParagraphsOS[0];
			Assert.IsNull(paraTitle.OwnerOfClass<IScrSection>(),
				"Expected no section for a title para");

			// Get the paragraph for an intro content para
			IStPara paraIntroContent = m_philemon.SectionsOS[0].ContentOA.ParagraphsOS[0];
			Assert.AreEqual(m_philemon.SectionsOS[0], paraIntroContent.OwnerOfClass<IScrSection>());

			// Get the paragraph for an intro section heading para
			IStPara paraIntroHeading = m_philemon.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(m_philemon.SectionsOS[0], paraIntroHeading.OwnerOfClass<IScrSection>());

			// Get the paragraph for a scripture content para
			IStPara paraScrContent = m_philemon.SectionsOS[2].ContentOA.ParagraphsOS[0];
			Assert.AreEqual(m_philemon.SectionsOS[2], paraScrContent.OwnerOfClass<IScrSection>());

			// Get the paragraph for a scripture section heading para
			IStPara paraScrHeading = m_philemon.SectionsOS[2].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(m_philemon.SectionsOS[2], paraScrHeading.OwnerOfClass<IScrSection>());
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
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);


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
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_matthew);
			IStTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "some text", null);
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "some text", null);
			AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "some text", null);


			// need 2 sections because we need a section without a chapter number

			IScrSection section2 = AddSectionToMockedBook(m_matthew);
			IStTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "some text", null);
			AddRunToMockedPara(para2, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "some text", null);
			AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "some text", null);


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
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);


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
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);


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
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "155", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);


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
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "155", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			// chapter number 40 is invalid in our book of Matthew
			AddRunToMockedPara(para, "40", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);


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
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "155", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);


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
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "155", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);


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
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "4", ScrStyleNames.ChapterNumber);
			// add text of implied verse one
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);

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
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "4", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);

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
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_matthew);
			IStTxtPara para = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "4", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);


			// make a second section without a chapter number
			IScrSection section2 = AddSectionToMockedBook(m_matthew);
			para = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "8", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);


			// Check the section references and squawk if wrong
			VerifySectionRefs(section1, 40004003, 40004005, 40004003, 40004005);

			// Check the section references and squawk if wrong
			VerifySectionRefs(section2, 40004006, 40004008, 40004006, 40004008);
		}
		#endregion

		#region GetFootnotes tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="IScrSection.GetFootnotes"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFootnotes_InHeadingAndContents()
		{
			IScrSection section1 = m_philemon.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section1.HeadingOA.ParagraphsOS[0];
			IStFootnote footnote = InsertTestFootnote(m_philemon, para, 0, 0);
			footnote.AddNewTextPara("heading footnote");

			para = (IStTxtPara)section1.ContentOA.ParagraphsOS[0];
			footnote = InsertTestFootnote(m_philemon, para, 1, 0);
			AddParaToMockedText(footnote, "Anything");
			footnote = InsertTestFootnote(m_philemon, para, 2, 5);
			AddParaToMockedText(footnote, "Bla");
			footnote = InsertTestFootnote(m_philemon, para, 3, 10);
			AddParaToMockedText(footnote, "hing");

			IScrSection section2 = m_philemon.SectionsOS[1];
			para = (IStTxtPara)section2.ContentOA.ParagraphsOS[0];
			footnote = InsertTestFootnote(m_philemon, para, 4, 0);
			AddParaToMockedText(footnote, "This one shouldn't be included");

			List<IScrFootnote> footnotes = section1.GetFootnotes();
			Assert.AreEqual(4, footnotes.Count);
			Assert.AreEqual(m_philemon.FootnotesOS[0], footnotes[0]);
			Assert.AreEqual(m_philemon.FootnotesOS[1], footnotes[1]);
			Assert.AreEqual(m_philemon.FootnotesOS[2], footnotes[2]);
			Assert.AreEqual(m_philemon.FootnotesOS[3], footnotes[3]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="IScrSection.GetFootnotes"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFootnotes_NoFootnotes()
		{
			IScrSection section1 = m_philemon.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section1.ContentOA.ParagraphsOS[0];
			IStFootnote footnote = InsertTestFootnote(m_philemon, para, 0, 0);
			AddParaToMockedText(footnote, "Anything");

			IScrSection section2 = m_philemon.SectionsOS[1];
			para = (IStTxtPara)section2.ContentOA.ParagraphsOS[0];

			List<IScrFootnote> footnotes = section2.GetFootnotes();
			Assert.AreEqual(0, footnotes.Count);
		}
		#endregion

		#region Add/Insert section
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests Add a section when the source section is in another book. We should
		/// throw an exception when an attempt is made to add this section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException),
			ExpectedMessage = "Scripture sections cannot be moved from another book.")]
		public void AddSection_InAnotherBook()
		{
			IScrSection philemonSection = m_philemon.SectionsOS[0];
			int philemonSectionCount = m_philemon.SectionsOS.Count;

			// Attempt to insert this section from Philemon into Matthew.
			m_matthew.SectionsOS.Add(philemonSection);

			// We should get an exception because we don't want to remove the original section
			// from Philemon. If we don't crash with this insert, we want to confirm that we
			// haven't deleted any sections from Philemon.
			Assert.AreEqual(philemonSectionCount, m_philemon.SectionsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests Insert a section when the source section is in another book. We should
		/// throw an exception when an attempt is made to insert this section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException),
			ExpectedMessage = "Scripture sections cannot be moved from another book.")]
		public void InsertSection_InAnotherBook()
		{
			IScrSection philemonSection = m_philemon.SectionsOS[0];
			int philemonSectionCount = m_philemon.SectionsOS.Count;

			// Attempt to insert this section from Philemon into Matthew.
			m_matthew.SectionsOS.Insert(0, philemonSection);

			// We should get an exception because we don't want to remove the original section
			// from Philemon. If we don't crash with this insert, we want to confirm that we
			// haven't deleted any sections from Philemon.
			Assert.AreEqual(philemonSectionCount, m_philemon.SectionsOS.Count);
		}
		#endregion

		#region MoveAllParas: Move content tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveAllParas when moving the content paragraphs to the end of the heading of
		/// another section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveContent_ToHeading_AtEnd()
		{
			IScrSection introSection1 = m_philemon.SectionsOS[0];
			IScrSection introSection2 = m_philemon.SectionsOS[1];
			// Add one more para to the content to make sure multiple paras are moved.
			IScrTxtPara newPara = AddParaToMockedSectionContent(introSection2, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(newPara, "A second para for the second intro section",
				Cache.DefaultVernWs);

			// Confirm the text in the first heading
			Assert.AreEqual(1, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro1", ((IScrTxtPara) introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);

			IStStyle headingStyle = m_scr.FindStyle(ScrStyleNames.IntroSectionHead);

			// Add the contents of the second section after the first paragraph.
			ReflectionHelper.CallMethod(typeof(ScrSection), "MoveAllParas", introSection2,
				ScrSectionTags.kflidContent, introSection1, ScrSectionTags.kflidHeading, true, headingStyle);

			// We expect the content of the second intro section to be added after the heading of
			// the original paragraph.
			Assert.AreEqual(3, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro1",
				((IScrTxtPara) introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);
			IStTxtPara para = (IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[1];
			Assert.AreEqual("some more intro text", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
			Assert.AreEqual("A second para for the second intro section",
				((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[2]).Contents.Text);
			// We also expect the source section contents to be empty.
			Assert.AreEqual(0, introSection2.ContentOA.ParagraphsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveAllParas when moving the content paragraphs to the start of the heading of
		/// another section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveContent_ToHeading_AtStart()
		{
			IScrSection introSection1 = m_philemon.SectionsOS[0];
			IScrSection introSection2 = m_philemon.SectionsOS[1];
			// Add one more para to the content to make sure multiple paras are moved.
			IScrTxtPara newPara = AddParaToMockedSectionContent(introSection2, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(newPara, "A second para for the second intro section",
				Cache.DefaultVernWs);

			// Confirm the text in the first heading
			Assert.AreEqual(1, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro1", ((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);

			IStStyle headingStyle = m_scr.FindStyle(ScrStyleNames.IntroSectionHead);

			// Add the contents of the second section before the first heading paragraph.
			ReflectionHelper.CallMethod(typeof(ScrSection), "MoveAllParas", introSection2,
				ScrSectionTags.kflidContent, introSection1, ScrSectionTags.kflidHeading, false, headingStyle);

			// We expect the content of the second intro section to be added before the heading of
			// the original paragraph.
			Assert.AreEqual(3, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("some more intro text",
				((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);
			IStTxtPara para = (IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[1];
			Assert.AreEqual("A second para for the second intro section", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
			Assert.AreEqual("Intro1",
				((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[2]).Contents.Text);
			// We also expect the source section contents to be empty.
			Assert.AreEqual(0, introSection2.ContentOA.ParagraphsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveAllParas when moving the content paragraphs before the content of another
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveContent_ToContent_AtStart()
		{
			IScrSection introSection1 = m_philemon.SectionsOS[0];
			IScrSection introSection2 = m_philemon.SectionsOS[1];

			// Confirm the text in the first content section.
			Assert.AreEqual(1, introSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text", ((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[0]).Contents.Text);

			// Add the contents of the second section before the first content paragraph.
			ReflectionHelper.CallMethod(typeof(ScrSection), "MoveAllParas", introSection2,
				ScrSectionTags.kflidContent, introSection1, ScrSectionTags.kflidContent, false, null);

			// We expect the content of the second intro section to be added before the content of
			// the original paragraph.
			Assert.AreEqual(2, introSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some more intro text",
				((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("some intro text",
				((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[1]).Contents.Text);
			// We also expect the source section contents to be empty.
			Assert.AreEqual(0, introSection2.ContentOA.ParagraphsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveAllParas when moving the content paragraphs after the content of another
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveContent_ToContent_AtEnd()
		{
			IScrSection introSection1 = m_philemon.SectionsOS[0];
			IScrSection introSection2 = m_philemon.SectionsOS[1];

			// Confirm the text in the first content section.
			Assert.AreEqual(1, introSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text", ((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[0]).Contents.Text);

			// Add the contents of the second section after the first content paragraph.
			ReflectionHelper.CallMethod(typeof(ScrSection), "MoveAllParas", introSection2,
				ScrSectionTags.kflidContent, introSection1, ScrSectionTags.kflidContent, true, null);

			// We expect the content of the second intro section to be added before the content of
			// the original paragraph.
			Assert.AreEqual(2, introSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text",
				((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("some more intro text",
				((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[1]).Contents.Text);
			// We also expect the source section contents to be empty.
			Assert.AreEqual(0, introSection2.ContentOA.ParagraphsOS.Count);
		}
		#endregion

		#region MoveAllParas: Move heading tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveAllParas when moving the heading paragraphs to the end of the heading of
		/// another section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveHeading_ToHeading_AtEnd()
		{
			IScrSection introSection1 = m_philemon.SectionsOS[0];
			IScrSection introSection2 = m_philemon.SectionsOS[1];

			// Confirm the text in the first heading
			Assert.AreEqual(1, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro1", ((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);

			// Add the contents of the second section after the first paragraph.
			ReflectionHelper.CallMethod(typeof(ScrSection), "MoveAllParas", introSection2,
				ScrSectionTags.kflidHeading, introSection1, ScrSectionTags.kflidHeading, true, null);

			// We expect the heading of the second intro section to be added after the heading of
			// the original paragraph.
			Assert.AreEqual(2, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro1",
				((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Intro2",
				((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[1]).Contents.Text);
			// We also expect the source section head to be empty.
			Assert.AreEqual(0, introSection2.HeadingOA.ParagraphsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveAllParas when moving the heading paragraphs to the start of the heading of
		/// another section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveHeading_ToHeading_AtStart()
		{
			IScrSection introSection1 = m_philemon.SectionsOS[0];
			IScrSection introSection2 = m_philemon.SectionsOS[1];

			// Confirm the text in the first heading
			Assert.AreEqual(1, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro1", ((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);

			// Add the contents of the second section before the first heading paragraph.
			ReflectionHelper.CallMethod(typeof(ScrSection), "MoveAllParas", introSection2,
				ScrSectionTags.kflidHeading, introSection1, ScrSectionTags.kflidHeading, false, null);

			// We expect the content of the second intro section to be added before the heading of
			// the original paragraph.
			Assert.AreEqual(2, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro2",
				((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Intro1",
				((IScrTxtPara)introSection1.HeadingOA.ParagraphsOS[1]).Contents.Text);
			// We also expect the source section head to be empty.
			Assert.AreEqual(0, introSection2.HeadingOA.ParagraphsOS.Count);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveAllParas when moving the heading paragraphs before the content of another
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveHeading_ToContent_AtStart()
		{
			IScrSection introSection1 = m_philemon.SectionsOS[0];
			IScrSection introSection2 = m_philemon.SectionsOS[1];
			// Add one more para to the heading to make sure multiple paras are moved.
			IScrTxtPara newPara = AddSectionHeadParaToSection(introSection2, "Heading para 2", ScrStyleNames.IntroSectionHead);

			// Confirm the text in the first content section.
			Assert.AreEqual(1, introSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text", ((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[0]).Contents.Text);

			IStStyle introParaStyle = m_scr.FindStyle(ScrStyleNames.IntroListItem2);

			// Add the heading paras of the second section before the first content paragraph.
			ReflectionHelper.CallMethod(typeof(ScrSection), "MoveAllParas", introSection2,
				ScrSectionTags.kflidHeading, introSection1, ScrSectionTags.kflidContent, false, introParaStyle);

			// We expect the content of the second intro section to be added before the content of
			// the original paragraph.
			Assert.AreEqual(3, introSection1.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IScrTxtPara)introSection1.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("Intro2", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroListItem2,
				para.StyleRules.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
			Assert.AreEqual("Heading para 2",
				((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual("some intro text",
				((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[2]).Contents.Text);
			// We also expect the source section head to be empty.
			Assert.AreEqual(0, introSection2.HeadingOA.ParagraphsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveAllParas when moving the heading paragraphs after the content of another
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveHeading_ToContent_AtEnd()
		{
			IScrSection introSection1 = m_philemon.SectionsOS[0];
			IScrSection introSection2 = m_philemon.SectionsOS[1];
			// Add one more para to the heading to make sure multiple paras are moved.
			IScrTxtPara newPara = AddSectionHeadParaToSection(introSection2, "Heading para 2", ScrStyleNames.IntroSectionHead);

			Assert.AreEqual(2, introSection2.HeadingOA.ParagraphsOS.Count);
			// Confirm the text in the first content section.
			Assert.AreEqual(1, introSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text", ((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[0]).Contents.Text);

			IStStyle introParaStyle = m_scr.FindStyle(ScrStyleNames.IntroListItem3);

			// Add the contents of the second section after the first content paragraph.
			ReflectionHelper.CallMethod(typeof(ScrSection), "MoveAllParas", introSection2,
				ScrSectionTags.kflidHeading, introSection1, ScrSectionTags.kflidContent, true, introParaStyle);

			// We expect the content of the second intro section to be added before the content of
			// the original paragraph.
			Assert.AreEqual(3, introSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("some intro text",
				((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Intro2",
				((IScrTxtPara)introSection1.ContentOA.ParagraphsOS[1]).Contents.Text);
			IStTxtPara para = (IScrTxtPara)introSection1.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("Heading para 2", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroListItem3,
				para.StyleRules.GetStrPropValue((int)FwTextStringProp.kstpNamedStyle));
			// We also expect the source section head to be empty.
			Assert.AreEqual(0, introSection2.HeadingOA.ParagraphsOS.Count);
		}
		#endregion

		#region MoveHeadingParasToContent
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveHeadingParasToContent when changing a heading paragraph to a content
		/// paragraph at the beginning of a section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveHeadingParasToContent()
		{
			IScrSection introSection1 = m_philemon[0];
			Assert.AreEqual(1, introSection1.ContentOA.ParagraphsOS.Count);

			// Move first content paragraph to a heading paragraph.
			IStStyle introListItem1Style = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.IntroListItem1);
			introSection1.MoveHeadingParasToContent(0, introListItem1Style);

			// We expect that the section head paragraph is now the first content paragraph.
			Assert.AreEqual(2, introSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro1", ((IStTxtPara)introSection1.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("some intro text", ((IStTxtPara)introSection1.ContentOA.ParagraphsOS[1]).Contents.Text);
			// We also expect that a new empty paragraph will be added to the heading.
			Assert.AreEqual(1, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.IsTrue(string.IsNullOrEmpty(((IStTxtPara)introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text));
		}
		#endregion

		#region MoveContentParasToHeading
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveContentParasToHeading when changing a content paragraph to a heading
		/// paragraph at the beginning of a section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveContentParasToHeading_SectionStart()
		{
			IScrSection introSection1 = m_philemon[0];
			Assert.AreEqual(1, introSection1.HeadingOA.ParagraphsOS.Count);

			// Move first content paragraph to a heading paragraph.
			IStStyle introHeadingStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.IntroSectionHead);
			introSection1.MoveContentParasToHeading(0, introHeadingStyle);

			// We expect that the first content paragraph is now a section head (the second section head
			// paragraph).
			Assert.AreEqual(2, introSection1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Intro1", ((IStTxtPara)introSection1.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("some intro text", ((IStTxtPara)introSection1.HeadingOA.ParagraphsOS[1]).Contents.Text);
			// We also expect that a new empty paragraph will be added to the content.
			Assert.AreEqual(1, introSection1.ContentOA.ParagraphsOS.Count);
			Assert.IsTrue(string.IsNullOrEmpty(((IStTxtPara)introSection1.ContentOA.ParagraphsOS[0]).Contents.Text));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveContentParasToHeading when changing a content paragraph to a heading
		/// paragraph that contains chapter/verse numbers. The paragraph should not be converted
		/// to a section heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidStructureException))]
		public void MoveContentParasToHeading_ParaHasChapterVerses()
		{
			IScrSection scrSection1 = m_philemon[2];
			Assert.AreEqual(1, scrSection1.HeadingOA.ParagraphsOS.Count);

			// Move first content paragraph to a heading paragraph.
			IStStyle headingStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.SectionHead);
			scrSection1.MoveContentParasToHeading(0, headingStyle);
		}
		#endregion

		#region SplitSectionContent_atIP tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is at the start of the section
		/// heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionHeading_atIP_HeadingStart()
		{
			IScrSection section = m_philemon.SectionsOS[2];

			// Add a split at the beginning of the first Scripture section heading.
			IScrSection newSection = section.SplitSectionHeading_atIP(0, 0);

			// verify that the original section has an empty heading paragraph and an empty content paragraph.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(null, ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(null, ((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has its original heading and content para
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture1", ((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("11some text2some text3-5some text",
				((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001001, 57001001, 57001001, 57001001);
			ScrSectionTests.VerifySectionRefs(newSection, 57001001, 57001005, 57001001, 57001005);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is at the end of the section
		/// heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionHeading_atIP_HeadingEnd()
		{
			IScrSection section = m_philemon.SectionsOS[2];
			IScrTxtPara headingPara = (IScrTxtPara)section.HeadingOA.ParagraphsOS[0];

			// Add a split at the end of the first Scripture section heading.
			IScrSection newSection = section.SplitSectionHeading_atIP(0, headingPara.Contents.Length);

			// verify that the original section has an empty heading paragraph and an empty content paragraph.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture1", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(null, ((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has its original heading and content para
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(null, ((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("11some text2some text3-5some text",
				((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001001, 57001001, 57001001, 57001001);
			ScrSectionTests.VerifySectionRefs(newSection, 57001001, 57001005, 57001001, 57001005);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is in the middle of a middle
		/// paragraph in the section heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionHeading_atIP_MidParaMidIch()
		{
			IScrSection section = m_philemon.SectionsOS[2];
			IScrTxtPara headingPara = (IScrTxtPara)section.HeadingOA.ParagraphsOS[0];
			AddSectionHeadParaToSection(section, "Heading Line 2", ScrStyleNames.SectionHead);
			AddSectionHeadParaToSection(section, "Heading Line 3", ScrStyleNames.SectionHead);

			// Add a split after the word Heading in the heading "Heading Line2".
			IScrSection newSection = section.SplitSectionHeading_atIP(1, 8);

			// verify that the original section has an two heading paragraph and an empty content paragraph.
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture1", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Heading ", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(null, ((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has its original heading and content para
			Assert.AreEqual(2, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Line 2", ((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Heading Line 3", ((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("11some text2some text3-5some text",
				((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001001, 57001001, 57001001, 57001001);
			ScrSectionTests.VerifySectionRefs(newSection, 57001001, 57001005, 57001001, 57001005);
		}
		#endregion

		#region SplitSectionContent_atIP tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is at the start of the section
		/// content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionContent_atIP_ContentStart()
		{
			IScrSection section = m_philemon.SectionsOS[2];

			// Add a split at the beginning of the first Scripture section.
			IScrSection newSection = section.SplitSectionContent_atIP(0, 0);

			// verify that the original section has its original heading paragraph and an empty content paragraph.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture1", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(null, ((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has an empty heading para and the content of the original section
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(null, ((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("11some text2some text3-5some text",
				((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001001, 57001001, 57001001, 57001001);
			ScrSectionTests.VerifySectionRefs(newSection, 57001001, 57001005, 57001001, 57001005);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is at the start of a paragraph
		/// in the middle of the section content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionContent_atIP_MidParaStart()
		{
			// Add two more paragraphs to the last section content.
			IScrSection section = m_philemon.SectionsOS[3];
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 8, "Verse eight. ");
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 9, "Verse nine. ");

			// Add a split at the start of the middle paragraph in the last Scripture section.
			IScrSection newSection = section.SplitSectionContent_atIP(1, 0);

			// verify that the original section is the same.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture2", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("6some text7some text",
				((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has empty heading and contents
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.IsNull(((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(2, newSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("8Verse eight. ", ((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("9Verse nine. ", ((IScrTxtPara)newSection.ContentOA.ParagraphsOS[1]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001006, 57001007, 57001006, 57001007);
			ScrSectionTests.VerifySectionRefs(newSection, 57001008, 57001009, 57001008, 57001009);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is at the end of a paragraph
		/// in the middle of the section content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionContent_atIP_MidParaEnd()
		{
			// Add two more paragraphs to the last section content.
			IScrSection section = m_philemon.SectionsOS[3];
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 8, "Verse eight. ");
			int lengthV8 = para.Contents.Length;
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 9, "Verse nine. ");

			// Add a split at the end of the middle paragraph in the last Scripture section.
			IScrSection newSection = section.SplitSectionContent_atIP(1, lengthV8);

			// verify that the original section has all but the last paragraph.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture2", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("6some text7some text",
				((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("8Verse eight. ", ((IScrTxtPara)section.ContentOA.ParagraphsOS[1]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has empty heading and the last paragraph of the original section.
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.IsNull(((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("9Verse nine. ", ((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001006, 57001008, 57001006, 57001008);
			ScrSectionTests.VerifySectionRefs(newSection, 57001009, 57001009, 57001009, 57001009);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is at the middle of a paragraph
		/// which is in the middle of the section content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionContent_atIP_MidParaMidIch()
		{
			// Add two more paragraphs to the last section content.
			IScrSection section = m_philemon.SectionsOS[3];
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 8, "Verse eight. ");
			int ichEndV8 = para.Contents.Length;
			AddVerse(para, 0, 9, "Verse nine. ");

			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 10, "Verse ten. ");

			// Add a split in the middle of the middle paragraph after verse 8.
			IScrSection newSection = section.SplitSectionContent_atIP(1, ichEndV8);

			// verify that the original section contains up to the end of verse 8.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture2", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("6some text7some text",
				((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("8Verse eight. ", ((IScrTxtPara)section.ContentOA.ParagraphsOS[1]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has empty heading and the contents from the middle of the second
			// paragraph to the last paragraph.
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.IsNull(((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(2, newSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("9Verse nine. ", ((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("10Verse ten. ", ((IScrTxtPara)newSection.ContentOA.ParagraphsOS[1]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001006, 57001008, 57001006, 57001008);
			ScrSectionTests.VerifySectionRefs(newSection, 57001009, 57001010, 57001009, 57001010);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is at the end of the section
		/// content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionContent_atIP_ContentEnd()
		{
			IScrSection section = m_philemon.SectionsOS[2];

			// Add a split at the end of the first Scripture section.
			IScrTxtPara para = (IScrTxtPara)section.ContentOA.ParagraphsOS[0];
			IScrSection newSection = section.SplitSectionContent_atIP(0, para.Contents.Length);

			// verify that the original section is the same.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture1", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("11some text2some text3-5some text",
				((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has empty heading and contents
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.IsNull(((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			Assert.IsNull(((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001001, 57001005, 57001001, 57001005);
			ScrSectionTests.VerifySectionRefs(newSection, 57001005, 57001005, 57001005, 57001005);
		}
		#endregion

		#region SplitSectionContent_atIP when inserting content tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is at the start of the section
		/// content and there is content to copy into the new section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionContent_atIP_NewContent_ContentStart()
		{
			IScrSection section = m_philemon.SectionsOS[2];

			// Add a split at the beginning of the first Scripture section.
			IScrSection newSection = section.SplitSectionContent_atIP(0, 0,
				m_philemon.SectionsOS[3].HeadingOA);

			// verify that the original section has its original heading paragraph and an empty content paragraph.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture1", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(null, ((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has an empty heading para and the content of the original section
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture2",
				((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("11some text2some text3-5some text",
				((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001001, 57001001, 57001001, 57001001);
			ScrSectionTests.VerifySectionRefs(newSection, 57001001, 57001005, 57001001, 57001005);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_atIP method when the IP is at the middle of a paragraph
		/// which is in the middle of the section content and there is content to copy into the
		/// new section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionContent_atIP_NewContent_MidParaMidIch()
		{
			// Add two more paragraphs to the last section content.
			IScrSection section = m_philemon.SectionsOS[3];
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 8, "Verse eight. ");
			int ichEndV8 = para.Contents.Length;
			AddVerse(para, 0, 9, "Verse nine. ");

			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 10, "Verse ten. ");

			// Add a split in the middle of the middle paragraph after verse 8.
			IScrSection newSection = section.SplitSectionContent_atIP(1, ichEndV8,
				m_philemon.SectionsOS[2].HeadingOA);

			// verify that the original section contains up to the end of verse 8.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture2", ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("6some text7some text",
				((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("8Verse eight. ", ((IScrTxtPara)section.ContentOA.ParagraphsOS[1]).Contents.Text);

			// verify that the new section follows the original section
			Assert.AreEqual(newSection.IndexInOwner, section.IndexInOwner + 1);

			// verify that the new section has empty heading and the contents from the middle of the second
			// paragraph to the last paragraph.
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Scripture1",
				((IScrTxtPara)newSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(2, newSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("9Verse nine. ", ((IScrTxtPara)newSection.ContentOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("10Verse ten. ", ((IScrTxtPara)newSection.ContentOA.ParagraphsOS[1]).Contents.Text);

			ScrSectionTests.VerifySectionRefs(section, 57001006, 57001008, 57001006, 57001008);
			ScrSectionTests.VerifySectionRefs(newSection, 57001009, 57001010, 57001009, 57001010);
		}
		#endregion

		#region SplitSectionContent_ExistingParaBecomesHeading tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitSectionContent_ExistingParaBecomesHeading method when a inner paragraph
		/// is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitSectionContent_ExistingParaBecomesHeading()
		{
			IScrSection section = AddSectionToMockedBook(m_matthew);
			AddSectionHeadParaToSection(section, "This is the heading", ScrStyleNames.SectionHead);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para1, 1, 1, "verse one");
			IScrTxtPara para2 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para2, 0, 0, "Future heading");
			IScrTxtPara para3 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para3, 0, 2, "verse two");

			// Now we simulate a style change, which is followed by a change in structure in ScrSection.
			para2.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead);
			IStStyle headingStyle = Cache.LangProject.TranslatedScriptureOA.FindStyle(ScrStyleNames.SectionHead);
			IScrSection newSection = section.SplitSectionContent_ExistingParaBecomesHeading(para2.IndexInOwner, 1, headingStyle);

			// verify that the original section now has one content paragraph and one heading paragraph.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("11verse one", ((IScrTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text);

			// verify that the text of the old para2 is now in the new section head
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			IStTxtPara newSecParaHeading = (IStTxtPara)newSection.HeadingOA.ParagraphsOS[0];
			Assert.IsTrue(newSecParaHeading.StyleRules.Style() == ScrStyleNames.SectionHead);
			Assert.AreEqual("Future heading", newSecParaHeading.Contents.Text);

			// verify that the text of the old para3 is now in the new section content
			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			IStTxtPara newSecParaContent = (IStTxtPara)newSection.ContentOA.ParagraphsOS[0];
			Assert.IsTrue(newSecParaContent.StyleRules.Style() == ScrStyleNames.NormalParagraph);
			Assert.AreEqual("2verse two", newSecParaContent.Contents.Text);

			VerifySectionRefs(section, 40001001, 40001001, 40001001, 40001001);
			VerifySectionRefs(newSection, 40001002, 40001002, 40001002, 40001002);
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
		/// <param name="contentRunText">Expected text of the new section's Content para</param>
		/// <param name="contentRunStyle">Expected style of the (one and only) run in the
		/// Content paragraph</param>
		/// <param name="defaultVernWs">Default vernacular writing system</param>
		/// <param name="verseRef">The expected start, end, min, and max section reference as a
		/// BBCCCVVV integer</param>
		/// ------------------------------------------------------------------------------------
		public static void VerifyInsertedBookSection(IScrSection section, bool isIntro,
			string contentRunText, string contentRunStyle, int defaultVernWs, int verseRef)
		{
			// Verify the new section heading paragraph count and the first para's style.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count,
				"Incorrect number of paras in section heading.");
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(
				isIntro ? ScrStyleNames.IntroSectionHead : ScrStyleNames.SectionHead),
				para.StyleRules);
			AssertEx.RunIsCorrect(para.Contents, 0, null, null, defaultVernWs);

			// Verify there's one content paragraph in new section, and its para style.
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(
				isIntro ? ScrStyleNames.IntroParagraph : ScrStyleNames.NormalParagraph),
				para.StyleRules);

			// Verify the paragraph has one run, with specified text, style, & ws.
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, contentRunText,
				contentRunStyle, defaultVernWs);

			// Check the section verse refs
			VerifySectionRefs(section, verseRef, verseRef, verseRef, verseRef);
		}

		internal static void VerifySectionRefs(IScrSection section, int refStart, int refEnd, int refMin, int refMax)
		{
			Assert.AreEqual(refStart, section.VerseRefStart);
			Assert.AreEqual(refEnd, section.VerseRefEnd);
			Assert.AreEqual(refMin, section.VerseRefMin);
			Assert.AreEqual(refMax, section.VerseRefMax);
		}
		#endregion
	}
}

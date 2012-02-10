// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrTxtParaTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using System.Collections.Generic;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region ScrTxtParaTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the <see cref="T:SIL.FieldWorks.FDO.DomainImpl.ScrTxtPara"/> class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrTxtParaTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private IScrBook m_book;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_book = AddBookToMockedScripture(40, "Matthew");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds content to the book of Matthew: one intro section and one Scripture section.
		/// Both sections will have vernacular and back translation text for the headings
		/// and content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddDataToMatthew()
		{
			// Add vernacular text.
			AddTitleToMockedBook(m_book, "Matthew");
			IScrSection introSection = AddSectionToMockedBook(m_book, true);
			AddSectionHeadParaToSection(introSection, "Heading 1", ScrStyleNames.IntroSectionHead);
			IStTxtPara introPara = AddParaToMockedSectionContent(introSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(introPara, "Intro text. We need lots of stuff here so that our footnote tests will work.", null);

			IScrSection scrSection = AddSectionToMockedBook(m_book);
			AddSectionHeadParaToSection(scrSection, "Heading 2", ScrStyleNames.SectionHead);
			IStTxtPara scrPara = AddParaToMockedSectionContent(scrSection, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(scrPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(scrPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(scrPara, "Verse one. ", null);
			AddRunToMockedPara(scrPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(scrPara, "Verse two.", null);

			// Add back translation text.
			int wsAnal = Cache.DefaultAnalWs;
			IScrTxtPara heading1Para = (IScrTxtPara)introSection.HeadingOA.ParagraphsOS[0];
			ICmTranslation trans = heading1Para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Heading 1", null);

			trans = introPara.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Intro text", null);

			IScrTxtPara scrHeadingPara = (IScrTxtPara)scrSection.HeadingOA.ParagraphsOS[0];
			trans = scrHeadingPara.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Heading 2", null);

			trans = scrPara.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse one", null);
		}
		#endregion

		#region SplitParaAt tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitParaAt method when there are no footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitParaAt_NoFootnotes()
		{
			AddDataToMatthew();
			IScrSection section = m_book.SectionsOS[1];
			IScrTxtPara para = (IScrTxtPara)section.ContentOA.ParagraphsOS[0];

			// Add a paragraph split at the end of verse one
			IScrTxtPara addedPara = (IScrTxtPara)para.SplitParaAt(13);

			// We expect that the new paragraph would contain the contents of the paragraph
			// following verse one.
			Assert.AreEqual(3, para.Contents.RunCount);
			Assert.AreEqual("11Verse one. ", para.Contents.Text);
			Assert.AreEqual(2, addedPara.Contents.RunCount);
			Assert.AreEqual("2Verse two.", addedPara.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the SplitParaAt method when there are footnotes before and after the para split.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SplitParaAt_WithFootnotes()
		{
			AddDataToMatthew();
			IScrSection section = m_book.SectionsOS[1];
			IScrTxtPara para = (IScrTxtPara)section.ContentOA.ParagraphsOS[0];
			IScrFootnote footnote1 = AddFootnote(m_book, para, 11, "Footnote one.");
			IScrFootnote footnote2 = AddFootnote(m_book, para, 24, "Footnote one.");
			Assert.AreEqual(2, m_book.FootnotesOS.Count);

			// Add a paragraph split at the end of verse one
			IScrTxtPara addedPara = (IScrTxtPara)para.SplitParaAt(14);

			Assert.AreEqual(2, m_book.FootnotesOS.Count);
			// We expect that the new paragraph would contain the contents of the paragraph
			// following verse one.
			Assert.AreEqual(5, para.Contents.RunCount);
			Assert.AreEqual("11Verse one" + StringUtils.kChObject + ". ", para.Contents.Text);
			Assert.AreEqual(4, addedPara.Contents.RunCount);
			Assert.AreEqual("2Verse two" + StringUtils.kChObject + ".", addedPara.Contents.Text);
		}
		#endregion

		#region StyleName property tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that StyleName gets the right name when StyleRules is not null and contains a
		/// ktptNamedStyle value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetStyleName_BaseOperation()
		{
			// add section and content
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara paraHead = AddSectionHeadParaToSection(section, "Wow", "Section Head Major");
			IScrTxtPara paraContent = AddParaToMockedSectionContent(section, "Line 1");
			AddRunToMockedPara(paraContent, "15", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(paraContent, "1", ScrStyleNames.VerseNumber);

			Assert.AreEqual("Section Head Major", paraHead.StyleName);
			Assert.AreEqual("Line 1", paraContent.StyleName);
		}
		#endregion

		#region OnStyleRulesChange tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that when we add a paragraph with a particular style that it is marked as
		/// an inuse style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnStyleRulesChange_ParaStyle()
		{
			IStStyle style = m_scr.FindStyle(ScrStyleNames.Line3);
			Assert.IsFalse(style.InUse);

			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			IScrSection section = AddSectionToMockedBook(book);
			AddParaToMockedSectionContent(section, ScrStyleNames.Line3);

			style = m_scr.FindStyle(ScrStyleNames.Line3);
			Assert.IsTrue(style.InUse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that when we add a run with a particular character style that it is marked as
		/// an inuse style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnStylesRuleChange_OneCharStyle()
		{
			IStStyle style = m_scr.FindStyle(ScrStyleNames.Doxology);
			Assert.IsFalse(style.InUse);

			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			IScrSection section = AddSectionToMockedBook(book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			// Add a range of text with a character style.
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultVernWs);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, ScrStyleNames.Doxology);
			ITsStrBldr tssBldr = para.Contents.GetBldr();
			tssBldr.ReplaceRgch(para.Contents.Length, para.Contents.Length, "new char style", 14, propsBldr.GetTextProps());
			para.Contents = tssBldr.GetString();

			// We expect that this character style will now be marked as InUse.
			style = m_scr.FindStyle(ScrStyleNames.Doxology);
			Assert.IsTrue(style.InUse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that when we add a run with a multiple character styles that they are marked as
		/// InUse styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnStylesRuleChange_MultiCharStyles()
		{
			// We need to confirm that the character styles we are checking are initially not in use.
			IStStyle style;
			style = m_scr.FindStyle(ScrStyleNames.Doxology);
			Assert.IsFalse(style.InUse);
			style = m_scr.FindStyle(ScrStyleNames.AlternateReading);
			Assert.IsFalse(style.InUse);

			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			IScrSection section = AddSectionToMockedBook(book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			// Add a range of text with a character style.
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultVernWs);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, ScrStyleNames.Doxology);
			ITsStrBldr tssBldr = para.Contents.GetBldr();
			tssBldr.ReplaceRgch(para.Contents.Length, para.Contents.Length, "Untranslated Word char style", 28, propsBldr.GetTextProps());
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, ScrStyleNames.AlternateReading);
			tssBldr.ReplaceRgch(para.Contents.Length, para.Contents.Length, "AlternateReading char style", 27, propsBldr.GetTextProps());
			para.Contents = tssBldr.GetString();

			// We expect that these character style will now be marked as InUse.
			style = m_scr.FindStyle(ScrStyleNames.Doxology);
			Assert.IsTrue(style.InUse);
			style = m_scr.FindStyle(ScrStyleNames.AlternateReading);
			Assert.IsTrue(style.InUse);
		}
		#endregion

		#region GetContextAndStructure method & Context property tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Context gets the correct value for Scripture Section Head and Content
		/// paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Context_Scripture()
		{
			// add section and content
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara paraHead = (IScrTxtPara)section.HeadingOA.AddNewTextPara(ScrStyleNames.SectionHead);
			IScrTxtPara paraContent = (IScrTxtPara)section.ContentOA.AddNewTextPara(ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraContent, "15", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(paraContent, "1", ScrStyleNames.VerseNumber);

			Assert.AreEqual(ContextValues.Text, paraHead.Context);
			Assert.AreEqual(ContextValues.Text, paraContent.Context);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Context gets the correct value for Intro section Head and Content
		/// paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Context_Intro()
		{
			// add section and content
			IScrSection section = AddSectionToMockedBook(m_book, true);
			IScrTxtPara paraHead = (IScrTxtPara)section.HeadingOA.AddNewTextPara(ScrStyleNames.SectionHead);
			IScrTxtPara para1 = (IScrTxtPara)section.ContentOA.AddNewTextPara(ScrStyleNames.NormalParagraph);

			Assert.AreEqual(ContextValues.Intro, paraHead.Context);
			Assert.AreEqual(ContextValues.Intro, para1.Context);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Context gets the correct value for Title paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Context_Title()
		{
			// add section and content
			ITsStrFactory factory = TsStrFactoryClass.Create();
			AddTitleToMockedBook(m_book, "Matthew");

			Assert.AreEqual(ContextValues.Title, ((IScrTxtPara)m_book.TitleOA.ParagraphsOS[0]).Context);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Context gets the correct value for Footnote paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Context_Footnote()
		{
			// add section and content
			ITsStrFactory factory = TsStrFactoryClass.Create();
			IStText title = AddTitleToMockedBook(m_book, "Matthew");
			IStTxtPara para = AddParaToMockedText(title, ScrStyleNames.MainBookTitle);
			IScrFootnote footnote = AddFootnote(m_book, para, 0, "Some text");
			IScrTxtPara scrParaFootnote = (IScrTxtPara)footnote.ParagraphsOS[0];

			Assert.AreEqual(ContextValues.Note, scrParaFootnote.Context);
		}
		#endregion

		#region GetSectionStartAndEndRefs tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that OwnerStartRef and OwnerEndRef get the section reference when called on
		/// content and heading paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OwnerStartAndEndRefs()
		{
			// add section and content
			IScrSection section = AddSectionToMockedBook(m_book);
			IStTxtPara paraHeading = AddSectionHeadParaToSection(section,
				"This is the heading", ScrStyleNames.SectionHead);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "15", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "some text", null);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 15, 1), ReflectionHelper.GetProperty(paraHeading, "OwnerStartRef"));
			Assert.AreEqual(new BCVRef(40, 15, 4), ReflectionHelper.GetProperty(paraHeading, "OwnerEndRef"));
			Assert.AreEqual(new BCVRef(40, 15, 1), ReflectionHelper.GetProperty(para, "OwnerStartRef"));
			Assert.AreEqual(new BCVRef(40, 15, 4), ReflectionHelper.GetProperty(para, "OwnerEndRef"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the section start and end reference for an intro paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionReferences_IntroPara()
		{
			// add section and empty paragraph
			IScrSection section = AddSectionToMockedBook(m_book, true);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 1, 0), ReflectionHelper.GetProperty(para, "OwnerStartRef"));
			Assert.AreEqual(new BCVRef(40, 1, 0), ReflectionHelper.GetProperty(para, "OwnerEndRef"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the section start and end reference for a book title
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionReferences_BookTitle()
		{
			// add title
			IStText title = AddTitleToMockedBook(m_book, "This is the title");
			IScrTxtPara para = (IScrTxtPara)title.ParagraphsOS[0];

			// verify the results
			Assert.AreEqual(new BCVRef(40, 1, 0), ReflectionHelper.GetProperty(para, "OwnerStartRef"));
			Assert.AreEqual(new BCVRef(40, 1, 0), ReflectionHelper.GetProperty(para, "OwnerEndRef"));
		}
		#endregion

		#region HasChapterOrVerseNumbers tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the HasChapterOrVerseNumbers method in intro section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HasChapterOrVerseNumbers_IntroSection()
		{
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book, true);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "some text", null);

			Assert.IsFalse(para.HasChapterOrVerseNumbers());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the HasChapterOrVerseNumbers method in scripture section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HasChapterOrVerseNumbers_InScripture()
		{
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "some text", null);

			Assert.IsTrue(para.HasChapterOrVerseNumbers());
		}
		#endregion

		#region NextFootnoteIndex tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get correct footnote index when current diff is in a book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteIndexTest_InTitle()
		{
			AddDataToMatthew();
			IScrTxtPara para = (IScrTxtPara)m_book.TitleOA[0];
			int iFootnote = (int)ReflectionHelper.GetResult(para, "NextFootnoteIndex", 0);
			Assert.AreEqual(0, iFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get correct footnote index (0) in a book with no footnotes and we
		/// start scanning at the beginning of the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteIndexTest_BookWithNoFootnotes_AtStart()
		{
			AddDataToMatthew();
			IFdoOwningSequence<IScrSection> sections = m_book.SectionsOS;
			IScrSection section = sections[sections.Count - 1];
			IScrTxtPara para = (IScrTxtPara)section.ContentOA[0];

			int iFootnote = (int)ReflectionHelper.GetResult(para, "NextFootnoteIndex", 0);
			Assert.AreEqual(0, iFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get correct footnote index (0) when book has no footnotes and we
		/// start scanning at the end of the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteIndexTest_BookWithNoFootnotes_AtEnd()
		{
			AddDataToMatthew();
			IScrTxtPara para = (IScrTxtPara)((IScrSection)m_book.SectionsOS[0]).ContentOA[0];

			int iFootnote = (int)ReflectionHelper.GetResult(para, "NextFootnoteIndex",
				para.Contents.Length);
			Assert.AreEqual(0, iFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get correct footnote index following a deleted section and a
		/// deleted paragraph within the current section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteIndexTest_AfterDeletedStuff()
		{
			AddDataToMatthew();

			// We need more sections and paragraphs.
			IScrSection scrSection2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(scrSection2, ScrStyleNames.NormalParagraph);
			AddVerse(para1, 0, 3, "verse three. ");
			AddFootnote(m_book, para1, para1.Contents.Length, "First footnote to be deleted.");
			AddVerse(para1, 0, 4, "verse four. ");
			AddFootnote(m_book, para1, para1.Contents.Length, "Second footnote to be deleted.");

			IScrTxtPara para2 = AddParaToMockedSectionContent(scrSection2, ScrStyleNames.NormalParagraph);
			AddVerse(para2, 0, 5, "verse five. ");
			AddVerse(para2, 0, 6, "verse six. ");

			// remove the first (introduction) section
			m_book.SectionsOS.RemoveAt(0);

			// remove the first paragraph in the last section which contains two footnotes.
			IScrSection lastSection = m_book.SectionsOS[m_book.SectionsOS.Count - 1];
			IFdoOwningSequence<IStPara> paras = lastSection.ContentOA.ParagraphsOS;
			paras.RemoveAt(0);

			// Put a footnote into the first paragraph of the last section
			AddFootnote(m_book, (IStTxtPara)paras[0], 0,
				"Added footnote at the start of the first para in last section");

			// get the last paragraph in the last section
			IScrTxtPara lastPara = (IScrTxtPara)paras[paras.Count - 1];
			int lastParaPriorLength = lastPara.Contents.Length;
			AddRunToMockedPara(lastPara, "Some text for the last paragraph.", Cache.DefaultVernWs);
			AddFootnote(m_book, lastPara, lastParaPriorLength + 22, "Footnote on the 'last' word.");

			// Start searching for a prior footnote in the last paragraph, but before the last
			// footnote.
			int iFootnote = (int)ReflectionHelper.GetResult(lastPara, "NextFootnoteIndex",
				lastParaPriorLength + 15);
			Assert.AreEqual(m_book.FootnotesOS.Count - 1, iFootnote,
				"The footnote index should have been for the second to last position.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get correct footnote index when our current position is after the
		/// last footnote in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteIndexTest_EndOfBook()
		{
			AddDataToMatthew();
			IFdoOwningSequence<IScrSection> sections = m_book.SectionsOS;
			// Add a footnote to the first paragraph in the first section.
			IScrTxtPara firstPara = AddParaToMockedSectionContent(sections[0],
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(firstPara, "Text", Cache.DefaultVernWs);
			AddFootnote(m_book, firstPara, firstPara.Contents.Length);

			// Get the last paragraph (where we will begin scanning for the previous footnote).
			IScrSection lastSection = sections[sections.Count - 1];
			IFdoOwningSequence<IStPara> paras = lastSection.ContentOA.ParagraphsOS;
			IScrTxtPara lastPara = (IScrTxtPara)paras[paras.Count - 1];

			int iFootnote = (int)ReflectionHelper.GetResult(lastPara, "NextFootnoteIndex",
				lastPara.Contents.Length);
			Assert.AreEqual(m_book.FootnotesOS.Count, iFootnote);
		}
		#endregion

		#region Reference tests

		/// <summary>
		/// Test the Reference method for Scripture paragraphs.
		/// </summary>
		[Test]
		public void Reference()
		{
			AddDataToMatthew();
			var para1 = (IStTxtPara) m_book.SectionsOS[1].ContentOA.ParagraphsOS[0]; // Actually ScrTxtPara
			var seg = para1.SegmentsOS[1]; // first content ref, after the chapter and verse number stuff.
			Assert.That(para1.Reference(seg, seg.BeginOffset + 1).Text, Is.EqualTo("MAT 1:1"));
			AddRunToMockedPara(para1, "Verse two second sentence.", null);
			var v2seg1 = para1.SegmentsOS[3]; // first segment of two-sentence verse
			Assert.That(para1.Reference(v2seg1, v2seg1.BeginOffset + 1).Text, Is.EqualTo("MAT 1:2a"));
			var v2seg2 = para1.SegmentsOS[4]; // first segment of two-sentence verse
			Assert.That(para1.Reference(v2seg2, v2seg2.BeginOffset + 1).Text, Is.EqualTo("MAT 1:2b"));
			IStTxtPara para2 = AddParaToMockedSectionContent((IScrSection)para1.Owner.Owner, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "Verse 2 seg 3", null);
			var v2seg3 = para2.SegmentsOS[0]; // third segment of three-sentence verse split over two paragraphs.
			Assert.That(para2.Reference(v2seg3, v2seg3.BeginOffset + 1).Text, Is.EqualTo("MAT 1:2c"));
			var newSection = AddSectionToMockedBook(m_book);
			IStTxtPara para3 = AddParaToMockedSectionContent(newSection, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para3, "Verse 2 seg 4", null);
			var v2seg4 = para3.SegmentsOS[0]; // fourth segment of four-sentence verse split over two sections(!).
			// JohnT: arguably this should give MAT 1:2d. The current implementation does not detect the
			// segments in the previous section.
			Assert.That(para3.Reference(v2seg4, v2seg4.BeginOffset + 1).Text, Is.EqualTo("MAT 1:2"));
		}
		#endregion

		#region Moving paragraphs between books tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moving a paragraph into a section owned by a different book should throw an
		/// exception. Adding a paragraph to a different owning sequence removes it from its
		/// original owner. In Scripture, this is not allowed if it would result in moving it to
		/// a totally different book. There is both a pragmatic reason for this and a technical
		/// one. Pragmatic: There is no logical need to move a paragraph from one book to
		/// another. Technical: Paragraph contents can include footnote ORCs which refer to
		/// footnotes owned by the book, so moving a paragraph would orphan any footnotes in
		/// the original owning book. (Technical reason goes away if we do FWR-234.)
		/// Note: copying contents of paragraphs is fine, as long as the footnote ORCs in the
		/// new copy are hooked up to new footnotes in the book by calling CreateOwnedObjects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void PreventMovingParagraphsBetweenBooks_Add()
		{
			IScrSection sectionInBook = AddSectionToMockedBook(m_book);
			IScrBook archivedBook = AddArchiveBookToMockedScripture(1, "Archived Genesis");
			IScrSection sectionInArchive = AddSectionToMockedBook(archivedBook);
			IScrTxtPara para = AddParaToMockedSectionContent(sectionInArchive, ScrStyleNames.NormalParagraph);
			// Attempt to move the paragraph from the archive into the current book.
			sectionInBook.ContentOA.ParagraphsOS.Add(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moving a paragraph into a section owned by a different book should throw an
		/// exception. Adding a paragraph to a different owning sequence removes it from its
		/// original owner. In Scripture, this is not allowed if it would result in moving it to
		/// a totally different book. There is both a pragmatic reason for this and a technical
		/// one. Pragmatic: There is no logical need to move a paragraph from one book to
		/// another. Technical: Paragraph contents can include footnote ORCs which refer to
		/// footnotes owned by the book, so moving a paragraph would orphan any footnotes in
		/// the original owning book. (Technical reason goes away if we do FWR-234.)
		/// Note: copying contents of paragraphs is fine, as long as the footnote ORCs in the
		/// new copy are hooked up to new footnotes in the book by calling CreateOwnedObjects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void PreventMovingParagraphsBetweenBooks_Insert()
		{
			IScrSection sectionInBook = AddSectionToMockedBook(m_book);
			IScrBook archivedBook = AddArchiveBookToMockedScripture(1, "Archived Genesis");
			IScrSection sectionInArchive = AddSectionToMockedBook(archivedBook);
			IScrTxtPara para = AddParaToMockedSectionContent(sectionInArchive, ScrStyleNames.NormalParagraph);
			// Attempt to move the paragraph from the archive into the current book.
			sectionInBook.ContentOA.ParagraphsOS.Insert(0, para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moving a paragraph into a section owned by a different book should throw an
		/// exception. Adding a paragraph to a different owning sequence removes it from its
		/// original owner. In Scripture, this is not allowed if it would result in moving it to
		/// a totally different book. There is both a pragmatic reason for this and a technical
		/// one. Pragmatic: There is no logical need to move a paragraph from one book to
		/// another. Technical: Paragraph contents can include footnote ORCs which refer to
		/// footnotes owned by the book, so moving a paragraph would orphan any footnotes in
		/// the original owning book. (Technical reason goes away if we do FWR-234.)
		/// Note: copying contents of paragraphs is fine, as long as the footnote ORCs in the
		/// new copy are hooked up to new footnotes in the book by calling CreateOwnedObjects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void PreventMovingParagraphsBetweenBooks_Replace()
		{
			IScrSection sectionInBook = AddSectionToMockedBook(m_book);
			IScrBook archivedBook = AddArchiveBookToMockedScripture(1, "Archived Genesis");
			IScrSection sectionInArchive = AddSectionToMockedBook(archivedBook);
			IScrTxtPara para = AddParaToMockedSectionContent(sectionInArchive, ScrStyleNames.NormalParagraph);
			// Attempt to move the paragraph from the archive into the current book.
			sectionInBook.ContentOA.ParagraphsOS.Replace(0, 0, new ICmObject[] {para});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moving a paragraph into a section owned by a different book should throw an
		/// exception. Adding a paragraph to a different owning sequence removes it from its
		/// original owner. In Scripture, this is not allowed if it would result in moving it to
		/// a totally different book. There is both a pragmatic reason for this and a technical
		/// one. Pragmatic: There is no logical need to move a paragraph from one book to
		/// another. Technical: Paragraph contents can include footnote ORCs which refer to
		/// footnotes owned by the book, so moving a paragraph would orphan any footnotes in
		/// the original owning book. (Technical reason goes away if we do FWR-234.)
		/// Note: copying contents of paragraphs is fine, as long as the footnote ORCs in the
		/// new copy are hooked up to new footnotes in the book by calling CreateOwnedObjects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void PreventMovingParagraphsBetweenBooks_IndexedSetter()
		{
			IScrSection sectionInBook = AddSectionToMockedBook(m_book);
			AddParaToMockedSectionContent(sectionInBook, ScrStyleNames.NormalParagraph);
			IScrBook archivedBook = AddArchiveBookToMockedScripture(1, "Archived Genesis");
			IScrSection sectionInArchive = AddSectionToMockedBook(archivedBook);
			IScrTxtPara para = AddParaToMockedSectionContent(sectionInArchive, ScrStyleNames.NormalParagraph);
			// Attempt to move the paragraph from the archive into the current book.
			sectionInBook.ContentOA.ParagraphsOS[0] = para;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests ability to insert a verse number in an empty back translation of a vernacular
		/// paragraph which starts with a chapter number (1) and verse number (1). The chapter
		/// number should get added automatically as well.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertNextVerseNumberInBt_VernC1V1_BtEmpty()
		{
			IScrSection sectionInBook = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(sectionInBook, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "Fun stuff");
			ITsStrBldr bldrExpected = para.Contents.GetBldr();
			bldrExpected.Replace(2, bldrExpected.Length, String.Empty, null);
			bldrExpected.SetIntPropValues(0, 2, (int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_wsEn);
			string sVerseNum, sChapterNum;
			int ichLimIns;
			para.InsertNextVerseNumberInBt(m_wsEn, 0, out sVerseNum, out sChapterNum, out ichLimIns);
			Assert.AreEqual("1", sVerseNum);
			Assert.AreEqual("1", sChapterNum);
			Assert.AreEqual(2, ichLimIns);
			AssertEx.AreTsStringsEqual(bldrExpected.GetString(), para.GetBT().Translation.get_String(m_wsEn));
		}

		#region Misc tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests to make sure any newly-created ScrTxtPara has a CmTranslation for the
		/// back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewlyCreatedHasBackTrans()
		{
			AddDataToMatthew();
			IScrTxtPara newPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				m_book.TitleOA, ScrStyleNames.MainBookTitle);
			Assert.IsNotNull(newPara.GetBT());
		}
		#endregion
	}
	#endregion

	#region GetRefsAtPositionTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the GetRefsAtPosition method of the
	/// <see cref="T:SIL.FieldWorks.FDO.DomainImpl.ScrTxtPara"/> class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class GetRefsAtPositionTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private IScrBook m_book;
		private IScrSection m_section;
		private IScrTxtPara m_para;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_book = AddBookToMockedScripture(40, "Matthews");

			// add section and paragraph
			m_section = AddSectionToMockedBook(m_book);
			m_para = AddParaToMockedSectionContent(m_section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(m_para, "15", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(m_para, "This is verse one. ", null);
			AddRunToMockedPara(m_para, "16", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(m_para, "This is a verse. ", null);
			AddRunToMockedPara(m_para, "17", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(m_para, "This is a verse. ", null);

			Assert.AreEqual(new BCVRef(40, 1, 15), m_section.VerseRefStart);
			Assert.AreEqual(new BCVRef(40, 1, 17), m_section.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates some additional test data needed for tests that have to look in a previous
		/// paragraph to find the starting chapter number.
		/// </summary>
		/// <returns>The second paragraph (since that's the only thing the tests need)</returns>
		/// ------------------------------------------------------------------------------------
		private IScrTxtPara CreateTestSectionWithTwoParas()
		{
			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is a verse. ", null);
			AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is a verse. ", null);

			IScrTxtPara para2 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse one. ", null);
			AddRunToMockedPara(para2, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is a verse. ", null);

			Assert.AreEqual(new BCVRef(40, 2, 1), section2.VerseRefStart);
			Assert.AreEqual(new BCVRef(40, 2, 5), section2.VerseRefEnd);

			return para2;
		}
		#endregion

		#region Tests with single paragraph
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the end of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtEnd()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, m_para.Contents.Length - 1, false, out refStart,  out refEnd);
			Assert.AreEqual(m_section.VerseRefEnd, refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from an invalid position: index before the start of the
		/// first paragraph in the section. Should just return the section refs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidPos_NegativeIndex()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, out refStart, out refEnd);
			// For this test the needed result is not precisely defined yet
			// when it is defined, modify this to that spec, but for now...
			Assert.AreEqual(m_section.VerseRefStart, refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from an invalid position: index before the start of the
		/// first paragraph in the section. Should just return the refs at the end of the para.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidPos_IndexPastEnd()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, m_para.Contents.Length + 1, false, out refStart, out refEnd);
			Assert.AreEqual(m_section.VerseRefEnd, refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the start of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StartOfPara_AssocPrevTrue()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, 0, true, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(m_section.VerseRefStart, refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from beginning of a paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StartOfPara_StartOfSection_AssocPrevFalse()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(0, out refStart, out refEnd);
			Assert.AreEqual(m_section.VerseRefStart, refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a verse number in a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MiddleOfVerseNumber()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, 1, true, out refStart, out refEnd);
			Assert.AreEqual(m_section.VerseRefStart, refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MiddleOfPara_MiddleOfSection_AssocPrevTrue()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, 25, true, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 1, 16), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MiddleOfPara_MiddleOfSection_AssocPrevFalse()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(23, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 1, 16), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a paragraph (with no chapter or verse numbers in a
		/// newly created section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewParaInNewSection()
		{
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "This is a test.", null);

			BCVRef refStart, refEnd;
			para.GetRefsAtPosition(1, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 1, 17), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a new paragraph added to the beginning of an existing
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewParaInExistingSection_Begin()
		{
			// Insert a new paragraph at the beginning of the first section
			IScrTxtPara newPara = (IScrTxtPara)m_section.ContentOA.InsertNewTextPara(0, ScrStyleNames.NormalParagraph);
			newPara.Contents = TsStringUtils.MakeTss("Some text at beginning of section without previous verse",
				Cache.DefaultVernWs);

			BCVRef refStart, refEnd;
			newPara.GetRefsAtPosition(0, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 1, 1), refStart);
			Assert.AreEqual(refStart, refEnd);
			// Because BCVRef is a class, not a value object, but it feels a lot like a value
			// object, history has shown that it's very easy to write code that re-uses the same
			// object for start and end refs, so we'll tweak the end ref's Verse number and
			// make sure it doesn't affect the start ref.
			refEnd.Verse++;
			Assert.AreEqual(new BCVRef(40, 1, 1), refStart);
			Assert.AreNotEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a new paragraph added to the end of an existing
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewParaInExistingSection_End()
		{
			// Add a new paragraph at the end of the first section
			IScrTxtPara newPara = (IScrTxtPara)m_section.ContentOA.AddNewTextPara(ScrStyleNames.NormalParagraph);
			newPara.Contents = TsStringUtils.MakeTss("Text at the end of the section.",
				Cache.DefaultVernWs);

			BCVRef refStart, refEnd;
			newPara.GetRefsAtPosition(1, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 1, 17), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a paragraph with a chapter number and verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberAndVerseBridgeInPara()
		{
			// Insert a new paragraph at the end of the first section
			IScrTxtPara newPara = (IScrTxtPara)m_section.ContentOA.AddNewTextPara(ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(newPara, "42", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(newPara, "6-9", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(newPara, "Some more text", null);

			BCVRef refStart, refEnd;
			newPara.GetRefsAtPosition(17, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 42, 6), refStart);
			Assert.AreEqual(new BCVRef(40, 42, 9), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a paragraph with a chapter number only.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberInPara()
		{
			// Insert a new paragraph at the end of the first section
			IScrTxtPara newPara = (IScrTxtPara)m_section.ContentOA.AddNewTextPara(ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(newPara, "42", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(newPara, "Some more text", null);

			BCVRef refStart, refEnd;
			newPara.GetRefsAtPosition(5, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 42, 1), refStart);
			Assert.AreEqual(refStart, refEnd);
		}
		#endregion

		#region Tests with two paragraphs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from previous paragraph only
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FromPrevParagraph_SecondSection()
		{
			IScrTxtPara para2 = CreateTestSectionWithTwoParas();

			BCVRef refStart, refEnd;
			para2.GetRefsAtPosition(-1, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 2, 3), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a paragraph where the chapter number is in previous
		/// paragraph in same section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberInPrevPara()
		{
			IScrTxtPara para2 = CreateTestSectionWithTwoParas();

			BCVRef refStart, refEnd;
			para2.GetRefsAtPosition(5, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 2, 4), refStart);
			Assert.AreEqual(refStart, refEnd);
		}
		#endregion
	}
	#endregion

	#region GetRefsAtPosition tests with single paragraph having multiple chapter numbers
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the GetRefsAtPosition method of the
	/// <see cref="T:SIL.FieldWorks.FDO.DomainImpl.ScrTxtPara"/> class when the paragraph contains
	/// a chapter number
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class GetRefsAtPositionTestsWithChapterNumbers : ScrInMemoryFdoTestBase
	{
		#region Data members
		private IScrBook m_book;
		private IScrSection m_section;
		private IScrTxtPara m_para;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_book = AddBookToMockedScripture(40, "Matthews");

			// add section and paragraph
			m_section = AddSectionToMockedBook(m_book);
			m_para = AddParaToMockedSectionContent(m_section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(m_para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(m_para, "This is verse one. ", null);
			AddRunToMockedPara(m_para, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(m_para, "This is a verse. ", null);
			AddRunToMockedPara(m_para, "17", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(m_para, "This is a verse. ", null);
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the end of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtEnd()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(m_para.Contents.Length - 1, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 2, 17), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the start of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StartOfPara()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(0, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 1, 1), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a verse number in a single paragraph
		/// with fAssocPrev set to false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BeforeChapterNumber_AssocPrevFalse()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, 20, false, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 2, 1), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a verse number in a single paragraph
		/// with fAssocPrev set to true
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BeforeChapterNumber_AssocPrevTrue()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, 20, true, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 1, 1), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a single paragraph if position is directly behind the
		/// second chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AfterChapterNumber()
		{
			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, 21, true, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 2, 1), refStart);
			Assert.AreEqual(refStart, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a single paragraph if position is directly behind a
		/// chapter number which is bogus (non-numeric).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRefsAtPosition_WithNonNumericChapter()
		{
			AddRunToMockedPara(m_para, "A", ScrStyleNames.ChapterNumber);

			BCVRef refStart, refEnd;
			m_para.GetRefsAtPosition(-1, m_para.Contents.Length, true, out refStart, out refEnd);
			Assert.AreEqual(new BCVRef(40, 2, 17), refStart);
			Assert.AreEqual(refStart, refEnd);
		}
		#endregion
	}
	#endregion

	#region ScrTxtParaTests_FootnoteRelated
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the ScrTxtPara when handling various footnote ORC scenarios
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrTxtParaTests_FootnoteRelated : ScrInMemoryFdoTestBase
	{
		#region Member data
		private IStText m_currentText;
		private IStText m_archivedText;
		private IFdoOwningSequence<IScrFootnote> m_archivedFootnotesOS;
		private IFdoOwningSequence<IScrFootnote> m_currentFootnotesOS;
		private IScrBook m_book;
		private IScrBook m_arcBook;

		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			// ScrBooks are only used because, currently, they are the only objects that
			// own footnotes
			m_book = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(1, out m_currentText);

			IStTxtPara para = AddParaToMockedText(m_currentText, "Normal");
			AddRunToMockedPara(para, "1", "CharacterStyle1");
			AddRunToMockedPara(para, "1", "CharacterStyle2");
			AddRunToMockedPara(para, "This text has no char style.", null);
			m_currentFootnotesOS = m_book.FootnotesOS;

			// make the archive text
			m_arcBook = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(2, out m_archivedText);

			para = AddParaToMockedText(m_archivedText, "Normal");
			AddRunToMockedPara(para, "1", "CharacterStyle1");
			AddRunToMockedPara(para, "1", "CharacterStyle2");
			AddRunToMockedPara(para, "This is the previous version of the text.", null);
			m_archivedFootnotesOS = m_arcBook.FootnotesOS;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method to set up the test paragraph in m_archivedText, including footnotes
		/// and back translations, plus any other fields deemed necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IStTxtPara SetUpParagraphInArchiveWithFootnotesAndBT()
		{
			// Prepare the Revision paragraph in m_archivedText
			// note: CreateTestData has already placed "11This is the previous version of the text."
			//  in paragraph in m_archivedText.

			IStTxtPara paraRev = m_archivedText[0];
			paraRev.StyleRules = StyleUtils.ParaStyleTextProps("Line 1");
			// add footnotes to existing paragraph.
			IStFootnote footnote1 = AddFootnote(m_arcBook, paraRev, 6, "Footnote1");
			IStFootnote footnote2 = AddFootnote(m_arcBook, paraRev, 10, "Footnote2");
			Assert.AreEqual(2, m_archivedFootnotesOS.Count);

			// Add two back translations of the para and footnotes
			int[] wsBt = new int[] { m_wsEn, m_wsDe };
			foreach (int ws in wsBt)
			{
				// add back translation of the para, and status
				ICmTranslation paraTrans = AddBtToMockedParagraph(paraRev, ws);
				AddRunToMockedTrans(paraTrans, ws, "BT of test paragraph" + ws.ToString(), null);
				// add BT footnotes, and status
				ICmTranslation footnoteTrans = AddBtFootnote(paraTrans, 2, ws, footnote1,
					"BT of footnote1 " + ws.ToString());
				footnoteTrans.Status.set_String(ws, BackTranslationStatus.Checked.ToString());
				footnoteTrans = AddBtFootnote(paraTrans, 6, ws, footnote2,
					"BT of footnote2 " + ws.ToString());
				footnoteTrans.Status.set_String(ws, BackTranslationStatus.Finished.ToString());
				paraTrans.Status.set_String(ws, BackTranslationStatus.Finished.ToString());
			}
			return paraRev;
		}
		#endregion

		#region Copy Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the .CopyTo method, copying a paragraph which has footnotes and back
		/// translation. Results should be identical to using other copy methods.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyTo()
		{
			IStTxtPara paraRev = SetUpParagraphInArchiveWithFootnotesAndBT();
			IStTxtPara newPara = AddParaToMockedText(m_currentText, "Normal");

			// Now, call the method under test
			paraRev.SetCloneProperties(newPara);

			VerifyCopiedPara(newPara);
			VerifyParagraphsAreDifferentObjects(paraRev, newPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the .ReplaceText method, replacing a range of text with footnotes with text
		/// from a paragraph which has footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceText()
		{
			IScrTxtPara paraRev = (IScrTxtPara)SetUpParagraphInArchiveWithFootnotesAndBT();
			IScrTxtPara paraCur = (IScrTxtPara)m_currentText.ParagraphsOS[0];
			IScrFootnote footnoteCur1 = AddFootnote(m_book, paraCur, 0,
				"Original footnote at start of current para");
			IScrFootnote footnoteCur2 = AddFootnote(m_book, paraCur, paraCur.Contents.Length,
				"Original footnote at end of current para");
			int initialRevFootnotes = m_arcBook.FootnotesOS.Count;
			Assert.AreEqual(2, m_book.FootnotesOS.Count, "Two footnotes expected in book at start");

			// Copy a range from the revision with two footnotes to the current paragraph.
			paraCur.ReplaceTextRange(0, 12, paraRev, 5, paraRev.Contents.Length);

			// We expect that the count of revision footnotes will remain the same.
			Assert.AreEqual(initialRevFootnotes, m_arcBook.FootnotesOS.Count);
			// We expect that the revision will have one more footnote. The first one should be removed
			// and two footnotes should be added from the revision.
			// and that the book will have one more footnote from the range in the revision.
			Assert.AreEqual(3, m_book.FootnotesOS.Count);
			Assert.AreEqual("s" + StringUtils.kChObject + " is" + StringUtils.kChObject +
				" the previous version of the text. has no char style." + StringUtils.kChObject,
				paraCur.Contents.Text);
			Assert.AreEqual("Footnote1", ((IScrTxtPara)m_book.FootnotesOS[0].ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Footnote2", ((IScrTxtPara)m_book.FootnotesOS[1].ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("Original footnote at end of current para",
				((IScrTxtPara)m_book.FootnotesOS[2].ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests MoveText when there are footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveText_WithFootnotes()
		{
			// Add Scripture sections with footnotes.
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara paraSrc = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(paraSrc, 1, 1, "verse one.");
			int iStartVerseTwo = paraSrc.Contents.Length;
			AddVerse(paraSrc, 0, 2, "verse two.");
			AddFootnote(m_book, paraSrc, iStartVerseTwo + 1, "footnote one");
			AddFootnote(m_book, paraSrc, paraSrc.Contents.Length, "footnote two");

			// Add another Scripture section
			section = AddSectionToMockedBook(m_book);
			IScrTxtPara paraDst = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(paraDst, 0, 3, "verse three.");
			AddFootnote(m_book, paraDst, paraDst.Contents.Length, "footnote three.");
			Assert.AreEqual(3, m_book.FootnotesOS.Count);

			// Move verse two to the next section.
			paraDst.MoveText(0, paraSrc, iStartVerseTwo, paraSrc.Contents.Length);

			// We expect that all the footnotes will be present and in the second section.
			Assert.AreEqual(3, m_book.FootnotesOS.Count);
			Assert.AreEqual("11verse one.", paraSrc.Contents.Text);
			Assert.AreEqual("2" + StringUtils.kChObject + "verse two." + StringUtils.kChObject + "3verse three."
				+ StringUtils.kChObject, paraDst.Contents.Text);
		}
		#endregion

		#region RemoveOwnedObjectsForString Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing one owned footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveOwnedObjectsForString_Simple()
		{
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = "Normal";
			paraBldr.AppendRun("Test Paragraph",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			IStTxtPara para = paraBldr.CreateParagraph(m_currentText);

			AddFootnote(m_book, para, 10, null);
			Assert.AreEqual(1, m_currentFootnotesOS.Count);

			ReflectionHelper.CallMethod(para, "RemoveOwnedObjectsForString", 5, 12);

			Assert.AreEqual(0, m_currentFootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing two footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveOwnedObjectsForString_TwoFootnotes()
		{
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = "Normal";
			paraBldr.AppendRun("Test Paragraph",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			IStTxtPara para = paraBldr.CreateParagraph(m_currentText);
			AddFootnote(m_book, para, 8, null);
			AddFootnote(m_book, para, 10, null);
			Assert.AreEqual(2, m_currentFootnotesOS.Count);

			ReflectionHelper.CallMethod(para, "RemoveOwnedObjectsForString", 5, 12);

			Assert.AreEqual(0, m_currentFootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing two footnotes which are referenced in the back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveOwnedObjectsForString_FootnotesWithBT()
		{
			IStTxtPara para = m_currentText[0];
			// Add footnotes to existing paragraph.
			IStFootnote footnote1 = AddFootnote(m_book, para, 6, "Footnote1");
			IStFootnote footnote2 = AddFootnote(m_book, para, 10, "Footnote2");
			Assert.AreEqual(2, m_currentFootnotesOS.Count);

			// add two back translations of the para and footnotes
			ICmTranslation trans;
			int[] wsBt = new int[] { m_wsEn, m_wsDe };
			foreach (int ws in wsBt)
			{
				// add back translation of the para
				trans = AddBtToMockedParagraph(para, ws);
				AddRunToMockedTrans(trans, ws, "BT of test paragraph", null);
				// add BT footnotes
				AddBtFootnote(trans, 2, ws, footnote1, "BT of footnote1");
				AddBtFootnote(trans, 6, ws, footnote2, "BT of footnote2");
				Assert.AreEqual("BT" + StringUtils.kChObject + " of" + StringUtils.kChObject + " test paragraph",
					trans.Translation.get_String(ws).Text); // confirm that ORCs were inserted in BTs
			}

			ReflectionHelper.CallMethod(para, "RemoveOwnedObjectsForString", 5, 12);

			Assert.AreEqual(0, m_currentFootnotesOS.Count);

			// We expect that the ORCs would have also been removed from both back translations.
			trans = para.GetBT();
			Assert.IsNotNull(trans);
			foreach (int ws in wsBt)
				Assert.AreEqual("BT of test paragraph", trans.Translation.get_String(ws).Text);
		}
		#endregion

		#region String splitting Tests
		/// <summary>
		/// Test that splitting a paragraph before a footnote marker correctly associates
		/// that footnote with the new paragraph and doesn't delete the marker. TE-9530.
		/// </summary>
		[Test]
		public void SplitParagraphBeforeFootnoteOrc()
		{
			// The main current application of this is a complex delete followed by inserting a character, so try that.
			IStTxtPara paraOrig = m_currentText[0];
			paraOrig.Contents = Cache.TsStrFactory.MakeString("First stinkin' Para", Cache.DefaultVernWs);
			IStFootnote footnote1 = AddFootnote(m_book, paraOrig, 14, "Footnote text");

			IScrTxtParaFactory paraFactory = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>();
			IScrTxtPara paraNew = paraFactory.CreateWithStyle(m_currentText, 1, ScrStyleNames.NormalParagraph);

			Cache.DomainDataByFlid.MoveString(paraOrig.Hvo, StTxtParaTags.kflidContents, 0, 5, paraOrig.Contents.Length,
						paraNew.Hvo, StTxtParaTags.kflidContents, 0, 0, true);
			Assert.AreEqual(" stinkin'" + StringUtils.kChObject + " Para", paraNew.Contents.Text);
			Assert.IsTrue(footnote1.IsValidObject);
		}
		#endregion

		#region CreateOwnedObjects Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of one owned footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_Footnote()
		{
			AddFootnote(m_arcBook, m_archivedText[0], 0, null);
			IScrTxtPara para = CopyObject<IScrTxtPara>.CloneFdoObject((IScrTxtPara)m_archivedText[0],
				x => m_currentText.ParagraphsOS.Add(x));

			ReflectionHelper.CallMethod(para, "CreateOwnedObjects", 0, 1);

			Assert.AreEqual(1, m_currentFootnotesOS.Count);
			VerifyFootnote(m_currentFootnotesOS[0], para, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of multiple owned footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_MultipleFootnotesStartAt0()
		{
			AddFootnote(m_arcBook, m_archivedText[0], 0, null);
			AddFootnote(m_arcBook, m_archivedText[0], 1, null);
			AddFootnote(m_arcBook, m_archivedText[0], 2, null);
			IScrTxtPara para = CopyObject<IScrTxtPara>.CloneFdoObject((IScrTxtPara)m_archivedText[0],
				x => m_currentText.ParagraphsOS.Add(x));

			ReflectionHelper.CallMethod(para, "CreateOwnedObjects", 0, 3);
			Assert.AreEqual(3, m_currentFootnotesOS.Count);

			IStFootnote testFootnote = m_currentFootnotesOS[0];
			VerifyFootnote(testFootnote, para, 0);

			testFootnote = m_currentFootnotesOS[1];
			VerifyFootnote(testFootnote, para, 1);

			testFootnote = m_currentFootnotesOS[2];
			VerifyFootnote(testFootnote, para, 2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of multiple owned footnotes, which are between existing footnotes.
		/// The previous footnote is also in a previous paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_MultipleFootnotesStartInMiddle()
		{
			// Set up the current book with a second paragraph
			IStTxtPara para1 = m_currentText[0];
			IStTxtPara para2 = AddParaToMockedText(m_currentText, "Normal");
			AddRunToMockedPara(para2, "This is the paragraph of the second section " +
				"of the first chapter of Genesis. This is here so that we have enough characters " +
				"to insert footnotes into it.", null);

			// add a typical footnote near the start of para1
			IScrFootnote footnotePrev = AddFootnote(m_book, para1, 0, null);
			// add a typical footnote in the middle of para2
			IScrFootnote footnoteAfter = AddFootnote(m_book, para2, 20, null);

			// Add two footnotes to the archived book.
			// The ORCs are placed near the start of para2 in the current book.
			// This may seem wierd, but it simulates text that has just been copied from the archive book
			//  to para2, but the owned objects need to created for the current book with para2
			m_archivedFootnotesOS = m_arcBook.FootnotesOS;
			AddFootnote(m_arcBook, para2, 4, "footnote para2 ich 4");
			AddFootnote(m_arcBook, para2, 7, "footnote para2 ich 7");

			// call the code under test
			ReflectionHelper.CallMethod((IScrTxtPara)para2, "CreateOwnedObjects", 0, 10);

			Assert.AreEqual(4, m_currentFootnotesOS.Count);
			// Verify that the original footnotes are in the current book in the correct sequence.
			Assert.AreEqual(footnotePrev, m_currentFootnotesOS[0], "Previous footnote shouldn't have moved");
			Assert.AreEqual(footnoteAfter, m_currentFootnotesOS[3],
				"Following footnote should have gotten bumped two places");

			// Verify that the copied footnotes were created.
			VerifyFootnote(m_currentFootnotesOS[1], para2, 4);
			Assert.AreEqual("footnote para2 ich 4", ((IScrTxtPara)m_currentFootnotesOS[1].ParagraphsOS[0]).Contents.Text);
			VerifyFootnote(m_currentFootnotesOS[2], para2, 7);
			Assert.AreEqual("footnote para2 ich 7", ((IScrTxtPara)m_currentFootnotesOS[2].ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of an owned picture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_Picture()
		{
			IStTxtPara para = m_currentText[0];

			ITsString tss = para.Contents;
			ITsStrFactory factory = Cache.TsStrFactory;
			using (DummyFileMaker fileMaker = new DummyFileMaker("junk.jpg", true))
			{
				ICmPicture pict = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(fileMaker.Filename,
					factory.MakeString("Test picture", Cache.DefaultVernWs),
					CmFolderTags.LocalPictures);
				para.Contents = pict.InsertORCAt(tss, 0);
				tss = para.Contents;
				int cchOrigStringLength = tss.Length;

				ReflectionHelper.CallMethod((IScrTxtPara)para, "CreateOwnedObjects", 0, 1);

				tss = para.Contents;
				Assert.AreEqual(cchOrigStringLength, tss.Length);
				string sObjData = tss.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptObjData);
				Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));

				byte odt = Convert.ToByte(sObjData[0]);
				Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
				Assert.IsTrue(pict.Guid != guid, "New guid was not inserted");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of an ORC at the end of the paragraph (TE-3191)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_AtEnd()
		{
			IStTxtPara para = m_currentText[0];

			// We use m_archivedText with para from m_currentText to create a footnote.
			// This simulates a "paragraph with footnote" just copied from m_archivedText.
			// The important thing here is that we have a footnote that links to a
			// different owner.
			m_archivedFootnotesOS = m_arcBook.FootnotesOS;
			AddFootnote(m_arcBook, para, para.Contents.Length, null);
			int paraLen = para.Contents.Length;
			Assert.AreEqual(0, m_currentFootnotesOS.Count);

			ReflectionHelper.CallMethod((IScrTxtPara)para, "CreateOwnedObjects", paraLen - 1, paraLen);

			Assert.AreEqual(1, m_currentFootnotesOS.Count);

			VerifyFootnote(m_currentFootnotesOS[0], para, paraLen - 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of multiple ORCs, one at the end of the paragraph (TE-3191)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_MultipleAtEnd()
		{
			IStTxtPara para = m_currentText[0];

			// We use m_archivedText with para from m_currentText to create a footnote.
			// This simulates a "paragraph with footnote" just copied from m_archivedText.
			// The important thing here is that we have a footnote that links to a
			// different owner.
			AddFootnote(m_arcBook, para, 0, null);
			AddFootnote(m_arcBook, para, para.Contents.Length, null);
			int paraLen = para.Contents.Length;
			Assert.AreEqual(0, m_currentFootnotesOS.Count);

			ReflectionHelper.CallMethod((IScrTxtPara)para, "CreateOwnedObjects", 0, paraLen);

			Assert.AreEqual(2, m_currentFootnotesOS.Count);

			VerifyFootnote(m_currentFootnotesOS[0], para, 0);
			VerifyFootnote(m_currentFootnotesOS[1], para, paraLen - 1);
		}
		#endregion

		#region GetFootnotes Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFootnotes method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetFootnotes_One()
		{
			IScrTxtPara para = (IScrTxtPara)m_currentText[0];
			IStFootnote footnote = AddFootnote(m_book, para, 0, null);
			footnote[0].StyleName = "Anything";
			List<FootnoteInfo> footnotes = para.GetFootnotes();
			Assert.AreEqual(1, footnotes.Count);
			Assert.AreEqual(m_currentFootnotesOS[0], footnotes[0].footnote);
			Assert.AreEqual("Anything", footnotes[0].paraStylename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFootnotes method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetFootnotes_None()
		{
			IScrTxtPara para = (IScrTxtPara)m_currentText[0];
			List<FootnoteInfo > footnotes = para.GetFootnotes();
			Assert.AreEqual(0, footnotes.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFootnotes method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetFootnotes_More()
		{
			IScrTxtPara para = (IScrTxtPara)m_currentText[0];
			IFdoOwningSequence<IScrFootnote> footnotesOS = m_book.FootnotesOS;
			IScrFootnote footnote = AddFootnote(m_book, para, 0, null);
			footnote[0].StyleName = "Anything";
			footnote = AddFootnote(m_book, para, 5, null);
			footnote[0].StyleName = "Bla";
			footnote = AddFootnote(m_book, para, 10, null);
			footnote[0].StyleName = "hing";

			List<FootnoteInfo> footnotes = para.GetFootnotes();
			Assert.AreEqual(3, footnotes.Count);
			Assert.AreEqual(m_currentFootnotesOS[0], footnotes[0].footnote);
			Assert.AreEqual("Anything", footnotes[0].paraStylename);
			Assert.AreEqual(m_currentFootnotesOS[1], footnotes[1].footnote);
			Assert.AreEqual("Bla", footnotes[1].paraStylename);
			Assert.AreEqual(m_currentFootnotesOS[2], footnotes[2].footnote);
			Assert.AreEqual("hing", footnotes[2].paraStylename);
		}
		#endregion

		#region SetContainingPara tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ParaContainingOrc property on ScrFootnote gets set when there is a
		/// footnote ORC added to a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetContainingPara_SingleFootnoteAdded()
		{
			IScrTxtPara para = (IScrTxtPara)m_currentText.ParagraphsOS[0];
			IScrFootnote footnote = AddFootnote(m_book, para, para.Contents.Length);

			Assert.AreEqual(para, footnote.ParaContainingOrcRA,
				"When a footnote caller is added to a para, its containing para should be set.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ParaContainingOrc property on ScrFootnote gets set when there are
		/// multiple footnote ORCs added to a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetContainingPara_MultipleFootnoteAdded()
		{
			IScrTxtPara para = (IScrTxtPara)m_currentText.ParagraphsOS[0];
			para.Contents = Cache.TsStrFactory.MakeString("Test data", Cache.DefaultVernWs);
			IScrFootnote footnote1 = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			IScrFootnote footnote2 = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			m_book.FootnotesOS.Add(footnote1);
			m_book.FootnotesOS.Add(footnote2);

			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Test data", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 0, 0, Cache.DefaultVernWs);
			TsStringUtils.InsertOrcIntoPara(footnote2.Guid, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 5, 5, Cache.DefaultVernWs);
			para.Contents = bldr.GetString();

			Assert.AreEqual(para, footnote1.ParaContainingOrcRA,
				"When a footnote caller is added to a para, its containing para should be set.");
			Assert.AreEqual(para, footnote2.ParaContainingOrcRA,
				"When a footnote caller is added to a para, its containing para should be set.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ParaContainingOrc property on ScrFootnote gets set when a footnote
		/// ORC gets replaced by another footnote ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetContainingPara_ReplaceFootnote()
		{
			IScrTxtPara para = (IScrTxtPara)m_currentText.ParagraphsOS[0];
			para.Contents = Cache.TsStrFactory.MakeString("Test data", Cache.DefaultVernWs);
			IScrFootnote originalFootnote = AddFootnote(m_book, para, 0);

			IScrFootnote newFootnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			m_book.FootnotesOS.Add(newFootnote);

			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Test data", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			TsStringUtils.InsertOrcIntoPara(newFootnote.Guid, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 0, 0, Cache.DefaultVernWs);
			para.Contents = bldr.GetString();

			Assert.AreEqual(para, newFootnote.ParaContainingOrcRA,
				"When a footnote caller is added to a para, its containing para should be set.");
			Assert.IsNull(originalFootnote.ParaContainingOrcRA,
				"When a footnote caller is removed from a para, its containing para should be removed.");
		}
		#endregion

		#region Helper methods to verify results
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify the cache.CopyObject method, copying a paragraph which has footnotes and back
		/// translation.  Results should be identical to using other copy methods.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyObject()
		{
			IStTxtPara paraRev = SetUpParagraphInArchiveWithFootnotesAndBT();

			// Now, call the method under test!
			IStTxtPara newPara = CopyObject<IStTxtPara>.CloneFdoObject(paraRev,
				x => m_currentText.ParagraphsOS.Add(x));

			VerifyCopiedPara(newPara);
			VerifyParagraphsAreDifferentObjects(paraRev, newPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method:
		/// Verify the given copied paragraph, including footnotes and back translation,
		/// plus any other fields deemed necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyCopiedPara(IStTxtPara newPara)
		{
			// Verify the para StyleRules
			Assert.AreEqual("Line 1", newPara.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify the para Contents
			Assert.AreEqual("11This" + StringUtils.kChObject + " is" + StringUtils.kChObject + " the previous version of the text.",
				newPara.Contents.Text);
			ITsString tssNewParaContents = newPara.Contents;
			Assert.AreEqual(7, tssNewParaContents.RunCount);
			AssertEx.RunIsCorrect(tssNewParaContents, 0, "1", "CharacterStyle1", Cache.DefaultVernWs, true);
			AssertEx.RunIsCorrect(tssNewParaContents, 1, "1", "CharacterStyle2", Cache.DefaultVernWs, true);
			AssertEx.RunIsCorrect(tssNewParaContents, 2, "This", null, Cache.DefaultVernWs, true);
			// Run #3 is ORC for footnote, checked below...
			AssertEx.RunIsCorrect(tssNewParaContents, 4, " is", null, Cache.DefaultVernWs, true);
			// Run #5 is ORC for footnote, checked below...
			AssertEx.RunIsCorrect(tssNewParaContents, 6, " the previous version of the text.", null, Cache.DefaultVernWs, true);

			// note: At this point, having done the Copyxx() but not CreateOwnedObjects(),
			//  the ORCs still refer to footnote objects owned by m_archivedText...
			IScrFootnote footnote1 = (IScrFootnote)m_archivedFootnotesOS[0];
			VerifyFootnote(footnote1, newPara, 6);
			Assert.AreEqual("Footnote1", footnote1[0].Contents.Text);
			IScrFootnote footnote2 = (IScrFootnote)m_archivedFootnotesOS[1];
			VerifyFootnote(footnote2, newPara, 10);
			Assert.AreEqual("Footnote2", footnote2[0].Contents.Text);
			// ...thus the footnotes are not yet in m_currentFootnotesOS.
			Assert.AreEqual(0, m_currentFootnotesOS.Count);

			// Verify the para translations
			Assert.AreEqual(1, newPara.TranslationsOC.Count); //only 1 translation, the BT
			ICmTranslation paraTrans = newPara.GetBT();
			// verify each alternate translation
			int[] wsBt = new int[] { m_wsEn, m_wsDe };
			foreach (int ws in wsBt)
			{
				ITsString tssBtParaContents = paraTrans.Translation.get_String(ws);
				Assert.AreEqual("BT" + StringUtils.kChObject + " of" + StringUtils.kChObject +
					" test paragraph" + ws.ToString(), tssBtParaContents.Text);
				Assert.AreEqual(5, tssBtParaContents.RunCount);
				// could check every run too, but we'll skip that
				Assert.AreEqual(BackTranslationStatus.Finished.ToString(),
					paraTrans.Status.get_String(ws).Text);
			}

			// Verify the footnote translations, their ORCs, and their status
			foreach (int ws in wsBt)
			{
				FdoTestHelper.VerifyBtFootnote(footnote1, newPara, ws, 2);
				ICmTranslation footnoteTrans = footnote1[0].GetBT();
				Assert.AreEqual("BT of footnote1 " + ws.ToString(),
					footnoteTrans.Translation.get_String(ws).Text);
				Assert.AreEqual(BackTranslationStatus.Checked.ToString(),
					footnoteTrans.Status.get_String(ws).Text);

				FdoTestHelper.VerifyBtFootnote(footnote2, newPara, ws, 6);
				footnoteTrans = footnote2[0].GetBT();
				Assert.AreEqual("BT of footnote2 " + ws.ToString(),
					footnoteTrans.Translation.get_String(ws).Text);
				Assert.AreEqual(BackTranslationStatus.Finished.ToString(),
					footnoteTrans.Status.get_String(ws).Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method  verifies that the copy was sufficiently "deep":
		/// copied paragraphs are different objects, and that owner and owned objects
		/// are different.
		/// </summary>
		/// <param name="srcPara">The para rev.</param>
		/// <param name="newPara">The new para.</param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyParagraphsAreDifferentObjects(IStTxtPara srcPara, IStTxtPara newPara)
		{
			Assert.AreNotEqual(srcPara, newPara);
			// owned by different StTexts
			Assert.AreNotEqual(srcPara.Owner, newPara.Owner);
			// owning different back translations
			Assert.AreNotEqual(srcPara.GetBT(), newPara.GetBT());
		}
		#endregion
	}
	#endregion
}

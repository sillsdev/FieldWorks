// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScriptureSideEffectsTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ScriptureChangeWatcher
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScriptureSideEffectsTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private IScrBook m_book;
		#endregion

		#region Test setup

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to TestSetup to clear book filters between tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_book = AddBookToMockedScripture(40, "Matthew");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_book = null;

			base.TestTearDown();
		}
		#endregion

		#region Adjust section references
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is extended by one verse when a new verse is added at
		/// the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedVerseAtEnd()
		{

			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is verse one. ", null);

			// Test inserting an additional verse number in the paragraph
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);

			// verify the results
			Assert.AreEqual(40001001, section.VerseRefMin);
			Assert.AreEqual(40001002, section.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference of following sections are updated if a change to the end
		/// reference of one section spills over into the following one because of inserting
		/// a verse number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertCascadeChangeToNextsection()
		{
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "This is more text. ", null);
			AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse three. ", null);

			// Test inserting an additional verse number in the first paragraph
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001002, section1.VerseRefMax);
			Assert.AreEqual(40001002, section2.VerseRefMin);
			Assert.AreEqual(40001003, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference of following sections are not updated if following
		/// section starts with a new verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeleteCascadeNoChangeIfNextSectionStartsWithVerse()
		{
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is more text. ", null);
			AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse three. ", null);

			// Test inserting an additional verse number in the first paragraph
			AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001003, section1.VerseRefMax);
			Assert.AreEqual(40001002, section2.VerseRefMin);
			Assert.AreEqual(40001003, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference of following sections are updated if a change to the end
		/// reference of one section spills over into the following one because of deleting
		/// a verse number. This also tests deleting a verse from the end of a section updates
		/// the section reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeleteCascadeChangeToNextsection()
		{
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "This is more text. ", null);
			AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse three. ", null);

			// Test deleting the verse number at the end of para1
			ITsStrBldr strBuilder = para1.Contents.GetBldr();
			strBuilder.ReplaceRgch(21, 22, "", 0, para1.Contents.get_PropertiesAt(3)); // delete verse number 2
			para1.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefEnd);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001001, section2.VerseRefMin);
			Assert.AreEqual(40001001, section2.VerseRefStart);
			Assert.AreEqual(40001003, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is not changed while a person is typing a new verse
		/// at the end of a section. E.g. section is 1:1-25 and user intends to type verse 26.
		/// When just the "2" has been typed, don't change the section ref to 1:1-2 before
		/// the "6" is typed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not sure if we want this rqmt. Expensive to do, and not a critical bug right now.")]
		public void AdjustSectionReferences_InsertedPartialVerseNumAtEnd()
		{
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is verse one. ", null);
			AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is verse twentyfive. ", null);

			// Test inserting an additional verse number in the paragraph
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);

			// verify the results
			Assert.AreEqual(40001001, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is not changed when a out of order verse number is added
		/// at the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedBadVerseNumAtEnd()
		{
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is verse one. ", null);
			AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is verse twentyfive. ", null);

			// Test inserting an additional verse number in the paragraph
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);

			// verify the results
			Assert.AreEqual(40001001, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
			Assert.AreEqual(40001001, section.VerseRefStart);
			Assert.AreEqual(40001002, section.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is not changed when a out of order verse number is added
		/// at the beginning.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedBadVerseNumAtBeginning()
		{
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is verse one. ", null);
			AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is verse twentyfive. ", null);

			// Test inserting an extra digit into the first verse number making it 224
			ITsStrBldr strBuilder = para.Contents.GetBldr();
			// replace the current verse number with 224 to simulate editing
			strBuilder.ReplaceRgch(0, 2, "224", 3, para.Contents.get_PropertiesAt(0));
			para.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001001, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
			Assert.AreEqual(40001001, section.VerseRefStart);
			Assert.AreEqual(40001025, section.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure verse reference on section is not reset to 0 when the text is deleted from
		/// the only paragraph in the section. To simulate deleting using BKSP, we need to delete
		/// all but the first character of the para, issue a prop changed, then delete the first
		/// character. Jira # is TE-2364.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_ParaContentsDeleted()
		{
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "2", ScrStyleNames.VerseNumber);

			// Test deleting the verse number at the start of para2
			ITsStrBldr strBuilder = para2.Contents.GetBldr();
			strBuilder.ReplaceRgch(0, 1, "", 0, para1.Contents.get_PropertiesAt(3));
			para2.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001001, section2.VerseRefMin);
			Assert.AreEqual(40001001, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure verse reference on section resets to the end ref of the following section
		/// when the all but the first and last characters of the paragraph are deleted, leaving
		/// only a verse #2 and a period in the para. Jira # is TE-2364.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This problem with section reference range is corrected when user corrects verse numbers")]
		public void AdjustSectionReferences_TrimFirstVerseToSingleDigit()
		{
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "23", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is more text. ", null);
			AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse three.", null);

			// Test deleting the verse number at the start of para2
			ITsStrBldr strBuilder = para2.Contents.GetBldr();
			int cch = strBuilder.Length;
			strBuilder.ReplaceRgch(1, cch - 1, "", 0,
				para2.Contents.get_Properties(1));
			para2.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001023, section1.VerseRefMin);
			Assert.AreEqual(40001023, section1.VerseRefMax);
			Assert.AreEqual(40001023, section2.VerseRefMin);
			Assert.AreEqual(40001023, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure verse reference on section is correct when paragraph begins with a verse
		/// bridge and has no other verse references in the para. Jira # is TE-2364.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_ExtendVerseRangeAtBeginning()
		{
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is a verse. ", null);


			// set up for the test: extend verse 24 to be a verse bridge
			// verse 25 is the last verse for chapter 2, so the recorded range will be 24-25
			ITsStrBldr strBuilder = para.Contents.GetBldr();
			strBuilder.Replace(2, 2, "-26", para.Contents.get_Properties(0));
			para.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001024, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
			Assert.AreEqual(40001024, section.VerseRefStart);
			Assert.AreEqual(40001025, section.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is extended by one verse when a new verse is added at
		/// the beginning of the paragraph of the first section of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedVerseAtBeginning_BeginOfBook()
		{
			// add section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is verse one. ", null);
			AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This is verse twentyfive. ", null);

			// Test inserting an additional verse in the paragraph
			ITsStrBldr strBuilder = para.Contents.GetBldr();
			strBuilder.ReplaceRgch(0, 0, "13", 2, para.Contents.get_Properties(0));
			string sVerseText = "This is a new verse. ";
			strBuilder.ReplaceRgch(2, 2, sVerseText, sVerseText.Length,
				para.Contents.get_Properties(1));
			para.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001013, section.VerseRefMin);
			Assert.AreEqual(40001025, section.VerseRefMax);
			Assert.AreEqual(40001013, section.VerseRefStart);
			Assert.AreEqual(40001025, section.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is extended by one verse when a new verse is added at
		/// the beginning of a paragraph in a non-first section of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedVerseAtBeginning_MiddleOfBook()
		{
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse one. ", null);
			AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse twentyfive. ", null);

			// Test inserting an additional verse in the first paragraph of the second section
			ITsStrBldr strBuilder = para2.Contents.GetBldr();
			strBuilder.ReplaceRgch(0, 0, "14", 2, para2.Contents.get_Properties(0));
			string sVerseText = "This is a new verse. ";
			strBuilder.ReplaceRgch(2, 2, sVerseText, sVerseText.Length,
				para2.Contents.get_Properties(1));
			para2.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001014, section2.VerseRefMin);
			Assert.AreEqual(40001025, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is reduced by one verse when a verse is deleted at
		/// the beginning.
		/// </summary>
		/// <remarks>When TE-3521 is done, we can change this test so that it uses only one
		/// section.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeletedVerseAtBeginning()
		{
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse one. ", null);
			AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse twentyfive. ", null);

			// Test deleting the first verse of the second para
			ITsStrBldr strBuilder = para2.Contents.GetBldr();
			strBuilder.ReplaceRgch(0, 21, "", 0,
				para2.Contents.get_Properties(1));
			para2.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001025, section2.VerseRefMin);
			Assert.AreEqual(40001025, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is expanded by one verse when a verse number is deleted
		/// at the beginning.
		/// </summary>
		/// <remarks>When TE-3521 is done, we can change this test so that it uses only one
		/// section.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeletedVerseNumAtBeginning()
		{
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse one. ", null);
			AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse twentyfive. ", null);

			// Test deleting the first verse number of the second para
			ITsStrBldr strBuilder = para2.Contents.GetBldr();
			strBuilder.ReplaceRgch(0, 2, "", 0,
				para2.Contents.get_Properties(1));
			para2.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001001, section2.VerseRefMin);
			Assert.AreEqual(40001025, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the chapter numbers of the section references are changed if a chapter
		/// number is deleted at the beginning of the section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_DeletedChapterNumAtBeginning()
		{
			// add section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse one. ", null);
			AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse twentyfive. ", null);

			// Test deleting the chapter number from the second para
			ITsStrBldr strBuilder = para2.Contents.GetBldr();
			strBuilder.ReplaceRgch(0, 1, "", 0,
				para2.Contents.get_Properties(1));
			para2.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40001001, section1.VerseRefMin);
			Assert.AreEqual(40001001, section1.VerseRefMax);
			Assert.AreEqual(40001024, section2.VerseRefMin);
			Assert.AreEqual(40001025, section2.VerseRefMax);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the chapter numbers of the section references are updated when a
		/// chapter number is changed at the beginning of the section.
		/// </summary>
		/// <remarks>When TE-3521 is done, we can get rid of section0.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_ChangedChapterNumAtBeginning()
		{
			// add section and paragraph
			IScrSection section0 = AddSectionToMockedBook(m_book);
			IScrTxtPara para0 = AddParaToMockedSectionContent(section0,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para0, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para0, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para0, "This is verse one. ", null);

			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "This is verse one. ", null);

			IScrSection section2 = AddSectionToMockedBook(m_book);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse one. ", null);
			AddRunToMockedPara(para2, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "This is verse twentyfive. ", null);

			// Test deleting the first verse number of the second para
			ITsStrBldr strBuilder = para1.Contents.GetBldr();
			strBuilder.ReplaceRgch(0, 1, "5", 1,
				para1.Contents.get_Properties(0));
			para1.Contents = strBuilder.GetString();

			// verify the results
			Assert.AreEqual(40005001, section1.VerseRefMin);
			Assert.AreEqual(40005001, section1.VerseRefMax);
			Assert.AreEqual(40005001, section1.VerseRefStart);
			Assert.AreEqual(40005001, section1.VerseRefEnd);

			Assert.AreEqual(40005024, section2.VerseRefMin);
			Assert.AreEqual(40005025, section2.VerseRefMax);
			Assert.AreEqual(40005024, section2.VerseRefStart);
			Assert.AreEqual(40005025, section2.VerseRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure section reference is not changed when a new Scripture section is inserted
		/// between last intro section and first Scripture section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustSectionReferences_InsertedScriptureSectionAfterIntro()
		{
			// add intro section and paragraph
			IScrSection section0 = AddSectionToMockedBook(m_book);
			IScrTxtPara para0 = AddParaToMockedSectionContent(section0,
				ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para0, "Intro para.", null);

			// add scripture section and paragraph
			IScrSection section1 = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1, "This is the verse.", null);

			// Create IScrTxtPara to be tested, and load it from database.
			IScrSection newSection =
				Cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateEmptySection(m_book, 1);

			Assert.AreEqual(40001001, newSection.VerseRefMin);
			Assert.AreEqual(40001001, newSection.VerseRefMax);
		}

		#endregion

		#region BT tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets marked as unfinished when the vernacular
		/// paragraph is edited
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MarkBtAsUnfinishedOnVernacularEdit()
		{
			// add scripture section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "This is the verse.", null);

			// Create a back translation paragraph.
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation btpara = AddBtToMockedParagraph(para, wsBT);
			AddRunToMockedTrans(btpara, wsBT, "My Back Translation", null);

			// set the state of the back translation to "finished".
			btpara.Status.SetAnalysisDefaultWritingSystem(BackTranslationStatus.Finished.ToString());

			// change the vernacular paragraph
			ITsStrBldr strBuilder = para.Contents.GetBldr();
			strBuilder.ReplaceRgch(0, 1, "5", 1, para.Contents.get_Properties(0));
			para.Contents = strBuilder.GetString();

			// make sure the back translation is changed to "unfinished".
			string checkState = BackTranslationStatus.Unfinished.ToString();
			Assert.AreEqual(checkState, btpara.Status.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation is left unchanged when the vernacular paragraph is
		/// "edited" in such a way that nothing actually changes. For example, the change
		/// watcher gets called when the user presses ENTER at the end of the para, even though
		/// nothing is inserted or deleted in the para. TE-4970
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LeaveBtStatusUnchangedIfNothingInParaChanged()
		{
			// add scripture section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "This is the verse.", null);

			// Create a back translation paragraph.
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation btpara = AddBtToMockedParagraph(para, wsBT);
			AddRunToMockedTrans(btpara, wsBT, "My Back Translation", null);

			// set the state of the back translation to "finished".
			btpara.Status.SetAnalysisDefaultWritingSystem(BackTranslationStatus.Finished.ToString());

			// make sure the back translation is unchanged.
			string checkState = BackTranslationStatus.Finished.ToString();
			Assert.AreEqual(checkState, btpara.Status.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets marked as unfinished when the BT paragraph is
		/// edited.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MarkBtAsUnfinishedOnBtEdit()
		{
			// add scripture section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "This is the verse.", null);

			// Create a back translation paragraph.
			int wsBT = Cache.DefaultAnalWs;
			ICmTranslation btpara = AddBtToMockedParagraph(para, wsBT);
			AddRunToMockedTrans(btpara, wsBT, "My Back Translation", null);

			// set the state of the back translation to "checked".
			btpara.Status.SetAnalysisDefaultWritingSystem(BackTranslationStatus.Checked.ToString());

			// change the back translation paragraph
			btpara.Translation.SetAnalysisDefaultWritingSystem("Your Back Translation");

			// make sure the back translation is changed to "unfinished".
			Assert.AreEqual(BackTranslationStatus.Unfinished.ToString(),
				btpara.Status.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets created when a vernacular paragraph is
		/// added to the section content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateBtWhenContentParagraphIsAdded()
		{
			// add scripture section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);

			// make sure the back translation is created.
			ICmTranslation bt = para.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets created when a mutliple vernacular paragraphs
		/// are pasted over an existing paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateBtsWhenContentParagraphIsReplacedByMultipleOtherParagraphs()
		{
			// add scripture section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para1 = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			IScrTxtPara para2 = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			IScrTxtPara para3 = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);

			// make sure the back translations got created.
			ICmTranslation bt = para1.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem.Text);

			bt = para2.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem.Text);

			bt = para3.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets created when a vernacular paragraph is
		/// added to the section heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateBtWhenHeadingParagraphIsAdded()
		{
			// add scripture section and paragraph
			IScrSection section = AddSectionToMockedBook(m_book);
			IScrTxtPara para = AddSectionHeadParaToSection(section, "Intro to Genesis or whatever",
				ScrStyleNames.IntroSectionHead);

			// make sure the back translation is created.
			ICmTranslation bt = para.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the back translation gets created when a vernacular paragraph is
		/// added to the book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateBtWhenBookTitleParagraphIsAdded()
		{
			// add title paragraphs
			IStText title = AddTitleToMockedBook(m_book, "Genesis or whatever");
			IStTxtPara para2 = AddParaToMockedText(title, "An anthology");
			AddParaToMockedText(title, "Written by God");
			AddParaToMockedText(title, "For Israel and everyone else");

			// Make sure the back translations got created.
			ICmTranslation bt = para2.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem.Text);

			bt = para2.GetBT();
			Assert.IsNotNull(bt);
			Assert.IsNull(bt.Status.AnalysisDefaultWritingSystem.Text);
		}
		#endregion
	}
}

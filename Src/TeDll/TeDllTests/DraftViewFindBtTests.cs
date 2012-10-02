// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewFindBtTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DraftViewFindBtTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FindBtTests : DraftViewTestBase
	{
		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			CreatePartialExodusBT(Cache.DefaultAnalWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to create the draft view showing back translation
		/// data or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool CreateBtDraftView
		{
			get { return true; }
		}
		#endregion

		#region Find previous unfinished BT tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// the title of the first book doesn't move the IP.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_BeginningOfTitle()
		{
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// the first section head of a book - should go to the book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_SectionHeadToTitle()
		{
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
				0, 0, 0);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// the first section head of a book and the book title is marked as finished -
		/// shouldn't move the IP.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_SectionHeadToTitle_TitleFinished()
		{
			ICmTranslation trans = m_exodus.TitleOA[0].GetOrCreateBT();
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
				0, 0, 0);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// the first section head of a book and the book title is marked as checked -
		/// shouldn't move the IP.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_SectionHeadToTitle_TitleChecked()
		{
			ICmTranslation trans = m_exodus.TitleOA[0].GetOrCreateBT();
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Checked);

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
				0, 0, 0);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// the first para of a section - should go to the section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_SectionContentsToSectionHead()
		{
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidHeading,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// the first para of a section and the section head is marked as finished - should
		/// go to the book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_SectionContentsToSectionHead_HeadFinished()
		{
			IScrSection section = m_exodus.SectionsOS[0];
			ICmTranslation trans = section.HeadingOA[0].GetOrCreateBT();
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// a paragraph of a section - should go to previous paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_SectionParaToPara()
		{
			m_draftView.SetInsertionPoint(0, 1, 2, 0, false);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidContent,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// a paragraph of a section and the previous paragraph is marked as finished - should
		/// go to the section heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_SectionParaToPara_PrevParaFinished()
		{
			IScrSection section = m_exodus.SectionsOS[1];
			ICmTranslation trans = section.ContentOA[0].GetOrCreateBT();
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_draftView.SetInsertionPoint(0, 1, 1, 0, false);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidHeading,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// the title of the second book brings us to the last para of the first book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_SecondBookTitleToFirstBook()
		{
			// add new book with a BT
			IScrBook book = AddBookToMockedScripture(3, "Leviticus");
			IStText text = AddTitleToMockedBook(book, "Title for Leviticus");
			AddBtToMockedParagraph(text[0],	Cache.DefaultAnalWs);

			// add BT for last para of first book
			IScrTxtPara para = (IScrTxtPara)m_exodus.SectionsOS[2].ContentOA[0];
			AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidContent,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the previous unfinished back translation when we are in
		/// the title of the second book and the last paragraph of the previous book is marked
		/// as finished - should bring us to the heading of the last section of the first book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPrevBackTrans_SecondBookTitleToFirstBook_LastParaFinished()
		{


			// add new book with a BT
			IScrBook book = AddBookToMockedScripture(3, "Leviticus");
			IStText text = AddTitleToMockedBook(book, "Title for Leviticus");
			AddBtToMockedParagraph(text[0],	Cache.DefaultAnalWs);

			// add BT for the last section heading of the first book
			IScrTxtPara para = (IScrTxtPara)m_exodus.SectionsOS[2].HeadingOA[0];
			AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			// add BT for last para of first book
			para = (IScrTxtPara) m_exodus.SectionsOS[2].ContentOA[0];
			ICmTranslation trans = AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Finished);
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);

			m_draftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidHeading,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}
		#endregion

		#region Find next unfinished BT tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the next unfinished back translation when we are in
		/// the title of the first book - should go to the first section head
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextBackTrans_TitleToSectionHead()
		{
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);

			m_draftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidHeading,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the next unfinished back translation when we are in
		/// the title of the first book and the section head is marked finished - should go to
		/// the first paragraph in the contents
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextBackTrans_TitleToSectionHead_SectionHeadFinished()
		{
			IScrSection section = m_exodus.SectionsOS[0];
			ICmTranslation trans = section.HeadingOA[0].GetOrCreateBT();
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);

			m_draftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidContent,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the next unfinished back translation when we are in
		/// the title of the first book and the section head is marked checked - should go to
		/// the first paragraph in the contents
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextBackTrans_TitleToSectionHead_SectionHeadChecked()
		{
			IScrSection section = m_exodus.SectionsOS[0];
			ICmTranslation trans = section.HeadingOA[0].GetOrCreateBT();
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Checked);

			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);

			m_draftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidContent,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the next unfinished back translation when we are in
		/// a section head - should go to the first paragraph in the section's content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextBackTrans_SectionHeadToContent()
		{
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 0, 0);

			m_draftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidContent,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the next unfinished back translation when we are in
		/// a section head and the first content para is already marked as finished - should go
		/// to the second paragraph in the section's content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextBackTrans_SectionHeadToContent_ContentFinished()
		{
			IScrSection section = m_exodus.SectionsOS[1];
			ICmTranslation trans = section.ContentOA[0].GetOrCreateBT();
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
				0, 1, 0);

			m_draftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidContent,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the next unfinished back translation when we are in
		/// the last para in a book - should go to the next book's title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextBackTrans_SectionContentToTitle()
		{
			// add new book
			IScrBook book = AddBookToMockedScripture(3, "Leviticus");
			IStText text = AddTitleToMockedBook(book, "Title for Leviticus");
			// add BT for last para of first book
			IScrTxtPara para = (IScrTxtPara)m_exodus.SectionsOS[2].ContentOA[0];
			AddBtToMockedParagraph(para, Cache.DefaultAnalWs);

			// add BT for title para of the last book
			para = (IScrTxtPara)text[0];
			AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			m_draftView.RefreshDisplay();

			int iLastSectionInExodus = m_exodus.SectionsOS.Count - 1;
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidContent,
				0, iLastSectionInExodus,
				m_exodus.SectionsOS[iLastSectionInExodus].ContentOA.ParagraphsOS.Count -1);

			m_draftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the next unfinished back translation when we are in
		/// the last para in a book and the title of the next book is finished - should go to
		/// the first section head of next book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextBackTrans_SectionContentToNextBook_TitleFinished()
		{
			// add new book
			IScrBook book = AddBookToMockedScripture(3, "Leviticus");
			IStText text = AddTitleToMockedBook(book, "Title for Leviticus");
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "Heading", ScrStyleNames.SectionHead);
			// add BT for last para of first book
			IScrTxtPara para = (IScrTxtPara) m_exodus.SectionsOS[2].ContentOA[0];
			AddBtToMockedParagraph(para, Cache.DefaultAnalWs);

			// add BT for title para of the last book
			para = (IScrTxtPara)text[0];
			ICmTranslation trans = AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			// add BT for the first section head of the last book
			para = (IScrTxtPara)book.SectionsOS[0].HeadingOA[0];
			AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			m_draftView.RefreshDisplay();

			int iLastSectionInExodus = m_exodus.SectionsOS.Count - 1;
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidContent,
				0, iLastSectionInExodus,
				m_exodus.SectionsOS[iLastSectionInExodus].ContentOA.ParagraphsOS.Count -1);

			m_draftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidHeading,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the next unfinished back translation when we are in
		/// the last para in a section - should go to the next section's heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextBackTrans_SectionContentToHead()
		{
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidContent, 0, 0, 0);

			m_draftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidHeading,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that trying to go to the next unfinished back translation when we are in
		/// the last para in a section and the first heading para of the following section is
		/// finished - should go to the second heading para.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextBackTrans_SectionContentToHead_HeadFinished()
		{
			// Mark the first section head paragraph BT as finished and add a second section
			// head paragraph
			IScrSection section2 = m_exodus.SectionsOS[1];
			IStTxtPara para = section2.HeadingOA[0];
			ICmTranslation trans = para.GetOrCreateBT();
			m_draftView.SetTransStatus(trans, BackTranslationStatus.Finished);
			para = AddSectionHeadParaToSection(section2, "Heading 2B", ScrStyleNames.SectionHead);
			AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidContent,
				0, 0, 0);

			m_draftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidHeading,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_draftView.TeEditingHelper.InBookTitle);
		}
		#endregion
	}
}

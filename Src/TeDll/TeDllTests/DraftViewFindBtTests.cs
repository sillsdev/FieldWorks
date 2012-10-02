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
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.TE;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DraftViewFindBtTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FindBtTests: TeTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_btDraftView;
		private IScrBook m_book;

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView(Cache, true);

			m_btDraftView = m_draftForm.DraftView;
			m_btDraftView.Width = 300;
			m_btDraftView.Height = 290;
			m_btDraftView.CallOnLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_btDraftView = null;
			m_draftForm.Close();
			m_draftForm = null;
			m_book = null;

			base.Exit();
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_draftForm != null)
				{
					m_draftForm.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftForm = null;
			m_btDraftView = null;
			m_book = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_book = CreateExodusData();
			// TODO (TimS): if we ever need tests to have more data write another method and
			// use the current method for the current tests.
			CreatePartialExodusBT(Cache.DefaultAnalWs);
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
			CheckDisposed();

			m_btDraftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			m_btDraftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 0, 0);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			CmTranslation trans = new CmTranslation(Cache,
				((StTxtPara)m_book.TitleOA.ParagraphsOS[0]).TranslationsOC.HvoArray[0]);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_btDraftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 0, 0);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			CmTranslation trans = new CmTranslation(Cache,
				((StTxtPara)m_book.TitleOA.ParagraphsOS[0]).TranslationsOC.HvoArray[0]);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Checked);

			m_btDraftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 0, 0);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			m_btDraftView.SetInsertionPoint(0, 0, 0, 0, false);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			IScrSection section = m_book.SectionsOS[0];
			ICmTranslation trans = new CmTranslation(Cache,
				((StTxtPara)section.HeadingOA.ParagraphsOS[0]).TranslationsOC.HvoArray[0]);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_btDraftView.SetInsertionPoint(0, 0, 0, 0, false);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			m_btDraftView.SetInsertionPoint(0, 1, 2, 0, false);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			IScrSection section = m_book.SectionsOS[1];
			ICmTranslation trans = new CmTranslation(Cache,
				((StTxtPara)section.ContentOA.ParagraphsOS[0]).TranslationsOC.HvoArray[0]);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_btDraftView.SetInsertionPoint(0, 1, 1, 0, false);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			// add new book with a BT
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
			StText text = m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Title for Leviticus");
			m_scrInMemoryCache.AddBtToMockedParagraph((StTxtPara)text.ParagraphsOS[0],
				Cache.DefaultAnalWs);

			// add BT for last para of first book
			ScrTxtPara para = new ScrTxtPara(Cache, m_book.SectionsOS[2].ContentOA.ParagraphsOS.HvoArray[0]);
			m_inMemoryCache.AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			m_btDraftView.RefreshDisplay();

			m_btDraftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			// add new book with a BT
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
			StText text = m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Title for Leviticus");
			m_scrInMemoryCache.AddBtToMockedParagraph((StTxtPara)text.ParagraphsOS[0],
				Cache.DefaultAnalWs);

			// add BT for the last section heading of the first book
			ScrTxtPara para = new ScrTxtPara(Cache, m_book.SectionsOS[2].HeadingOA.ParagraphsOS.HvoArray[0]);
			m_inMemoryCache.AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			// add BT for last para of first book
			para = new ScrTxtPara(Cache, m_book.SectionsOS[2].ContentOA.ParagraphsOS.HvoArray[0]);
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Finished);
			m_btDraftView.RefreshDisplay();

			m_btDraftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);

			m_btDraftView.CallPrevUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			m_btDraftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0);

			m_btDraftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			IScrSection section = m_book.SectionsOS[0];
			ICmTranslation trans = new CmTranslation(Cache,
				((StTxtPara)section.HeadingOA.ParagraphsOS[0]).TranslationsOC.HvoArray[0]);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_btDraftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0);

			m_btDraftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			IScrSection section = m_book.SectionsOS[0];
			ICmTranslation trans = new CmTranslation(Cache,
				((StTxtPara)section.HeadingOA.ParagraphsOS[0]).TranslationsOC.HvoArray[0]);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Checked);

			m_btDraftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0);

			m_btDraftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			m_btDraftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 0, 0);

			m_btDraftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			IScrSection section = m_book.SectionsOS[1];
			ICmTranslation trans = new CmTranslation(Cache,
				((StTxtPara)section.ContentOA.ParagraphsOS[0]).TranslationsOC.HvoArray[0]);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			m_btDraftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 1, 0);

			m_btDraftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			// add new book
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
			StText text = m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Title for Leviticus");
			// add BT for last para of first book
			ScrTxtPara para = new ScrTxtPara(Cache, m_book.SectionsOS[2].ContentOA.ParagraphsOS.HvoArray[0]);
			m_inMemoryCache.AddBtToMockedParagraph(para, Cache.DefaultAnalWs);

			// add BT for title para of the last book
			para = new ScrTxtPara(Cache, text.ParagraphsOS.HvoArray[0]);
			m_inMemoryCache.AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			m_btDraftView.RefreshDisplay();

			int iLastSectionInExodus = m_book.SectionsOS.Count - 1;
			m_btDraftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent,
				0, iLastSectionInExodus,
				m_book.SectionsOS[iLastSectionInExodus].ContentOA.ParagraphsOS.Count -1);

			m_btDraftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(1, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.IsTrue(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			// add new book
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
			StText text = m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Title for Leviticus");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Heading",
				ScrStyleNames.SectionHead);
			// add BT for last para of first book
			ScrTxtPara para = new ScrTxtPara(Cache, m_book.SectionsOS[2].ContentOA.ParagraphsOS.HvoArray[0]);
			m_inMemoryCache.AddBtToMockedParagraph(para, Cache.DefaultAnalWs);

			// add BT for title para of the last book
			para = new ScrTxtPara(Cache, text.ParagraphsOS.HvoArray[0]);
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Finished);

			// add BT for the first section head of the last book
			para = new ScrTxtPara(Cache, book.SectionsOS[0].HeadingOA.ParagraphsOS.HvoArray[0]);
			m_inMemoryCache.AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			m_btDraftView.RefreshDisplay();

			int iLastSectionInExodus = m_book.SectionsOS.Count - 1;
			m_btDraftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent,
				0, iLastSectionInExodus,
				m_book.SectionsOS[iLastSectionInExodus].ContentOA.ParagraphsOS.Count -1);

			m_btDraftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(1, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			m_btDraftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent,
				0, 0, 0);

			m_btDraftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
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
			CheckDisposed();

			// Mark the first section head paragraph BT as finished and add a second section
			// head paragraph
			IScrSection section2 = m_book.SectionsOS[1];
			StTxtPara para = (StTxtPara)section2.HeadingOA.ParagraphsOS[0];
			ICmTranslation trans = new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			m_btDraftView.SetTransStatus(trans, BackTranslationStatus.Finished);
			para = m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo, "Heading 2B",
				ScrStyleNames.SectionHead);
			m_inMemoryCache.AddBtToMockedParagraph(para, Cache.DefaultAnalWs);
			m_btDraftView.RefreshDisplay();

			m_btDraftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent,
				0, 0, 0);

			m_btDraftView.CallNextUnfinishedBackTrans();
			SelectionHelper helper = m_btDraftView.EditingHelper.CurrentSelection;

			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_btDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_btDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_btDraftView.ParagraphIndex);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				m_btDraftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsFalse(m_btDraftView.TeEditingHelper.InBookTitle);
		}
		#endregion
	}
}

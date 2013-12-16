// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeEditingHelper_NavigationTests.cs
// Responsibility: TE Team

using System.Diagnostics;
using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.DomainServices;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE.TeEditingHelpers
{
	#region TeDummyBasicView
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class TeDummyBasicView : DummyBasicView, ITeView
	{
		private LocationTrackerImpl m_locationTracker;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TE editing helper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeEditingHelper TeEditingHelper
		{
			get { return EditingHelper as TeEditingHelper; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper used for processing editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			return new DummyTeEditingHelper(m_fdoCache, this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the view constructor.
		/// </summary>
		/// <param name="flid">The flid - ignored here.</param>
		/// ------------------------------------------------------------------------------------
		protected override VwBaseVc CreateVc(int flid)
		{
			m_locationTracker = new LocationTrackerImpl(m_fdoCache, 0);

			DraftViewVc vc = new DraftViewVc(TeStVc.LayoutViewTarget.targetDraft, 0, m_styleSheet, false);
			vc.Cache = m_fdoCache;
			return vc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the OnLayout methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void CallLayout()
		{
			CreateHandle();
			base.CallLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reconstruct the rootbox. The real code does it only if the view is visible, so this
		/// doesn't work for the tests where we don't display the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool RefreshDisplay()
		{
			m_rootb.Reconstruct();
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a root box and initializes it with appropriate data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			MakeRoot(m_fdoCache.LangProject.TranslatedScriptureOA.Hvo, /* not relevant */ -1,
				(int)ScrFrags.kfrScripture);
		}

		#region ITeView Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location tracker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILocationTracker LocationTracker
		{
			get { return m_locationTracker; }
		}
		#endregion
	}
	#endregion

	#region Navigation tests with one rootbox for multiple books
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests navigation methods in TeEditingHelper
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class NavigationTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private TeDummyBasicView m_draftView;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to start an undoable UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

			Debug.Assert(m_draftView == null);
			m_draftView = new TeDummyBasicView();
			m_draftView.Cache = Cache;
			m_draftView.Visible = false;
			m_draftView.StyleSheet = styleSheet;

			m_draftView.Width = 300;
			m_draftView.Height = 290;
			m_draftView.CallLayout();

			var exod = CreateExodusData();
			var levi = CreateLeviticusData();
			((TeEditingHelper)m_draftView.EditingHelper).BookFilter.Add(new IScrBook[] { exod, levi });

			m_actionHandler.EndUndoTask(); // This should cause a PropChanged to update the view
			m_actionHandler.BeginUndoTask("Test", "Test");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_draftView.CloseRootBox();
			m_draftView.Dispose();
			m_draftView = null;

			base.TestTearDown();
		}

		#region Data related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book (Leviticus) with 2 sections with the following layout:
		/// Leviticus
		/// Heading 1
		/// (1)1Verse one.
		/// Heading 2
		/// (2)1Verse one.2Verse two.
		/// (empty heading)
		/// (3)1Verse one.
		///
		/// Numbers in () are chapter numbers.
		/// </summary>
		/// <returns>the book of Leviticus for testing</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateLeviticusData()
		{
			return CreateBook(3, "Leviticus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book with 2 sections with the following layout:
		/// bookName
		/// Heading 1
		/// (1)1Verse one.
		/// Heading 2
		/// (2)1Verse one.2Verse two.
		/// (3)1Verse one.
		///
		/// Numbers in () are chapter numbers.
		/// </summary>
		/// <returns>the book for testing</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateBook(int nBookNumber, string bookName)
		{
			IScrBook book = AddBookToMockedScripture(nBookNumber, bookName);
			AddTitleToMockedBook(book, bookName);
			IScrSection section1 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section1, "Heading 1", ScrStyleNames.SectionHead);
			IStTxtPara para11 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Verse one.", null);

			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Heading 2", ScrStyleNames.SectionHead);
			IStTxtPara para21 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para21, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para21, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse one.", null);
			AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse two.", null);
			IStTxtPara para22 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para22, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para22, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para22, "Verse one.", null);

			return book;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that several properties of the draft view's current selection match the
		/// specified properties.
		/// </summary>
		/// <param name="levelCount"></param>
		/// <param name="iBook"></param>
		/// <param name="iSection"></param>
		/// <param name="tag"></param>
		/// <param name="ichAnchor"></param>
		/// <param name="ichEnd"></param>
		/// ------------------------------------------------------------------------------------
		private void VerifySelection(int levelCount, int iBook, int iSection, int tag,
			int ichAnchor, int ichEnd)
		{
			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(levelCount, selHelper.NumberOfLevels);

			// Check the book
			Assert.AreEqual(iBook, selHelper.LevelInfo[levelCount - 1].ihvo);

			if (levelCount > 3)
			{
				// Check the section
				Assert.AreEqual(iSection, selHelper.LevelInfo[2].ihvo);
			}

			Assert.AreEqual(tag, selHelper.LevelInfo[1].tag);
			Assert.AreEqual(ichAnchor, selHelper.IchAnchor);
			Assert.AreEqual(ichEnd, selHelper.IchEnd);
		}
		#endregion

		#region Goto book tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToFirstBook()
		{
			IFdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;

			// Set up to start in the the second to last book.
			int iBook = (books.Count > 1 ? books.Count - 2 : 0);
			IScrBook book = books[iBook];
			// Set up to start in the last section of the the second to last book.
			int iSection = (book.SectionsOS.Count > 0 ? book.SectionsOS.Count - 1 : 0);

			// Start in the title of the second to last book and goto the first book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrBookTags.kflidTitle, iBook, 0);
			m_draftView.TeEditingHelper.GoToFirstBook();
			VerifySelection(3, 0, 0, ScrBookTags.kflidTitle, 0, 0);

			// Start in the last section's heading of the second to last book and
			// goto the first book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidHeading,
				iBook, iSection);
			m_draftView.TeEditingHelper.GoToFirstBook();
			VerifySelection(3, 0, 0, ScrBookTags.kflidTitle, 0, 0);

			// Start in the last section's contents of the second to last book and
			// goto the first book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				iBook, iSection);
			m_draftView.TeEditingHelper.GoToFirstBook();
			VerifySelection(3, 0, 0, ScrBookTags.kflidTitle, 0, 0);

			// At this point, we should already be in the first book. Goto the contents of
			// the last section in the book and verify the IP moves to the book's title.
			book = books[0];
			iSection = (book.SectionsOS.Count > 0 ? book.SectionsOS.Count - 1 : 0);

			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				0, iSection);
			m_draftView.TeEditingHelper.GoToFirstBook();
			VerifySelection(3, 0, 0, ScrBookTags.kflidTitle, 0, 0);

			// We should already be in the first books title. Goto the first book and verify
			// we didn't move.
			m_draftView.TeEditingHelper.GoToFirstBook();
			VerifySelection(3, 0, 0, ScrBookTags.kflidTitle, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToPrevBook()
		{
			IFdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;

			// Set up to start in the the second to last book.
			int iBook = (books.Count > 1 ? books.Count - 1 : 0);

			m_draftView.TeEditingHelper.SetInsertionPoint(ScrBookTags.kflidTitle, iBook, 0);
			for (int i = books.Count - 2; i >= 0; i--)
			{
				// Goto previous book and verify we moved.
				m_draftView.TeEditingHelper.GoToPrevBook();
				VerifySelection(3, i, 0, ScrBookTags.kflidTitle, 0, 0);
			}

			// The difference between the two loops is that this one starts at the first section
			// of the content of the book
			for (int i = books.Count - 1; i > 0; i--)
			{
				// Goto the first section's contents.
				m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
					i, 0);
				// Goto previous book and verify we moved
				m_draftView.TeEditingHelper.GoToPrevBook();
				VerifySelection(3, i - 1, 0, ScrBookTags.kflidTitle, 0, 0);
			}

			// Goto the first section's contents in the first book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				0, 0);
			// Goto previous book and verify we didn't move
			m_draftView.TeEditingHelper.GoToPrevBook();
			VerifySelection(4, 0, 0, ScrSectionTags.kflidContent, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToNextBook()
		{
			IFdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;

			// Set up to start in the the second to last book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);
			for (int i = 1; i < books.Count - 1; i++)
			{
				// Goto next book and verify we moved.
				m_draftView.TeEditingHelper.GoToNextBook();
				VerifySelection(3, i, 0, ScrBookTags.kflidTitle, 0, 0);
			}

			// The difference between the two loops is that this one starts at the first section
			// of the content of the book
			for (int i = 0; i < books.Count - 2; i++)
			{
				// Goto the first section's contents.
				m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
					i, 0);
				// Goto next book and verify we moved
				m_draftView.TeEditingHelper.GoToNextBook();
				VerifySelection(3, i + 1, 0, ScrBookTags.kflidTitle, 0, 0);
			}

			// Goto the last section's contents in the first book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				books.Count - 1, 0);
			// Goto previous book and verify we didn't move
			m_draftView.TeEditingHelper.GoToNextBook();
			VerifySelection(4, books.Count - 1, 0,
				ScrSectionTags.kflidContent, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToLastBook()
		{
			IFdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;

			int lastBook = books.Count - 1;

			// Set up to start in the the second to last book.
			int iBook = (books.Count > 1 ? books.Count - 2 : 0);
			IScrBook book = books[iBook];
			// Set up to start in the last section of the the second to last book.
			int iSection = (book.SectionsOS.Count > 0 ? book.SectionsOS.Count - 1 : 0);

			// Start in the title of the second to last book and goto the last book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrBookTags.kflidTitle, iBook, 0);
			m_draftView.TeEditingHelper.GoToLastBook();
			VerifySelection(3, lastBook, 0, ScrBookTags.kflidTitle, 0, 0);

			// Start in the last section's heading of the second to last book and
			// goto the last book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidHeading,
				iBook, iSection);
			m_draftView.TeEditingHelper.GoToLastBook();
			VerifySelection(3, lastBook, 0, ScrBookTags.kflidTitle, 0, 0);

			// Start in the last section's contents of the second to last book and
			// goto the last book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				iBook, iSection);
			m_draftView.TeEditingHelper.GoToLastBook();
			VerifySelection(3, lastBook, 0, ScrBookTags.kflidTitle, 0, 0);

			// Start in the last section's contents of the first book and goto the last book.
			book = books[0];
			iSection = (book.SectionsOS.Count > 0 ? book.SectionsOS.Count - 1 : 0);
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				0, iSection);
			m_draftView.TeEditingHelper.GoToLastBook();
			VerifySelection(3, lastBook, 0, ScrBookTags.kflidTitle, 0, 0);

			// At this point, we should already be in the last book. Goto the contents of
			// the last section in the book and verify the IP moves to the book's title.
			book = books[lastBook];
			iSection = (book.SectionsOS.Count > 0 ? book.SectionsOS.Count - 1 : 0);

			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				lastBook, iSection);
			m_draftView.TeEditingHelper.GoToLastBook();
			VerifySelection(3, lastBook, 0, ScrBookTags.kflidTitle, 0, 0);

			// We should already be in the last books title. Goto the last book and verify
			// we didn't move.
			m_draftView.TeEditingHelper.GoToLastBook();
			VerifySelection(3, lastBook, 0, ScrBookTags.kflidTitle, 0, 0);
		}
		#endregion

		#region Goto section tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TeEditingHelper.GoToFirstSection"/> feature.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToFirstSection()
		{
			IFdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;

			// Start in the title of the second book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);

			m_draftView.TeEditingHelper.GoToFirstSection();

			VerifySelection(4, 1, 0, ScrSectionTags.kflidHeading, 0, 0);

			// Start in the last section head of the last book.
			IScrBook book = books[books.Count - 1];
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidHeading,
				books.Count - 1, book.SectionsOS.Count - 1);

			m_draftView.TeEditingHelper.GoToFirstSection();

			VerifySelection(4, books.Count - 1, 0,
				ScrSectionTags.kflidHeading, 0, 0);

			// Start in the last para of the contents of the first section of the first book.
			book = books[0];
			IScrSection firstSection = book.SectionsOS[0];
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, firstSection.ContentOA.ParagraphsOS.Count - 1,
				2, true);

			m_draftView.TeEditingHelper.GoToFirstSection();

			int targetTag = firstSection.HeadingOA.ParagraphsOS.Count > 0 ?
				ScrSectionTags.kflidHeading :
				ScrSectionTags.kflidContent;
			VerifySelection(4, 0, 0, targetTag, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TeEditingHelper.GoToPrevSection"/> feature.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToPrevSection()
		{
			IFdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;

			// Start in the title of the third book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);
			int iTargBook = 0;
			IScrBook targetBook = books[iTargBook];

			m_draftView.TeEditingHelper.GoToPrevSection();

			VerifySelection(4, iTargBook, targetBook.SectionsOS.Count - 1,
				ScrSectionTags.kflidHeading, 0, 0);

			// Start in the last section head of the first book.
			IScrBook startBook = books[0];
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidHeading,
				books.Count - 1, startBook.SectionsOS.Count - 1);

			m_draftView.TeEditingHelper.GoToPrevSection();

			VerifySelection(4, 0, startBook.SectionsOS.Count - 2,
				ScrSectionTags.kflidHeading, 0, 0);

			// Start in the last para of the contents of the first section of the first book.
			startBook = books[0];
			IScrSection firstSection = startBook.SectionsOS[0];
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, firstSection.ContentOA.ParagraphsOS.Count - 1,
				2, true);

			// Nothing should move
			m_draftView.TeEditingHelper.GoToPrevSection();

			VerifySelection(4, 0, 0, ScrSectionTags.kflidContent, 2, 2);

			// Start with a multi-text range selection
			startBook = ((IScrBook)books[0]);
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidHeading,
				0, startBook.SectionsOS.Count - 1);
			IVwSelection vwsel1 = m_draftView.RootBox.Selection;
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				0, startBook.SectionsOS.Count - 2);
			IVwSelection vwsel2 = m_draftView.RootBox.Selection;
			m_draftView.RootBox.MakeRangeSelection(vwsel1, vwsel2, true);

			m_draftView.TeEditingHelper.GoToPrevSection();

			VerifySelection(4, 0, startBook.SectionsOS.Count - 3,
				ScrSectionTags.kflidHeading, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TeEditingHelper.GoToNextSection"/> feature.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToNextSection()
		{
			IFdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;

			// Start in the title of the second book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);

			m_draftView.TeEditingHelper.GoToNextSection();

			VerifySelection(4, 1, 0, ScrSectionTags.kflidHeading, 0, 0);

			// Start in the last section of the first book.
			IScrBook book = books[0];
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				0, book.SectionsOS.Count - 1);

			m_draftView.TeEditingHelper.GoToNextSection();

			VerifySelection(4, 1, 0, ScrSectionTags.kflidHeading, 0, 0);

			// Start in the last para of the contents of the last section of the last book.
			int iBook = books.Count - 1;
			book = books[iBook];
			int iSection = book.SectionsOS.Count - 1;
			IScrSection lastSection = book.SectionsOS[iSection];
			int iPara = lastSection.ContentOA.ParagraphsOS.Count - 1;
			m_draftView.TeEditingHelper.SetInsertionPoint(iBook, iSection, iPara, 2, true);

			// Nothing should move
			m_draftView.TeEditingHelper.GoToNextSection();

			VerifySelection(4, iBook, iSection, ScrSectionTags.kflidContent,
				2, 2);

			// Check the para
			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(iPara, selHelper.LevelInfo[0].ihvo);

			// Start with a multi-text range selection
			IScrBook startBook = books[0];
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidHeading,
				0, startBook.SectionsOS.Count - 2);
			IVwSelection vwsel1 = m_draftView.RootBox.Selection;
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidContent,
				0, startBook.SectionsOS.Count - 3);
			IVwSelection vwsel2 = m_draftView.RootBox.Selection;
			m_draftView.RootBox.MakeRangeSelection(vwsel1, vwsel2, true);

			m_draftView.TeEditingHelper.GoToNextSection();

			VerifySelection(4, 0, startBook.SectionsOS.Count - 1,
				ScrSectionTags.kflidHeading, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TeEditingHelper.GoToLastSection"/> feature.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToLastSection()
		{
			IFdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;

			// Start in the title of the second book.
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);
			IScrBook book = books[1];

			m_draftView.TeEditingHelper.GoToLastSection();

			VerifySelection(4, 1, book.SectionsOS.Count - 1,
				ScrSectionTags.kflidHeading, 0, 0);

			// Start in the last section head of the last book.
			book = books[books.Count - 1];
			m_draftView.TeEditingHelper.SetInsertionPoint(ScrSectionTags.kflidHeading,
				books.Count - 1, book.SectionsOS.Count - 1);

			m_draftView.TeEditingHelper.GoToLastSection();

			VerifySelection(4, books.Count - 1, book.SectionsOS.Count - 1,
				ScrSectionTags.kflidHeading, 0, 0);

			// Start in the last para of the contents of the second section of the first book.
			int iTargBook = 0;
			book = books[iTargBook];
			int iStartsection = 1;
			IScrSection secondSection = book.SectionsOS[iStartsection];
			m_draftView.TeEditingHelper.SetInsertionPoint(iTargBook, iStartsection,
				secondSection.ContentOA.ParagraphsOS.Count - 1, 2, true);

			m_draftView.TeEditingHelper.GoToLastSection();

			VerifySelection(4, iTargBook, book.SectionsOS.Count - 1,
				ScrSectionTags.kflidHeading, 0, 0);
		}
		#endregion

		#region Goto chapter tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TeEditingHelper.GetPrevChapter"/> feature.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToPrevChapter()
		{
			// Leviticus 1:1
			// Beginning of Leviticus, now we jump into the end of Exodus.
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(3, 1, 1, m_scr.Versification));
			Assert.AreEqual(new ScrReference(2, 1, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetPrevChapter());

			// Exodus 1:1
			// Already at the start of the first book, nowhere special to go.
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 5, m_scr.Versification));
			Assert.AreEqual(new ScrReference(2, 1, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetPrevChapter());

			// Leviticus 2:2
			// Yum-cha jump to the previous paragraph, nothing interesting to see
			// here folks, moving on.
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(3, 2, 2, m_scr.Versification));
			Assert.AreEqual(new ScrReference(3, 1, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetPrevChapter());

			// Leviticus 0:0
			// Start in the title of leviticus, jump to the previous chapter. (TE-8480)
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(3, 0, 0, m_scr.Versification));
			Assert.AreEqual(new ScrReference(2, 1, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetPrevChapter());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TeEditingHelper.GetNextChapter"/> feature.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToNextChapter()
		{
			// Leviticus 3:1
			// Already at the end, so go nowhere.
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(3, 3, 1, m_scr.Versification));
			Assert.AreEqual(new ScrReference(3, 3, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetNextChapter());

			// Exodus 1:5
			// Moving out of Exodus into the beginning of Leviticus.
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 5, m_scr.Versification));
			Assert.AreEqual(new ScrReference(3, 1, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetNextChapter());

			// Leviticus
			// Yum-cha jump from beginning of Leviticus 1 into Leviticus 2.
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(3, 1, 1, m_scr.Versification));
			Assert.AreEqual(new ScrReference(3, 2, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetNextChapter());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TeEditingHelper.GetLastChapter"/> feature.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToLastChapter()
		{
			// Leviticus 3:1
			// Already at the end, go nowhere.
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(3, 3, 1, m_scr.Versification));
			Assert.AreEqual(new ScrReference(3, 3, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetLastChapter());

			// Exodus 1:3
			// Only one chapter in this version of Exodus, go nowhere special.
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 3, m_scr.Versification));
			Assert.AreEqual(new ScrReference(2, 1, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetLastChapter());

			// Leviticus 1:1
			// Start of Leviticus, jump to the end of Leviticus
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(3, 1, 1, m_scr.Versification));
			Assert.AreEqual(new ScrReference(3, 3, 1, m_scr.Versification),
				m_draftView.TeEditingHelper.GetLastChapter());
		}
		#endregion
	}
	#endregion
}

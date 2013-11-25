// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportedBooksTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using System.Windows.Forms;
using SIL.Utils;
using System.Collections;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ImportedBooks class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ImportedBooksTests : ScrInMemoryFdoTestBase
	{
		#region DummyImportedBooks class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes methods of the ImportedBooks class for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DummyImportedBooks : ImportedBooks
		{
			#region Data members
			internal OverwriteType m_typeOfOverwrite = OverwriteType.DataLoss;
			internal List<IScrSection> m_sectionsToRemove = new List<IScrSection>();
			internal bool m_fSimulateUserConfirmationToOverwrite = true;
			internal string m_sOverwriteWillBlowAwayMergedBook;
			internal bool m_fPartialOverwriteWasCalled = false;
			#endregion

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DummyImportedBooks"/> class.
			/// </summary>
			/// <param name="cache">The cache.</param>
			/// <param name="booksImported">The books imported.</param>
			/// <param name="backupVersion">The backup version.</param>
			/// --------------------------------------------------------------------------------
			public DummyImportedBooks(FdoCache cache, IScrDraft booksImported,
				IScrDraft backupVersion) : base(cache, null, booksImported, 1.0f, 1.0f,
				backupVersion, cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(987),
				new Set<int>(booksImported.BooksOS.Select(b => b.CanonicalNum)), null, null)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the status of the specified item.
			/// </summary>
			/// <param name="item">The item (currently always 0).</param>
			/// --------------------------------------------------------------------------------
			public ImportedBookStatus GetStatus(int item)
			{
				return (ImportedBookStatus)lstImportedBooks.Items[item].Tag;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the selected listview items. This is needed to allow override for testing
			/// because lstBooksToMerge.SelectedItems doesn't contain any items unless the window
			/// is actually displayed.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			protected override IEnumerable SelectedBookItems
			{
				get
				{
					return lstImportedBooks.Items.Cast<ListViewItem>().Where(item => item.Selected);
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Simulates pressing the Overwrite button
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void SimulateOverwrite()
			{
				// The overwrite button just changes the status
				btnOverwrite_Click(null, null);
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Simulates pressing the Compare button
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void SimulateCompare()
			{
				// The first step after the user pressed the Compare button is to backup the
				// current book.
				BookMerger bookMerger = (BookMerger)lstImportedBooks.Items[0].Tag;
				ReflectionHelper.SetField(bookMerger, "m_origDiffCount", 893);
				m_backupVersion.AddBookCopy(bookMerger.BookCurr);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Determines the overwritability.
			/// </summary>
			/// <param name="bookMerger">The book merger.</param>
			/// <param name="sDetails">The details of what would be lost if the user overwrites.
			/// </param>
			/// <param name="sectionsToRemove">The sections to remove.</param>
			/// <param name="missingBtWs">The back translation writing systems in use
			/// in the Current book, but missing in the saved version.</param>
			/// <returns>The type of overwrite.</returns>
			/// ------------------------------------------------------------------------------------
			protected override OverwriteType DetermineOverwritability(BookMerger bookMerger,
				out string sDetails, out List<IScrSection> sectionsToRemove, out HashSet<int> missingBtWs)
			{
				sDetails = null;
				sectionsToRemove = m_sectionsToRemove;
				missingBtWs = new HashSet<int>();
				return m_typeOfOverwrite;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Confirms the overwrite of merged version.
			/// </summary>
			/// <returns><c>true</c></returns>
			/// --------------------------------------------------------------------------------
			protected override bool ConfirmOverwriteOfMergedVersion(string bookName)
			{
				m_sOverwriteWillBlowAwayMergedBook = bookName;
				return true;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Performs a partial overwrite of the book using the imported portion of the book.
			/// </summary>
			/// <param name="bookMerger">The book merger.</param>
			/// <param name="sectionsToRemove">The sections to remove as the first step (before
			/// calling the auto-merge code).</param>
			/// ------------------------------------------------------------------------------------
			protected override void PartialOverwrite(BookMerger bookMerger,
				List<IScrSection> sectionsToRemove)
			{
				Assert.AreEqual((BookMerger)lstImportedBooks.Items[0].Tag, bookMerger);
				Assert.AreEqual(m_sectionsToRemove, sectionsToRemove);
				m_fPartialOverwriteWasCalled = true;
			}
		}
		#endregion

		#region Member variables
		private IScrDraft m_savedVersion;
		private IScrDraft m_importedVersion;
		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

#pragma warning disable 219
			// We add a book (without a title)
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
#pragma warning restore 219

			// And an archived book (with a title)
			IScrBook archiveBook = AddArchiveBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(archiveBook, "Book of Genesis");
			m_importedVersion = (IScrDraft)archiveBook.Owner;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_savedVersion = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create("ImportedBooksTests");
		}
		#endregion

		#region Overwrite Tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests overwriting an existing book
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Overwrite()
		{
			AddSection(m_scr.ScriptureBooksOS[0], 1, 1, 15);
			AddSection(m_importedVersion.BooksOS[0], 1, 1, 15);

			// We have to end an Undo task because another one is within the btnOverwrite_Click method
			// called from SimulateOverwrite.
			m_actionHandler.EndUndoTask();

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				importedBooks.m_typeOfOverwrite = OverwriteType.FullNoDataLoss;
				importedBooks.SimulateOverwrite();
				Assert.AreEqual(ImportedBooks.ImportedBookStatus.Overwritten, importedBooks.GetStatus(0));
			}

			Assert.AreEqual("Book of Genesis", m_scr.ScriptureBooksOS[0].TitleOA[0].Contents.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests overwriting an existing book after the user has pressed Compare. This tests
		/// TE-7214.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void OverwriteAfterCompare()
		{
			AddSection(m_scr.ScriptureBooksOS[0], 1, 1, 15);
			AddSection(m_importedVersion.BooksOS[0], 1, 1, 15);

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				importedBooks.SimulateCompare();

				// We have to end an Undo task because another one is within the btnOverwrite_Click method
				// called from SimulateOverwrite.
				m_actionHandler.EndUndoTask();

				importedBooks.m_typeOfOverwrite = OverwriteType.FullNoDataLoss;
				importedBooks.SimulateOverwrite();
				Assert.AreEqual("Genesis", importedBooks.m_sOverwriteWillBlowAwayMergedBook);
				Assert.AreEqual(ImportedBooks.ImportedBookStatus.Overwritten, importedBooks.GetStatus(0));
			}

			Assert.AreEqual("Book of Genesis", m_scr.ScriptureBooksOS[0].TitleOA[0].Contents.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests partial overwrite of an existing book
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Overwrite_Partial()
		{
			AddSection(m_scr.ScriptureBooksOS[0], 1, 1, 15);
			AddSection(m_importedVersion.BooksOS[0], 1, 1, 15);

			// Add a new section to the current that is not in the revision so that a partial overwrite
			// will be done.
			AddSection(m_scr.ScriptureBooksOS[0], 2, 1, 20);

			// We have to end an Undo task because another one is within the btnOverwrite_Click method
			// called from SimulateOverwrite.
			m_actionHandler.EndUndoTask();

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				importedBooks.m_typeOfOverwrite = OverwriteType.Partial;
				importedBooks.SimulateOverwrite();
				Assert.AreEqual(0, ((List<IScrBook>)ReflectionHelper.GetField(importedBooks, "m_newBooks")).Count);
				Assert.AreEqual(0, ((List<IScrBook>)ReflectionHelper.GetField(importedBooks, "m_overwrittenBooks")).Count);
				Assert.AreEqual(ImportedBooks.ImportedBookStatus.Overwritten, importedBooks.GetStatus(0));
				Assert.IsTrue(importedBooks.m_fPartialOverwriteWasCalled);
			}
		}
		#endregion

		#region New Book Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the import dialog to verify that new books are correctly detected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewBookImported()
		{
			m_scr.ScriptureBooksOS.Clear();
#pragma warning disable 219
			IScrSection section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
#pragma warning restore 219

			// We have to end an Undo task because another one is within the ImportedBooks
			// constructor.
			m_actionHandler.EndUndoTask();

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				Assert.AreEqual(ImportedBooks.ImportedBookStatus.New, importedBooks.GetStatus(0));
				ReflectionHelper.CallMethod(importedBooks, "UpdateButtonStatus", (BookMerger)null);
				Button btnOverwrite = (Button)ReflectionHelper.GetField(importedBooks, "btnOverwrite");
				Assert.IsFalse(btnOverwrite.Enabled, "The overwrite button should  be disabled for new books.");
				Button btnCompare = (Button)ReflectionHelper.GetField(importedBooks, "btnCompare");
				Assert.IsFalse(btnCompare.Enabled, "The compare button should  be disabled for new books.");
			}
			Assert.AreEqual(1, m_scr.ScriptureBooksOS.Count);
		}
		#endregion

		#region Book name tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting book name when it is a full book.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetBookInfo_FullBookWithoutIntro()
		{
			IScrSection section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "first verse in Genesis");
			AddVerse(para, 50, 26, "last verse in Genesis");

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				string bookName = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
					"GetBookInfo", m_importedVersion.BooksOS[0]);
				Assert.AreEqual("1:1-50:26", bookName);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting book name when it is a full book with an introduction.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		// To whoever "we" is: MarkP said that, for the user, knowing that the intro material is present is as important as a range of Scripture.
		//[Ignore("We don't think it's very useful to indicate whether or not the imported book has an introduction.")]
		public void GetBookInfo_FullBookWithIntro()
		{
#pragma warning disable 219
			IScrSection introSection = AddSectionToMockedBook(m_importedVersion.BooksOS[0], true);
#pragma warning restore 219
			IScrSection section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "first verse in Genesis");
			AddVerse(para, 50, 26, "last verse in Genesis");

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				string bookName = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
					"GetBookInfo", m_importedVersion.BooksOS[0]);
				Assert.AreEqual("1:1-50:26 (with intro)", bookName);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting book name when the first portion is missing.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetBookInfo_FirstPartMissing()
		{
			IScrSection section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 2, "NOT the first verse in Genesis");
			AddVerse(para, 50, 26, "last verse in Genesis");

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				string bookName = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
					"GetBookInfo", m_importedVersion.BooksOS[0]);
				Assert.AreEqual("1:2-50:26", bookName);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting book name when the last portion is missing.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetBookInfo_LastPartMissing()
		{
			IScrSection section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "first verse in Genesis");
			AddVerse(para, 50, 25, "NOT the last verse in Genesis");

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				string bookName = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
					"GetBookInfo", m_importedVersion.BooksOS[0]);
				Assert.AreEqual("1:1-50:25", bookName);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting book name when the first portion is missing and their is an introduction.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		// To whoever "we" is: MarkP said that, for the user, knowing that the intro material is present is as important as a range of Scripture.
		//[Ignore("We don't think it's very useful to indicate whether or not the imported book has an introduction.")]
		public void GetBookInfo_FirstPartMissingWithIntro()
		{
#pragma warning disable 219
			IScrSection introSection = AddSectionToMockedBook(m_importedVersion.BooksOS[0], true);
#pragma warning restore 219
			IScrSection section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 2, "NOT the first verse in Genesis");
			AddVerse(para, 50, 25, "NOT the last verse in Genesis");

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				string bookName = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
					"GetBookInfo", m_importedVersion.BooksOS[0]);
				Assert.AreEqual("1:2-50:25 (with intro)", bookName);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting book name when their is only an introduction.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		// To whoever "we" is: MarkP said that, for the user, knowing that the intro material is present is as important as a range of Scripture.
		//[Ignore("We don't think it's very useful to indicate whether or not the imported book has an introduction.")]
		public void GetBookInfo_IntroOnly()
		{
#pragma warning disable 219
			IScrSection introSection = AddSectionToMockedBook(m_importedVersion.BooksOS[0], true);
#pragma warning restore 219

			using (DummyImportedBooks importedBooks = new DummyImportedBooks(Cache, m_importedVersion,
				m_savedVersion))
			{
				string bookName = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
					"GetBookInfo", m_importedVersion.BooksOS[0]);
				Assert.AreEqual("(intro only)", bookName);
			}
		}
		#endregion

		#region Tests for helper methods used in ConfirmBtOverwrite

		#region GetLanguageNames tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetLanguageNames when there is only one missing back translation language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLanguageNames_OneBtLanguage()
		{
			List<int> missingBtWs = new List<int>(new int[] {
				Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("es") });

			string language = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
				"GetLanguageNames",	Cache, missingBtWs);
			Assert.AreEqual("   Spanish" + Environment.NewLine, language);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetLanguageNames when there are multiple missing back translation languages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLanguageNames_MultiBtLanguages()
		{
			List<int> missingBtWs = new List<int>(new int[] {
				Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("es"),
				Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("en"),
				Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("de") });

			string languages = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
				"GetLanguageNames", Cache, missingBtWs);
			Assert.AreEqual("   Spanish" + Environment.NewLine + "   English" + Environment.NewLine +
				"   German" + Environment.NewLine, languages);
		}
		#endregion

		#region GetScriptureReferences tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetScriptureReferences method when there is one section that would be
		/// removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetScriptureReferences_OneSection()
		{
			IScrBook genesis = m_scr.ScriptureBooksOS[0];
			AddSection(genesis, 1, 1, 15);
			List<IScrSection> sectionList =
				new List<IScrSection>(new IScrSection[] { genesis.SectionsOS[0] } );

			string strReferences = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
				"GetScriptureReferences", sectionList);

			Assert.AreEqual("   1:1-15" + Environment.NewLine, strReferences);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetScriptureReferences method when there is one section that would be
		/// removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetScriptureReferences_WithIntro()
		{
			IScrBook genesis = m_scr.ScriptureBooksOS[0];
			IScrSection section = AddSectionToMockedBook(genesis, true);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 0, 0, "Intro text");
			AddSection(genesis, 1, 1, 15);
			List<IScrSection> sectionList =
				new List<IScrSection>(new IScrSection[] { genesis.SectionsOS[0], genesis.SectionsOS[1] });

			string strReferences = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
				"GetScriptureReferences", sectionList);

			Assert.AreEqual("   Intro" + Environment.NewLine + "   1:1-15" + Environment.NewLine,
				strReferences);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetScriptureReferences method when there is a continuous number (3) of
		/// sections that would be removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetScriptureReferences_ContinuousSections()
		{
			IScrBook genesis = m_scr.ScriptureBooksOS[0];
			AddSection(genesis, 1, 1, 31);
			AddSection(genesis, 2, 1, 3, 3);
			AddSection(genesis, 3, 4, 24);
			List<IScrSection> sectionList =	new List<IScrSection>(new IScrSection[]
				{ genesis.SectionsOS[0], genesis.SectionsOS[1], genesis.SectionsOS[2] });

			string strReferences = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
				"GetScriptureReferences", sectionList);

			Assert.AreEqual("   1:1-3:24" + Environment.NewLine, strReferences);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetScriptureReferences method when there are non contiguous sections that
		/// would be removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetScriptureReferences_SectionsWithGaps()
		{
			IScrBook genesis = m_scr.ScriptureBooksOS[0];
			AddSection(genesis, 1, 1, 31);
			AddSection(genesis, 3, 1, 24);
			AddSection(genesis, 5, 1, 32);
			List<IScrSection> sectionList = new List<IScrSection>(new IScrSection[]
				{ genesis.SectionsOS[0], genesis.SectionsOS[1], genesis.SectionsOS[2] });

			string strReferences = (string)ReflectionHelper.GetStrResult(typeof(ImportedBooks),
				"GetScriptureReferences", sectionList);

			Assert.AreEqual("   1:1-31" + Environment.NewLine + "   3:1-24" +	Environment.NewLine +
				"   5:1-32" + Environment.NewLine, strReferences);
		}
		#endregion

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a section with references within a single chapter.
		/// </summary>
		/// <param name="book">The book.</param>
		/// <param name="chapter">The chapter.</param>
		/// <param name="firstVerse">The first verse.</param>
		/// <param name="lastVerse">The last verse.</param>
		/// ------------------------------------------------------------------------------------
		private void AddSection(IScrBook book, int chapter, int firstVerse, int lastVerse)
		{
			AddSection(book, chapter, firstVerse, chapter, lastVerse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a section with references in the specified book.
		/// </summary>
		/// <param name="book">The book.</param>
		/// <param name="startChapter">The start chapter.</param>
		/// <param name="firstVerse">The first verse.</param>
		/// <param name="endChapter">The end chapter.</param>
		/// <param name="lastVerse">The last verse.</param>
		/// ------------------------------------------------------------------------------------
		private void AddSection(IScrBook book, int startChapter, int firstVerse, int endChapter,
			int lastVerse)
		{
			IScrSection section = AddSectionToMockedBook(book);
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, startChapter, firstVerse, "verse " + firstVerse.ToString() + ". ");
			AddVerse(para, (endChapter > startChapter) ? endChapter : 0,
				lastVerse, "verse " + lastVerse.ToString() + ". ");
		}
		#endregion
	}
}

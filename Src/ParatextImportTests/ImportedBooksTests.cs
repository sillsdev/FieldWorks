// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Scripture;

namespace ParatextImport
{
#if RANDYTODO
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ImportedBooks class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ImportedBooksTests : ScrInMemoryLcmTestBase
	{

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
			var genesis = AddBookToMockedScripture(1, "Genesis");
#pragma warning restore 219

			// And an archived book (with a title)
			var archiveBook = AddArchiveBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(archiveBook, "Book of Genesis");
			m_importedVersion = (IScrDraft)archiveBook.Owner;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the LCM cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_savedVersion = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create("ImportedBooksTests");
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
			var section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
#pragma warning restore 219

			// We have to end an Undo task because another one is within the ImportedBooks
			// constructor.
			m_actionHandler.EndUndoTask();
			var importedBooks = new Dictionary<int, bool>();
			importedBooks[m_importedVersion.BooksOS[0].CanonicalNum] = true;

			Assert.AreEqual(0, m_scr.ScriptureBooksOS.Count);
			ImportedBooks.SaveImportedBooks(Cache, m_importedVersion, m_savedVersion, importedBooks.Keys, null);
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
			var section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
			var para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "first verse in Genesis");
			AddVerse(para, 50, 26, "last verse in Genesis");
			//SUT
			Assert.AreEqual("1:1-50:26", ImportedBooks.GetBookInfo(m_importedVersion.BooksOS[0]));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting book name when it is a full book with an introduction.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetBookInfo_FullBookWithIntro()
		{
#pragma warning disable 219
			var introSection = AddSectionToMockedBook(m_importedVersion.BooksOS[0], true);
#pragma warning restore 219
			var section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
			var para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "first verse in Genesis");
			AddVerse(para, 50, 26, "last verse in Genesis");

			Assert.AreEqual("1:1-50:26 (with intro)", ImportedBooks.GetBookInfo(m_importedVersion.BooksOS[0]));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting book name when the first portion is missing.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetBookInfo_FirstPartMissing()
		{
			var section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
			var para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 2, "NOT the first verse in Genesis");
			AddVerse(para, 50, 26, "last verse in Genesis");

			Assert.AreEqual("1:2-50:26", ImportedBooks.GetBookInfo(m_importedVersion.BooksOS[0]));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting book name when the last portion is missing.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetBookInfo_LastPartMissing()
		{
			var section = AddSectionToMockedBook(m_importedVersion.BooksOS[0]);
			var para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "first verse in Genesis");
			AddVerse(para, 50, 25, "NOT the last verse in Genesis");

			Assert.AreEqual("1:1-50:25", ImportedBooks.GetBookInfo(m_importedVersion.BooksOS[0]));
		}
	#endregion

	#region Helper methods

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
			AddVerse(para, startChapter, firstVerse, "verse " + firstVerse + ". ");
			AddVerse(para, endChapter > startChapter ? endChapter : 0, lastVerse, "verse " + lastVerse + ". ");
		}
	#endregion
	}
#endif
}

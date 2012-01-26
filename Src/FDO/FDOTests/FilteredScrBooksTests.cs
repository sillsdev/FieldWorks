// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2004' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FilteredScrBooksTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FilteredScrBooks class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FilteredScrBooksTests : ScrInMemoryFdoTestBase
	{
		private FilteredScrBooks m_filter;
		private static int m_filterNum;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_filter = Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(m_filterNum++);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			AddBookToMockedScripture(1, "Genesis");
			AddBookToMockedScripture(2, "Exodus");
			AddBookToMockedScripture(3, "Leviticus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test verifies that an unfiltered instance of the class returns all books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnfilteredBooksTest()
		{
			int filteredCount = m_filter.BookCount;
			Assert.AreEqual(m_scr.ScriptureBooksOS.Count, filteredCount);
			for (int i = 0; i < m_scr.ScriptureBooksOS.Count; i++)
			{
				int bookHvo = m_filter.GetBook(i).Hvo;
				Assert.AreEqual(bookHvo, m_scr.ScriptureBooksOS[i].Hvo, "Mismatch on book " + i);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test verifies that filter is correctly updated and that accessor methods work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FilteredBooksTest()
		{
			IScrBook book = m_scr.ScriptureBooksOS[1];
			m_filter.FilteredBooks = new IScrBook[] {book};
			Assert.AreEqual(0, m_filter.GetBookIndex(book));
			Assert.AreEqual(-1, m_filter.GetBookIndex(m_scr.ScriptureBooksOS[0])); //invalid index
			Assert.AreEqual(-1, m_filter.GetBookIndex(m_scr.ScriptureBooksOS[2])); //invalid index
			Assert.AreEqual(book.Hvo, m_filter.GetBook(0).Hvo);
			Assert.AreEqual(book.Hvo, m_filter.GetBookByOrd(book.CanonicalNum).Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test for the GetUnfilteredIndex method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetUnfilteredIndexTest()
		{
			IScrBook book = m_scr.ScriptureBooksOS[1];
			m_filter.FilteredBooks = new IScrBook[] {book};
			Assert.AreEqual(1, m_filter.GetUnfilteredIndex(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Remove method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveTest()
		{
			m_filter.Remove(1);
			Assert.AreEqual(2, m_filter.BookCount);
			Assert.AreEqual(m_scr.ScriptureBooksOS[0].Hvo, m_filter.GetBook(0).Hvo);
			Assert.AreEqual(m_scr.ScriptureBooksOS[2].Hvo, m_filter.GetBook(1).Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test saving a filter and deleting a book removes it out of the saved filter list
		/// (TE-6087)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveRestoreFilterTest_DeleteBook1()
		{
			IScrBook book = m_scr.ScriptureBooksOS[1];
			m_filter.FilteredBooks = new IScrBook[] { book }; // saves the filter list
			m_filter.ShowAllBooks();
			m_scr.ScriptureBooksOS.Remove(book);

			// Restore the previously saved list. This should realize that the book no longer
			// exists and show all the books instead.
			Assert.AreEqual(0, m_filter.SavedFilter.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test saving a filter and deleting a book removes it out of the saved filter list
		/// when there is more then one book in the saved filter list (TE-6087)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SaveRestoreFilterTest_DeleteBook2()
		{
			IScrBook book1 = m_scr.ScriptureBooksOS[0];
			IScrBook book2 = m_scr.ScriptureBooksOS[1];
			m_filter.FilteredBooks = new IScrBook[] { book1, book2 }; // saves the filter list
			m_filter.ShowAllBooks();
			m_scr.ScriptureBooksOS.Remove(book1);

			// Restore the previously saved list. This should realize that the book no longer
			// exists and show all the books instead.
			m_filter.FilteredBooks = m_filter.SavedFilter;
			Assert.AreEqual(1, m_filter.BookCount);
			Assert.AreEqual(book2.Hvo, m_filter.GetBook(0).Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Add method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddTest()
		{
			m_filter.FilteredBooks = new IScrBook[] { };
			Assert.AreEqual(0, m_filter.BookCount);

			IScrBook bookGen = m_scr.ScriptureBooksOS[0];
			IScrBook bookExo = m_scr.ScriptureBooksOS[1];
			IScrBook bookLev = m_scr.ScriptureBooksOS[2];

			// try adding a null list of books
			m_filter.Add(null);
			Assert.AreEqual(0, m_filter.BookCount);

			// try adding a list containing a null book
			m_filter.Add(new IScrBook[] { null });
			Assert.AreEqual(0, m_filter.BookCount);

			// Add the middle book
			m_filter.Add(bookExo);
			Assert.AreEqual(1, m_filter.BookCount);
			Assert.AreEqual(bookExo.Hvo, m_filter.GetBook(0).Hvo);

			// Add a book that will go at the start of the list
			m_filter.Add(bookGen);
			Assert.AreEqual(2, m_filter.BookCount);
			Assert.AreEqual(bookGen.Hvo, m_filter.GetBook(0).Hvo);
			Assert.AreEqual(bookExo.Hvo, m_filter.GetBook(1).Hvo);

			// Add a book that will go at the end of the list
			m_filter.Add(bookLev);
			Assert.AreEqual(3, m_filter.BookCount);
			Assert.AreEqual(bookGen.Hvo, m_filter.GetBook(0).Hvo);
			Assert.AreEqual(bookExo.Hvo, m_filter.GetBook(1).Hvo);
			Assert.AreEqual(bookLev.Hvo, m_filter.GetBook(2).Hvo);

			// try to add one that already exists
			m_filter.Add(bookLev);
			Assert.AreEqual(3, m_filter.BookCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test verifies that filter is correctly updated and that accessor methods work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRefRangeForContiguousBooks_SingleBook()
		{
			m_filter.FilteredBooks = new[] { m_scr.ScriptureBooksOS[1] };
			ScrReference start, end;
			m_filter.GetRefRangeForContiguousBooks(out start, out end);
			Assert.AreEqual(02001001, start);
			Assert.AreEqual(02040038, end);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test verifies that filter is correctly updated and that accessor methods work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRefRangeForContiguousBooks_ContiguousBooks()
		{
			m_filter.FilteredBooks = new[] { m_scr.ScriptureBooksOS[1], m_scr.ScriptureBooksOS[2] };
			ScrReference start, end;
			m_filter.GetRefRangeForContiguousBooks(out start, out end);
			Assert.AreEqual(02001001, start);
			Assert.AreEqual(03027034, end);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test verifies that filter is correctly updated and that accessor methods work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRefRangeForContiguousBooks_DisjointBooks()
		{
			m_filter.FilteredBooks = new[] { m_scr.ScriptureBooksOS[0], m_scr.ScriptureBooksOS[2] };
			ScrReference start, end;
			m_filter.GetRefRangeForContiguousBooks(out start, out end);
			Assert.AreEqual(0, start.BBCCCVVV);
			Assert.AreEqual(m_scr.Versification, start.Versification);
			Assert.AreEqual(0, end.BBCCCVVV);
			Assert.AreEqual(m_scr.Versification, end.Versification);
		}
	}
}

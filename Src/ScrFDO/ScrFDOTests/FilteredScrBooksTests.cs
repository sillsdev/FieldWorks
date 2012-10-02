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
// File: FilteredScrBooksTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using NUnit.Framework;

namespace SIL.FieldWorks.FDO.Scripture
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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_filter = null; // TODO: Needs to implement IDisposable.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

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

			m_filter = new FilteredScrBooks(Cache, m_filterNum++);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test verifies that an unfiltered instance of the class returns all books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnfilteredBooksTest()
		{
			CheckDisposed();

			int filteredCount = Cache.GetVectorSize(m_scr.Hvo, m_filter.Tag);
			Assert.AreEqual(m_scr.ScriptureBooksOS.Count, filteredCount);
			for (int i = 0; i < m_scr.ScriptureBooksOS.Count; i++)
			{
				int bookHvo = Cache.GetVectorItem(m_scr.Hvo, m_filter.Tag, i);
				Assert.AreEqual(bookHvo, m_scr.ScriptureBooksOS.HvoArray[i], "Mismatch on book " + i);
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
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[1];
			m_filter.UpdateFilter(new int[] {book.Hvo});
			Assert.AreEqual(0, m_filter.GetBookIndex(book.Hvo));
			Assert.AreEqual(-1, m_filter.GetBookIndex(m_scr.ScriptureBooksOS[0].Hvo)); //invalid index
			Assert.AreEqual(-1, m_filter.GetBookIndex(m_scr.ScriptureBooksOS[2].Hvo)); //invalid index
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
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[1];
			m_filter.UpdateFilter(new int[] {book.Hvo});
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
			CheckDisposed();

			m_filter.Remove(1);
			Assert.AreEqual(2, m_filter.BookCount);
			Assert.AreEqual(m_scr.ScriptureBooksOS[0].Hvo, m_filter.GetBook(0).Hvo);
			Assert.AreEqual(m_scr.ScriptureBooksOS[2].Hvo, m_filter.GetBook(1).Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Insert method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertTest()
		{
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[1];
			m_filter.UpdateFilter(new int[] { book.Hvo });
			m_filter.Insert(0, m_scr.ScriptureBooksOS[0].Hvo);
			Assert.AreEqual(2, m_filter.BookCount);
			Assert.AreEqual(m_scr.ScriptureBooksOS[0].Hvo, m_filter.GetBook(0).Hvo);
			Assert.AreEqual(m_scr.ScriptureBooksOS[1].Hvo, m_filter.GetBook(1).Hvo);
			m_filter.UpdateFilter(new int[] {});
			Assert.AreEqual(0, m_filter.BookCount);
			m_filter.Insert(0, m_scr.ScriptureBooksOS[2].Hvo);
			Assert.AreEqual(1, m_filter.BookCount);
			Assert.AreEqual(m_scr.ScriptureBooksOS[2].Hvo, m_filter.GetBook(0).Hvo);
			m_filter.Insert(0, 123); // invalid book
			Assert.AreEqual(1, m_filter.BookCount);
			Assert.AreEqual(m_scr.ScriptureBooksOS[2].Hvo, m_filter.GetBook(0).Hvo);
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
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[1];
			m_filter.UpdateFilter(new int[] { book.Hvo }); // saves the filter list
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
			CheckDisposed();

			IScrBook book1 = m_scr.ScriptureBooksOS[0];
			IScrBook book2 = m_scr.ScriptureBooksOS[1];
			m_filter.UpdateFilter(new int[] { book1.Hvo, book2.Hvo }); // saves the filter list
			m_filter.ShowAllBooks();
			m_scr.ScriptureBooksOS.Remove(book1);

			// Restore the previously saved list. This should realize that the book no longer
			// exists and show all the books instead.
			m_filter.UpdateFilter(m_filter.SavedFilter);
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
			CheckDisposed();

			m_filter.UpdateFilter(new int[] { });
			Assert.AreEqual(0, m_filter.BookCount);

			IScrBook bookGen = m_scr.ScriptureBooksOS[0];
			IScrBook bookExo = m_scr.ScriptureBooksOS[1];
			IScrBook bookLev = m_scr.ScriptureBooksOS[2];

			// try adding a book HVO that does not exist
			m_filter.Add(12345678);
			Assert.AreEqual(0, m_filter.BookCount);

			// Add the middle book
			m_filter.Add(bookExo.Hvo);
			Assert.AreEqual(1, m_filter.BookCount);
			Assert.AreEqual(bookExo.Hvo, m_filter.GetBook(0).Hvo);

			// Add a book that will go at the start of the list
			m_filter.Add(bookGen.Hvo);
			Assert.AreEqual(2, m_filter.BookCount);
			Assert.AreEqual(bookGen.Hvo, m_filter.GetBook(0).Hvo);
			Assert.AreEqual(bookExo.Hvo, m_filter.GetBook(1).Hvo);

			// Add a book that will go at the end of the list
			m_filter.Add(bookLev.Hvo);
			Assert.AreEqual(3, m_filter.BookCount);
			Assert.AreEqual(bookGen.Hvo, m_filter.GetBook(0).Hvo);
			Assert.AreEqual(bookExo.Hvo, m_filter.GetBook(1).Hvo);
			Assert.AreEqual(bookLev.Hvo, m_filter.GetBook(2).Hvo);

			// try to add one that already exists
			m_filter.Add(bookLev.Hvo);
			Assert.AreEqual(3, m_filter.BookCount);
		}
	}
}

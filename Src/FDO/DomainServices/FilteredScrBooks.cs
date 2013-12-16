// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FilteredScrBooks.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	#region FilteredScrBooks class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements the IVwVirtualHandler interface to create a filtered sequence of books property
	/// for Scripture.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FilteredScrBooks : IEnumerable
	{
		#region Data Members
		/// <summary>Event to indicate a change in the filter</summary>
		public event EventHandler FilterChanged;

		private FdoCache m_cache;
		private IScripture m_scr;

		/// <summary>
		/// Current books in the filter. This is distinct from m_savedFilteredBooks int that
		/// it gets emptied whenever the book filter is turned off and filled whenever it's
		/// turned on.
		/// </summary>
		private List<IScrBook> m_filteredBooks;

		/// <summary>
		/// List of books in filter, even when filter isn't enabled. This value comes from the
		/// registry when an instance of FilterScrBooks is created and updated from the
		/// set book filter dialog.
		/// </summary>
		private List<IScrBook> m_savedFilteredBooks;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This constructor is for testing so the class can be mocked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected FilteredScrBooks()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredScrBooks"/> class.
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		internal FilteredScrBooks(FdoCache cache)
		{
			m_cache = cache;
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_filteredBooks = new List<IScrBook>(m_scr.ScriptureBooksOS);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets number of books currently included in the filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int BookCount
		{
			get
			{
				bool fFilterChanged = CheckListForDeletedBooks(ref m_filteredBooks);
				Debug.Assert(!fFilterChanged || FilterChanged == null, "We should probably notify listeners that the filter changed!");
				return m_filteredBooks.Count;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the filter includes all of the books in the database.  This is a
		/// "no-filter" condition.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllBooks
		{
			get
			{
				bool fFilterChanged = CheckListForDeletedBooks(ref m_filteredBooks);
				Debug.Assert(!fFilterChanged || FilterChanged == null, "We should probably notify listeners that the filter changed!");
				return (m_scr.ScriptureBooksOS.Count == BookCount);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of books in the saved filter as a list of books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrBook[] SavedFilter
		{
			get
			{
				CheckListForDeletedBooks(ref m_savedFilteredBooks);
				return m_savedFilteredBooks.ToArray();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of the book Ids (i.e. the canonical number) contained in the filter.
		/// If there are no books in the filter, then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> BookIds
		{
			get
			{
				Debug.Assert(m_filteredBooks != null);
				if (m_filteredBooks.Count == 0)
					return null;

				List<int> ids = new List<int>(m_filteredBooks.Count);
				ids.AddRange(m_filteredBooks.Select(book => book.CanonicalNum));
				return ids;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the filtered books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<IScrBook> FilteredBooks
		{
			get { return m_filteredBooks; }
			set
			{
				m_filteredBooks.Clear();
				Add(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of the book hvos contained in the filter. If there are no books in the
		/// filter, an empty list is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> BookHvos
		{
			get
			{
				bool fFilterChanged = CheckListForDeletedBooks(ref m_filteredBooks);
				Debug.Assert(!fFilterChanged || FilterChanged == null, "We should probably notify listeners that the filter changed!");

				List<int> hvos = new List<int>(m_filteredBooks.Count);
				hvos.AddRange(m_filteredBooks.Select(book => book.Hvo));
				return hvos;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tag. (This is temporary until we figure out how we're going to deal with
		/// book filters.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Tag
		{
			get { return ScriptureTags.kflidScriptureBooks; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the book filter to show only the specified books and create an undo action
		/// for the change.
		/// </summary>
		/// <param name="newFilterList">The new list of books to show in the filter.</param>
		/// <returns>A new undo action that can be used to undo/redo this change to the
		/// filter</returns>
		/// ------------------------------------------------------------------------------------
		public IUndoAction UpdateFilterAndCreateUndoAction(params IScrBook[] newFilterList)
		{
			if (newFilterList.Length == 0 && m_scr.ScriptureBooksOS.Count > 0)
				newFilterList = m_scr.ScriptureBooksOS.ToArray();
			UpdateBookFilterAction undoAction = new UpdateBookFilterAction(this, newFilterList);
			FilteredBooks = newFilterList;
			return undoAction;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method used to notify the book filter that books in the database have been deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void BooksDeleted()
		{
			CheckListForDeletedBooks(ref m_filteredBooks);
			// If the last book was deleted from the filter, then show all books; otherwise
			// simply notify the world of the change.
			if (m_filteredBooks.Count == 0)
				ShowAllBooks(); // This takes care of notification
			else
				NotifyFilterChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show all the books in the filter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ShowAllBooks()
		{
			if (!AllBooks)
				FilteredBooks = m_scr.ScriptureBooksOS;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book corresponding to the filtered index.
		/// </summary>
		/// <param name="index">Index into filtered books</param>
		/// <returns>Book corresponding to index</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook GetBook(int index)
		{
			bool fFilterChanged = CheckListForDeletedBooks(ref m_filteredBooks);
			Debug.Assert(!fFilterChanged || FilterChanged == null, "We should probably notify listeners that the filter changed!");

			Debug.Assert(index >= 0 && index < m_filteredBooks.Count);
			return m_filteredBooks[index];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of a book in the filtered list.
		/// </summary>
		/// <param name="book">The book</param>
		/// <returns>Index of book</returns>
		/// ------------------------------------------------------------------------------------
		public int GetBookIndex(IScrBook book)
		{
			bool fFilterChanged = CheckListForDeletedBooks(ref m_filteredBooks);
			Debug.Assert(!fFilterChanged || FilterChanged == null, "We should probably notify listeners that the filter changed!");

			return m_filteredBooks.IndexOf(book);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets book from list of filtered books by book ordinal.
		/// </summary>
		/// <param name="canonicalNum">Ordinal of book - <code>ScrBook.CanonicalNum</code></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook GetBookByOrd(int canonicalNum)
		{
			bool fFilterChanged = CheckListForDeletedBooks(ref m_filteredBooks);
			Debug.Assert(!fFilterChanged || FilterChanged == null, "We should probably notify listeners that the filter changed!");

			foreach (IScrBook book in m_filteredBooks)
			{
				if (book.CanonicalNum == canonicalNum)
					return book;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Swaps books in the filter.
		/// </summary>
		/// <param name="bookOld">The old book to be swapped out of the filter.</param>
		/// <param name="bookNew">The new book to be swapped into the filter.</param>
		/// ------------------------------------------------------------------------------------
		public void SwapBooks(IScrBook bookOld, IScrBook bookNew)
		{
			if (bookOld != null)
			{
				int index = GetBookIndex(bookOld);
				Remove(index);
			}
			if (bookNew != null)
				Add(bookNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove a book from the filter
		/// </summary>
		/// <param name="bookIndex">Index of the book to remove.</param>
		/// ------------------------------------------------------------------------------------
		public void Remove(int bookIndex)
		{
			IScrBook removedBook = m_filteredBooks[bookIndex];
			m_filteredBooks.RemoveAt(bookIndex);
			CheckListForDeletedBooks(ref m_filteredBooks);

			// If the last book was deleted from the filter, then show all the other books;
			// otherwise simply notify the world of the change.
			if (m_filteredBooks.Count == 0)
			{
				foreach (IScrBook book in m_scr.ScriptureBooksOS)
				{
					if (book != removedBook)
						AddInternal(book);
				}
			}
			NotifyFilterChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add one or multiple books into the book filter at the appropriate location.
		/// </summary>
		/// <param name="books">List of books</param>
		/// <remarks>This method is virtual to support testing with mocks</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void Add(params IScrBook[] books)
		{
			Add((IEnumerable<IScrBook>)books);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add one or multiple books into the book filter at the appropriate location.
		/// </summary>
		/// <param name="books">List of books</param>
		/// <remarks>This method is virtual to support testing with mocks</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void Add(IEnumerable<IScrBook> books)
		{
			if (books == null)
				return;

			foreach (IScrBook book in books)
				if (book != null)
					AddInternal(book);

			// Don't save the filtered books if the array contains all the books, because
			// that means the filter is off.
			if (m_filteredBooks.Count < m_scr.ScriptureBooksOS.Count)
				m_savedFilteredBooks = new List<IScrBook>(m_filteredBooks);

			CheckListForDeletedBooks(ref m_filteredBooks);
			NotifyFilterChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a book without raising the FilterChanged event or checking for deleted books.
		/// </summary>
		/// <param name="book">The book.</param>
		/// ------------------------------------------------------------------------------------
		private void AddInternal(IScrBook book)
		{
			int canonicalNum = book.CanonicalNum;

			// find the spot to insert the book
			int insertIndex = 0;
			for (; insertIndex < m_filteredBooks.Count; insertIndex++)
			{
				int checkCanonicalNum = m_filteredBooks[insertIndex].CanonicalNum;

				// if the book already exists, overwrite it
				if (checkCanonicalNum == canonicalNum)
				{
					m_filteredBooks.RemoveAt(insertIndex);
					break;
				}

				// found the insert location
				if (checkCanonicalNum > canonicalNum)
					break;
			}

			// insert the book at the found location or the end
			Debug.Assert(!m_filteredBooks.Contains(book));
			m_filteredBooks.Insert(insertIndex, book);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an unfiltered book index from a filtered one.  The filtered index refers
		/// to books in the filter.  For accessing the database list, you need an unfiltered
		/// index.
		/// </summary>
		/// <param name="filteredIndex"></param>
		/// <returns>unfiltered (real) index into scripture.ScriptureBooksOS</returns>
		/// ------------------------------------------------------------------------------------
		public int GetUnfilteredIndex(int filteredIndex)
		{
			if (BookCount > 0)
			{
				IScrBook filteredBook = m_filteredBooks[filteredIndex];
				return m_scr.ScriptureBooksOS.IndexOf(filteredBook);
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a comma seperated list of hvos that are in the saved filter list as a string
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetSavedFilterAsString()
		{
			bool fFilterChanged = CheckListForDeletedBooks(ref m_savedFilteredBooks);

			string guidListString = string.Empty;
			foreach (IScrBook book in m_savedFilteredBooks)
			{
				if (guidListString != string.Empty)
					guidListString += ",";
				guidListString += book.Guid;
			}

			return guidListString;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the saved filter list based on the specified comma seperated list of guids
		/// </summary>
		/// <param name="guidListString">A comma seperated list of guids</param>
		/// ------------------------------------------------------------------------------------
		public void SetSavedFilterFromString(string guidListString)
		{
#if __MonoCS__ // Work around for #FWNX-152
			if (guidListString == null)
				guidListString = String.Empty;
#endif

			Debug.Assert(guidListString != null);

			m_savedFilteredBooks = new List<IScrBook>();
			String[] guids = guidListString.Split(',');
			foreach (String guidString in guids)
			{
				if (string.IsNullOrEmpty(guidString))
					continue;

				Guid bookGuid = new Guid(guidString);
				try
				{
					IScrBook book = m_cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(bookGuid);
					m_savedFilteredBooks.Add(book);
				}
				catch (KeyNotFoundException)
				{
					// A book with a guid in the list could not be found. Just ignore it. This
					// usually means that a book was removed while the program wasn't running
					// (like from a remote machine to the same DB) or from a newly restored DB.
					// The worst that will happen is they won't have any books in their view
					// (i.e. None of the books could be found).
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start and ending Scripture references that cover the entire range of books
		/// represented by this filter if the set of filtered books is contiguous books in the
		/// canon of Scripture. If the set of filtered books is not contiguous or there are no
		/// books in the filter, then "Empty" references are returned.
		/// </summary>
		/// <param name="start">The start reference.</param>
		/// <param name="end">The end reference.</param>
		/// ------------------------------------------------------------------------------------
		public void GetRefRangeForContiguousBooks(out ScrReference start, out ScrReference end)
		{
			start = new ScrReference(0, m_scr.Versification);
			end = new ScrReference(0, m_scr.Versification);
			if (BookCount == 0)
				return;

			List<int> bookIds = BookIds;
			for (int book = 0; book < bookIds.Count - 1; book++)
			{
				if (bookIds[book] + 1 != bookIds[book + 1])
					return;
			}
			start = new ScrReference(bookIds[0], 1, 1, m_scr.Versification);
			end = new ScrReference(bookIds.Last(), 1, 1, m_scr.Versification).LastReferenceForBook;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that all books in the specified list are still valid.
		/// </summary>
		/// <returns>True if books were found in the filter that were deleted (and thus the
		/// list was changed), false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool CheckListForDeletedBooks(ref List<IScrBook> filteredBooks)
		{
			bool fChangedList = false;
			for (int i = filteredBooks.Count - 1; i >= 0; i--)
			{
				if (!m_scr.ScriptureBooksOS.Contains(filteredBooks[i]))
				{
					filteredBooks.RemoveAt(i);
					fChangedList = true;
				}
			}
			Debug.Assert(filteredBooks.Count <= m_scr.ScriptureBooksOS.Count, "Book filter contains duplicate book!");

			return fChangedList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Notifies any listeners that the filter has changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void NotifyFilterChanged()
		{
			if (FilterChanged != null)
				FilterChanged(this, EventArgs.Empty);
		}
		#endregion

		#region IEnumerable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to
		/// iterate through the collection.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerator GetEnumerator()
		{
			return m_filteredBooks.GetEnumerator();
		}
		#endregion
	}
	#endregion

	#region UpdateBookFilterAction
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for updating the book filter with a new list of books. Instances of this
	/// class should be created before the book filter has been updated with the changed list
	/// of books.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class UpdateBookFilterAction : UndoActionBase
	{
		private readonly FilteredScrBooks m_bookFilter;
		private readonly List<IScrBook> m_filteredBooks;
		private readonly IScrBook[] m_newFilterList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UpdateBookFilterAction"/> class.
		/// Instances of this class should be created before the book filter has been updated
		/// with the changed list of books because the passed-in book filter is used as the
		/// base for the change. (i.e. when inserting a book, the newFilterList will contain
		/// the new book when the passed-in book filter does not yet contain the new book)
		/// </summary>
		/// <param name="bookFilter">The book filter.</param>
		/// <param name="newFilterList">The list of books that will be the filter when the
		/// action is complete.</param>
		/// ------------------------------------------------------------------------------------
		public UpdateBookFilterAction(FilteredScrBooks bookFilter, IScrBook[] newFilterList)
		{
			m_bookFilter = bookFilter;
			m_filteredBooks = new List<IScrBook>(m_bookFilter.FilteredBooks);
			m_newFilterList = newFilterList;
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True for most actions, which make changes to data; false for actions that represent
		/// updates to the user interface, like replacing the selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsDataChange
		{
			get
			{
				// Although this action isn't technically a data change, the time it needs to
				// be called during the undo/redo process is the same as if it were a data change.
				// So instead of coming up with another property (like a
				// "NeedsToHappenRightBeforePropChanged") we just opted to returning true here.
				// (FWR-1675).
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			m_bookFilter.FilteredBooks = m_filteredBooks;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") the removal of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			m_bookFilter.FilteredBooks = m_newFilterList;

			return true;
		}
		#endregion
	}
	#endregion
}

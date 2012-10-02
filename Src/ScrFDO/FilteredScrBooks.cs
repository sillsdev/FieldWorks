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
// File: FilteredScrBooks.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements the IVwVirtualHandler interface to create a filtered sequence of books property
	/// for Scripture.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FilteredScrBooks : BaseVirtualHandler, IEnumerable
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
		private List<int> m_filteredBooks;

		/// <summary>
		/// List of books in filter, even when filter isn't enabled. This value comes from the
		/// registry when an instance of FilterScrBooks is created and updated from the
		/// set book filter dialog.
		/// </summary>
		private List<int> m_savedFilteredBooks;

		/// <summary>Constant for class of virtual property</summary>
		public const string kFilteredClass = "Scripture";
		/// <summary>Constant for field name of virtual property</summary>
		public const string kFilteredField = "FilteredScrBooks";
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This constructor is for testing so the class can be mocked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FilteredScrBooks()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredScrBooks"/> class.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public FilteredScrBooks(FdoCache cache, int filterInstance)
		{
			m_cache = cache;
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			ClassName = kFilteredClass;
			FieldName = kFilteredField + "_" + filterInstance;
			Type = (int) CellarModuleDefns.kcptReferenceSequence;
			// Due to the possibility of a book being deleted from the database by an
			// undo request after an insert of a book, we cannot store the filtered books
			// in the cache.  Instead, we use the ComputeEveryTime flag so that the load
			// method will be called every time that the filtered books sequence is accessed.
			ComputeEveryTime = true;
			cache.InstallVirtualProperty(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets virtual property handler corresponding to filter instance.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static FilteredScrBooks GetFilterInstance(FdoCache cache, int filterInstance)
		{
			if (cache == null)
				return null;

			return cache.GetVirtualProperty(kFilteredClass,
				kFilteredField + "_" + filterInstance) as FilteredScrBooks;
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
				CheckFilteredBooks();
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
				CheckFilteredBooks();
				return (m_scr.ScriptureBooksOS.Count == BookCount);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of books in the saved filter as an array of hvos
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int[] SavedFilter
		{
			get
			{
				CheckListForDeletedBooks(ref m_savedFilteredBooks, false);
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
				if (m_filteredBooks == null || m_filteredBooks.Count == 0)
					return null;

				List<int> ids = new List<int>(m_filteredBooks.Count);
				for (int i = 0; i < m_filteredBooks.Count; i++)
				{
					IScrBook book = new ScrBook(m_cache, m_filteredBooks[i]);
					ids.Add(book.CanonicalNum);
				}

				return ids;
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
				CheckFilteredBooks(false);
				return m_filteredBooks;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update filtered books.
		/// </summary>
		/// <param name="filteredBooks">An array of HVOs for books that should be displayed</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateFilter(params int[] filteredBooks)
		{
			CheckFilteredBooks();
			int prevBookCount = m_filteredBooks.Count;
			m_filteredBooks = new List<int>(filteredBooks);

			CheckFilteredBooks();

			// Don't save the filtered books if the array contains all the books, because
			// that means the filter is off.
			if (m_filteredBooks.Count < m_scr.ScriptureBooksOS.Count)
				m_savedFilteredBooks = new List<int>(m_filteredBooks);

			DoPropChange(prevBookCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the current book filter. This basically calls PropChanged for the current
		/// filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Refresh()
		{
			CheckFilteredBooks();
			DoPropChange(m_filteredBooks.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method used to notify the book filter that books in the database have been deleted.
		/// </summary>
		/// <remarks>This method is virtual to support testing with mocks</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void BooksDeleted()
		{
			CheckFilteredBooks();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show all the books in the filter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ShowAllBooks()
		{
			UpdateFilter(m_scr.ScriptureBooksOS.HvoArray);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book corresponding to the filtered index.
		/// </summary>
		/// <param name="index">Index into filtered books</param>
		/// <returns>Book corresponding to index</returns>
		/// ------------------------------------------------------------------------------------
		public ScrBook GetBook(int index)
		{
			CheckFilteredBooks();
			Debug.Assert(index >= 0 && index < m_filteredBooks.Count);
			return new ScrBook(m_cache, m_filteredBooks[index]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of a book in the filtered list.
		/// </summary>
		/// <param name="bookHvo">Hvo of book</param>
		/// <returns>Index of book</returns>
		/// ------------------------------------------------------------------------------------
		public int GetBookIndex(int bookHvo)
		{
			CheckFilteredBooks();
			return m_filteredBooks.IndexOf(bookHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets book from list of filtered books by book ordinal.
		/// </summary>
		/// <param name="canonicalNum">Ordinal of book - <code>ScrBook.CanonicalNum</code></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ScrBook GetBookByOrd(int canonicalNum)
		{
			CheckFilteredBooks();
			for (int i = 0; i < m_filteredBooks.Count; i++)
			{
				ScrBook book = new ScrBook(m_cache, m_filteredBooks[i]);
				if (book.CanonicalNum == canonicalNum)
					return book;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove a book from the filter
		/// </summary>
		/// <param name="bookIndex"></param>
		/// ------------------------------------------------------------------------------------
		public void Remove(int bookIndex)
		{
			CheckFilteredBooks();
			int prevBookCount = m_filteredBooks.Count;
			RemoveInternal(bookIndex);
			DoPropChange(prevBookCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove a book from the filter
		/// </summary>
		/// <param name="bookIndex">Index of the book to remove.</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveInternal(int bookIndex)
		{
			if (bookIndex >= 0 && bookIndex < m_filteredBooks.Count)
				m_filteredBooks.RemoveAt(bookIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert an HVO into the book filter at a given index
		/// </summary>
		/// <param name="index">index for where to insert the hvo</param>
		/// <param name="bookHvo">book hvo to insert</param>
		/// ------------------------------------------------------------------------------------
		public void Insert(int index, int bookHvo)
		{
			CheckFilteredBooks();
			int prevBookCount = m_filteredBooks.Count;
			Debug.Assert(!m_filteredBooks.Contains(bookHvo));
			m_filteredBooks.Insert(index, bookHvo);
			DoPropChange(prevBookCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add one or multiple book hvos into the book filter at the appropriate location.
		/// </summary>
		/// <param name="bookHvos">List of book hvos</param>
		/// <remarks>This method is virtual to support testing with mocks</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void Add(params int[] bookHvos)
		{
			CheckFilteredBooks();
			int prevBookCount = m_filteredBooks.Count;

			foreach (int bookHvo in bookHvos)
				AddInternal(bookHvo);

			DoPropChange(prevBookCount);

			// Make sure the saved filter is the same as the new filter
			m_savedFilteredBooks = m_filteredBooks;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a book without issuing a PropChanged or checking for deleted books.
		/// </summary>
		/// <param name="bookHvo">The book hvo.</param>
		/// ------------------------------------------------------------------------------------
		private void AddInternal(int bookHvo)
		{
			if (!m_cache.IsRealObject(bookHvo, ScrBook.kClassId))
				return;

			int canonicalNum = new ScrBook(m_cache, bookHvo).CanonicalNum;

			// find the spot to insert the book
			int insertIndex = 0;
			for (; insertIndex < m_filteredBooks.Count; insertIndex++)
			{
				int checkCanonicalNum = new ScrBook(m_cache, m_filteredBooks[insertIndex]).CanonicalNum;

				// if the book already exists, overwrite it
				if (checkCanonicalNum == canonicalNum)
				{
					RemoveInternal(insertIndex);
					break;
				}

				// found the insert location
				if (checkCanonicalNum > canonicalNum)
					break;
			}

			// insert the book at the found location or the end
			Debug.Assert(!m_filteredBooks.Contains(bookHvo));
			m_filteredBooks.Insert(insertIndex, bookHvo);
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
				int[] books = m_scr.ScriptureBooksOS.HvoArray;
				for (int i = 0; i < books.Length; i++)
				{
					if (books[i] == m_filteredBooks[filteredIndex])
						return i;
				}
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
			string hvoListString = string.Empty;
			foreach (int hvo in m_savedFilteredBooks)
			{
				if (hvoListString != string.Empty)
					hvoListString += ",";
				hvoListString += hvo;
			}

			return hvoListString;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the saved filter list based on the specified comma seperated list of hvos
		/// </summary>
		/// <param name="hvoListString">A comma seperated list of hvos</param>
		/// ------------------------------------------------------------------------------------
		public void SetSavedFilterFromString(string hvoListString)
		{
			Debug.Assert(hvoListString != null);

			List<int> hvoList = new List<int>();
			while (hvoListString != string.Empty)
			{
				bool skipComma = true;
				int substrSize = hvoListString.IndexOf(",", 0);
				if (substrSize < 0)
				{
					substrSize = hvoListString.Length;
					skipComma = false;
				}
				int hvo = Convert.ToInt32(hvoListString.Substring(0, substrSize));
				hvoListString = hvoListString.Substring(skipComma ? substrSize + 1: substrSize);
				hvoList.Add(hvo);
			}

			m_savedFilteredBooks = hvoList;
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates books in current filter. If no filter exists, filtered list of books
		/// will be created containing all books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckFilteredBooks()
		{
			CheckFilteredBooks(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates books in current filter. If no filter exists, filtered list of books
		/// will be created containing all books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckFilteredBooks(bool doPropChange)
		{
			if (m_filteredBooks == null)
			{
				m_filteredBooks = new List<int>(m_scr.ScriptureBooksOS.HvoArray);

				if (FilterChanged != null)
					FilterChanged(this, EventArgs.Empty);

				return;
			}

			CheckListForDeletedBooks(ref m_filteredBooks, doPropChange);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that all books in the specified list are still valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckListForDeletedBooks(ref List<int> list, bool doPropChange)
		{
			List<int> databaseBooks = new List<int>(m_scr.ScriptureBooksOS.HvoArray);
			List<int> checkedBooks = new List<int>(list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				if (databaseBooks.Contains(list[i]))
					checkedBooks.Add(list[i]);
			}
			Debug.Assert(checkedBooks.Count <= databaseBooks.Count,
				"Duplicate book is about to be added to book filter!");

			// If some invalid books were found, update the list of filtered books
			if (checkedBooks.Count != list.Count)
			{
				int prevBookCount = list.Count;
				list = checkedBooks;
				if (doPropChange)
					DoPropChange(prevBookCount);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does a prop change to update the views.
		/// </summary>
		/// <param name="prevFilterBookCount">The previous filter book count.</param>
		/// ------------------------------------------------------------------------------------
		private void DoPropChange(int prevFilterBookCount)
		{
			// We don't want to do prop changeds in the middle of an undo or redo because it is
			// quite possible that we're in the middle of reconstructing the view. Calling
			// PropChanged now would cause another reconstruct that destroys the existing
			// view which will result in bad things (TE-5981). However, we have to make sure
			// that we call PropChanged later, otherwise we might end up with an empty view (e.g.
			// undo inserting a book with no book filter applied).
			if (m_cache.ActionHandlerAccessor != null &&
				m_cache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
			{
				return;
			}

			m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, m_scr.Hvo,
				(int)Scripture.ScriptureTags.kflidScriptureBooks, 0, m_filteredBooks.Count,
				prevFilterBookCount);

			if (FilterChanged != null)
				FilterChanged(this, EventArgs.Empty);
		}
		#endregion

		#region Overridden IVwVirtualHandler Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load filtered books for scripture - load all books if no filter in place.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="_cda"></param>
		/// ------------------------------------------------------------------------------------
		public override void Load(int hvo, int tag, int ws, IVwCacheDa _cda)
		{
			CheckFilteredBooks(false);
			_cda.CacheVecProp(hvo, tag, m_filteredBooks.ToArray(), m_filteredBooks.Count);
			//Debug.WriteLine(string.Format("Load hvo={0}, filter count={1}", hvo, m_filteredBooks.Count));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace may be first call to reference this virtual property - need to make sure
		/// that property gets loaded into cache, but don't need to do any actual replace since
		/// cache code will handle that.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ihvoMin"></param>
		/// <param name="ihvoLim"></param>
		/// <param name="_rghvo"></param>
		/// <param name="chvo"></param>
		/// <param name="_sda"></param>
		/// ------------------------------------------------------------------------------------
		public override void Replace(int hvo, int tag, int ihvoMin, int ihvoLim,
			int[] _rghvo, int chvo, ISilDataAccess _sda)
		{
			Debug.Assert(m_filteredBooks != null);
			if (ihvoMin == 0 && ihvoLim == m_filteredBooks.Count)
				m_filteredBooks = new List<int>(_rghvo);
			else if (ihvoMin == ihvoLim)
			{
#if DEBUG
				foreach (int hvoToInsert in _rghvo)
					Debug.Assert(!m_filteredBooks.Contains(hvoToInsert));
#endif
				// Inserting entries into vector
				m_filteredBooks.InsertRange(ihvoMin, _rghvo);
			}
			else if (chvo == 0)
			{
				// Deleting entries from vector
				m_filteredBooks.RemoveRange(ihvoMin, ihvoLim - ihvoMin);
			}

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
			for (int i = 0; i < m_filteredBooks.Count; i++)
				yield return new ScrBook(m_cache, m_filteredBooks[i]);
		}

		#endregion
	}
}

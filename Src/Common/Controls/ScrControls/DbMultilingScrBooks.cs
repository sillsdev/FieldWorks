// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DbMultilingScrBooks.cs

using System;
using System.Collections.Generic;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DBMultilingScrBooks.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DBMultilingScrBooks : MultilingScrBooks
	{
		#region Data members
		private IScripture m_scripture;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DBMultilingScrBooks(IScripture scr) : base(scr.ScrProjMetaDataProvider)
		{
			m_scripture = scr;
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ordinal values of books in project.
		/// </summary>
		/// <returns>Array list of ordinal values of books.</returns>
		/// ------------------------------------------------------------------------------------
		private List<int> GetBookOrds()
		{
			var bookOrds = new List<int>();
			foreach (var book in m_scripture.ScriptureBooksOS)
				bookOrds.Add(book.CanonicalNum);

			return bookOrds;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the user typed in string. Creates and returns a ScrReference object.
		/// </summary>
		/// <param name="sTextToBeParsed">Reference string the user types in.</param>
		/// <returns>The generated scReference object.</returns>
		/// ------------------------------------------------------------------------------------
		public override ScrReference ParseRefString(string sTextToBeParsed)
		{

			var scrRef = new ScrReference();
			var bookOrds = GetBookOrds();

			// Search for a reference that is actually in the database.
			for (var startBook = 0; startBook < 66; )
			{
				var prevStartBook = startBook;
				scrRef = base.ParseRefString(sTextToBeParsed, startBook);

				// If the book is in the Scripture project
				// (or if we get the same book back from the parse method or go back to the start)...
				if (bookOrds != null && bookOrds.Contains(scrRef.Book) ||
					prevStartBook == scrRef.Book || prevStartBook > scrRef.Book)
				{
					break; // we're finished searching.
				}

				startBook = scrRef.Book; // start searching in next book returned.
			}

			// If the Scripture reference is not in the project (and we have books)...
			if (!bookOrds.Contains(scrRef.Book) && m_scripture.ScriptureBooksOS.Count > 0)
			{
				// set it to the first book in the project.
				return new ScrReference(m_scripture.ScriptureBooksOS[0].CanonicalNum, 1, 1,
										m_scripture.Cache.LanguageProject.TranslatedScriptureOA.Versification);
			}
			return scrRef;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns false, if hooked to a scr database and the book text does not exist.
		/// </summary>
		/// <param name="nBook">Book number used to see if book text exists in database.
		/// NOTE: book number is one-based (i.e. Genesis = 1).</param>
		/// <returns>In implementation (a): always return true.
		/// In implementation (b) (hooked to a scr database): return true if book text exists.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsBookAvailableInDb(int nBook)
		{
			return (GetBookFromDB(nBook) != null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified book index is within the range of valid
		/// books.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>True if the book is valid. Otherwise false.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsBookValid(int nBook)
		{
			return IsBookAvailableInDb(nBook);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the reference string refers to scripture found in the DB.
		/// </summary>
		/// <param name="reference">Scripture reference</param>
		/// <returns>True if the reference is found in the DB. Otherwise false.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsReferenceValid(string reference)
		{
			var scRef = base.ParseRefString(reference);

			// Don't bother checking whether or not the reference is in the DB if the
			// reference can't even be parsed to get a valid reference. However, this
			// validation is useless (as of June 23, 2003) for determining whether or
			// not a reference contains a chapter or verse number that is out of range
			// for the specified book.
			try
			{
				if (!scRef.Valid)
					return false;
			}
			catch (Exception e)
			{
				throw new InstallationException(e);
			}

			// Determine whether or not the book is in the DB.
			var book = GetBookFromDB(scRef.Book);
			if (book == null)
				return false;

			// Determine whether or not the chapter for 'book' is in the DB.
			foreach (var section in book.SectionsOS)
			{
				if (section.ContainsChapter(scRef.Chapter))
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the ScrBook object from the DB corresponding to the specified book index.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>The ScrBook DB object corresponding to a book index.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook GetBookFromDB(int nBook)
		{
			if (m_scripture == null)
				return null;

			return m_scripture.FindBook(nBook);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the book abbreviation for a given book number.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>The book abbreviation as a string.</returns>
		/// ------------------------------------------------------------------------------------
		public override string GetBookAbbrev(int nBook)
		{
			var book = GetBookFromDB(nBook);
			return (book == null ? ScrReference.NumberToBookCode(nBook) : book.BestUIAbbrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the book name for a given book number.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>The book name as a string.</returns>
		/// ------------------------------------------------------------------------------------
		public override string GetBookName(int nBook)
		{
			var book = GetBookFromDB(nBook);
			return (book == null ? ScrReference.NumberToBookCode(nBook) : book.BestUIName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of book labels for the books available in the associated Scripture
		/// project. If name of one book is not available in the first (primary) writing system,
		/// the info in the next available (secondary) writing system is substituted.
		/// If not available in any given writing system, the 3-letter SIL/UBS book code is
		/// returned.
		/// </summary>
		/// <returns>An array of BookNameInfo objects in the requested primary writing system,
		/// as far as possible.</returns>
		/// ------------------------------------------------------------------------------------
		public override BookLabel[] BookLabels
		{
			get
			{
				// TODO: ToddJ (BryanW)	Implement the description above
				// foreach (int nEnc in m_requestedEncodings)
				// Assert that knEncSilCodes is in the list

				if (m_scripture == null)
					return null;
				var rgblBookNames = new BookLabel[m_scripture.ScriptureBooksOS.Count];

				for (var i = 0; i < m_scripture.ScriptureBooksOS.Count; i++)
				{
					var book = m_scripture.ScriptureBooksOS[i];
					var sBookName = book.BestUIName;
					rgblBookNames[i] = new BookLabel(sBookName, (short)(book.CanonicalNum));
				}

				return rgblBookNames;
			}
		}
		#endregion
	}

}

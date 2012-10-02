// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeUndoActons.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	#region UndoRemoveBookAction
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for removing books
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoRemoveBookAction: UndoActionBase
	{
		private FdoCache m_cache;
		private FilteredScrBooks m_bookFilter;
		private int m_bookID;
		private int m_bookHvo;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="filter">book filter in place</param>
		/// <param name="bookID">ordinal ID of the book being removed</param>
		/// ------------------------------------------------------------------------------------
		public UndoRemoveBookAction(FdoCache cache, FilteredScrBooks filter, int bookID)
		{
			m_cache = cache;
			m_bookFilter = filter;
			m_bookID = bookID;
			m_bookHvo = ScrBook.FindBookByID(m_cache, bookID).Hvo;
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			if (FwApp.App != null)
			{
				// find out the HVO of the book that is being restored.
				// We can't use m_bookHvo because if multiple users are connected with
				// this database the HVO might already be gone.
				IScrBook book = ScrBook.FindBookByID(m_cache, m_bookID);
				m_bookHvo = book.Hvo;
				FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncScriptureDeleteBook, m_bookHvo,
					(int)Scripture.ScriptureTags.kflidScriptureBooks), m_cache);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			// remove the book from the filter
			int index = m_bookFilter.GetBookIndex(m_bookHvo);
			if (index >= 0)
				m_bookFilter.Remove(index);

			if (FwApp.App != null)
			{
				FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncScriptureNewBook, m_bookHvo,
					(int)Scripture.ScriptureTags.kflidScriptureBooks), m_cache);
			}
			return true;
		}
		#endregion
	}
	#endregion

	#region UndoInsertBookAction
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for inserting books
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoInsertBookAction: UndoActionBase
	{
		private FdoCache m_cache;
		private FilteredScrBooks m_bookFilter;
		private int m_bookHvo;
		private int m_bookID;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="filter">book filter in place</param>
		/// <param name="bookID">ordinal ID of the book being inserted</param>
		/// ------------------------------------------------------------------------------------
		public UndoInsertBookAction(FdoCache cache, FilteredScrBooks filter, int bookID)
		{
			m_cache = cache;
			m_bookFilter = filter;
			m_bookID = bookID;
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undoes the Insert Book - the book is being removed
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			if (FwApp.App != null)
			{
				// This sync message gets processed at the end of the Undo. At that time we'll
				// show all books if this is the last book that gets removed from the book
				// filter.
				FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncScriptureDeleteBook, m_bookHvo,
					(int)Scripture.ScriptureTags.kflidScriptureBooks), m_cache);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Redoes the Insert Book - the book is being added
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			// find out the HVO of the book that is being restored.
			// We can't use m_bookHvo because if multiple users are connected with
			// this database the HVO might already be gone.
			IScrBook book = ScrBook.FindBookByID(m_cache, m_bookID);
			m_bookHvo = book.Hvo;
			if (FwApp.App != null)
			{
				FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncScriptureNewBook, m_bookHvo,
					(int)Scripture.ScriptureTags.kflidScriptureBooks), m_cache);
			}
			m_bookFilter.UpdateFilter(m_bookHvo);
			m_bookFilter.Load(m_bookHvo, m_bookFilter.Tag, m_cache.DefaultVernWs,
				m_cache.VwCacheDaAccessor);
			return true;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a book
		/// </summary>
		/// <returns>The new book</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook Do()
		{
			IScripture scr = m_cache.LangProject.TranslatedScriptureOA;
			int hvoTitle;
			IScrBook newBook = ScrBook.CreateNewScrBook(m_bookID, scr, out hvoTitle);
			m_bookHvo = newBook.Hvo;

			// Insert the new book title and set the book names
			newBook.InitTitlePara();
			newBook.Name.CopyAlternatives(newBook.BookIdRA.BookName);
			newBook.Abbrev.CopyAlternatives(newBook.BookIdRA.BookAbbrev);

			// Now insert the first section for the new book.
			ITsTextProps textProps = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				m_cache.DefaultVernWs);
			ScrSection.CreateScrSection(newBook, 0, scr.ConvertToString(1), textProps, false);

			// Do synchronize stuff
			if (FwApp.App != null)
			{
				FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncReloadScriptureControl, 0, 0),
					m_cache);
			}

			// Use Redo for rest of sync'ing and updating book filter
			Redo(false);

			return newBook;
		}
	}
	#endregion

	#region UndoWithSyncAction
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generic undo action to allow for synchronization messages
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoWithSyncAction: UndoActionBase
	{
		private FdoCache m_cache;
		private SyncMsg m_syncMsg;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="msg"></param>
		/// ------------------------------------------------------------------------------------
		public UndoWithSyncAction(FdoCache cache, SyncMsg msg)
		{
			m_cache = cache;
			m_syncMsg = msg;
		}

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") an action.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			if (FwApp.App != null)
				FwApp.App.Synchronize(new SyncInfo(m_syncMsg, 0, 0), m_cache);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-applies (or "re-does") an action.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			// does the same thing for both undo and redo
			return Undo(fRefreshPending);
		}
		#endregion
	}
	#endregion
}

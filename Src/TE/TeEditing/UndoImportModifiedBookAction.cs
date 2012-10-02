// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoImportModifiedBookAction.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for import of a book which is modifying an existing book. We no longer
	/// directly overwrite the vernacular during import, but if importing the back translation
	/// only, we attach it to the existing paragraphs, which overwrites any existing BT.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoImportModifiedBookAction : UndoRefreshAction
	{
		#region Data members
		/// <summary>The UndoImportManager that created this action</summary>
		protected UndoImportManager m_manager;
		/// <summary>The canonical number of the modified book</summary>
		protected int m_canonicalBookNum;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:UndoImportOverriddenBookAction"/>
		/// class.
		/// </summary>
		/// <param name="manager">A class that manages undoable aspects of importing and holds
		/// the state info needed for importing and later undoing the import.</param>
		/// <param name="existingBook">The existing book.</param>
		/// <remarks>This is internal because we only want UndoImportManager to create these
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		internal UndoImportModifiedBookAction(UndoImportManager manager, IScrBook existingBook)
			: base()
		{
			m_manager = manager;
			m_canonicalBookNum = existingBook.CanonicalNum;
			m_manager.ScriptureObj.AddBookToSavedVersion(manager.BackupVersion, existingBook);
		}
		#endregion

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") the import of this book.
		/// </summary>
		/// <param name="fRefreshPending">Set to <c>true</c> if app will call refresh after all
		/// Undo actions are finished. This means the UndoImportAction won't call PropChanged.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public override bool Undo(bool fRefreshPending)
		{
			IScrBook bookToRemove = m_manager.ScriptureObj.FindBook(m_canonicalBookNum);
			Debug.Assert(bookToRemove != null);
			Debug.Assert(m_manager.m_savedVersion != null);
			IScrBook backupBook = m_manager.m_savedVersion.FindBook(m_canonicalBookNum);
			Debug.Assert(backupBook != null);

			int iScrBook = bookToRemove.IndexInOwner;

			m_manager.m_cache.DeleteObject(bookToRemove.Hvo);
			bookToRemove = null;

			// Do NOT use bookMerger.BookCurr.OwnOrd, ownord$ is not guranteed to be contiguous.
			int srcIndex = backupBook.IndexInOwner;
			m_manager.m_cache.MoveOwningSequence(
				m_manager.m_savedVersion.Hvo,
				(int)ScrDraft.ScrDraftTags.kflidBooks,
				srcIndex,
				srcIndex,
				m_manager.ScriptureObj.Hvo,
				(int)Scripture.ScriptureTags.kflidScriptureBooks,
				iScrBook);

			if (!fRefreshPending)
			{
				// Do a prop change event for the new book
				m_manager.m_cache.PropChanged(null, PropChangeType.kpctNotifyAll,
					m_manager.ScriptureObj.Hvo, (int)Scripture.ScriptureTags.kflidScriptureBooks,
					iScrBook, 1, 1);
			}

			// We also have to update the bookfilter - DeleteObject removed the book from the
			// filter, but we have to re-insert the new one.
			if (m_manager.BookFilter != null)
				m_manager.BookFilter.Add(backupBook.Hvo);

			// TODO: Undo any changes to the stylesheet and force application windows to reload their stylesheets.

			// REVIEW: Do we need to do this? Seems like we wouldn't since we're replacing an existing book.
			//if (FwApp.App != null)
			//{
			//    FwApp.App.BeginUpdate(m_manager.m_cache);
			//    FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncReloadScriptureControl, 0, 0), m_manager.m_cache);
			//    FwApp.App.EndUpdate(m_manager.m_cache);
			//}
			return true;
		}

			//// A simple index will work. Archive can't have "holes" in the sequence
			//// because only the last book can be deleted/restored.
			//int iArchivedBook = m_savedVersion.BooksOS.Count - 1;
			//IScrBook bookToRestore = (IScrBook)m_savedVersion.BooksOS[iArchivedBook];

			//m_cache.MoveOwningSequence(
			//    m_savedVersion.Hvo,
			//    bookToRestore.OwningFlid,
			//    iArchivedBook,
			//    iArchivedBook,
			//    m_scr.Hvo,
			//    (int)Scripture.ScriptureTags.kflidScriptureBooks,
			//    iScrBookRestore);

			//// If there are no books left in this saved version, remove it.
			//if (m_savedVersion.BooksOS.Count == 0)
			//{
			//    m_scr.ArchivedDraftsOC.Remove(m_savedVersion);
			//    m_savedVersion = null;
			//}

			//return bookToRestore;
		#endregion

		#region Overrides of UndoRefreshAction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns always <c>true</c>
		/// </summary>
		/// <returns>always <c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsDataChange()
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>false</c> because this can't be redone.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsRedoable()
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Redoing an import isn't valid, so this method throws an exception.
		/// </summary>
		/// <param name="fRefreshPending">Ignored</param>
		/// ------------------------------------------------------------------------------------
		public override bool Redo(bool fRefreshPending)
		{
			throw new NotImplementedException("Import cannot be redone.");
		}
		#endregion
	}
}

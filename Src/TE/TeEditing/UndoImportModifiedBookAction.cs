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
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Undo action for import of a book which is modifying an existing book. We no longer
	/// directly overwrite the vernacular during import, but if importing the back translation
	/// only, we attach it to the existing paragraphs, which overwrites any existing BT.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoImportModifiedBookAction : UndoActionBase
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
			manager.BackupVersion.AddBook(existingBook);
		}
		#endregion

		#region Overrides of UndoActionBase
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverses (or "un-does") the import of this book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Undo()
		{
			IScrBook bookToRemove = m_manager.ScriptureObj.FindBook(m_canonicalBookNum);
			Debug.Assert(bookToRemove != null);
			Debug.Assert(m_manager.m_savedVersion != null);
			IScrBook backupBook = m_manager.m_savedVersion.FindBook(m_canonicalBookNum);
			Debug.Assert(backupBook != null);

			int iScrBook = bookToRemove.IndexInOwner;

			m_manager.m_cache.DomainDataByFlid.DeleteObj(bookToRemove.Hvo);
			bookToRemove = null;

			// Do NOT use bookMerger.BookCurr.OwnOrd, ownord$ is not guranteed to be contiguous.
			int srcIndex = backupBook.IndexInOwner;
			m_manager.m_cache.DomainDataByFlid.MoveOwnSeq(
				m_manager.m_savedVersion.Hvo, ScrDraftTags.kflidBooks, srcIndex, srcIndex,
				m_manager.ScriptureObj.Hvo, ScriptureTags.kflidScriptureBooks, iScrBook);

			// We also have to update the bookfilter - DeleteObject removed the book from the
			// filter, but we have to re-insert the new one.
			if (m_manager.BookFilter != null)
				m_manager.BookFilter.Add(backupBook);

			// TODO: Undo any changes to the stylesheet and force application windows to reload their stylesheets.

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns always <c>true</c>
		/// </summary>
		/// <returns>always <c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public override bool IsDataChange
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>false</c> because this can't be redone.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool IsRedoable
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Redoing an import isn't valid, so this method throws an exception.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Redo()
		{
			throw new NotImplementedException("Import cannot be redone.");
		}
		#endregion
	}
}

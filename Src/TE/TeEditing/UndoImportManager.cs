// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoImportManager.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Manages info and stuff for doing import in such a way that it can be undone.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoImportManager
	{
		#region Data members
		// saved version for immediate merge (contains books from original)
		internal IScrDraft m_savedVersion;
		// saved version into which to put selected imported books.
		internal IScrDraft m_importedSavedVersion;
		internal FdoCache m_cache = null;
		private int m_hMark = 0;
		private IScripture m_scr = null;
		private IScrBookAnnotations m_annotations;
		private FilteredScrBooks m_bookFilter;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UndoImportManager"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="bookFilter">The book filter.</param>
		/// ------------------------------------------------------------------------------------
		public UndoImportManager(FdoCache cache, FilteredScrBooks bookFilter)
		{
			m_cache = cache;
			m_bookFilter = bookFilter;
			m_scr = m_cache.LanguageProject.TranslatedScriptureOA;
		}
		#endregion

		#region Internal Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the scripture object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal IScripture ScriptureObj
		{
			get { return m_scr; }
		}
		#endregion

		/// <summary>
		/// This must be called before starting import.
		/// </summary>
		public void StartImportingFiles()
		{
			Debug.Assert(m_cache.DomainDataByFlid.GetActionHandler() != null);
			Debug.Assert(m_cache.DomainDataByFlid.GetActionHandler().CurrentDepth == 0);
			m_hMark = m_cache.DomainDataByFlid.GetActionHandler().Mark();
			IActionHandler actionHandler = m_cache.ActionHandlerAccessor;
			actionHandler.BeginUndoTask("Create saved version", "Create saved version");
			// Create new archive for saving backup versions of imported book
			m_savedVersion = m_cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(
				TeResourceHelper.GetResourceString("kstidSavedVersionDescriptionOriginal"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the new book (to the saved version...create it if need be).
		/// </summary>
		/// <param name="nCanonicalBookNumber">The canonical book number.</param>
		/// <param name="title">The title of the newly created book.</param>
		/// <param name="savedVersionDescription">Description to use for the imported saved
		/// version.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook AddNewBook(int nCanonicalBookNumber, string savedVersionDescription, out IStText title)
		{
			// Pretty trivial...at one point there was another option to overwrite the current book.
			return AddBookToImportedSavedVersion(nCanonicalBookNumber, savedVersionDescription, out title);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a book to the imported saved version. If necessary, create the imported saved version,
		/// and set up an UndoAction so it will be deleted if we Undo.
		/// </summary>
		/// <param name="nCanonicalBookNumber"></param>
		/// <param name="description">Description ot use for newly created imported version if
		/// necessary</param>
		/// <param name="title">The book's title object</param>
		/// <returns>The newly created book (which has been added to the imported version)</returns>
		/// ------------------------------------------------------------------------------------
		private IScrBook AddBookToImportedSavedVersion(int nCanonicalBookNumber, string description,
			out IStText title)
		{
			IActionHandler actionHandler = SetCurrentBookInternal(nCanonicalBookNumber);
			Debug.Assert(nCanonicalBookNumber > 0);
			if (m_importedSavedVersion == null)
			{
				m_importedSavedVersion = m_cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(
					description, ScrDraftType.ImportedVersion);
			}

			IScrBook existingBook = m_importedSavedVersion.FindBook(nCanonicalBookNumber);
			if (existingBook != null)
			{
				// For some reason, typically reading multiple independent SF files, we've been
				// asked to create a book we already have. Just return it.
				title = existingBook.TitleOA;
				return existingBook;
			}

			IScrBook newScrBook = m_cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(m_importedSavedVersion.BooksOS,
				nCanonicalBookNumber, out title);
			return newScrBook;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current book, particularly picking the right set of annotations
		/// to add new ones to. Also (and more conspicuously) ends the current Undo task
		/// and makes a new one for importing the new book. Should therefore be called
		/// BEFORE setting up the Undo action for the creation of the book.
		/// </summary>
		/// <param name="nCanonicalBookNumber">The canonical book number.</param>
		/// <returns>The action handler</returns>
		/// ------------------------------------------------------------------------------------
		private IActionHandler SetCurrentBookInternal(int nCanonicalBookNumber)
		{
			IActionHandler actionHandler = m_cache.DomainDataByFlid.GetActionHandler();

			if ((m_importedSavedVersion != null && m_importedSavedVersion.BooksOS.Count > 0) ||
				(m_savedVersion != null && m_savedVersion.BooksOS.Count > 0))
			{
				// We want a new undo task for each new book, except the first one
				actionHandler.EndUndoTask();
			}

			if (actionHandler.CurrentDepth == 0)
			{
				// No need to use localizable string from resources because the user will never
				// see these labels because we collapse to a single undo task when the import
				// completes.
				actionHandler.BeginUndoTask("Undo Import Book " + nCanonicalBookNumber,
					"Redo Import Book " + nCanonicalBookNumber);
			}
			m_annotations = m_scr.BookAnnotationsOS[nCanonicalBookNumber - 1];

			return actionHandler;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current book.
		/// </summary>
		/// <remarks>If importing annotations and/or BTs without importing the vernacular, the
		/// importer is responsible for calling this directly.</remarks>
		/// <param name="nCanonicalBookNumber">The canonical book number.</param>
		/// ------------------------------------------------------------------------------------
		public void SetCurrentBook(int nCanonicalBookNumber)
		{
			SetCurrentBookInternal(nCanonicalBookNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when we are about to import a book but are not importing the main translation.
		/// </summary>
		/// <param name="nCanonicalBookNumber"></param>
		/// <param name="fMakeBackup">This should be true if we are importing a back
		/// translation.</param>
		/// <returns>The version of the book in the imported version if available; otherwise
		/// the current version of the book (in which case a backup will be made first if
		/// <c>fMakeBackup</c> is <c>true</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook PrepareBookNotImportingVern(int nCanonicalBookNumber, bool fMakeBackup)
		{
			IActionHandler actionHandler = SetCurrentBookInternal(nCanonicalBookNumber);
			IScrBook cvBook = m_scr.FindBook(nCanonicalBookNumber);
			if (ImportedSavedVersion != null)
			{
				IScrBook isvBook = ImportedSavedVersion.FindBook(nCanonicalBookNumber);
				if (isvBook != null)
				{
					// We won't make a new undo action in this case. The import of a BT for an
					// imported book will be undone if the import of the book is undone.
					return isvBook;
				}
			}

			if (cvBook != null && fMakeBackup && m_savedVersion.FindBook(nCanonicalBookNumber) == null)
				BackupVersion.AddBook(cvBook);

			return cvBook;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creator of this object MUST call this after import is done, whether or not it
		/// succeeded.
		/// </summary>
		/// <param name="fRollbackLastSequence"><c>false</c> if import completed normally or was
		/// stopped by user after importing one or more complete books. <c>true</c> if an error
		/// occurred during import or user cancelled import in the middle of a book.</param>
		/// ------------------------------------------------------------------------------------
		public void DoneImportingFiles(bool fRollbackLastSequence)
		{
			if (m_cache.ActionHandlerAccessor.CurrentDepth == 0)
			{
				Logger.WriteEvent("DoneImportingFiles called when no UOW is in progress");
				Debug.Fail("DoneImportingFiles called when no UOW is in progress");
				return;
			}
			if (fRollbackLastSequence)
				m_cache.ActionHandlerAccessor.Rollback(0);
			else
				m_cache.ActionHandlerAccessor.EndUndoTask();

			m_cache.ServiceLocator.WritingSystemManager.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the saved version for backups of any overwritten books if it is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveEmptyBackupSavedVersion()
		{
			if (m_savedVersion != null && m_savedVersion.IsValidObject && m_savedVersion.BooksOS.Count == 0)
			{
				using (UndoableUnitOfWorkHelper uow =
					new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor, "Remove saved version"))
				{
					m_scr.ArchivedDraftsOC.Remove(m_savedVersion);
					uow.RollBack = false;
				}
				m_savedVersion = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When really, truly done with all merging etc, collapse all the things we can Undo
		/// for the import into a single Undo Item.
		/// </summary>
		/// <returns>True if some actions were collapsed, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool CollapseAllUndoActions()
		{
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidImport", out undo, out redo);
			return m_cache.DomainDataByFlid.GetActionHandler().CollapseToMark(m_hMark, undo, redo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Discards the imported version and the undo actions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DiscardImportedVersionAndUndoActions()
		{
			RemoveEmptyBackupSavedVersion();
			m_scr.ArchivedDraftsOC.Remove(m_importedSavedVersion);
			m_importedSavedVersion = null;
			m_cache.ActionHandlerAccessor.DiscardToMark(m_hMark);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undoes the entire import. Presumably there was either nothing in the file, or the
		/// user canceled during the first book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UndoEntireImport()
		{
			if (CollapseAllUndoActions())
				m_cache.ActionHandlerAccessor.Undo();
			m_importedSavedVersion = null;
			m_savedVersion = null;

			// TODO (TE-4711): Undo any changes to the stylesheet and force application windows
			// to reload their stylesheets.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a Scriture annotation for the book currently being imported.
		/// </summary>
		/// <param name="bcvStartReference">The starting BCV reference.</param>
		/// <param name="bcvEndReference">The ending BCV reference.</param>
		/// <param name="obj">The object being annotated (either a paragraph or a ScrBook)</param>
		/// <param name="bldr">The paragraph builder containing the guts of the annotation
		/// description</param>
		/// <param name="guidNoteType">The GUID representing the CmAnnotationDefn to use for
		/// the type</param>
		/// <returns>The newly created annotation</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote InsertNote(int bcvStartReference, int bcvEndReference,
			ICmObject obj, StTxtParaBldr bldr, Guid guidNoteType)
		{
			IScrScriptureNote note = m_annotations.InsertImportedNote(
				bcvStartReference, bcvEndReference, obj, obj, guidNoteType, bldr);

			return note;
		}

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version we use to back up originals of merged or overwritten books,
		/// creating a new one if necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrDraft BackupVersion
		{
			get { return m_savedVersion; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The saved version into which we are putting new books imported.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrDraft ImportedSavedVersion
		{
			get { return m_importedSavedVersion; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sequence of saved versions of Scripture books that will be restored in the
		/// event of an Undo.
		/// </summary>
		/// <remarks>This is implemented to support testing but could be useful for displaying
		/// somewhere in the UI some day.</remarks>
		/// <value>An FdoOwningSequence containing the books to restore.</value>
		/// ------------------------------------------------------------------------------------
		public IFdoOwningSequence<IScrBook> BooksToRestore
		{
			get { return (m_savedVersion == null) ? null : m_savedVersion.BooksOS; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal FilteredScrBooks BookFilter
		{
			get { return m_bookFilter; }
		}
		#endregion
	}
}

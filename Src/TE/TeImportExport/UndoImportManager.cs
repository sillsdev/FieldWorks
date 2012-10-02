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
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SILUBS.SharedScrUtils;

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
		private IScrDraft m_backupVersion;
		// saved version into which to put selected imported books.
		private IScrDraft m_importedVersion;
		private readonly FdoCache m_cache;
		private int m_lastBookAddedToImportedBooks;
		private int m_hMark = 0;
		private readonly IScripture m_scr;
		private IScrBookAnnotations m_annotations;
		/// <summary>For each book we import, we indicate whether or not we have imported the
		/// vernacular</summary>
		private readonly Dictionary<int, bool> m_importedBooks;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UndoImportManager"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public UndoImportManager(FdoCache cache)
		{
			m_cache = cache;
			m_scr = m_cache.LanguageProject.TranslatedScriptureOA;
			m_importedBooks = new Dictionary<int, bool>();
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

		#region Public methods
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
			m_backupVersion = GetOrCreateVersion(TeResourceHelper.GetResourceString("kstidSavedVersionDescriptionOriginal"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the new book (to the saved version...create it if need be) for which the
		/// vernacular is about to be imported.
		/// </summary>
		/// <param name="nCanonicalBookNumber">The canonical book number.</param>
		/// <param name="description">Description to use for the newly created imported version
		/// if necessary.</param>
		/// <param name="title">The title of the newly created book.</param>
		/// <returns>The newly created book (which has been added to the imported version)</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook AddNewBook(int nCanonicalBookNumber, string description, out IStText title)
		{
			if (m_importedVersion == null)
				m_importedVersion = GetOrCreateVersion(description);
			IScrBook existingBook = SetCurrentBook(nCanonicalBookNumber, true);

			if (existingBook != null)
			{
				if (m_lastBookAddedToImportedBooks == 0)
				{
					// We've been asked to create a book we have already imported (typically
					// reading multiple independent SF files).
					title = existingBook.TitleOA;
					return existingBook;
				}

				// Replace any previous book with the one we're about to import.
				m_importedVersion.BooksOS.Remove(existingBook);
			}

			IScrBook newScrBook = m_cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(m_importedVersion.BooksOS,
				nCanonicalBookNumber, out title);
			return newScrBook;
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
			IScrBook isvBook = SetCurrentBook(nCanonicalBookNumber, false);
			if (isvBook != null && m_importedBooks.ContainsKey(nCanonicalBookNumber))
				return isvBook;

			IScrBook cvBook = m_scr.FindBook(nCanonicalBookNumber);
			if (cvBook != null && fMakeBackup)
			{
				// Replace any existing book with the imported one.
				IScrBook oldBook = m_backupVersion.FindBook(nCanonicalBookNumber);
				if (oldBook != null)
					m_backupVersion.BooksOS.Remove(oldBook);
				m_backupVersion.AddBookCopy(cvBook);
			}

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
			{
				m_cache.ActionHandlerAccessor.Rollback(0);
				m_importedBooks.Remove(m_lastBookAddedToImportedBooks);
			}
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
			if (m_backupVersion != null && m_backupVersion.IsValidObject && m_backupVersion.BooksOS.Count == 0)
			{
				using (UndoableUnitOfWorkHelper uow =
					new UndoableUnitOfWorkHelper(m_cache.ActionHandlerAccessor, "Remove saved version"))
				{
					m_scr.ArchivedDraftsOC.Remove(m_backupVersion);
					uow.RollBack = false;
				}
				m_backupVersion = null;
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
		/// Undoes the entire import. Presumably there was either nothing in the file, or the
		/// user canceled during the first book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UndoEntireImport()
		{
			if (CollapseAllUndoActions())
				m_cache.ActionHandlerAccessor.Undo();
			m_importedBooks.Clear();
			m_importedVersion = null;
			m_backupVersion = null;

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
		#endregion

		#region Non-private properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected FdoCache Cache
		{
			get { return m_cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the canonical numbers of the books that were imported.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Dictionary<int, bool> ImportedBooks
		{
			get { return m_importedBooks; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the version we use to back up originals of merged or overwritten books,
		/// creating a new one if necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrDraft BackupVersion
		{
			get { return m_backupVersion; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The saved version into which we are putting new books imported.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrDraft ImportedVersion
		{
			get { return m_importedVersion; }
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
			get { return (m_backupVersion == null) ? null : m_backupVersion.BooksOS; }
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or creates an imported ScrDraft with the specified description.
		/// </summary>
		/// <param name="description">The description of the draft to get.</param>
		/// ------------------------------------------------------------------------------------
		private IScrDraft GetOrCreateVersion(string description)
		{
			IScrDraft draft = m_cache.ServiceLocator.GetInstance<IScrDraftRepository>().GetDraft(
				description, ScrDraftType.ImportedVersion);
			if (draft == null)
			{
				draft = m_cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(
					description, ScrDraftType.ImportedVersion);
			}

			return draft;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current book, particularly picking the right set of annotations
		/// to add new ones to. Also (and more conspicuously) ends the current Undo task
		/// and makes a new one for importing the new book. Should therefore be called
		/// BEFORE setting up the Undo action for the creation of the book.
		/// </summary>
		/// <param name="nCanonicalBookNumber">The canonical book number.</param>
		/// <param name="fVernacular">if set to <c>true</c> currently importing the vernacular.
		/// </param>
		/// <returns>The existing book in the current imported version, if any; otherwise
		/// <c>null</c></returns>
		/// <remarks>If importing annotations and/or BTs without importing the vernacular, the
		/// importer is responsible for calling this directly.</remarks>
		/// ------------------------------------------------------------------------------------
		private IScrBook SetCurrentBook(int nCanonicalBookNumber, bool fVernacular)
		{
			if (nCanonicalBookNumber <= 0 || nCanonicalBookNumber > BCVRef.LastBook)
				throw new ArgumentOutOfRangeException("nCanonicalBookNumber", nCanonicalBookNumber, "Expected a canonical book number.");

			IActionHandler actionHandler = m_cache.DomainDataByFlid.GetActionHandler();

			// We want a new undo task for each new book, except the first one
			if (m_importedBooks.Count > 0)
				actionHandler.EndUndoTask();

			if (actionHandler.CurrentDepth == 0)
			{
				// No need to use localizable string from resources because the user will never
				// see these labels because we collapse to a single undo task when the import
				// completes.
				actionHandler.BeginUndoTask("Undo Import Book " + nCanonicalBookNumber,
					"Redo Import Book " + nCanonicalBookNumber);
			}
			if (m_importedBooks.ContainsKey(nCanonicalBookNumber))
				m_lastBookAddedToImportedBooks = 0;
			else
			{
				m_lastBookAddedToImportedBooks = nCanonicalBookNumber;
				m_importedBooks[nCanonicalBookNumber] = fVernacular;
			}

			m_annotations = m_scr.BookAnnotationsOS[nCanonicalBookNumber - 1];

			return (m_importedVersion == null) ? null : m_importedVersion.FindBook(nCanonicalBookNumber);
		}
		#endregion
	}
}

// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using ParatextImport.Properties;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace ParatextImport
{
	/// <summary>
	/// This class has a handful of static methods involved in dealing with ImportedBooks
	/// </summary>
	public class ImportedBooks
	{
		/// <summary>
		///  Save the books that are being imported into the cache
		/// </summary>
		internal static void SaveImportedBooks(LcmCache cache, IScrDraft importedVersion, IScrDraft backupSavedVersion, Dictionary<int, bool>.KeyCollection booksImported, Form wnd)
		{
			var wasCancelled = false;
			var translatedScripture = cache.LangProject.TranslatedScriptureOA;
			foreach (var bookId in booksImported)
			{
				var rev = importedVersion.FindBook(bookId);
				// if the imported version does not have the book then we should just quit trying
				if (rev == null)
				{
					return;
				}
				var curBook = translatedScripture?.FindBook(rev.CanonicalNum);
				if (curBook == null)
				{
					// User should not see this undo task so we don't need to localize the strings.
					UndoableUnitOfWorkHelper.Do("Add book", "Add book", cache.ServiceLocator.GetInstance<IActionHandler>(),
						() => { AddBook(translatedScripture.CopyBookToCurrent(rev)); });
				}
				else
				{
					using (var bookMerger = new BookMerger(cache, rev))
					using (var progressDlg = new ProgressDialogWithTask(wnd))
					{
						bookMerger.AttemptAutoMerge = true;
						bookMerger.UseFilteredDiffList = true;
						progressDlg.Title = Resources.kstidImportProgressCaption;
						Debug.Assert(bookMerger.BookRev != null);
						progressDlg.Message = string.Format(Resources.kstidMergeProgress, bookMerger.BookRev.BestUIName);
						progressDlg.CancelButtonText = Resources.kstidStopImporting;

						// User should not see undo tasks, so the strings don't need to be localized.
						UndoableUnitOfWorkHelper.Do("Automerge", "Automerge", cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
						{
							try
							{
								progressDlg.RunTask(bookMerger.DetectDifferences);
								progressDlg.Message = string.Format(Resources.kstidAutoAcceptMergeProgress, bookMerger.BookRev.BestUIName);
								progressDlg.RunTask(true, bookMerger.AcceptAllChanges);
							}
							catch (WorkerThreadException e)
							{
								if (e.InnerException is CancelException)
								{
									// The current version of
									wasCancelled = true;
									return;
								}
								throw;
							}
						});
					}
					if (wasCancelled)
					{
						return;
					}
				}
			}

			using (new WaitCursor(wnd))
			{
				// User should not see undo tasks, so the strings don't need to be localized.
				UndoableUnitOfWorkHelper.Do("Remove empty", "Remove empty", cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					// Enhance JohnT: Normally we would make sure all these changes were a single Undo action,
					// but for now we know this is part of a still larger Undoable task, the whole import.
					// CollapseToMark is used to convert them all into one.

					// Delete empty revisions created for this import.
					//if (m_booksImported.BooksOS.Count == 0)
					//    m_booksImported.DeleteUnderlyingObject();
					if (backupSavedVersion.BooksOS.Count == 0)
					{
						translatedScripture.ArchivedDraftsOC.Remove(backupSavedVersion);
						backupSavedVersion = null;
					}
				});
			}
		}

		#region Internal static methods

		/// <summary>
		/// Reports potential data loss to the user if they could overwrite the book.
		/// </summary>
		/// <param name="originalBook">The original book.</param>
		/// <param name="scrDraftType">
		///     whether the draft is from a saved version or an imported
		///     version.
		/// </param>
		/// <param name="owner">The window's owner.</param>
		/// <param name="sDetails">
		///     The details about which verses would be deleted with an
		///     overwrite.
		/// </param>
		internal static void ReportDataLoss(IScrBook originalBook, ScrDraftType scrDraftType, IWin32Window owner, string sDetails)
		{
			var sType = scrDraftType == ScrDraftType.ImportedVersion ? Resources.kstidImported : Resources.kstidSaved;
			var sMsg = string.Format(Resources.kstidDataLossMsg, sType, originalBook.BestUIName, sDetails);
			MessageBox.Show(owner, sMsg, Resources.kstidDataLossCaption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
		}

		#endregion

		#region Private helpers

		/// <summary>
		/// Create a book name.
		/// </summary>
		/// <param name="book">specified book</param>
		/// <returns>
		///     If the specified book is a full book, only the name of the book will be returned.
		/// Otherwise, the name of the book followed by its first and last reference in parentheses
		///     will be returned.
		/// </returns>
		internal static string GetBookInfo(IScrBook book)
		{
			// Determine the first and last reference and whether the book has an introduction
			BCVRef verseMin;
			BCVRef verseMax;
			bool fHasIntro;
			GetBookMinMaxVerse(book, out verseMin, out verseMax, out fHasIntro);

			if (fHasIntro)
			{
				return verseMin == null
					? Resources.kstidIntroOnly
					: ReferenceRangeStr(verseMin, verseMax) + " " +
					  Resources.kstidIntro;
			}
			if (verseMin != null)
			{
				return ReferenceRangeStr(verseMin, verseMax);
			}

			// A book with no sections should never happen (but unfortunately has). This
			// text ("no sections") is used in this case to prevent a crash, but is not localized.
			var bookName = !string.IsNullOrEmpty(book.BestUIName) ? book.BestUIName : "book";
			Debug.Fail("No introduction or Scripture sections in " + bookName);
			return "(no sections)";
		}

		/// <summary>
		/// Create a reference range string for the specified beginning and ending verse numbers.
		/// </summary>
		/// <param name="verseMin">beginning verse number</param>
		/// <param name="verseMax">ending verse number</param>
		/// <returns>
		///     Chapter/Verse range in this format: CC:VV-CC:VV if the beginning and
		///     ending chapters are different OR CC:VV-VV if the chapters are the same
		/// </returns>
		private static string ReferenceRangeStr(BCVRef verseMin, BCVRef verseMax)
		{
			return ReferenceStr(verseMin, true) + "-" + ReferenceStr(verseMax, verseMin.Chapter != verseMax.Chapter);
		}

		/// <summary>
		/// Gets the chapter:verse as a string.
		/// </summary>
		/// <param name="verse">The verse.</param>
		/// <param name="includeChapter">if set to <c>true</c> include the chapter.</param>
		private static string ReferenceStr(BCVRef verse, bool includeChapter)
		{
			return includeChapter ? verse.Chapter + ":" + verse.Verse : verse.Verse.ToString();
		}

		/// <summary>
		/// Get the minimum and maximum reference for the book. If all the verses are present
		/// for a book or no sections are present, the verseMin and VerseMax will be null.
		/// </summary>
		/// <param name="book">book to check for verse range.</param>
		/// <param name="verseMin">out: beginning Scripture reference.</param>
		/// <param name="verseMax">out: ending Scripture reference.</param>
		/// <param name="fBookHasIntro">out: <c>true</c> if the book has an introduction section</param>
		private static void GetBookMinMaxVerse(IScrBook book, out BCVRef verseMin, out BCVRef verseMax, out bool fBookHasIntro)
		{
			verseMin = null;
			verseMax = null;
			fBookHasIntro = false;
			if (book.SectionsOS.Count == 0)
			{
				Debug.Fail("Book has zero sections.");
				return;
			}

			// Determine if the first sentence has an introduction
			fBookHasIntro = book.SectionsOS[0].IsIntro;

			// Get the beginning Scripture reference in the book.
			foreach (var section in book.SectionsOS)
			{
				if (!section.IsIntro)
				{
					verseMin = section.VerseRefMin;
					break;
				}
			}

			// Get the beginning Scripture reference in the book.
			for (var iSection = book.SectionsOS.Count - 1; iSection >= 0; iSection--)
			{
				if (!book.SectionsOS[iSection].IsIntro)
				{
					verseMax = book.SectionsOS[iSection].VerseRefMax;
					break;
				}
			}
		}
		#endregion

		#region internal stuff to facilitate testing

		/// <summary>
		/// Called when a book is added.
		/// </summary>
		internal static void AddBook(IScrBook newBook)
		{
		}

		#endregion
	}
}

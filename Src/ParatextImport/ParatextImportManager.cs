// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.Reporting;
using SIL.LCModel.Utils;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Infrastructure;

namespace ParatextImport
{
	/// <summary>
	/// Helper class that manages the different aspects of import: interacting with user,
	/// settings and then delegating the real work...
	/// </summary>
	public class ParatextImportManager
	{
		#region Member data

		/// <summary>
		/// Import settings provided by FLEx.
		/// </summary>
		protected readonly IScrImportSet m_importSettings;
		private readonly Form m_mainWnd;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly IApp m_app;
		/// <summary>
		/// This keeps track of stuff we may need to Undo.
		/// </summary>
		protected UndoImportManager m_undoImportManager;
		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="ParatextImportManager"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window initiating the import</param>
		/// <param name="cache"></param>
		/// <param name="importSettings"></param>
		/// <param name="styleSheet">The stylesheet.</param>
		/// <param name="app">The app.</param>
		internal ParatextImportManager(Form mainWnd, LcmCache cache, IScrImportSet importSettings, LcmStyleSheet styleSheet, IApp app)
		{
			m_mainWnd = mainWnd; // Null for tests.
			Cache = cache;
			m_importSettings = importSettings;
			m_helpTopicProvider = app as IHelpTopicProvider;
			m_app = app;
			StyleSheet = styleSheet;
		}
		#endregion

		#region Public Static methods

		/// <summary>
		/// Perform a Paratext streamlined import
		/// </summary>
		/// <param name="mainWnd">The main window.</param>
		/// <param name="cache"></param>
		/// <param name="importSettings">Import settings for this import. Theory has it it is only one book.</param>
		/// <param name="stylesheet">The Scripture stylesheet.</param>
		/// <param name="app">The app.</param>
		/// <returns><c>true</c> if something got imported; <c>false</c> otherwise</returns>
		/// <remarks>
		/// Called using Reflection from TextsTriStateTreeView.
		/// </remarks>
		public static bool ImportParatext(Form mainWnd, LcmCache cache, IScrImportSet importSettings, LcmStyleSheet stylesheet, IApp app)
		{
			var mgr = new ParatextImportManager(mainWnd, cache, importSettings, stylesheet, app);
			return mgr.ImportSf();
		}
		#endregion

		#region Miscellaneous protected methods
		/// <summary>
		/// Get settings for and perform the Standard Format import
		/// </summary>
		/// <returns><c>true</c> if something got imported; <c>false</c> otherwise</returns>
		private bool ImportSf()
		{
			try
			{
				ScrReference firstImported;
				using (new WaitCursor(m_mainWnd, true))
				{
					m_app.EnableMainWindows(false);
					firstImported = ImportWithUndoTask(true, "ImportStandardFormat");
				}
				firstImported = CompleteImport(firstImported);

				// Remove all archived drafts produced on import, as FLEx doesn't need them.
				// Keeping them around only serves to grow the data set forever for no benefit.
				// NB: This will also delete archived books from other imports, even back to when TE was making them.
				using (new WaitCursor(m_mainWnd, true))
				{
					NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					{
						foreach (var archivedDraft in Cache.LanguageProject.TranslatedScriptureOA.ArchivedDraftsOC.ToList())
						{
							archivedDraft.Delete();
						}
					});
				}

				return firstImported != ScrReference.Empty;
			}
			finally
			{
				m_app.EnableMainWindows(true);
			}
		}

		/// <summary>
		/// Creates a ParatextImportUi object.
		/// </summary>
		/// <param name="progressDialog">The progress dialog.</param>
		/// <returns>A ParatextImportUi object</returns>
		/// <remarks>Can be overriden in tests</remarks>
		protected virtual ParatextImportUi CreateParatextImportUi(ProgressDialogWithTask progressDialog)
		{
			return new ParatextImportUi(progressDialog, m_helpTopicProvider);
		}

		/// <summary>
		/// Gets the undo manager (currently only used for tests).
		/// </summary>
		internal UndoImportManager UndoManager => m_undoImportManager;

		/// <summary>
		/// Gets the imported saved version (currenly only used for tests).
		/// </summary>
		protected IScrDraft ImportedVersion => m_undoImportManager.ImportedVersion;

		/// <summary>
		/// Gets the cache.
		/// </summary>
		protected LcmCache Cache { get; }

		/// <summary>
		/// Gets the style sheet.
		/// </summary>
		protected LcmStyleSheet StyleSheet { get; }

		/// <summary>
		/// Import scripture and embed it in a Undo task so that it is undoable.
		/// Nb: this creates a Mark in the undo stack (unless no books at all are imported or an
		/// error occurs), which is designed to be 'collapsed' to after all books in the batch
		/// are imported, so we get a single Undo task for the whole Import in the end. Code
		/// (such as tests) that calls this directly should call
		/// UndoManager.CollapseAllUndoActions().
		/// </summary>
		/// <param name="fDisplayUi">set to <c>true</c> to display the progress dialog,
		/// <c>false</c> to run without UI.</param>
		/// <param name="updateDescription">description of the data update being done (i.e.,
		/// which type of import).</param>
		/// <returns>The reference of the first thing that was imported</returns>
		protected ScrReference ImportWithUndoTask(bool fDisplayUi, string updateDescription)
		{
			m_undoImportManager = new UndoImportManager(Cache);
			if (m_mainWnd == null)
			{
				// Can happen in tests (and is probably the only time we'll get here).
				return InternalImport(fDisplayUi);
			}

			using (new DataUpdateMonitor(m_mainWnd, updateDescription))
			{
				return InternalImport(fDisplayUi);
			}
		}

		/// <summary>
		/// Actually does the import, really.
		/// </summary>
		/// <param name="fDisplayUi">if set to <c>true</c> shows the UI.</param>
		/// <returns>The first reference that was imported</returns>
		private ScrReference InternalImport(bool fDisplayUi)
		{
			var firstImported = ScrReference.Empty;
			var fPartialBtImported = false;
			try
			{
				Logger.WriteEvent("Starting import");
				using (var progressDlg = new ProgressDialogWithTask(m_mainWnd))
				{
					progressDlg.CancelButtonText = Properties.Resources.kstidStopImporting;
					progressDlg.Title = Properties.Resources.kstidImportProgressCaption;
					progressDlg.Message = Properties.Resources.kstidImportInitializing;

					using (var importUi = CreateParatextImportUi(progressDlg))
					{
						firstImported = (ScrReference)progressDlg.RunTask(fDisplayUi, ImportTask, importUi);
					}
				}
			}
			catch (WorkerThreadException e)
			{
				if (e.InnerException is ScriptureUtilsException)
				{
					var se = (ScriptureUtilsException)e.InnerException;
					if (m_helpTopicProvider != null)
					{
						var sCaption = GetDialogCaption(se.ImportErrorCodeType);
						// TODO-Linux: Help is not implemented in Mono
						MessageBox.Show(m_mainWnd, se.Message, sCaption, MessageBoxButtons.OK,
							MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0, m_helpTopicProvider.HelpFile,
							HelpNavigator.Topic, se.HelpTopic);
					}
					if (se.ImportErrorCodeType == ErrorCodeType.BackTransErrorCode && !se.InterleavedImport)
						fPartialBtImported = true;
				}
				else if (e.InnerException is ParatextLoadException)
				{
					if (!MiscUtils.RunningTests)
					{
						Logger.WriteError(e);
						var sCaption = ScriptureUtilsException.GetResourceString("kstidImportErrorCaption");
						var innerE = e.InnerException;
						var sbMsg = new StringBuilder(innerE.Message);
						while (innerE.InnerException != null)
						{
							innerE = innerE.InnerException;
							sbMsg.AppendLine();
							sbMsg.Append(innerE.Message);
						}

						MessageBoxUtils.Show(m_mainWnd, sbMsg.ToString(), sCaption, MessageBoxButtons.OK,
						MessageBoxIcon.Error);
					}
				}
				else if (!(e.InnerException is CancelException))
				{
					// User didn't just cancel import in the middle of a book -- let's die.
					throw e;
				}
			}

			if (m_undoImportManager.ImportedBooks.Count == 0 && !fPartialBtImported)
			{
				Logger.WriteEvent("Nothing imported. Undoing Import.");
				// Either there was nothing in the file, or the user canceled during the first book.
				// In any case, we didn't get any books, so whatever has been done should be undone.
				m_undoImportManager.UndoEntireImport();
				return null;
			}
			return firstImported;
		}

		/// <summary>
		/// Gets the caption for the Unable to Import message box.
		/// </summary>
		/// <param name="codeType">Type of the code.</param>
		/// <returns>string for the message box caption</returns>
		private string GetDialogCaption(ErrorCodeType codeType)
		{
			switch(codeType)
			{
				case ErrorCodeType.BackTransErrorCode:
					return ScriptureUtilsException.GetResourceString("kstidBTImportErrorCaption");
				case ErrorCodeType.XmlErrorCode:
					return ScriptureUtilsException.GetResourceString("kstidXmlImportErrorCaption");
				default:
					return ScriptureUtilsException.GetResourceString("kstidImportErrorCaption");
			}
		}

		/// <summary>
		/// Completes the import.
		/// </summary>
		/// <param name="firstImported">The reference of the first thing that was imported</param>
		protected ScrReference CompleteImport(ScrReference firstImported)
		{
			if (firstImported == null)
			{
				return ScrReference.Empty;
			}

			// Display the ImportedBooks dialog if we imported any vernacular Scripture.
			if (m_undoImportManager.ImportedBooks.Any(x => x.Value))
			{
				SaveImportedBooks(m_undoImportManager.BackupVersion);
			}

			m_undoImportManager.RemoveEmptyBackupSavedVersion();
			// Keeping versions we made just for PT imports (which always entirely replace the current non-archived ones)
			// just clutters things up and makes S/R more expensive.
				m_undoImportManager.RemoveImportedVersion();
			m_undoImportManager.CollapseAllUndoActions();
			// sync stuff
			if (m_app == null)
			{
				return firstImported;
			}
			using (new WaitCursor(m_mainWnd))
			{
				// Refresh all the views of all applications connected to the same DB. This
				// will cause any needed Scripture data to be reloaded lazily.
				m_app.Synchronize(SyncMsg.ksyncStyle);
			}
			return firstImported;
		}

		/// <summary>
		/// The import task.
		/// </summary>
		/// <param name="progressDlg">The progress DLG.</param>
		/// <param name="parameters">The parameters.</param>
		/// <returns></returns>
		private object ImportTask(IProgress progressDlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			var importUi = (ParatextImportUi)parameters[0];

			var fRollbackPartialBook = true;
			try
			{
				Logger.WriteEvent("Starting import task");
				m_undoImportManager.StartImportingFiles();
				var firstRef = Import(importUi);
				fRollbackPartialBook = false;
				return firstRef;
			}
			catch (ScriptureUtilsException e)
			{
				if (e.ImportErrorCodeType == ErrorCodeType.BackTransErrorCode && !e.InterleavedImport)
				{
					// Errors in non-interleaved BT can never leave the data in a bad state,
					// so we can keep whatever part was successfully imported.
					fRollbackPartialBook = false;
				}
				throw;
			}
			finally
			{
				m_undoImportManager.DoneImportingFiles(fRollbackPartialBook);
				Logger.WriteEvent("Finished importing");
			}
		}

		/// <summary>
		/// Calls the importer.
		/// </summary>
		/// <param name="importUi">The import UI.</param>
		/// <returns></returns>
		protected virtual ScrReference Import(ParatextImportUi importUi)
		{
			return ParatextSfmImporter.Import(m_importSettings, Cache, StyleSheet, m_undoImportManager, importUi);
			}

		#endregion

		#region Merging Differences
		/// <summary>
		/// Displays the imported books dialog box (virtual to allow tests to suppress display
		/// of dialog box).
		/// </summary>
		/// <param name="backupSavedVersion">The saved version for backups of any overwritten
		/// books.</param>
		protected virtual void SaveImportedBooks(IScrDraft backupSavedVersion)
		{
			ImportedBooks.SaveImportedBooks(Cache, ImportedVersion, backupSavedVersion, UndoManager.ImportedBooks.Keys, m_mainWnd);
		}
		#endregion
	}
}

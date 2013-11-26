// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2005' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeImportManager.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using System;
using System.Collections.Specialized; // for StringCollection
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;
using XCore;
using ProgressBarStyle = SIL.FieldWorks.Common.FwUtils.ProgressBarStyle;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class that manages the different aspects of import: interacting with user,
	/// settings and then delegating the real work...
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeImportManager
	{
		#region Member data
		private readonly FdoCache m_cache;
		private readonly Form m_mainWnd;
		private readonly ITeImportCallbacks m_importCallbacks;
		private readonly FwStyleSheet m_styleSheet;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly IApp m_app;
		private readonly bool m_fParatextStreamlinedImport;
		private string m_sOXESFile;

		/// <summary>
		/// This keeps track of stuff we may need to Undo.
		/// </summary>
		private UndoImportManager m_undoImportManager;
		#endregion

		private class DummyImportCallbacks : ITeImportCallbacks
		{
			#region ITeImportCallbacks Members

			public FilteredScrBooks BookFilter
			{
				get { return null; }
			}

			public float DraftViewZoomPercent
			{
				get { return 1.0f; }
			}

			public float FootnoteZoomPercent
			{
				get { return 1.0f; }
			}

			public bool GotoVerse(ScrReference targetRef)
			{
				throw new NotImplementedException();
			}

			public void UpdateKeyTermsView()
			{
				throw new NotImplementedException();
			}
			#endregion
		}

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeImportManager"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window initiating the import</param>
		/// <param name="importCallbacks">The import callbacks.</param>
		/// <param name="app">The app.</param>
		/// <param name="fParatextStreamlinedImport">if set to <c>true</c> do a Paratext
		/// streamlined import (minimal UI).</param>
		/// ------------------------------------------------------------------------------------
		internal TeImportManager(FwMainWnd mainWnd, ITeImportCallbacks importCallbacks,
			FwApp app, bool fParatextStreamlinedImport)
			: this(app.Cache, mainWnd.StyleSheet, app, fParatextStreamlinedImport)
		{
			m_mainWnd = mainWnd;
			m_importCallbacks = importCallbacks;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeImportManager"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window initiating the import</param>
		/// <param name="styleSheet">The stylesheet.</param>
		/// <param name="app">The app.</param>
		/// <param name="fParatextStreamlinedImport">if set to <c>true</c> do a Paratext
		/// streamlined import (minimal UI).</param>
		/// ------------------------------------------------------------------------------------
		internal TeImportManager(Form mainWnd, FwStyleSheet styleSheet, FwApp app,
			bool fParatextStreamlinedImport)
			: this(app.Cache, styleSheet, app, fParatextStreamlinedImport)
		{
			m_mainWnd = mainWnd;
			m_importCallbacks = new DummyImportCallbacks();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeImportManager"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="app">The app.</param>
		/// <param name="fParatextStreamlinedImport">if set to <c>true</c> do a Paratext
		/// streamlined import (minimal UI).</param>
		/// <remarks>This version is for testing only</remarks>
		/// ------------------------------------------------------------------------------------
		protected TeImportManager(FdoCache cache, FwStyleSheet styleSheet, IApp app,
			bool fParatextStreamlinedImport)
		{
			m_cache = cache;
			m_helpTopicProvider = app as IHelpTopicProvider;
			m_app = app;
			m_styleSheet = styleSheet;
			m_fParatextStreamlinedImport = fParatextStreamlinedImport;
		}
		#endregion

		#region Public Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the Standard Format import
		/// </summary>
		/// <param name="mainWnd">The main window.</param>
		/// <param name="importCallbacks">The import callbacks.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public static void ImportSf(FwMainWnd mainWnd, ITeImportCallbacks importCallbacks,
			FwApp app)
		{
			TeImportManager mgr = new TeImportManager(mainWnd, importCallbacks, app, false);
			mgr.ImportSf();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform a Paratext streamlined import
		/// </summary>
		/// <param name="mainWnd">The main window.</param>
		/// <param name="stylesheet">The Scripture stylesheet.</param>
		/// <param name="app">The app.</param>
		/// <returns><c>true</c> if something got imported; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool ImportParatext(Form mainWnd, FwStyleSheet stylesheet, FwApp app)
		{
			TeImportManager mgr = new TeImportManager(mainWnd, stylesheet, app, true);
			return mgr.ImportSf();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import an OXES (Open XML for Editing Scripture) file.
		/// </summary>
		/// <param name="mainWnd">The main WND.</param>
		/// <param name="importCallbacks">The import callbacks.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public static void ImportXml(FwMainWnd mainWnd, ITeImportCallbacks importCallbacks,
			FwApp app)
		{
			TeImportManager mgr = new TeImportManager(mainWnd, importCallbacks, app, false);
			mgr.ImportXml();
		}
		#endregion

		#region Miscellaneous protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get settings for and perform the Standard Format import
		/// </summary>
		/// <returns><c>true</c> if something got imported; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected bool ImportSf()
		{
			IScrImportSet importSettings;

			if (m_fParatextStreamlinedImport)
			{
				importSettings = m_cache.LangProject.TranslatedScriptureOA.FindImportSettings(TypeOfImport.Paratext6);
				if (importSettings == null)
					throw new InvalidOperationException("Caller must set import settings before attempting a streamlined Paratext import");
			}
			else
			{
				using (new WaitCursor(m_mainWnd))
				{
					importSettings = GetImportSettings();
				}
				if (importSettings == null) // User cancelled in import wizard
					return false;

				// Display ImportDialog
				using (ImportDialog importDlg = new ImportDialog(m_styleSheet, m_cache,
					importSettings, m_helpTopicProvider, m_app))
				{
					importDlg.ShowDialog(m_mainWnd);
					if (importDlg.DialogResult == DialogResult.Cancel)
					{
						Logger.WriteEvent("User canceled import dialog");
						return false;
					}
					// Settings could have changed if the user went into the wizard.
					importSettings = importDlg.ImportSettings;
				}
			}

			return DoImport(importSettings, "ImportStandardFormat");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does the import.
		/// </summary>
		/// <param name="importSettings">The import settings (can be null, as is the case for
		/// OXES import).</param>
		/// <param name="updateDescription">description of the data update being done (i.e.,
		/// which type of import).</param>
		/// <returns><c>true</c> if something got imported; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool DoImport(IScrImportSet importSettings, string updateDescription)
		{
			try
			{
				ScrReference firstImported;
				using (new WaitCursor(m_mainWnd, true))
				{
					m_app.EnableMainWindows(false);
					firstImported = ImportWithUndoTask(importSettings, true, updateDescription);
				}
				firstImported = CompleteImport(firstImported);
				if (!m_fParatextStreamlinedImport)
					SetSelectionsAfterImport(firstImported);
				return (firstImported != ScrReference.Empty);
			}
			finally
			{
				m_app.EnableMainWindows(true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the settings for Import, either from database or from wizard
		/// </summary>
		/// <returns>Import settings, or <c>null</c> if user canceled dialog.</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrImportSet GetImportSettings()
		{
			ILangProject proj = m_cache.LangProject;
			IScripture scr = proj.TranslatedScriptureOA;
			IScrImportSet importSettings = null;
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				importSettings =
					scr.FindOrCreateDefaultImportSettings(TypeOfImport.Unknown);
			});
			importSettings.StyleSheet = m_styleSheet;
			importSettings.HelpFile = m_helpTopicProvider.HelpFile;

			importSettings.OverlappingFileResolver = new ConfirmOverlappingFileReplaceDialog(m_helpTopicProvider);
			if (!importSettings.BasicSettingsExist)
			{
				using (NonUndoableUnitOfWorkHelper undoHelper = new NonUndoableUnitOfWorkHelper(
					m_cache.ServiceLocator.GetInstance<IActionHandler>()))
				{
					using (ImportWizard importWizard = new ImportWizard(m_cache.ProjectId.Name,
						scr, m_styleSheet, m_helpTopicProvider, m_app))
					{
						if (importWizard.ShowDialog() == DialogResult.Cancel)
							return null;
						// Scripture reference range may have changed
						ImportDialog.ClearDialogReferences();
						importSettings = scr.DefaultImportSettings;
					}
					undoHelper.RollBack = false;
				}
			}
			else
			{
				StringCollection sInvalidFiles;
				bool fCompletedWizard = false;
				while (!importSettings.ImportProjectIsAccessible(out sInvalidFiles))
				{
					// Display the "Project Not Found" message box
					using (ScrImportSetMessage dlg = new ScrImportSetMessage())
					{
						string[] files = new string[sInvalidFiles.Count];
						sInvalidFiles.CopyTo(files, 0);
						dlg.InvalidFiles = files;
						dlg.HelpURL = m_helpTopicProvider.HelpFile;
						dlg.HelpTopic = "/Beginning_Tasks/Import_Standard_Format/Project_Files_Unavailable.htm";
						dlg.DisplaySetupOption = true;
						switch(dlg.ShowDialog())
						{
							case DialogResult.OK: // Setup...
							{
								using (NonUndoableUnitOfWorkHelper undoHelper = new NonUndoableUnitOfWorkHelper(
									m_cache.ServiceLocator.GetInstance<IActionHandler>()))
								{
									using (ImportWizard importWizard = new ImportWizard(
										m_cache.ProjectId.Name, scr, m_styleSheet, m_helpTopicProvider, m_app))
									{
										if (importWizard.ShowDialog() == DialogResult.Cancel)
											return null;
										// Scripture reference range may have changed
										ImportDialog.ClearDialogReferences();
										importSettings = scr.DefaultImportSettings;
										fCompletedWizard = true;
									}
									undoHelper.RollBack = false;
								}
								break;
							}
							case DialogResult.Cancel:
								return null;
							case DialogResult.Retry:
								// Loop around until user gets tired.
								break;
						}
					}
				}
				if (!fCompletedWizard)
				{
					if (ParatextProjHasUnmappedMarkers(importSettings))
					{
						// TODO: Show message box and then bring up import wizard
					}
				}
			}

			return importSettings;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a TeImportUi object.
		/// </summary>
		/// <param name="progressDialog">The progress dialog.</param>
		/// <returns>A TeImportUi object</returns>
		/// <remarks>Can be overriden in tests</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual TeImportUi CreateTeImportUi(ProgressDialogWithTask progressDialog)
		{
			return new TeImportUi(progressDialog, m_helpTopicProvider);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the undo manager (currenly only used for tests).
		/// </summary>
		/// <value>T.</value>
		/// ------------------------------------------------------------------------------------
		protected UndoImportManager UndoManager
		{
			get { return m_undoImportManager; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the imported saved version (currenly only used for tests).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected IScrDraft ImportedVersion
		{
			get { return m_undoImportManager.ImportedVersion; }
		}

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
		/// Gets the style sheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected FwStyleSheet StyleSheet
		{
			get { return m_styleSheet; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import scripture and embed it in a Undo task so that it is undoable.
		/// Nb: this creates a Mark in the undo stack (unless no books at all are imported or an
		/// error occurs), which is designed to be 'collapsed' to after all books in the batch
		/// are imported, so we get a single Undo task for the whole Import in the end. Code
		/// (such as tests) that calls this directly should call
		/// UndoManager.CollapseAllUndoActions().
		/// </summary>
		/// <param name="importSettings">The SFM import settings.  If null, then this is an XML import.</param>
		/// <param name="fDisplayUi">set to <c>true</c> to display the progress dialog,
		/// <c>false</c> to run without UI.</param>
		/// <param name="updateDescription">description of the data update being done (i.e.,
		/// which type of import).</param>
		/// <returns>The reference of the first thing that was imported</returns>
		/// ------------------------------------------------------------------------------------
		protected ScrReference ImportWithUndoTask(IScrImportSet importSettings,
			bool fDisplayUi, string updateDescription)
		{
			m_undoImportManager = new UndoImportManager(m_cache);
			if (m_mainWnd == null)
			{
				// Can happen in tests (and is probably the only time we'll get here).
				return InternalImport(importSettings, fDisplayUi);
			}

			using (new DataUpdateMonitor(m_mainWnd, updateDescription))
			{
				return InternalImport(importSettings, fDisplayUi);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Actually does the import, really.
		/// </summary>
		/// <param name="importSettings">The import settings.</param>
		/// <param name="fDisplayUi">if set to <c>true</c> shows the UI.</param>
		/// <returns>The first reference that was imported</returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private ScrReference InternalImport(IScrImportSet importSettings, bool fDisplayUi)
		{
			ScrReference firstImported = ScrReference.Empty;
			bool fPartialBtImported = false;
			try
			{
				Logger.WriteEvent("Starting import");
				using (var progressDlg = new ProgressDialogWithTask(m_mainWnd, m_cache.ThreadHelper))
				{
					progressDlg.CancelButtonText =
						TeResourceHelper.GetResourceString("kstidStopImporting");
					progressDlg.Title =
						TeResourceHelper.GetResourceString("kstidImportProgressCaption");
					progressDlg.Message =
						TeResourceHelper.GetResourceString("kstidImportInitializing");
					if (importSettings == null) // XML (OXES) import
						progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;

					using (TeImportUi importUi = CreateTeImportUi(progressDlg))
					{
						firstImported = (ScrReference)progressDlg.RunTask(fDisplayUi,
							ImportTask, importSettings, m_undoImportManager, importUi);
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
						string sCaption = GetDialogCaption(se.ImportErrorCodeType);
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
						string sCaption = ScriptureUtilsException.GetResourceString("kstidImportErrorCaption");
						Exception innerE = e.InnerException;
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
					throw;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the caption for the Unable to Import message box.
		/// </summary>
		/// <param name="codeType">Type of the code.</param>
		/// <returns>string for the message box caption</returns>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Completes the import.
		/// </summary>
		/// <param name="firstImported">The reference of the first thing that was imported</param>
		/// ------------------------------------------------------------------------------------
		protected ScrReference CompleteImport(ScrReference firstImported)
		{
			if (firstImported == null)
				return ScrReference.Empty;

			// An empty first imported reference can happen if we imported just the BT.
			//Debug.Assert(!firstImported.IsEmpty, "We should have a useful reference if we imported something!");

			// Display the ImportedBooks dialog if we imported any vernacular Scripture.
			if (m_undoImportManager.ImportedBooks.Any(x => x.Value))
				DisplayImportedBooksDlg(m_undoImportManager.BackupVersion);

			m_undoImportManager.RemoveEmptyBackupSavedVersion();
			m_undoImportManager.CollapseAllUndoActions();
			// sync stuff
			if (m_app != null)
			{
				using (new WaitCursor(m_mainWnd))
				{
					// Refresh all the views of all applications connected to the same DB. This
					// will cause any needed Scripture data to be reloaded lazily.
					m_app.Synchronize(SyncMsg.ksyncStyle);
				}
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
			Debug.Assert(parameters.Length == 3);
			var importSettings = (IScrImportSet)parameters[0];
			var undoManager = (UndoImportManager)parameters[1];
			var importUi = (TeImportUi)parameters[2];

			bool fRollbackPartialBook = true;
			try
			{
				Logger.WriteEvent("Starting import task");
				undoManager.StartImportingFiles();
				ScrReference firstRef = Import(importSettings, undoManager, importUi);
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
				undoManager.DoneImportingFiles(fRollbackPartialBook);
				Logger.WriteEvent("Finished importing");
			}
		}

		/// <summary>
		/// Calls the importer.
		/// </summary>
		/// <param name="importSettings">The import settings.</param>
		/// <param name="undoManager">The undo manager.</param>
		/// <param name="importUi">The import UI.</param>
		/// <returns></returns>
		protected virtual ScrReference Import(IScrImportSet importSettings, UndoImportManager undoManager,
			TeImportUi importUi)
		{
			if (importSettings != null)
			{
				return TeSfmImporter.Import(importSettings, m_cache, m_styleSheet,
					undoManager, importUi);
			}

			return (ScrReference) TeXmlImporter.Import(m_cache, m_styleSheet, m_sOXESFile,
				undoManager, importUi);
		}

		#endregion

		#region Methods for supporting checking for unmapped Paratext markers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Iterate through the ScriptureTexts' lists of tags. If any missing mappings are found
		/// return true to give the user a chance to use the ImportWizard to map everything.
		/// </summary>
		/// <param name="settings">Import settings object</param>
		/// <returns><c>true</c> if the settings represent a P6 project which has markers (tags)
		/// in its stylesheet which the user has not had a chance to map in the import wizard.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected bool ParatextProjHasUnmappedMarkers(IScrImportSet settings)
		{
			// Load ScriptureText object
			if (settings.ImportTypeEnum != TypeOfImport.Paratext6)
				return false;

			return (ParatextProjHasUnmappedMarkers(settings.ParatextScrProj) ||
				ParatextProjHasUnmappedMarkers(settings.ParatextBTProj) ||
				ParatextProjHasUnmappedMarkers(settings.ParatextNotesProj));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Iterate through the ScriptureText's list of tags. If any missing mappings are found
		/// return true to give the user a chance to use the ImportWizard to map everything.
		/// </summary>
		/// <param name="sParatextProjectId">P6 project id</param>
		/// <returns><c>true</c> if the P6 project has markers (tags) in its stylesheet which
		/// the user has not had a chance to map in the import wizard.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected bool ParatextProjHasUnmappedMarkers(string sParatextProjectId)
		{
			if (sParatextProjectId == string.Empty)
				return false;
			return false;

//			ImportStyleProxy styleProxy;
//			int iscTag = 0;
//			string sMarker, sName;
//
//			ParatextSOLib.ISCScriptureText3 scText =
//				(ParatextSOLib.ISCScriptureText3)new ParatextSOLib.SCScriptureTextClass();
//			scText.Load(sParatextProjectId);
//
//			ParatextSOLib.SCTextProperties scTextProps;
//			ParatextSOLib.SCTag scTag;
//
//			// for every tag in Paratext ScriptureText
//			while (GetPTScrTextNthTag(scText, iscTag++, out scTag, out sMarker))
//			{
//				if ((scTextProps & ParatextSOLib.SCTextProperties.scBook) != 0)
//				{
//					// Map for processing purposes, but not to a style
//					m_htStyleProxy[sMarker] = new ImportStyleProxy(null,
//						StyleType.kstParagraph, m_wsVern, ContextValues.Book);
//					continue;
//				}
//
//				// Is this marker missing from the hashtable of proxies?
//				if (!m_htStyleProxy.ContainsKey(sMarker))
//				{
//					// ENHANCE: Bring up wizard (open to mappings page) if any mappings are not set
//
//					string sTagName = SOWrapper.TagName;
//					sName = (sTagName == string.Empty) ? sMarker : sTagName;
//
//					ParatextSOLib.SCStyleType scStyleType = SOWrapper.TagStyleType;
//
//					// set our import tag type, style type, writing system
//					ContextValues context = ContextValues.General;
//					StyleType styleType;
//
//					if ((scTextProps & ParatextSOLib.SCTextProperties.scChapter) != 0)
//					{
//						// map to chapter style (Structure and Function will get set automatically)
//						m_htStyleProxy[sMarker] = new ImportStyleProxy("Chapter Number",
//							StyleType.kstCharacter, m_wsVern, ContextValues.Text);
//						continue;
//					}
//					else if ((scTextProps & ParatextSOLib.SCTextProperties.scVerse) != 0)
//					{
//						// map to verse style (Structure and Function will get set automatically)
//						m_htStyleProxy[sMarker] = new ImportStyleProxy("Verse Number",
//							StyleType.kstCharacter, m_wsVern, ContextValues.Text);
//						continue;
//					}
//					else if (scStyleType == ParatextSOLib.SCStyleType.scEndStyle)
//					{
//						context = ContextValues.EndMarker;
//
//						// set our style type, writing system
//						//note that for ContextValues.EndMarker, styleType & writing system will be ignored
//						styleType = StyleType.kstCharacter; // Pretend that endmarker is a character style
//					}
//					else //for most styles
//					{
//						styleType = (scStyleType == ParatextSOLib.SCStyleType.scParagraphStyle) ?
//							StyleType.kstParagraph :
//							StyleType.kstCharacter;
//					}
//					int writingSystem = ((scTextProps & ParatextSOLib.SCTextProperties.scVernacular) > 0) ?
//					m_wsVern : m_wsAnal;
//
//					//REVIEW: we should probably support char style text inheriting its ws from the para
//
//					// add a new proxy to the hash map
//					styleProxy = new ImportStyleProxy(sName, styleType, writingSystem, context);
//					m_htStyleProxy[sMarker] = styleProxy;
//					// The actual type and context may not be what we requested, if this is an
//					// existing style.
//					styleType = styleProxy.StyleType;
//					context = styleProxy.Context;
//
//					// Save the end marker of the scTag, if appropriate
//					if (styleType == StyleType.kstCharacter || context == ContextValues.Note)
//					{
//						string sEndMarker = m_scParatextTag.Endmarker;
//						if (sEndMarker.Length > 0)
//							sEndMarker = @"\" + sEndMarker;
//						if (sEndMarker.Length > 0)
//							styleProxy.EndMarker = sEndMarker;
//					}
//
//					// set formatting of this new proxy if needed (unmapped)
//					if (styleProxy.IsUnknownMapping //if name is not in stylesheet
//						&& context != ContextValues.EndMarker) // and this is not an endmarker
//					{
//						//set formatting info in proxy
//						ITsTextProps tsTextPropsFormat;
//						ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
//						//REVIEW: Should we get formatting info from scTag
//						tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptItalic,
//							(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvInvert); //italic for now
//						tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptBold,
//							(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvInvert); //bold also
//						tsTextPropsFormat = tsPropsBldr.GetTextProps();
//						bool fPublishableText = ((scTextProps & ParatextSOLib.SCTextProperties.scPublishable) > 0 ? true : false);
//						styleProxy.SetFormat(tsTextPropsFormat, fPublishableText);
//					}
//				}
//			}
		}
		#endregion

		#region Merging Differences
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the imported books dialog box (virtual to allow tests to suppress display
		/// of dialog box).
		/// </summary>
		/// <param name="backupSavedVersion">The saved version for backups of any overwritten
		/// books.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisplayImportedBooksDlg(IScrDraft backupSavedVersion)
		{
			using (ImportedBooks dlg = new ImportedBooks(m_cache, m_styleSheet,
				ImportedVersion, m_importCallbacks.DraftViewZoomPercent,
				m_importCallbacks.FootnoteZoomPercent, backupSavedVersion, m_importCallbacks.BookFilter,
				UndoManager.ImportedBooks.Keys, m_helpTopicProvider, m_app))
			{
				dlg.ShowOrSave(m_mainWnd, m_fParatextStreamlinedImport);
			}
		}
		#endregion

		#region Methods to support XML (OXES) import
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare for and perform the OXES import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ImportXml()
		{
			// Prevent creation of unnecessary multiple undo tasks.
			// Display ImportDialog
			using (var importDlg = new ImportXmlDialog(m_cache, m_helpTopicProvider))
			{
				importDlg.ShowDialog(m_mainWnd);
				if (importDlg.DialogResult == DialogResult.Cancel)
				{
					Logger.WriteEvent("User canceled import XML dialog");
					return;
				}
				m_sOXESFile = importDlg.FileName;
			}

			DoImport(null, "ImportXml");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set selections after import.
		/// </summary>
		/// <param name="firstReference">The first reference of imported stuff.</param>
		/// ------------------------------------------------------------------------------------
		private void SetSelectionsAfterImport(ScrReference firstReference)
		{
			if (!firstReference.IsEmpty)
			{
				// Set the IP at the beginning of the imported material (i.e. the first reference,
				// not the first segment).
				m_importCallbacks.GotoVerse(firstReference);
			}

			// If we are in the Key Terms view then update the view in case we imported
			// a book so that it is possible to select the verse for the current key term.
			m_importCallbacks.UpdateKeyTermsView();
		}
		#endregion
	}
}

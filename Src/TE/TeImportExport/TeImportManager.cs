// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2005' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeImportManager.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
// define DEBUG_SINGLE_THREADED to debug the import code. This will run the import on the main
// thread. However, since this prevents the progress dialog to work properly this should be
// used only during a debug session.
//#define DEBUG_SINGLE_THREADED

using System;
using System.Collections.Specialized; // for StringCollection
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.ScrImportComponents;
using SIL.Utils;

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
		/// <summary>The FDO cache</summary>
		protected FdoCache m_cache;
		/// <summary>The TE main window</summary>
		protected FwMainWnd m_mainWnd;
		/// <summary></summary>
		protected ITeImportCallbacks m_importCallbacks;
		/// <summary></summary>
		protected FilteredScrBooks m_bookFilter;
		/// <summary></summary>
		protected FwStyleSheet m_styleSheet;
		/// <summary></summary>
		protected string m_sOXESFile;

		/// <summary>
		/// The new ScrDraft created by importing the books.
		/// </summary>
		private IScrDraft m_importedSavedVersion;

		/// <summary>
		/// This keeps track of stuff we may need to Undo.
		/// </summary>
		UndoImportManager m_undoImportManager;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeImportManager"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window we belong to</param>
		/// <param name="importCallbacks">The import callbacks.</param>
		/// ------------------------------------------------------------------------------------
		protected TeImportManager(FwMainWnd mainWnd, ITeImportCallbacks importCallbacks)
		{
			m_mainWnd = mainWnd;
			m_cache = m_mainWnd.Cache;
			m_styleSheet = m_mainWnd.StyleSheet;
			m_importCallbacks = importCallbacks;
			m_bookFilter = m_importCallbacks.BookFilter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeImportManager"/> class.
		/// </summary>
		/// <remarks>This version is for testing only</remarks>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// ------------------------------------------------------------------------------------
		protected TeImportManager(FdoCache cache, FwStyleSheet styleSheet)
		{
			m_mainWnd = null;
			m_cache = cache;
			m_styleSheet = styleSheet;
			m_bookFilter = null; // not used in testing
		}
		#endregion

		#region Public Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the Standard Format import
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ImportSf(FwMainWnd mainWnd, ITeImportCallbacks importCallbacks)
		{
			TeImportManager mgr = new TeImportManager(mainWnd, importCallbacks);
			mgr.ImportSf();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import an OXES (Open XML for Editing Scripture) file.
		/// </summary>
		/// <param name="mainWnd">The main WND.</param>
		/// <param name="importCallbacks">The import callbacks.</param>
		/// ------------------------------------------------------------------------------------
		public static void ImportXml(FwMainWnd mainWnd, ITeImportCallbacks importCallbacks)
		{
			TeImportManager mgr = new TeImportManager(mainWnd, importCallbacks);
			mgr.ImportXml();
		}
		#endregion

		#region Miscellaneous protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get settings for and perform the Standard Format import
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ImportSf()
		{
			ScrImportSet importSettings = null;

			// Prevent creation of undo task.
			using (new SuppressSubTasks(m_cache))
			{
				using (new WaitCursor(m_mainWnd))
				{
					importSettings = GetImportSettings();
				}
				if (importSettings == null) // User cancelled in import wizard
					return;

				// Display ImportDialog
				using (ImportDialog importDlg = new ImportDialog(m_styleSheet, m_cache,
					importSettings, FwApp.App.HelpFile))
				{
					importDlg.ShowDialog(m_mainWnd);
					if (importDlg.DialogResult == DialogResult.Cancel)
					{
						Logger.WriteEvent("User canceled import dialog");
						return;
					}
					// Settings could have changed if the user went into the wizard.
					importSettings = importDlg.ImportSettings;
				}
				if (!m_importCallbacks.EncourageBackup())
				{
					Logger.WriteEvent("Import canceled in encourage backup dialog");
					return;
				}
			}

			DoImport(importSettings, "ImportStandardFormat");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does the import.
		/// </summary>
		/// <param name="importSettings">The import settings (can be null, as is the case for
		/// OXES import).</param>
		/// <param name="updateDescription">description of the data update being done (i.e.,
		/// which type of import).</param>
		/// ------------------------------------------------------------------------------------
		private void DoImport(ScrImportSet importSettings, string updateDescription)
		{
			try
			{
				ScrReference firstImported;
				using (new WaitCursor(m_mainWnd, true))
				{
					FwApp.App.EnableSameProjectWindows(m_cache, false);
					firstImported = ImportWithUndoTask(importSettings, true, updateDescription);
				}
				firstImported = CompleteImport(firstImported);
				SetSelectionsAfterImport(firstImported);
			}
			finally
			{
				FwApp.App.EnableSameProjectWindows(m_cache, true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the settings for Import, either from database or from wizard
		/// </summary>
		/// <returns>Import settings, or <c>null</c> if user canceled dialog.</returns>
		/// ------------------------------------------------------------------------------------
		protected ScrImportSet GetImportSettings()
		{
			ILangProject proj = m_cache.LangProject;
			Scripture scr = (Scripture)proj.TranslatedScriptureOA;
			ScrImportSet importSettings = new ScrImportSet(m_cache, scr.DefaultImportSettingsHvo,
				m_styleSheet, FwApp.App.HelpFile);

			importSettings.OverlappingFileResolver = new ConfirmOverlappingFileReplaceDialog();
			if (!importSettings.BasicSettingsExist)
			{
				// REVIEW DavidO: Should I use AnalysisDefaultWritingSystem or
				// VernacularDefaultWritingSystem or something else.
				using (ImportWizard importWizard = new ImportWizard(proj.Name.UserDefaultWritingSystem,
					scr, m_styleSheet, m_cache, FwApp.App.HelpFile))
				{
					if (importWizard.ShowDialog() == DialogResult.Cancel)
						return null;
					// Scripture reference range may have changed
					ImportDialog.ClearDialogReferences(m_cache);
					importSettings = (ScrImportSet)scr.DefaultImportSettings;
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
						dlg.HelpURL = FwApp.App.HelpFile;
						dlg.HelpTopic = "/Beginning_Tasks/Import_Standard_Format/Project_Files_Unavailable.htm";
						dlg.DisplaySetupOption = true;
						switch(dlg.ShowDialog())
						{
							case DialogResult.OK: // Setup...
							{
								using (ImportWizard importWizard = new ImportWizard(
									proj.Name.UserDefaultWritingSystem, scr, m_styleSheet, m_cache,
									FwApp.App.HelpFile))
								{
									if (importWizard.ShowDialog()== DialogResult.Cancel)
										return null;
									// Scripture reference range may have changed
									ImportDialog.ClearDialogReferences(m_cache);
									importSettings = (ScrImportSet)scr.DefaultImportSettings;
									fCompletedWizard = true;
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
			return new TeImportUi(progressDialog);
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
		protected IScrDraft ImportedSavedVersion
		{
			get { return m_importedSavedVersion; }
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
			m_undoImportManager = new UndoImportManager(m_cache, m_bookFilter);
			if (m_mainWnd == null)
			{
				// Can happen in tests (and is probably the only time we'll get here).
				return InternalImport(importSettings, fDisplayUi);
			}

			using (new DataUpdateMonitor(m_mainWnd, m_cache.MainCacheAccessor,
					m_mainWnd.ActiveView as IVwRootSite, updateDescription))
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
		private ScrReference InternalImport(IScrImportSet importSettings, bool fDisplayUi)
		{
			ScrReference firstImported = ScrReference.Empty;
			using (new IgnorePropChanged(m_cache))
			{
				bool fRollbackPartialBook = false;
				bool fPartialBtImported = false;
				try
				{
					Logger.WriteEvent("Starting import");
					using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(m_mainWnd))
					{
						progressDlg.CancelButtonText =
							TeResourceHelper.GetResourceString("kstidStopImporting");
						progressDlg.Title =
							TeResourceHelper.GetResourceString("kstidImportProgressCaption");
						progressDlg.StatusMessage =
							TeResourceHelper.GetResourceString("kstidImportInitializing");
						if (importSettings == null)	// XML (OXES) import
							progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;

						TeImportUi importUi = CreateTeImportUi(progressDlg);
#if DEBUG_SINGLE_THREADED
						if (importSettings != null)
							firstImported = (ScrReference)progressDlg.RunTask_DebuggingOnly(fDisplayUi,
								new BackgroundTaskInvoker(Import),
								importSettings, undoImportManager, importUi);
						else
							firstImported = (ScrReference)progressDlg.RunTask_DebuggingOnly(fDisplayUi,
								new BackgroundTaskInvoker(ImportXml),
								undoImportManager, importUi);
#else
						if (importSettings != null)
						{
							firstImported = (ScrReference)progressDlg.RunTask(fDisplayUi,
								new BackgroundTaskInvoker(ImportSf), importSettings,
								m_undoImportManager, importUi);
						}
						else
						{
							firstImported = (ScrReference)progressDlg.RunTask(fDisplayUi,
								new BackgroundTaskInvoker(ImportXml),
								m_undoImportManager, importUi);
						}
#endif
					}
				}
				catch (WorkerThreadException e)
				{
					if (e.InnerException is ScriptureUtilsException)
					{
						ScriptureUtilsException se = e.InnerException as ScriptureUtilsException;
						if (FwApp.App != null)
						{
							string sCaption = ScriptureUtilsException.GetResourceString(
								se.IsBackTransError ? "kstidBTImportErrorCaption" : "kstidImportErrorCaption");
							MessageBox.Show(m_mainWnd, se.Message, sCaption, MessageBoxButtons.OK,
								MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0, FwApp.App.HelpFile,
								HelpNavigator.Topic, se.HelpTopic);
						}
						if (se.IsBackTransError && !se.InterleavedImport)
							fPartialBtImported = true;
						else
							fRollbackPartialBook = true;
					}
					else if (e.InnerException is ParatextLoadException)
					{
						if (FwApp.App != null)
						{
							string sCaption = ScriptureUtilsException.GetResourceString("kstidImportErrorCaption");
							Exception innerE = e.InnerException;
							StringBuilder sbMsg = new StringBuilder(innerE.Message);
							while (innerE.InnerException != null)
							{
								innerE = innerE.InnerException;
								sbMsg.Append("\r");
								sbMsg.Append(innerE.Message);
							}

							MessageBox.Show(m_mainWnd, sbMsg.ToString(), sCaption, MessageBoxButtons.OK,
								MessageBoxIcon.Error);
						}
						fRollbackPartialBook = true;
					}
					else if (e.InnerException is CancelException)
					{
						// User cancelled import in the middle of a book
						fRollbackPartialBook = true;
					}
					else
					{
						m_undoImportManager.DoneImportingFiles(true);
						m_undoImportManager.CollapseAllUndoActions();
						throw;
					}
				}
				finally
				{
					m_importedSavedVersion = m_undoImportManager.ImportedSavedVersion;
					Logger.WriteEvent("Finished importing");
				}
				m_undoImportManager.DoneImportingFiles(fRollbackPartialBook);
				if (m_importedSavedVersion != null && m_importedSavedVersion.BooksOS.Count == 0 &&
					!fPartialBtImported)
				{
					// Either there was nothing in the file, or the user canceled during the first book.
					// In any case, we didn't get any books, so whatever has been done should be undone.
					m_undoImportManager.UndoEntireImport();
					m_importedSavedVersion = null;
					return null;
				}
			}
			return firstImported;
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

			// An ampty first imported reference can happen if we imported just the BT.
			//Debug.Assert(!firstImported.IsEmpty, "We should have a useful reference if we imported something!");

			if (m_importedSavedVersion != null && m_importedSavedVersion.BooksOS.Count > 0)
			{
				DisplayImportedBooksDlg(m_undoImportManager.BackupVersion);
				// Now re-check to see if we have any imported books left. If not, and if we didn't
				// import a partial BT either, then the net effect of this import was to do nothing.
				// So we discard our undo action altogether.
				if (m_importedSavedVersion.BooksOS.Count == 0/* && !fPartialBtImported*/)
				{
					m_undoImportManager.DiscardImportedVersionAndUndoActions();
					return ScrReference.Empty;
				}
			}

			m_undoImportManager.RemoveEmptyBackupSavedVersion();
			m_undoImportManager.CollapseAllUndoActions();
			// sync stuff
			if (FwApp.App != null)
			{
				using (new WaitCursor(m_mainWnd))
				{
					// Refresh all the views of all applications connected to the same DB. This
					// will cause any needed Scripture data to be reloaded lazily.
					FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncScriptureImport, 0, 0), m_cache);
					FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncStyle, 0, 0), m_cache);
					FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncReloadScriptureControl, 0, 0), m_cache);
				}
			}
			return firstImported;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Imports using the specified import settings.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The paramaters: import settings, undo action, and book
		/// merger, TeImportUi.</param>
		/// <returns>The Scripture reference of the first thing imported</returns>
		/// <remarks>This method runs on the background thread!</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual object ImportSf(IAdvInd4 progressDlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			IScrImportSet importSettings = (IScrImportSet)parameters[0];
			UndoImportManager undoManager = (UndoImportManager)parameters[1];
			TeImportUi importUi = (TeImportUi)parameters[2];

			return TeSfmImporter.Import(importSettings, m_cache, m_styleSheet,
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
		protected bool ParatextProjHasUnmappedMarkers(ScrImportSet settings)
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the scTag object, at the given index, from the Paratext scripture text.
		/// </summary>
		/// <param name="scText">the Paratext6 Scripture project</param>
		/// <param name="iTag">index of the tag to retrieve</param>
		/// <param name="scParatextTag">set to the tag</param>
		/// <param name="sMarker">set to the tag's marker</param>
		/// <returns>True if successful. False if there are no more tags.</returns>
		/// <remarks>virtual for testing purposes</remarks>
		/// ------------------------------------------------------------------------------------
		private bool GetPTScrTextNthTag(SCRIPTUREOBJECTSLib.ISCScriptureText3 scText, int iTag,
			SCRIPTUREOBJECTSLib.ISCTag scParatextTag, out string sMarker)
		{
			scParatextTag = scText.NthTag(iTag);
			if (scParatextTag == null)
			{
				sMarker = null;
				return false;
			}

			sMarker = @"\" + scParatextTag.Marker;
			return true;
		}
		#endregion

		#region Merging Differences
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the imported books dialog box (virtual to allow tests to supress display
		/// of dialog box).
		/// </summary>
		/// <param name="backupSavedVersion">The saved version for backups of any overwritten
		/// books.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisplayImportedBooksDlg(IScrDraft backupSavedVersion)
		{
			using (ImportedBooks dlg = new ImportedBooks(m_cache, m_styleSheet,
				m_importedSavedVersion, m_importCallbacks.DraftViewZoomPercent,
				m_importCallbacks.FootnoteZoomPercent, backupSavedVersion, m_bookFilter))
			{
				dlg.ShowOrSave(m_mainWnd);
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
			using (new SuppressSubTasks(m_cache))
			{
				// Display ImportDialog
				using (ImportXmlDialog importDlg = new ImportXmlDialog(m_cache))
				{
					importDlg.ShowDialog(m_mainWnd);
					if (importDlg.DialogResult == DialogResult.Cancel)
					{
						Logger.WriteEvent("User canceled import XML dialog");
						return;
					}
					m_sOXESFile = importDlg.FileName;
				}
				// Encouraging backup is always a good idea!
				if (!m_importCallbacks.EncourageBackup())
				{
					Logger.WriteEvent("Import XML canceled in encourage backup dialog");
					return;
				}
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import from the specified XML (OXES) file.
		/// </summary>
		/// <param name="progressDlg"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual object ImportXml(IAdvInd4 progressDlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 2);
			return TeXmlImporter.Import(m_cache, m_styleSheet, m_sOXESFile,
				(UndoImportManager)parameters[0],
				(TeImportUi)parameters[1]);
		}
		#endregion
	}
}

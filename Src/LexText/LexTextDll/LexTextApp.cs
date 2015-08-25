// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: LexTextApp.cs
// Responsibility: RandyR
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.LexText.Controls.DataNotebook;
using System.Diagnostics.CodeAnalysis;
using SIL.CoreImpl.MessageBoxEx;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// Summary description for LexTextApp.
	/// </summary>
	public class LexTextApp : FwXApp
	{
		/* TODO: Make sure these old style Mediator commands/methods are handled in the best IArea/ITool manner.
		 * TODO: This will likely mean some/all of these get moved elsewhere, since they are not global.
Old Mediator methods/commands
	LaunchConnectedDialog: OnDisplayLaunchConnectedDialog & OnLaunchConnectedDialog
		Services these xml faux global commands:
			CmdImportSFMLexicon
			CmdImportLinguaLinksData
		 * CmdImportLiftData
		 * CmdImportInterlinearSfm
		 * CmdImportWordsAndGlossesSfm
		 * CmdImportInterlinearData
	ConfigureHomographs: OnConfigureHomographs (no display check)
		 * Services this global(?) command CmdConfigHomographs
	OnRefresh (not used by Mediator now)
		 */
		/// <summary>
		///  Web browser to use in Linux
		/// </summary>
		private string webBrowserProgramLinux = "firefox";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fwManager">The FieldWorks manager for dealing with FieldWorks-level
		/// stuff.</param>
		/// <param name="helpTopicProvider">An application-specific help topic provider.</param>
		/// <param name="appArgs">The application arguments.</param>
		/// ------------------------------------------------------------------------------------
		public LexTextApp(IFieldWorksManager fwManager, IHelpTopicProvider helpTopicProvider,
			FwAppArgs appArgs) : base(fwManager, helpTopicProvider, appArgs)
		{
		}

		/// <summary>
		/// Needed for automated tests
		/// </summary>
		public LexTextApp() : base(null, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this for slow operations that should happen during the splash screen instead of
		/// during app construction
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use.</param>
		/// ------------------------------------------------------------------------------------
		public override void DoApplicationInitialization(IProgress progressDlg)
		{
			base.DoApplicationInitialization(progressDlg);
			InitializeMessageDialogs(progressDlg);
			if (progressDlg != null)
				progressDlg.Message = LexTextStrings.ksLoading_;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the required inventories.
		/// </summary>
		/// <param name="progressDlg">The progress dialog</param>
		/// ------------------------------------------------------------------------------------
		private void InitializeMessageDialogs(IProgress progressDlg)
		{
			if (progressDlg != null)
				progressDlg.Message = LexTextStrings.ksInitializingMessageDialogs_;
			MessageBoxExManager.DefineMessageBox("TextChartNewFeature",
				LexTextStrings.ksInformation,
				LexTextStrings.ksChartTemplateWarning, true, "info");
			MessageBoxExManager.DefineMessageBox("CategorizedEntry-Intro",
				LexTextStrings.ksInformation,
				LexTextStrings.ksUsedForSemanticBasedEntry, true, "info");
			MessageBoxExManager.DefineMessageBox("CreateNewFromGrammaticalCategoryCatalog",
				LexTextStrings.ksInformation,
				LexTextStrings.ksCreatingCustomGramCategory, true, "info");
			MessageBoxExManager.DefineMessageBox("CreateNewLexicalReferenceType",
				LexTextStrings.ksInformation,
				LexTextStrings.ksCreatingCustomLexRefType, true, "info");
			MessageBoxExManager.DefineMessageBox("ClassifiedDictionary-Intro",
				LexTextStrings.ksInformation,
				LexTextStrings.ksShowingSemanticClassification, true, "info");

			MessageBoxExManager.ReadSettingsFile();
			if (progressDlg != null)
				progressDlg.Message = string.Empty;
		}

		/// <summary>
		/// Initialize the required inventories.
		/// </summary>
		private void InitializePartInventories(IProgress progressDlg, bool fLoadUserOverrides)
		{
			if (progressDlg != null)
				progressDlg.Message = LexTextStrings.ksInitializingLayouts_;
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, this, fLoadUserOverrides,
				Cache.ProjectId.ProjectFolder);

			var currentReversalIndices = Cache.LanguageProject.LexDbOA.CurrentReversalIndices;
			if (currentReversalIndices.Count == 0)
				currentReversalIndices = new List<IReversalIndex>(Cache.LanguageProject.LexDbOA.ReversalIndexesOC.ToArray());

			foreach (var reversalIndex in currentReversalIndices)
			{
				LayoutCache.InitializeLayoutsForWsTag(
					reversalIndex.WritingSystem,
					Cache.ProjectId.Name);
			}
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed || BeingDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the product executable filename
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ProductExecutableFile
		{
			get { return FwDirectoryFinder.FlexExe; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Guid for the application (used for uniquely identifying DB items that "belong" to
		///		this app.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public static Guid AppGuid
		{
			get
			{
				return new Guid("E716C901-3171-421f-83E1-3E012DEC9489");
			}
		}

		/// <summary>
		/// This application processes DB sync records.
		/// </summary>
		public override Guid SyncGuid
		{
			get
			{
				CheckDisposed();
				return AppGuid;
			}
		}

		private static bool m_fResourceFailed = false;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>string</returns>
		/// -----------------------------------------------------------------------------------
		public string ResourceString(string stid)
		{
			CheckDisposed();

			try
			{
				// No need to allocate a different ResourceManager than the one the generated code
				// produces, and it should be more reliable (I hope).
				//s_stringResources = new System.Resources.ResourceManager(
				//    "SIL.FieldWorks.XWorks.LexText.LexTextStrings", Assembly.GetExecutingAssembly());
				return (stid == null ? "NullStringID" : LexTextStrings.ResourceManager.GetString(stid));
			}
			catch (Exception e)
			{
				if (!m_fResourceFailed)
				{
					MessageBox.Show(null,
						String.Format(LexTextStrings.ksErrorLoadingResourceStrings, e.Message),
						LexTextStrings.ksError);
					m_fResourceFailed = true;
				}
				if (stid == null)
					return "NullStringID";
				else
					return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ApplicationName
		{
			get { return FwUtils.ksFlexAppName; }
		}

		/// <summary>
		/// Gets the registry settings key name for the application.
		/// </summary>
		/// <remarks>Subclasses should override this, or all its settings will go in "FwXapp".</remarks>
		protected override string SettingsKeyName
		{
			get { return FwSubKey.LexText; }
		}

#if RANDYTODO
		public bool OnDisplaySFMImport(object parameters, ref UIItemDisplayProperties display)
		{
			return true;
		}
#endif

		public bool OnSFMImport(object parameters)
		{
			Form formActive = ActiveForm;
			IFwMainWnd wndActive = (IFwMainWnd)formActive;
			using (var importWizard = new LexImportWizard())
			{
				((IFwExtension)importWizard).Init(Cache, wndActive.PropertyTable, wndActive.Publisher);
				importWizard.ShowDialog(formActive);
			}
			return true;
		}

#if RANDYTODO
		/// <summary>
		/// Display the import commands only while in the appropriate area.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "mediator is a reference")]
		public bool OnDisplayLaunchConnectedDialog(object parameters, ref UIItemDisplayProperties display)
		{
			display.Enabled = false;
			display.Visible = false;
			XCore.Command command = parameters as XCore.Command;
			if (command == null)
				return true;
			Form formActive = ActiveForm;
			IFwMainWnd wndActive = formActive as IFwMainWnd;
			if (wndActive == null)
				return true;
			Mediator mediator = wndActive.Mediator;
			if (mediator == null)
				return true;
			string area = wndActive.PropTable.GetValue<string>("areaChoice");
			bool fEnabled = true;
			switch (command.Id)
			{
				case "CmdImportSFMLexicon": // Fall through
				case "CmdImportLinguaLinksData": // Fall through
				case "CmdImportLiftData":
					fEnabled = area == "lexicon";
					break;
				case "CmdImportInterlinearSfm": // Fall through
				case "CmdImportWordsAndGlossesSfm": // Fall through
				case "CmdImportInterlinearData":
					if (wndActive.PropTable.GetValue<string>("currentContentControl") == "concordance" || wndActive.PropTable.GetValue<string>("currentContentControl") == "concordance")

					{
						fEnabled = false;
					}
					else
					{
						fEnabled = area == "textsWords";
					}
					break;
				case "CmdImportSFMNotebook":
					fEnabled = area == "notebook";
					break;
			}
			display.Enabled = fEnabled;
			display.Visible = fEnabled;
			return true;
		}
#endif

		/// <summary>
		/// Used to launch various import dialogs, but could do other things
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnLaunchConnectedDialog(object commandObject)
		{
			CheckDisposed();

#if RANDYTODO
			XCore.Command command = (XCore.Command)commandObject;
			System.Xml.XmlNode first = command.Parameters[0];
			System.Xml.XmlNode classInfo = first.SelectSingleNode("dynamicloaderinfo");
#endif

			Form formActive = ActiveForm;

			IFwMainWnd wndActive = formActive as IFwMainWnd;
			IFwExtension dlg = null;
			try
			{
				try
				{
#if RANDYTODO // TODO: Create it another way.
					dlg = (IFwExtension)DynamicLoader.CreateObject(classInfo);
#endif
				}
				catch (Exception error)
				{
#if RANDYTODO
					string message = XmlUtils.GetOptionalAttributeValue(classInfo, "notFoundMessage", null);
						// Make this localizable!
					if (message != null)
						throw new ApplicationException(message, error);
#endif
				}
				var oldWsUser = Cache.WritingSystemFactory.UserWs;
				dlg.Init(Cache, wndActive.PropertyTable, wndActive.Publisher);
				DialogResult dr = ((Form) dlg).ShowDialog(ActiveForm);
				if (dr == DialogResult.OK)
				{
					if (dlg is LexOptionsDlg)
					{
						LexOptionsDlg loDlg = dlg as LexOptionsDlg;
						if ((oldWsUser != Cache.WritingSystemFactory.UserWs) || loDlg.PluginsUpdated)
							ReplaceMainWindow(wndActive);
					}
					else if (dlg is LinguaLinksImportDlg || dlg is InterlinearImportDlg ||
							 dlg is LexImportWizard || dlg is NotebookImportWiz || dlg is LiftImportDlg)
					{
						// Make everything we've imported visible.
						wndActive.Publisher.Publish("MasterRefresh", wndActive);
					}
				}
			}
			finally
			{
				if (dlg != null && dlg is IDisposable)
					(dlg as IDisposable).Dispose();
			}
			return true;
		}

		/// <summary>
		/// Closes and re-opens the argument window, in the same place, as a drastic way of applying new settings.
		/// </summary>
		internal void ReplaceMainWindow(IFwMainWnd wndActive)
		{
			wndActive.SaveSettings();
			FwManager.OpenNewWindowForApp();
			m_windowToCloseOnIdle = wndActive;
			Application.Idle += CloseOldWindow;
		}

		private IFwMainWnd m_windowToCloseOnIdle;

		void CloseOldWindow(object sender, EventArgs e)
		{
			Application.Idle -= CloseOldWindow;
			if (m_windowToCloseOnIdle != null)
				m_windowToCloseOnIdle.Close();
			m_windowToCloseOnIdle = null;
		}

		public bool OnConfigureHomographs(object commandObject)
		{
			CheckDisposed();
			var configDlg = commandObject as XmlDocConfigureDlg;

			Form formActive = ActiveForm;
			IFwMainWnd wndActive = formActive as IFwMainWnd;
			if (wndActive == null && configDlg != null)
				wndActive = configDlg.Owner as IFwMainWnd;
			if (wndActive != null)
			{
				var hc = wndActive.Cache.ServiceLocator.GetInstance<HomographConfiguration>();
				using (var dlg = new ConfigureHomographDlg())
				{
#if RANDYTODO
					dlg.SetupDialog(hc, wndActive.Cache, wndActive.ActiveStyleSheet, this, this);
#endif
					dlg.StartPosition = FormStartPosition.CenterScreen;
					if (dlg.ShowDialog((Form)wndActive) != DialogResult.OK)
						return true;
					dlg.GetResults(hc);
					// If called from config dlg, it will do its own refresh when it closes.
					if (configDlg == null)
						OnMasterRefresh(null);
					else
						configDlg.MasterRefreshRequired = true;
				}
			}
			return true;
		}

		public bool OnRestoreDefaultLayouts(object commandObject)
		{
			CheckDisposed();

			Form formActive = ActiveForm;
			IFwMainWnd wndActive = formActive as IFwMainWnd;
			if (wndActive != null)
			{
				bool fRestore;
				using (RestoreDefaultsDlg dlg = new RestoreDefaultsDlg(this))
					fRestore = (dlg.ShowDialog(formActive) == DialogResult.Yes);
				if (fRestore)
				{
					InitializePartInventories(null, false);
					ReplaceMainWindow(wndActive);
				}
			}
			return true;
		}

		/// <summary>
		/// On Refresh, we want to reload the XML configuration files.  This greatly facilitates developing
		/// those files, even though it's not as useful for normal use.  It might prove useful whenever we
		/// get around to allowing user customization (or it might not).
		/// </summary>
		/// <param name="sender"></param>
		/// <returns></returns>
		public bool OnRefresh(object sender)
		{
			CheckDisposed();
			Set<string> setDatabases = new Set<string>();
			foreach (IFwMainWnd wnd in m_rgMainWindows)
			{
				string sDatabase = wnd.Cache.ProjectId.Name;
				if (setDatabases.Contains(sDatabase))
					continue;
				setDatabases.Add(sDatabase);
				Inventory.GetInventory("layouts", sDatabase).ReloadIfChanges();
				Inventory.GetInventory("parts", sDatabase).ReloadIfChanges();
			}
			return false;
		}

		public bool OnHelpNotesLinguaLinksDatabaseImport(object sender)
		{
			CheckDisposed();

			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on LinguaLinks Database Import.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			});
			return true;
		}

		public bool OnHelpNotesInterlinearImport(object sender)
		{
			CheckDisposed();

			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on Interlinear Import.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			});
			return true;
		}

		public bool OnHelpNotesSFMDatabaseImport(object sender)
		{
			CheckDisposed();

			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on SFM Database Import.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			});
			return true;
		}

		/// <summary>
		/// Display a file given a path relative to the FieldWorks/Helps directory.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnHelpLexicographyIntro(object commandObject)
		{
			CheckDisposed();

#if RANDYTODO
			XCore.Command command = (XCore.Command)commandObject;
			string fileName = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "file");
			fileName = fileName.Replace('\\', Path.DirectorySeparatorChar);
			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}" + fileName, Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotShowX, path),
					LexTextStrings.ksError);
			});
#endif
			return true;
		}

		/// <summary>
		/// Display a file given a path relative to the FieldWorks/Helps directory.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnHelpHelpsFile(object commandObject)
		{
			CheckDisposed();

#if RANDYTODO
			XCore.Command command = (XCore.Command)commandObject;
			string fileName = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "file");
			fileName = fileName.Replace('\\', Path.DirectorySeparatorChar);
			string path = String.Format(FwDirectoryFinder.CodeDirectory + "{0}Helps{0}" + fileName,
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotShowX, path),
					LexTextStrings.ksError);
			});
#endif
			return true;
		}

		public bool OnHelpMorphologyIntro(object sender)
		{
			CheckDisposed();

			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}WW-ConceptualIntro{0}ConceptualIntroduction.htm",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			});
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and opens a main FLEx window.
		/// </summary>
		/// <param name="progressDlg">The progress DLG.</param>
		/// <param name="isNewCache">if set to <c>true</c> [is new cache].</param>
		/// <param name="wndCopyFrom">The WND copy from.</param>
		/// <param name="fOpeningNewProject">if set to <c>true</c> [f opening new project].</param>
		/// ------------------------------------------------------------------------------------
		public override Form NewMainAppWnd(IProgress progressDlg, bool isNewCache,
			Form wndCopyFrom, bool fOpeningNewProject)
		{
			if (progressDlg != null)
				progressDlg.Message = String.Format(LexTextStrings.ksCreatingWindowForX, Cache.ProjectId.Name);
			Form form = base.NewMainAppWnd(progressDlg, isNewCache, wndCopyFrom, fOpeningNewProject);

			if (form is IFwMainWnd)
				m_activeMainWindow = form;

			if (isNewCache && form != null)
				InitializePartInventories(progressDlg, true);
			return form;
		}

		/// <summary>
		///	App-specific initialization of the cache.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <returns>True if the initialize was successful, false otherwise</returns>
		public override bool InitCacheForApp(IThreadedProgress progressDlg)
		{
			Cache.ServiceLocator.DataSetup.LoadDomainAsync(BackendBulkLoadDomain.All);
			AddDefaultWordformingOverridesIfNeeded();

			// The try-catch block is modeled after that used by TeScrInitializer.Initialize(),
			// as the suggestion for fixing LT-8797.
			try
			{
				// Make sure this DB uses the current stylesheet version.
				FlexStylesXmlAccessor.EnsureCurrentStylesheet(Cache.LangProject, progressDlg);
			}
			catch (WorkerThreadException e)
			{
				MessageBox.Show(Form.ActiveForm, e.InnerException.Message,
					ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Adds the default word-forming character overrides to the list of valid
		/// characters for each vernacular writing system that is using the old
		/// valid characters representation.
		/// </summary>
		private void AddDefaultWordformingOverridesIfNeeded()
		{
			foreach (IWritingSystem wsObj in Cache.ServiceLocator.WritingSystems.VernacularWritingSystems)
			{
				string validCharsSrc = wsObj.ValidChars;
				if (!ValidCharacters.IsNewValidCharsString(validCharsSrc))
				{
					ValidCharacters valChars = ValidCharacters.Load(wsObj, LoadException, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
					valChars.AddDefaultWordformingCharOverrides();
					wsObj.ValidChars = valChars.XmlString;
				}
			}
			Cache.ServiceLocator.WritingSystemManager.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reports a ValidCharacters load exception.
		/// </summary>
		/// <param name="e">The exception.</param>
		/// ------------------------------------------------------------------------------------
		void LoadException(ArgumentException e)
		{
			ErrorReporter.ReportException(e, SettingsKey, SupportEmailAddress);
		}

		/// <summary>
		/// Provides an application-wide default for allowed style contexts for windows that
		/// don't have an FwEditingHelper (i.e., all but TE windows at present).
		/// For Flex, we currently want to allow general styles only. This is mainly to rule out
		/// ContextValues.InternalConfigureView ones, which are only used when configuring
		/// a view, not for applying styles directly to individual docs.
		/// </summary>
		public override List<ContextValues> DefaultStyleContexts
		{
			get
			{
				CheckDisposed();
				return new List<ContextValues>(new ContextValues[] { ContextValues.General });
			}
		}

		/// <summary>
		/// Returns str surrounded by double-quotes.
		/// This is useful for paths containing spaces in Linux.
		/// </summary>
		private static string Enquote(string str)
		{
			return "\"" + str + "\"";
		}

		/// <summary>
		/// Uses Process.Start to run path. If running in Linux and path ends in .html or .htm,
		/// surrounds the path in double quotes and opens it with a web browser.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="exceptionHandler"/>
		/// Delegate to run if an exception is thrown. Takes the exception as an argument.

		private void OpenDocument(string path, Action<Exception> exceptionHandler)
		{
			OpenDocument<Exception>(path, exceptionHandler);
		}

		/// <summary>
		/// Like OpenDocument(), but allowing specification of specific exception type T to catch.
		/// </summary>
		private void OpenDocument<T>(string path, Action<T> exceptionHandler) where T : Exception
		{
			try
			{
				if (MiscUtils.IsUnix && (path.EndsWith(".html") || path.EndsWith(".htm")))
				{
					using (Process.Start(webBrowserProgramLinux, Enquote(path)))
					{
					}
				}
				else
				{
					using (Process.Start(path))
					{
					}
				}
			}
			catch (T e)
			{
				if (exceptionHandler != null)
					exceptionHandler(e);
			}
		}
	}
}

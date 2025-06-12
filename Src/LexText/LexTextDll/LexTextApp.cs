// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.LexText.Controls.DataNotebook;
using SIL.LCModel;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// Summary description for LexTextApp.
	/// </summary>
	public class LexTextApp : FwXApp, IApp, IxCoreColleague
	{
		protected XMessageBoxExManager m_messageBoxExManager;
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
			m_messageBoxExManager = XMessageBoxExManager.CreateXMessageBoxExManager(ApplicationName);
			m_messageBoxExManager.DefineMessageBox("TextChartNewFeature",
				LexTextStrings.ksInformation,
				LexTextStrings.ksChartTemplateWarning, true, "info");
			m_messageBoxExManager.DefineMessageBox("CategorizedEntry-Intro",
				LexTextStrings.ksInformation,
				LexTextStrings.ksUsedForSemanticBasedEntry, true, "info");
			m_messageBoxExManager.DefineMessageBox("CreateNewFromGrammaticalCategoryCatalog",
				LexTextStrings.ksInformation,
				LexTextStrings.ksCreatingCustomGramCategory, true, "info");
			m_messageBoxExManager.DefineMessageBox("CreateNewLexicalReferenceType",
				LexTextStrings.ksInformation,
				LexTextStrings.ksCreatingCustomLexRefType, true, "info");
			m_messageBoxExManager.DefineMessageBox("ClassifiedDictionary-Intro",
				LexTextStrings.ksInformation,
				LexTextStrings.ksShowingSemanticClassification, true, "info");

			m_messageBoxExManager.ReadSettingsFile();
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
				if (m_messageBoxExManager != null)
					m_messageBoxExManager.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_messageBoxExManager = null;
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

		public override string DefaultConfigurationPathname
		{
			get
			{
				CheckDisposed();
				return @"Language Explorer/Configuration/Main.xml";
			}
		}

		public override string WindowClassName
		{
			get { return "fieldworks-flex"; }
		}

		private static bool m_fResourceFailed = false;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>string</returns>
		/// -----------------------------------------------------------------------------------
		string SIL.FieldWorks.Common.RootSites.IApp.ResourceString(string stid)
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
		/// override this with the name of your icon
		/// This icon file should be included in the assembly, and its "build action" should be set to "embedded resource"
		/// </summary>
		protected override string ApplicationIconName
		{
			get { return "lt.ico"; }
		}

		/// <summary>
		/// Gets the registry settings key name for the application.
		/// </summary>
		/// <remarks>Subclasses should override this, or all its settings will go in "FwXapp".</remarks>
		protected override string SettingsKeyName
		{
			get { return FwSubKey.LexText; }
		}

		public bool OnDisplaySFMImport(object parameters, ref UIItemDisplayProperties display)
		{
			return true;
		}

		public bool OnSFMImport(object parameters)
		{
			Form formActive = ActiveForm;
			FwXWindow wndActive = (FwXWindow)formActive;
			using (var importWizard = new LexImportWizard())
			{
				((IFwExtension)importWizard).Init(Cache, wndActive.Mediator, wndActive.PropTable);
				importWizard.ShowDialog(formActive);
			}
			return true;
		}

		/// <summary>
		/// Display the import commands only while in the appropriate area.
		/// </summary>
		public bool OnDisplayLaunchConnectedDialog(object parameters, ref UIItemDisplayProperties display)
		{
			display.Enabled = false;
			display.Visible = false;
			XCore.Command command = parameters as XCore.Command;
			if (command == null)
				return true;
			Form formActive = ActiveForm;
			FwXWindow wndActive = formActive as FwXWindow;
			if (wndActive == null)
				return true;
			Mediator mediator = wndActive.Mediator;
			if (mediator == null)
				return true;
			string area = wndActive.PropTable.GetValue<string>("areaChoice");
			bool fEnabled = true;
			switch (command.Id)
			{
				case "CmdImportSFMLexicon":
					fEnabled = area == "lexicon";
					break;
				case "CmdImportLinguaLinksData":
					fEnabled = area == "lexicon";
					break;
				case "CmdImportLiftData":
					fEnabled = area == "lexicon";
					break;
				case "CmdImportInterlinearSfm":
				case "CmdImportWordsAndGlossesSfm":
				case "CmdImportInterlinearData":
					// LT-11998: importing texts in the Concordance tool can crash
					fEnabled = area == "textsWords" && wndActive.PropTable.GetStringProperty("currentContentControl", null) != "concordance";
					break;
				case "CmdImportSFMNotebook":
					fEnabled = area == "notebook";
					break;
				default:
					break;
			}
			display.Enabled = fEnabled;
			display.Visible = fEnabled;
			return true;
		}

		/// <summary>
		/// Used to launch various import dialogs, but could do other things
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnLaunchConnectedDialog(object commandObject)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command)commandObject;
			System.Xml.XmlNode first = command.Parameters[0];
			System.Xml.XmlNode classInfo = first.SelectSingleNode("dynamicloaderinfo");

			Form formActive = ActiveForm;

			FwXWindow wndActive = formActive as FwXWindow;
			IFwExtension dlg = null;
			if (((classInfo as System.Xml.XmlElement)?.Attributes["class"]?.Value ?? "").Contains(
				"LinguaLinks"))
			{
				// ReSharper disable LocalizableElement
				// Message is deliberately not localized. We expect this to affect maybe one person every couple of years based on recent
				// occurrences. Doubt it's worth translating.
				// The reason for the disabling is that model changes require significant changes to the Import code,
				// and we don't think it's worth the effort. What few LinguaLinks projects still require import can be handled
				// by installing a version 7 FieldWorks, importing, and then migrating.
				// (For example, the currently generated stage 5 XML assumes Senses still reference ReveralEntries, rather than the
				// current opposite link; and there were problems importing texts even in FLEx 8 (LT-2084)).
				MessageBox.Show(
					"Fieldworks no longer supports import of LinguaLinks data. For any remaining projects that need this, our support staff can help convert your data. Please send a message to flex_errors@sil.org",
					"Sorry", MessageBoxButtons.OK, MessageBoxIcon.Information);
				// ReSharper restore LocalizableElement
				return true;
			}
			try
			{
				try
				{
					dlg = (IFwExtension) DynamicLoader.CreateObject(classInfo);
				}
				catch (Exception error)
				{
					string message = XmlUtils.GetOptionalAttributeValue(classInfo, "notFoundMessage", null);
						// Make this localizable!
					if (message != null)
						throw new ApplicationException(message, error);
					throw;
				}
				var oldWsUser = Cache.WritingSystemFactory.UserWs;
				dlg.Init(Cache, wndActive.Mediator, wndActive.PropTable);
				DialogResult dr = ((Form) dlg).ShowDialog(ActiveForm);
				if (dr == DialogResult.OK)
				{
					if (dlg is LexOptionsDlg loDlg)
					{
						if (oldWsUser != Cache.WritingSystemFactory.UserWs ||
							loDlg.PluginsUpdated)
						{
							wndActive.SaveSettings();
						}
					}
					else if (dlg is LinguaLinksImportDlg || dlg is InterlinearImportDlg ||
							 dlg is LexImportWizard || dlg is NotebookImportWiz ||
							 dlg is LiftImportDlg || dlg is CombineImportDlg)
					{
						// Make everything we've imported visible.
						wndActive.Mediator.SendMessage("MasterRefresh", wndActive);
					}
				}
			}
			finally
			{
				(dlg as IDisposable)?.Dispose();
			}
			return true;
		}

		/// <summary>
		/// Closes and re-opens the argument window, in the same place, as a drastic way of applying new settings.
		/// </summary>
		internal void ReplaceMainWindow(FwXWindow wndActive)
		{
			wndActive.SaveSettings();
			FwManager.OpenNewWindowForApp(this, null);
			m_windowToCloseOnIdle = wndActive;
			Application.Idle += CloseOldWindow;
		}

		private FwXWindow m_windowToCloseOnIdle;

		void CloseOldWindow(object sender, EventArgs e)
		{
			Application.Idle -= CloseOldWindow;
			if (m_windowToCloseOnIdle != null)
				m_windowToCloseOnIdle.Close();
			m_windowToCloseOnIdle = null;
		}

		public bool OnConfigureHeadwordNumbers(object commandObject)
		{
			CheckDisposed();
			var configDlg = commandObject as XmlDocConfigureDlg;

			Form formActive = ActiveForm;
			FwXWindow wndActive = formActive as FwXWindow;
			if (wndActive == null && configDlg != null)
				wndActive = configDlg.Owner as FwXWindow;
			if (wndActive != null)
			{
				var hc = wndActive.Cache.ServiceLocator.GetInstance<HomographConfiguration>();
				using (var dlg = new ConfigureHomographDlg())
				{
					dlg.SetupDialog(hc, wndActive.Cache, wndActive.ActiveStyleSheet, this, this);
					dlg.StartPosition = FormStartPosition.CenterScreen;
					if (dlg.ShowDialog(wndActive) != DialogResult.OK)
						return true;
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
			FwXWindow wndActive = formActive as FwXWindow;
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
		/// This implements the "Synchronize with LiftShare..." menu command.
		/// </summary>
		/// <param name="sender"></param>
		/// <returns></returns>
		/// <remarks>Until LiftShare is fully implemented, this is irrelevant.</remarks>
		public bool OnSynchronize(object sender)
		{
#if WANTPORT // FWR-2845; this was not enabled in 6.0 and may be superseded by Randy's LiftBridge.
			Form formActive = ActiveForm;
			if (cache != null)
			{
				FwXWindow wndActive = formActive as FwXWindow;
				LiftSynchronizeDlg dlg = new LiftSynchronizeDlg(Cache, wndActive.Mediator);
				dlg.ShowDialog(formActive);
				return true;
			}
			else
			{
				return false;
			}
#else
			return false;
#endif
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
			var setDatabases = new HashSet<string>();
			foreach (FwXWindow wnd in m_rgMainWindows)
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
		/// <summary>
		/// When user selects Help -> Training, display a webpage in place of the Word Training documents.
		/// </summary>
		/// <param name="sender"></param>
		/// <returns></returns>
		public bool OnHelpTraining(object sender)
		{
			CheckDisposed();

			using (var process = new Process())
			{
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.FileName = "https://lingtran.net/FLEx+8";
				process.Start();
				process.Close();
			}
			return true;
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

		public bool OnHelpNotesSendReceive(object sender)
		{
			CheckDisposed();

			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on FieldWorks Send-Receive.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			});
			return true;
		}

		/// <summary>
		/// Display a file from the Language Explorer\Training directory.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnHelpTrainingFile(object commandObject)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command)commandObject;
			string fileName = SIL.Utils.XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "file");
			fileName = fileName.Replace('\\', Path.DirectorySeparatorChar);
			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}" + fileName, Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotShowX, path),
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

			XCore.Command command = (XCore.Command)commandObject;
			string fileName = SIL.Utils.XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "file");
			fileName = fileName.Replace('\\', Path.DirectorySeparatorChar);
			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}" + fileName, Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotShowX, path),
					LexTextStrings.ksError);
			});
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

			XCore.Command command = (XCore.Command)commandObject;
			string fileName = SIL.Utils.XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "file");
			fileName = fileName.Replace('\\', Path.DirectorySeparatorChar);
			string path = String.Format(FwDirectoryFinder.CodeDirectory + "{0}Helps{0}" + fileName,
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotShowX, path),
					LexTextStrings.ksError);
			});
			return true;
		}

		public bool OnHelpDemoMovies(object commandObject)
		{
			CheckDisposed();

			const string moviesUrl = "https://software.sil.org/fieldworks/download/demo-movies/index-of-demo-movies/";

			OpenDocument(moviesUrl, e =>
				MessageBox.Show(null, string.Format(LexTextStrings.ksErrorCannotOpenMovies, moviesUrl), LexTextStrings.ksError));

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

		/// <summary>
		/// Launch the main help file for Flex.
		/// </summary>
		/// <param name="sender"></param>
		/// <returns></returns>
		public bool OnHelpLanguageExplorer(object sender)
		{
			CheckDisposed();

			try
			{
				// When the help window is closed it will return focus to the window that opened it (see MSDN
				// documentation for HtmlHelp()). We don't want to use the main window as the parent, because if
				// a modal dialog is visible, it will still return focus to the main window, allowing the main window
				// to perform some behaviors (such as refresh by pressing F5) while the modal dialog is visible,
				// which can be bad. So, we just create a dummy control and pass that in as the parent.
				Help.ShowHelp(new Control(), HelpFile);
				TrackingHelper.TrackHelpRequest(HelpFile, "Help (Browse)");
			}
			catch(Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, HelpFile),
					LexTextStrings.ksError);
			}
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

			if (form is FwXWindow)
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

			// The try-catch block is modeled after that used by TeScrInitializer.Initialize(),
			// as the suggestion for fixing LT-8797.
			try
			{
				SafelyEnsureStyleSheetPostTERemoval(progressDlg);
			}
			catch (WorkerThreadException e)
			{
				MessageBox.Show(Form.ActiveForm, e.InnerException.Message,
					ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			return true;
		}

		private void SafelyEnsureStyleSheetPostTERemoval(IThreadedProgress progressDlg)
		{
			// Ensure that we have up-to-date versification information so that projects with old TE styles
			// will be able to migrate them from the Scripture area to the LanguageProject model
			ScrReference.InitializeVersification(FwDirectoryFinder.EditorialChecksDirectory, false);
			// Make sure this DB uses the current stylesheet version
			// Suppress adjusting scripture sections since isn't safe to do so at this point
			SectionAdjustmentSuppressionHelper.Do(() => FlexStylesXmlAccessor.EnsureCurrentStylesheet(Cache.LangProject, progressDlg));
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

		#region IxCoreColleague Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization. Never called because we don't use the xWindow class.
		/// </summary>
		/// <param name="mediator">Message mediator</param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters">Not used</param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, PropertyTable propertyTable, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the message targets in the right order (i.e. main window that has focus first)
		/// </summary>
		/// <returns>List of main windows (which are possible message targets)</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[] { this };
		}

		#endregion

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
				if (Platform.IsUnix && (path.EndsWith(".html") || path.EndsWith(".htm")))
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
				TrackingHelper.TrackHelpRequest(path, Path.GetFileName(path));
			}
			catch (T e)
			{
				if (exceptionHandler != null)
					exceptionHandler(e);
			}
		}
	}
}

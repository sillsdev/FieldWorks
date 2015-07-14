// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwXWindow.cs
// Responsibility: FLEx Team
//
// <remarks>
//	This just wraps the FieldWorks-agnostic XWindow in a form that FwApp can swallow.
// </remarks>

using System;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using L10NSharp;
using SIL.Archiving;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.XWorks.Archiving;
using SIL.Utils;
using SIL.Utils.FileDialog;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FwCoreDlgControls;
using XCore;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using Logger = SIL.Utils.Logger;
#if !__MonoCS__
using NetSparkle;
#endif

namespace SIL.FieldWorks.XWorks
{

	/// <summary>
	/// Summary description for FwXWindow.
	/// </summary>
	public class FwXWindow : XWindow, IFwMainWnd, ISettings, IRecordListOwner,
		IMainWindowDelegatedFunctions, IMainWindowDelegateCallbacks, IFindAndReplaceContext
	{
		#region Member variables
		/// <summary>
		/// Shared functionality of FwXWindow and FwMainWnd may be delegated here.
		/// </summary>
		protected MainWindowDelegate m_delegate;
		/// <summary>
		/// Flag indicating whether or not this instance of MainWnd is a copy of
		/// another Wnd (i.e. created by choosing the "Window/New Window" menu).
		/// </summary>
		protected bool m_fWindowIsCopy = false;

		/// <summary>
		/// Configuration file pathname.
		/// </summary>
		protected string m_configFile;

		/// <summary>
		///
		/// </summary>
		protected ActiveViewHelper m_viewHelper;

		/// <summary>
		/// Set by the owning application during startup when it has a url/link command line parameter
		/// </summary>
		protected FwLinkArgs m_startupLink;

		/// <summary>
		/// list of the virtual handlers we loaded.
		/// </summary>
		protected List<IVwVirtualHandler> m_installedVirtualHandlers;

		static bool m_fInUndoRedo; // true while executing an Undo/Redo command.

		private static LocalizationManager s_localizationMgr;

		/// <summary>
		/// The stylesheet used for all views in this window.
		/// </summary>
		protected FwStyleSheet m_StyleSheet;

		protected FwApp m_app; // protected so the test mock can get to it.
		#endregion

		/// <summary>
		/// This is the one (and should be only) handler for the user Refresh command.
		/// Refresh wants to first clean up the cache, then give things like Clerks a
		/// chance to reload stuff (calling the old OnRefresh methods), then give
		/// windows a chance to redisplay themselves.
		/// </summary>
		public virtual void OnMasterRefresh(object sender)
		{
			CheckDisposed();

			// Susanna asked that refresh affect only the currently active project, which is
			// what the string and List variables below attempt to handle.  See LT-6444.
			FwXWindow activeWnd = ActiveForm as FwXWindow;

			FdoCache activeCache = null;
			if (activeWnd != null)
				activeCache = activeWnd.Cache;

			List<FwXWindow> rgxw = new List<FwXWindow>();
			foreach (IFwMainWnd wnd in m_app.MainWindows)
			{
				FwXWindow xwnd = wnd as FwXWindow;
				if (xwnd != null)
				{
					if (activeCache == null || xwnd.Cache == activeCache)
					{
						xwnd.PrepareToRefresh();
						rgxw.Add(xwnd);
					}
				}
			}
			if (activeWnd != null)
				rgxw.Remove(activeWnd);

			foreach (FwXWindow xwnd in rgxw)
			{
				xwnd.FinishRefresh();
				xwnd.Refresh();
			}

			// LT-3963: active window changes as a result of a refresh.
			// Make sure focus doesn't switch to another FLEx application / window also
			// make sure the application focus isn't lost all together.
			// ALSO, after doing a refresh with just a single application / window,
			// the application would loose focus and you'd have to click into it to
			// get that back, this will reset that too.
			if (activeWnd != null)
			{
				// Refresh it last, so its saved settings get restored.
				activeWnd.FinishRefresh();
				activeWnd.Refresh();
				activeWnd.Activate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Find menu command.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnEditFind(object args)
		{
			return m_app.ShowFindReplaceDialog(false, ActiveView as RootSite);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Find menu command.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnEditReplace(object args)
		{
			return m_app.ShowFindReplaceDialog(true, ActiveView as RootSite);
		}

		#region Overridden properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application registry key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override RegistryKey ApplicationRegistryKey
		{
			get { return m_app.SettingsKey; }
		}

		/// <summary>
		/// Gets an offset from the system's local application data folder.
		/// Subclasses should override this property
		/// if they want a folder within the base folder.
		/// </summary>
		protected override string LocalApplicationDataOffset
		{
			get { return Path.Combine("SIL", "FieldWorks"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the flid of the owning property of the stylesheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int StyleSheetOwningFlid
		{
			get
			{
				CheckDisposed();
				// If the active view is using the AnthroStyleSheet, return the appropriate flid.
				if (ActiveView != null)
				{
					IVwRootSite site = ActiveView.CastAsIVwRootSite();
					if (site != null)
					{
						IVwRootBox rootb = site.RootBox;
						if (rootb != null)
						{
							IVwStylesheet vss = rootb.Stylesheet;
							FwStyleSheet fss = vss as FwStyleSheet;
							if (fss != null)
							{
								int hvo = fss.RootObjectHvo;
								if (hvo > 0)
								{
									int clid = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetClsid(hvo);
									if (clid == LangProjectTags.kClassId)
										return LangProjectTags.kflidStyles;
								}
							}
						}
					}
				}
				return LangProjectTags.kflidStyles;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hvo of the main "root object" associated with the application to which this
		/// main window belongs. For example, for TE, this would be the HVO of Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int HvoAppRootObject
		{
			get
			{
				CheckDisposed();
				return Cache.LanguageProject.LexDbOA.Hvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the setting for style levels to show. If custom lists or sub-lists of styles
		/// are displayed in the application, this setting should be overridden.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int MaxStyleLevelToShow
		{
			get
			{
				CheckDisposed();
				return Int32.MaxValue;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when styles are renamed or deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void OnStylesRenamedOrDeleted()
		{
			CheckDisposed();

			// Need to reload cache because styles might be renamed which might have
			// changed the paragraphs
			PrepareToRefresh();
			//FinishRefresh();
			Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows individual implementations to override the default behavior when populating
		/// the paragraph style list.
		/// </summary>
		/// <returns><c>false</c> by default, but overridden versions may return <c>true</c> if
		/// to prevent the default behavior.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool PopulateParaStyleListOverride()
		{
			return false;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default c'tor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwXWindow()
		{
			BasicInit(null);
		}

		private void BasicInit(FwApp app)
		{
			m_delegate = new MainWindowDelegate(this);
			m_delegate.App = app;
			m_mediator.HelpTopicProvider = app;
			m_mediator.FeedbackInfoProvider = app;
			m_mediator.PropertyTable.SetProperty("App", app);
			m_mediator.PropertyTable.SetPropertyPersistence("App", false);

			string path = null;
			if (app != null) // if configFile in FwXApp == null
			{
				path = FdoFileHelper.GetConfigSettingsDir(app.Cache.ProjectId.ProjectFolder);
				Directory.CreateDirectory(path);
			}
			m_mediator.PropertyTable.UserSettingDirectory = path;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor for TESTING with MockFwXWindow
		/// </summary>
		/// <param name="app"></param>
		/// <param name="configFile">Only sets the member variable here, does NOT load UI.</param>
		/// ------------------------------------------------------------------------------------
		public FwXWindow(FwApp app, string configFile)
		{
			BasicInit(app);

			Debug.Assert(File.Exists(configFile));
			m_app = app;
			m_configFile = configFile;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a new form
		/// </summary>
		/// <param name="app">The app.</param>
		/// <param name="wndCopyFrom">Source window to copy from</param>
		/// <param name="iconStream">The icon stream.</param>
		/// <param name="configFile">The config file.</param>
		/// <param name="startupLink">The link to follow once the window is initialized.</param>
		/// <param name="inAutomatedTest">if set to <c>true</c>, (well behaved) code will avoid
		/// bringing up dialogs that we cannot respond to (like confirmation and error dialogs)
		/// and it should cause the system to avoid saving the settings to a file (or at least
		/// saving them in some special way so as not to mess up the user.)</param>
		/// ------------------------------------------------------------------------------------
		public FwXWindow(FwApp app, Form wndCopyFrom, Stream iconStream,
			string configFile, FwLinkArgs startupLink, bool inAutomatedTest)
		{
			BasicInit(app);
			m_startupLink = startupLink;

			Debug.Assert(File.Exists(configFile));
			m_app = app;
			m_configFile = configFile;

			Init(iconStream, wndCopyFrom, app.Cache);
			if(inAutomatedTest)
			{
				Mediator.PropertyTable.SetProperty("DoingAutomatedTest", true);
				Mediator.PropertyTable.SetPropertyPersistence("DoingAutomatedTest", false);
			}

			// The order of the next two lines has been changed because the loading of the UI
			// properly depends on m_viewHelper being initialized.  Why this was not so with DNB
			// I do not know.
			// Here is the orginal order (along with a comment between them that seemed to imply this
			// new order could be a problem, but no obvious ones have appeared in my testing.

		   /*
			* LoadUI(configFile);
			* // Reload additional property settings that depend on knowing the database name.
			* m_viewHelper = new ActiveViewHelper(this);
			*/

			m_viewHelper = new ActiveViewHelper(this);
			LoadUI(configFile);

			if (!Mediator.PropertyTable.GetBoolProperty("DidAutomaticParseIsCurrentReset", false))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
					() =>
					{
						var paraRepo = Cache.ServiceLocator.GetInstance<IStTxtParaRepository>();
						foreach (var para in paraRepo.AllInstances().Where(para => para.ParseIsCurrent))
						{
							para.ParseIsCurrent = false;
						}
					});
				Mediator.PropertyTable.SetProperty("DidAutomaticParseIsCurrentReset", true);
				Mediator.PropertyTable.SetPropertyPersistence("DidAutomaticParseIsCurrentReset", true);
			}

			m_viewHelper.ActiveViewChanged += new EventHandler<EventArgs>(ActiveViewChanged);
		}

		/// <summary>
		/// Different active view, we need to update the combo box.
		/// </summary>
		private void ActiveViewChanged(object sender, EventArgs e)
		{
			m_delegate.InitStyleComboBox();
		}

		/// <summary>
		/// FwXWindow also restores database-specific properties.
		/// </summary>
		protected override void RestoreProperties()
		{
			base.RestoreProperties();
			m_mediator.PropertyTable.RestoreFromFile(m_mediator.PropertyTable.LocalSettingsId);
			GlobalSettingServices.RestoreSettings(Cache.ServiceLocator, m_mediator.PropertyTable);
		}

		/// <summary>
		/// If we are discarding saved settings, we must not keep any saved sort sequences,
		/// as they may represent a filter we are not restoring (LT-11647)
		/// </summary>
		protected override void DiscardProperties()
		{
			var tempDirectory = Path.Combine(Cache.ProjectId.ProjectFolder, FdoFileHelper.ksSortSequenceTempDir);
			Palaso.IO.DirectoryUtilities.DeleteDirectoryRobust(tempDirectory);
		}

		public void ClearInvalidatedStoredData()
		{
			DiscardProperties();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Construct a new form
		/// </summary>
		/// <param name="app">the app.</param>
		/// <param name="wndCopyFrom">Source window to copy from.</param>
		/// <param name="iconStream"></param>
		/// <param name="configStream"></param>
		/// -----------------------------------------------------------------------------------
		public FwXWindow(FwApp app, Form wndCopyFrom, Stream iconStream,
			Stream configStream) : this()
		{
			m_app = app;
			m_configFile = null;
			Init(iconStream, wndCopyFrom, app.Cache);
			LoadUI(configStream);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_viewHelper != null)
					m_viewHelper.Dispose();
				if (m_app != null)
				{
					// The removing of the window needs to happen later; after this main window is
					// already disposed of. This is needed for side-effects that require a running
					// message loop (such as closing the TE notes view which would normally
					// happen at this call without a running message loop)
					m_app.FwManager.ExecuteAsync(m_app.RemoveWindow, this);
				}
			}

			// NOTE: base.Dispose() may need the FdoCache which RemoveWindow() wants to delete.
			base.Dispose(disposing);

			m_viewHelper = null;
			m_delegate = null;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			Logger.WriteEvent(WindowHandleInfo("Created new window"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This main window is now the active one handle any data changes made in other
		/// applications. (By analogy with FwMainWindow, may also need to do something about
		/// Find/Replace?? (JT)).
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			Logger.WriteEvent(WindowHandleInfo("Activated window"));

			/* Bad things happen, when this is done and the parser is running.
			 * TODO: Figure out how they can co-exist.
			if (FwApp.App != null)
			{
				FwApp.App.SyncFromDb();
			}
			*/
			m_delegate.OnActivated();
		}


		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Logger.WriteEvent(WindowHandleInfo("Closed window"));
		}

		private string WindowHandleInfo(string eventMsg)
		{
			return String.Format(eventMsg + " [{0}] handle: [0x{1}]",
				Cache.ProjectId.Name, this.Handle.ToString("x"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Common initializations
		/// </summary>
		/// <param name="iconStream"></param>
		/// <param name="configFile"></param>
		/// <param name="app"></param>
		/// <param name="wndCopyFrom"></param>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		private void Init(Stream iconStream, Form wndCopyFrom, FdoCache cache)
		{
			m_fWindowIsCopy = (wndCopyFrom != null);
			InitMediatorValues(cache);

			if(iconStream != null)
				Icon = new System.Drawing.Icon(iconStream);
		}

		protected void InitMediatorValues(FdoCache cache)
		{
			Mediator.PropertyTable.LocalSettingsId = "local";
			Mediator.PropertyTable.SetProperty("cache", cache);
			Mediator.PropertyTable.SetPropertyPersistence("cache", false);
			Mediator.PropertyTable.SetProperty("DocumentName", GetProjectName(cache));
			Mediator.PropertyTable.SetPropertyPersistence("DocumentName", false);
			Mediator.PathVariables["{DISTFILES}"] = FwDirectoryFinder.CodeDirectory;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string that will go in the caption of the main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string GetMainWindowCaption(FdoCache cache)
		{
			string sCaption = m_delegate.GetMainWindowCaption(cache);
			return sCaption ?? Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the project name from the specified cache. If the connection is to a remote
		/// server, the string returned will include the server name, formatted in a form
		/// suitable for including in a window caption.
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// ------------------------------------------------------------------------------------
		public string GetProjectName(FdoCache cache)
		{
			return m_delegate.GetProjectName(cache);
		}

		/// <summary>
		/// factory method for getting a progress state which is already hooked up to the correct progress panel
		/// </summary>
		/// <param name="taskLabel"></param>
		/// <returns></returns>
		public static ProgressState CreatePredictiveProgressState(Mediator mediator, string taskLabel)
		{
			if (mediator == null || mediator.PropertyTable == null)
				return new NullProgressState();//not ready to be doing progress bars


			StatusBarProgressPanel panel = mediator.PropertyTable.GetValue("ProgressBar") as StatusBarProgressPanel;
			if (panel == null)
				return new NullProgressState();//not ready to be doing progress bars

			IApp app = (IApp)mediator.PropertyTable.GetValue("App");
			PredictiveProgressState s = new PredictiveProgressState(panel, app.SettingsKey, taskLabel);
			return s;
		}
		/// <summary>
		/// factory method for getting a progress state which is already hooked up to the correct progress panel
		/// </summary>
		/// <param name="taskLabel"></param>
		/// <returns></returns>
		public static ProgressState CreateMilestoneProgressState(Mediator mediator)
		{
			if (mediator == null || mediator.PropertyTable == null)
				return new NullProgressState();//not ready to be doing progress bars


			StatusBarProgressPanel panel = mediator.PropertyTable.GetValue("ProgressBar") as StatusBarProgressPanel;
			if (panel == null)
				return new NullProgressState();//not ready to be doing progress bars

			return new MilestoneProgressState(panel);
		}
		/// <summary>
		/// factory method for getting a simple progress state which is already hooked up to the correct progress panel
		/// </summary>
		/// <param name="taskLabel"></param>
		/// <returns></returns>
		public static ProgressState CreateSimpleProgressState(Mediator mediator)
		{
			if (mediator == null || mediator.PropertyTable == null)
				return new NullProgressState();//not ready to be doing progress bars


			StatusBarProgressPanel panel = mediator.PropertyTable.GetValue("ProgressBar") as StatusBarProgressPanel;
			if (panel == null)
				return new NullProgressState();//not ready to be doing progress bars

			return new ProgressState(panel);
		}


		#region XCore Message Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnEditCut(object arg)
		{
			CheckDisposed();

			if (m_viewHelper.ActiveView != null)
			{
				using (new DataUpdateMonitor(this, "EditCut"))
					return m_viewHelper.ActiveView.EditingHelper.CutSelection();
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnEditCopy(object arg)
		{
			CheckDisposed();

			if (m_viewHelper.ActiveView != null)
				return m_viewHelper.ActiveView.EditingHelper.CopySelection();
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnEditPaste(object arg)
		{
			CheckDisposed();

			if (m_viewHelper.ActiveView != null)
			{
				string stUndo, stRedo;
				ResourceHelper.MakeUndoRedoLabels("kstidEditPaste", out stUndo, out stRedo);
				using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
					Cache.ServiceLocator.GetInstance<IActionHandler>(), stUndo, stRedo))
				using (new DataUpdateMonitor(this, "EditPaste"))
				{
					if (m_viewHelper.ActiveView.EditingHelper.PasteClipboard())
						undoHelper.RollBack = false;
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Paste what is in the clipboard as a URL
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public bool OnPasteUrl(object arg)
		{
			CheckDisposed();

			if (EditingHelper is RootSiteEditingHelper)
				return ((RootSiteEditingHelper)EditingHelper).PasteUrl(StyleSheet);
			return false;
		}

		/// <summary>
		/// Enable the PasteUrl command
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayPasteUrl(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = EditingHelper is RootSiteEditingHelper && ((RootSiteEditingHelper)EditingHelper).CanPasteUrl();
			return true;
		}

		/// <summary>
		/// Handle the InsertLinkToFile command
		/// </summary>
		public bool OnInsertLinkToFile(object arg)
		{
			CheckDisposed();

			RootSiteEditingHelper helper = EditingHelper as RootSiteEditingHelper;
			if (helper == null || !helper.CanInsertLinkToFile())
				return false;
			string pathname = null;
			using (var fileDialog = new OpenFileDialogAdapter())
			{
				fileDialog.Filter = ResourceHelper.FileFilter(FileFilterType.AllFiles);
				fileDialog.RestoreDirectory = true;
				if (fileDialog.ShowDialog() != DialogResult.OK)
					return false;
				pathname = fileDialog.FileName;
			}
			if (string.IsNullOrEmpty(pathname))
				return false;
			pathname = MoveOrCopyFilesDlg.MoveCopyOrLeaveExternalFile(pathname,
				Cache.LangProject.LinkedFilesRootDir, m_mediator.HelpTopicProvider,  Cache.ProjectId.IsLocal);
			if (String.IsNullOrEmpty(pathname))
				return false;
			// JohnT: don't use m_StyleSheet, no guarantee it has been created (see LT-7034)
			helper.ConvertSelToLink(pathname, StyleSheet);
			return true;
		}

		/// <summary>
		/// Enable the InsertLinkToFile command
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertLinkToFile(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			bool fAllow = Mediator.PropertyTable.GetBoolProperty("AllowInsertLinkToFile", true);

			if (fAllow)
			{
				display.Enabled = EditingHelper is RootSiteEditingHelper &&
					((RootSiteEditingHelper)EditingHelper).CanInsertLinkToFile();
				display.Visible = true;
			}
			else
			{
				display.Enabled = false;
				display.Visible = false;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is very similar to OnUpdateEditCut, but for xCore applications.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true to indicate handled.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayEditCut(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = m_viewHelper != null && m_viewHelper.ActiveView != null &&
				m_viewHelper.ActiveView.EditingHelper.CanCut();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is very similar to OnUpdateEditCopy, but for xCore applications.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true to indicate handled.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayEditCopy(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = m_viewHelper.ActiveView != null &&
				m_viewHelper.ActiveView.EditingHelper.CanCopy();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is very similar to OnUpdateEditPaste, but for xCore applications.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true to indicate handled.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayEditPaste(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = m_viewHelper.ActiveView != null &&
				m_viewHelper.ActiveView.EditingHelper.CanPaste();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement the SelectAll command (for the active view).
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnEditSelectAll(object arg)
		{
			CheckDisposed();

			if (m_viewHelper.ActiveView != null)
			{
				try
				{
					EnableBulkLoadingDisableIdleProcessing(true);
					m_viewHelper.ActiveView.EditingHelper.SelectAll();
				}
				finally
				{
					EnableBulkLoadingDisableIdleProcessing(false);
				}
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable the SelectAll command if appropriate
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true to indicate handled.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayEditSelectAll(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = m_viewHelper.ActiveView != null;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUtilities(object command)
		{
			CheckDisposed();

			using (UtilityDlg dlg = new UtilityDlg(m_app))
			{
				dlg.SetDlgInfo(m_mediator, (command as XCore.Command).Parameters[0]);
				dlg.ShowDialog(this);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true to indicate handled.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayUtilities(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method enables the Character Map command if the exe exists in the system dir.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayShowCharMap(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (MiscUtils.IsUnix)
			{
				// Always enable menu command on Linux. If it's not installed we display an error
				// message when the user tries to launch it. See FWNX-567 for more info.
				display.Enabled = true;
				return true;
			}

			// If we can find the CharMap program, then enable the menu command
			string sysStr = Environment.GetFolderPath(Environment.SpecialFolder.System);
			string charmapPath = sysStr + "\\charmap.exe";
			display.Enabled = System.IO.File.Exists(charmapPath);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the Character Map program (if it's found).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OnShowCharMap(object command)
		{
			CheckDisposed();

			var program = "charmap.exe";
			Action<Exception> errorHandler = null;
			if (MiscUtils.IsUnix)
			{
				program = "gucharmap";
				errorHandler = (exception) => {
					MessageBox.Show(string.Format(xWorksStrings.ksUnableToStartGnomeCharMap,
						program));
				};
			}

			using (MiscUtils.RunProcess(program, null, errorHandler))
				return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnNewWindow(object command)
		{
			CheckDisposed();

			// Ensure that the new window opens in the same tool and location as this window.
			// See LT-1648.
			SaveSettings();
			m_app.FwManager.OpenNewWindowForApp(m_app, this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start logging events.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnStartLogging(object args)
		{
			// For now, it attaches itself to various things through events, and runs until
			// the program exits.
			using (ScriptMaker sm = new ScriptMaker(ActiveForm))
			{
				LinkListener ll = (LinkListener)m_mediator.PropertyTable.GetValue("LinkListener",
					null);
				if (ll == null)
					return true;
				sm.GoTo(ll.CurrentContext.ToString());
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnSaveLangProject(object command)
		{
			CheckDisposed();

			Cache.DomainDataByFlid.GetActionHandler().Commit();

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is very similar to OnUpdateFileSave, but for xCore applications.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true to indicate handled.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplaySaveLangProject(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = (Cache != null &&
					Cache.ServiceLocator.GetInstance<IUndoStackManager>().HasUnsavedChanges);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnChooseLangProject(object command)
		{
			CheckDisposed();

			//there appears to be a problem with the DotNetBar balloon help which causes
			//it to crash when the user hovers over something that should have a balloon but that window
			//is behind a modeless dialog.
			var balloonActive=m_mediator.PropertyTable.GetBoolProperty("ShowBalloonHelp", false);
			m_mediator.PropertyTable.SetProperty("ShowBalloonHelp", false);

			m_delegate.FileOpen(this);

			m_mediator.PropertyTable.SetProperty("ShowBalloonHelp", balloonActive);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show dialog to report a bug in the system
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnHelpMakeSuggestion(object args)
		{
			ErrorReporter.MakeSuggestion(FwRegistryHelper.FieldWorksRegistryKey, "FLExDevteam@sil.org", this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show dialog to report a bug in the system
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnHelpReportProblem(object args)
		{
			ErrorReporter.ReportProblem(FwRegistryHelper.FieldWorksRegistryKey, m_app.SupportEmailAddress, this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check if any updates to FW are available
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnHelpCheckForUpdates(object args)
		{
#if !__MonoCS__
			var sparkle = SingletonsContainer.Item("Sparkle") as Sparkle;
			if (sparkle == null)
				MessageBox.Show("Updates do not work unless FieldWorks was installed via the installer.");
			else
				sparkle.CheckForUpdatesAtUserRequest();
#endif
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show Help About dialog
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnHelpAbout(object args)
		{
			m_delegate.ShowHelpAbout();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnNewLangProject(object command)
		{
			CheckDisposed();
			m_delegate.FileNew(this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle click on Archive With RAMP menu item
		/// </summary>
		/// <param name="command">Not used</param>
		/// <returns>true (handled)</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnArchiveWithRamp(object command)
		{
			CheckDisposed();

			// show the RAMP dialog
			var filesToArchive = m_app.FwManager.ArchiveProjectWithRamp(m_app, this);

			// if there are no files to archive, return now.
			if((filesToArchive == null) || (filesToArchive.Count == 0))
				return true;

			ReapRamp ramp = new ReapRamp();
			return ramp.ArchiveNow(this, MainMenuStrip.Font, Icon, filesToArchive, m_mediator, m_app, Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update Archive With RAMP menu item
		/// </summary>
		/// <param name="args">the toolbar/menu item properties</param>
		/// <returns>true if handled</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateArchiveWithRamp(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Visible = !(MiscUtils.IsMono);
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle whether to enable Archive With RAMP menu item (enabled iff properly installed)
		/// </summary>
		/// <param name="command">Not used</param>
		/// <param name="display">Display properties</param>
		/// <returns>true (handled)</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnDisplayArchiveWithRamp(object command, ref UIItemDisplayProperties display)
		{
			display.Enabled = ReapRamp.Installed;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle whether to enable menu item.
		/// </summary>
		/// <param name="command">Not used</param>
		/// <param name="display">Display properties</param>
		/// <returns>true (handled)</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnDisplayPublishToWebonary(object command, ref UIItemDisplayProperties display)
		{
			display.Enabled = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle click on menu item
		/// </summary>
		/// <param name="command">Not used</param>
		/// <returns>true (handled)</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnPublishToWebonary(object command)
		{
			CheckDisposed();
			ShowPublishToWebonaryDialog(m_mediator);
			return true;
		}

		internal static void ShowPublishToWebonaryDialog(Mediator mediator)
		{
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");

			var reversals = cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances().Select(item => item.Name.BestAnalysisAlternative.Text);
			var publications = cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Select(p => p.Name.BestAnalysisAlternative.Text).ToList();

			var projectConfigDir = DictionaryConfigurationListener.GetProjectConfigurationDirectory(mediator, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var defaultConfigDir = DictionaryConfigurationListener.GetDefaultConfigurationDirectory(DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var configurations = DictionaryConfigurationController.GetDictionaryConfigurationLabels(cache, defaultConfigDir, projectConfigDir);

			// show dialog
			var controller = new PublishToWebonaryController
			{
				Cache = cache,
				Mediator = mediator
			};
			var model = new PublishToWebonaryModel(mediator)
			{
				Reversals = reversals,
				Configurations = configurations,
				Publications = publications
			};
			using (var dialog = new PublishToWebonaryDlg(controller, model, mediator))
			{
				dialog.ShowDialog();
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the project properties dialog
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileProjectProperties(object command)
		{
			LaunchProjPropertiesDlg(false);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exits the application :)
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnExitApplication(object param)
		{
			CheckDisposed();
			m_delegate.FileExit();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Launches the proj properties DLG.
		/// </summary>
		/// <param name="startOnWSPage">if set to <c>true</c> [start on WS page].</param>
		/// ------------------------------------------------------------------------------------
		private void LaunchProjPropertiesDlg(bool startOnWSPage)
		{
			if (!ClientServerServicesHelper.WarnOnOpeningSingleUserDialog(Cache))
				return;
			if (!SharedBackendServicesHelper.WarnOnOpeningSingleUserDialog(Cache))
				return;

			FdoCache cache = Cache;
			bool fDbRenamed = false;
			string sProject = cache.ProjectId.Name;
			string sLinkedFilesRootDir = cache.LangProject.LinkedFilesRootDir;
			using (var dlg = new FwProjPropertiesDlg(cache, m_app, m_app, FontHeightAdjuster.StyleSheetFromMediator(Mediator)))
			{
				dlg.ProjectPropertiesChanged += OnProjectPropertiesChanged;
				if (startOnWSPage)
					dlg.StartWithWSPage();
				if (dlg.ShowDialog(this) != DialogResult.Abort)
				{
					fDbRenamed = dlg.ProjectNameChanged();
					if (fDbRenamed)
					{
						sProject = dlg.ProjectName;
					}
					bool fFilesMoved = false;
					if (dlg.LinkedFilesChanged())
					{
						fFilesMoved = m_app.UpdateExternalLinks(sLinkedFilesRootDir);
					}
					// no need for any of these refreshes if entire window has been/will be
					// destroyed and recreated.
					if (!fDbRenamed && !fFilesMoved)
					{
						Mediator.PropertyTable.SetProperty("DocumentName", cache.ProjectId.UiName);
						Mediator.PropertyTable.SetPropertyPersistence("DocumentName", false);
					}
				}
			}
			if (fDbRenamed)
				m_app.FwManager.RenameProject(sProject, m_app);
		}

		private void OnProjectPropertiesChanged(object sender, EventArgs eventArgs)
		{
			// this event is fired before the Project Properties dialog is closed, so that we have a chance
			// to refresh everything before Paint events start getting fired, which can cause problems if
			// any writing systems are removed that a rootsite is currently displaying
			var dlg = (FwProjPropertiesDlg) sender;
			if (dlg.WritingSystemsChanged())
			{
				if (m_app is FwXApp)
					((FwXApp)m_app).OnMasterRefresh(null);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fullName"></param>
		/// <param name="oldString"></param>
		/// <param name="newString"></param>
		/// <returns></returns>
		public static string CreateNewFileName(string fullName, string oldString, string newString)
		{
			string oldName = fullName;
			StringBuilder strBuilderNewName = new StringBuilder(fullName);
			int index = fullName.LastIndexOf(oldString);
			if (index != -1)
			{
				strBuilderNewName.Replace(oldString, newString, index, oldString.Length);
				oldName = strBuilderNewName.ToString();
			}
			return oldName;
		}



		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the project properties dialog, but starting with the WS page.
		/// </summary>
		/// <param name="arg"></param>
		/// ------------------------------------------------------------------------------------
		public bool OnWritingSystemProperties(object arg)
		{
			CheckDisposed();

			LaunchProjPropertiesDlg(true);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is very similar to OnUpdateEditCut, but for xCore applications.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true to indicate handled.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayWritingSystemProperties(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Backup and Restore menu command
		/// </summary>
		/// <param name="arg"></param>
		/// <returns>true</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnRestoreAProject(object arg)
		{
			m_app.FwManager.RestoreProject(m_app, this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Restore menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileProjectSharingLocation(object arg)
		{
			if (m_app.Cache.ProjectId.IsLocal)
				m_app.FwManager.FileProjectSharingLocation(m_app, this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the enabled state of the File Project Sharing Location menu item
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayFileProjectSharingLocation(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = m_app.Cache.ProjectId.IsLocal && FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.CanWriteKey();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Backup and Restore menu command
		/// </summary>
		/// <param name="arg"></param>
		/// <returns>true</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackupThisProject(object arg)
		{
			SaveSettings(); // so they can be backed up!
			m_app.FwManager.BackupProject(m_app, this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/"Project Management"/Delete... menu command
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDeleteProject(object args)
		{
			m_app.FwManager.DeleteProject(m_app, this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the File/"Project Management"/Delete... menu item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayDeleteProject(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = true;
			return true;
		}

		/// <summary>
		/// See whether the focus control wants to handle the specified message.
		/// It is assumed that if the focus control has a public or non-public method of the
		/// given name, it takes the supplied arguments and returns boolean.
		/// Call it and return what it returns.
		/// Special case: if the focus control is 'this', that is, the recipient xWindow has
		/// focus, just return false. This is because this method is called from, say,
		/// the implementation of OnDisplayUndo, to see whether some (other) focus control
		/// wants to handle the message. Returning false allows the correct default implementation
		/// of OnDisplayUndo when no (other) focused control wants to do it. Calling OnDisplayUndo
		/// on the focus control, on the other hand, leads to a stack overflow, since the first
		/// thing this.OnDisplayUndo does is to call FocusControlHandlesMessage again!
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private bool FocusControlHandlesMessage(string methodName, object[] args)
		{
			// See whether the control that has focus wants to process this message.
			// Normally we would try to arrange that this control comes before the main window
			// in the list of message targets, but the FwXWindow is before the clerk which
			// says it has to be before the content control in the master list of targets.

			// first, try to use a control that has 'registered' to be first control to handle messages.
			// this provides certain controls the opportunity to still try to handle messages even if
			// they are not precisely in Focus. (see LT-7791)
			Control focusControl = GetFocusControl();
			if (focusControl != null && focusControl != this)
			{
				MethodInfo mi = focusControl.GetType().GetMethod(methodName,
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (mi != null)
					return (bool)mi.Invoke(focusControl, args);
			}
			return false;
		}

		private Control GetFocusControl()
		{
			Control focusControl = null;
			object control = m_mediator.PropertyTable.GetValue("FirstControlToHandleMessages");
			if (control != null)
				focusControl = control as Control;
			if (focusControl == null || focusControl.IsDisposed)
				focusControl = XWindow.FocusedControl();
			return focusControl;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will undo the last changes done to the project.
		/// This function is executed when the user clicks the undo menu item.
		/// </summary>
		/// <param name="args">Unused</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUndo(object args)
		{
			if (FocusControlHandlesMessage("OnUndo", new[] { args }))
				return true;

			var ah = Cache.DomainDataByFlid.GetActionHandler();
			if (ah.CanUndo())
			{
				// start hour glass
				using (new WaitCursor(this))
				{
					try
					{
						m_fInUndoRedo = true;
						ah.Undo();
					}
					finally
					{
						m_fInUndoRedo = false;
					}
				}
				// Trigger a selection changed, to force updating of controls like the writing system combo
				// that might be affected, if relevant.
				var focusRootSite = GetFocusControl() as SimpleRootSite;
				if (focusRootSite != null && !focusRootSite.IsDisposed &&
					focusRootSite.RootBox != null && focusRootSite.RootBox.Selection != null)
				{
					focusRootSite.SelectionChanged(focusRootSite.RootBox, focusRootSite.RootBox.Selection);
				}
				return true;
			}
			return false;
		}

		private void HandleUndoResult(UndoResult ures, bool fPrivate)
		{
			// Enhance JohnT: may want to display messages for kuresFailed, kuresError
			if (ures != UndoResult.kuresSuccess)
			{

				if (!fPrivate && m_app != null)
				{
					// currently implemented, this will cause this app to do a master refresh,
					m_app.Synchronize(SyncMsg.ksyncUndoRedo);
				}
				else
				{
					// EricP/JohnT -- this path will probably never be called in a production
					// context, since we'll have an FwApp. And even in the case of tests
					// taking this path, we wonder if we should issue a "MasterRefresh" instead
					m_mediator.SendMessage("Refresh", this);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Undo menu item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayUndo(object commandObject, ref UIItemDisplayProperties display)
		{
			if (FocusControlHandlesMessage("OnDisplayUndo", new object[] { commandObject, display }))
				return true;

			// Normal processing.
			IActionHandler ah = Cache.DomainDataByFlid.GetActionHandler();
			bool canUndo = (ah.UndoableSequenceCount > 0);
			display.Enabled = canUndo;
			string sUndo = canUndo ? ah.GetUndoText() : xWorksStrings.Undo;
			display.Text = (sUndo == null || sUndo == "") ? xWorksStrings.Undo : sUndo;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will redo the last changes undone to the project.
		/// This function is executed when the user clicks the redo menu item.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnRedo(object args)
		{
			if (FocusControlHandlesMessage("OnRedo", new[] { args }))
				return true;

			var ah = Cache.DomainDataByFlid.GetActionHandler();
			if (ah.CanRedo())
			{
				// start hour glass
				using (new WaitCursor(this))
				{
					try
					{
						m_fInUndoRedo = true;
						ah.Redo();
					}
					finally
					{
						m_fInUndoRedo = false;
					}
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Called in FwXApp.OnMasterRefresh BEFORE clearing the cache, typically to
		/// save any work in progress.
		/// </summary>
		public void PrepareToRefresh()
		{
			CheckDisposed();

			// Use SendMessageToAllNow as this needs to go to everyone right now.  This
			// method on the mediator will not stop when the message is handled, nor is it deferred.
			m_mediator.SendMessageToAllNow("PrepareToRefresh", null);
		}

		/// <summary>
		/// Called in FwXApp.OnMasterRefresh AFTER clearing the cache, to reset everything.
		/// </summary>
		/// <returns></returns>
		public bool FinishRefresh()
		{
			CheckDisposed();

			SuspendLayout();
			// force our stylesheet to resync (LT-7382).
			ResyncStylesheet();
			RefreshDisplay();

			Refresh();
			ResumeLayout(true);

			return true;
		}


		/// <summary>
		/// Collect refreshable caches from every child which has one and refresh them (once each).
		/// Call RefreshDisplay for every child control (recursively) which implements it.
		///
		/// returns true if all the children have been processed by this class.
		/// </summary>
		public bool RefreshDisplay()
		{
			Cache.ServiceLocator.GetInstance<IUndoStackManager>().Refresh();
			var cacheCollector = new HashSet<IRefreshCache>();
			var clerkCollector = new HashSet<RecordClerk>();
			CollectCachesToRefresh(this, cacheCollector, clerkCollector);
			foreach (var cache in cacheCollector)
				cache.Refresh();
			foreach (var clerk in clerkCollector)
				clerk.ReloadIfNeeded();
			try
			{
				// In many cases ReconstructViews, which calls RefreshViews, will also try to Refresh the same caches.
				// Not only is this a waste, but it may wipe out data that ReloadIfNeeded has carefully re-created.
				// So suspend refresh for the caches we have already refreshed.
				foreach (var cache in cacheCollector)
				{
					if (cache is ISuspendRefresh)
						((ISuspendRefresh)cache).SuspendRefresh();
				}
				// Don't be lured into simplifying this to ReconstructViews(this). That has the loop,
				// but also calls this method again, making a stack overflow.
				foreach (Control c in Controls)
					ReconstructViews(c);
			}
			finally
			{
				foreach (var cache in cacheCollector)
				{
					if (cache is ISuspendRefresh)
						((ISuspendRefresh)cache).ResumeRefresh();
				}
			}
			return true;
		}

		/// <summary>
		/// Collect refreshable caches from the specified control and its subcontrols.
		/// We currently handle controls that are rootsites, and check their own SDAs as well
		/// as any base SDAs that those SDAs wrap.
		/// </summary>
		private void CollectCachesToRefresh(Control c, HashSet<IRefreshCache> cacheCollector, HashSet<RecordClerk> clerkCollector)
		{
			var rootSite = c as IVwRootSite;
			if (rootSite != null && rootSite.RootBox != null)
			{
				var sda = rootSite.RootBox.DataAccess;
				while (sda != null)
				{
					if (sda is IRefreshCache)
						cacheCollector.Add((IRefreshCache)sda);
					if (sda is DomainDataByFlidDecoratorBase)
						sda = ((DomainDataByFlidDecoratorBase)sda).BaseSda;
					else
						break;
				}
			}
			var clerkView = c as XWorksViewBase;
			if (clerkView != null && clerkView.ExistingClerk != null)
				clerkCollector.Add(clerkView.ExistingClerk);

			foreach (Control child in c.Controls)
				CollectCachesToRefresh(child, cacheCollector, clerkCollector);
		}

		/// <summary>
		/// Call RefreshDisplay on the passed in control if it has a public (no argument) method of that name.
		/// Recursively call ReconstructViews on each control that the given control contains.
		/// </summary>
		/// <param name="control"></param>
		private void ReconstructViews(Control control)
		{
			bool childrenRefreshed = false;

			var refreshable = control as IRefreshableRoot;
			if (refreshable != null)
			{
				childrenRefreshed = refreshable.RefreshDisplay();
			}
			if (!childrenRefreshed)
			{
				foreach (Control c in control.Controls)
					ReconstructViews(c);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="control"></param>
		/// <returns>List contain zero or more IVwRootbox objects.</returns>
		private List<IVwRootBox> FindAllRootBoxes(Control control)
		{
			List<IVwRootBox> rootboxes = new List<IVwRootBox>();
			if (control is IRootSite)
			{

				rootboxes.AddRange((control as IRootSite).AllRootBoxes());
			}
			foreach (Control c in control.Controls)
			{
				rootboxes.AddRange(FindAllRootBoxes(c));
			}
			return rootboxes;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Redo menu item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayRedo(object commandObject, ref UIItemDisplayProperties display)
		{
			if (FocusControlHandlesMessage("OnDisplayRedo", new object[] { commandObject, display }))
				return true;
			IActionHandler ah = Cache.DomainDataByFlid.GetActionHandler();
			bool canRedo = (ah.RedoableSequenceCount > 0);
			display.Enabled = canRedo;
			string sRedo = canRedo ? ah.GetRedoText() : xWorksStrings.Redo;
			display.Text = (sRedo == null || sRedo == "") ? xWorksStrings.Redo : sRedo;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the Styles dialog
		/// </summary>
		/// <param name="args">ignored</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFormatStyle(object args)
		{
			ShowStylesDialog(ParaStyleListHelper != null ? ParaStyleListHelper.SelectedStyle.Name : null,
				CharStyleListHelper != null ? CharStyleListHelper.SelectedStyle.Name : null,
				null);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required interface member to launch the style dialog.
		/// </summary>
		/// <param name="paraStyleName">Name of the initially selected paragraph style.</param>
		/// <param name="charStyleName">Name of the initially selected character style.</param>
		/// <param name="setPropsToFactorySettings">Delegate to set style info properties back
		/// to the default factory settings</param>
		/// <returns>true if refresh should be called to reload the cache</returns>
		/// ------------------------------------------------------------------------------------
		public bool ShowStylesDialog(string paraStyleName, string charStyleName,
			Action<StyleInfo> setPropsToFactorySettings)
		{
			CheckDisposed();

			if (m_delegate.ShowStylesDialog(paraStyleName, charStyleName, setPropsToFactorySettings))
			{
				// Need to refresh to reload the cache.  See LT-6265.
				(m_app as FwXApp).OnMasterRefresh(null);
			}
			return false;	// refresh already called if needed
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether this window is in a state where it can Apply a style.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool CanApplyStyle
		{
			get { CheckDisposed(); return m_delegate.CanApplyStyle; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the apply style dialog
		/// </summary>
		/// <param name="args">ignored</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFormatApplyStyle(object args)
		{
			if (!m_delegate.CanApplyStyle)
				return false;
			ShowApplyStyleDialog(
				ParaStyleListHelper != null ? ParaStyleListHelper.SelectedStyle.Name : null,
				CharStyleListHelper != null ? CharStyleListHelper.SelectedStyle.Name : null, 0);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the Format Apply Style dialog
		/// </summary>
		/// <param name="paraStyleName">The currently-selected Paragraph style name</param>
		/// <param name="charStyleName">The currently-selected Character style name</param>
		/// <param name="maxStyleLevel">The maximum style level that will be shown in this
		/// dialog. (apps that do not use style levels in their stylesheets can pass 0)</param>
		/// ------------------------------------------------------------------------------------
		public void ShowApplyStyleDialog(string paraStyleName, string charStyleName, int maxStyleLevel)
		{
			m_delegate.ShowApplyStyleDialog(paraStyleName, charStyleName, maxStyleLevel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable the menu command for Format/Styles, if we can.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayFormatApplyStyle(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = m_delegate.CanApplyStyle;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Create Shortcut on Desktop menu/toolbar item.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnCreateShortcut(object args)
		{
			CheckDisposed();

			return m_delegate.OnCreateShortcut(args);
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public override IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();
			if(m_app is IxCoreColleague)
				return new IxCoreColleague[] { this, m_app as IxCoreColleague };
			else
				return new IxCoreColleague[]{this};
		}

		/// <summary>
		/// Display this import command everywhere.
		/// </summary>
		public bool OnDisplayImportTranslatedLists(object parameters, ref UIItemDisplayProperties display)
		{
			display.Enabled = true;
			display.Visible = true;
			return true;
		}

		/// <summary>
		/// Import a file contained translated strings for one or more lists.
		/// </summary>
		/// <remarks>See FWR-1739.</remarks>
		public bool OnImportTranslatedLists(object commandObject)
		{
			string filename = null;
			// ActiveForm can go null (see FWNX-731), so cache its value, and check whether
			// we need to use 'this' instead (which might be a better idea anyway).
			var form = ActiveForm;
			if (form == null)
				form = this;
			using (var dlg = new OpenFileDialogAdapter())
			{
				dlg.CheckFileExists = true;
				dlg.RestoreDirectory = true;
				dlg.Title = ResourceHelper.GetResourceString("kstidOpenTranslatedLists");
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = ResourceHelper.FileFilter(FileFilterType.FieldWorksTranslatedLists);
				if (dlg.ShowDialog(form) != DialogResult.OK)
					return true;
				filename = dlg.FileName;
			}
#if DEBUG
			var dtBegin = DateTime.Now;
#endif
			using (new WaitCursor(form, true))
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
					() => ImportTranslatedLists(filename));
			}
#if DEBUG
			var dtEnd = DateTime.Now;
			var span = new TimeSpan(dtEnd.Ticks - dtBegin.Ticks);
			Debug.WriteLine(String.Format("Total elapsed time for loading translated list(s) from {0} and handling PropChanges = {1}",
				filename, span));
#endif
			return true;
		}

		private void ImportTranslatedLists(string filename)
		{
			using (var dlg = new ProgressDialogWithTask(this))
			{
				dlg.AllowCancel = true;
				dlg.Maximum = 200;
				dlg.Message = filename;
				dlg.RunTask(true, FdoCache.ImportTranslatedLists, filename, Cache);
			}
		}

		#endregion // XCore Message Handlers

		/// <summary>
		/// Either enable bulk loading and disable idle processing, or disable bulk loading and
		/// enable idle processing.
		/// </summary>
		/// <param name="fEnable"></param>
		public void EnableBulkLoadingDisableIdleProcessing(bool fEnable)
		{
			if (fEnable)
				SuspendIdleProcessing();
			else
				ResumeIdleProcessing();
		}

		/// <summary>
		/// This is true while executing Undo or Redo (and associated changes, like the consequent
		/// PropChanged or Refresh). It is currently used by the Clerk to tell that it should not
		/// force a Save, even if the current record changes.
		/// </summary>
		public static bool InUndoRedo
		{
			get { return m_fInUndoRedo; }
		}

		protected override void XWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			SaveSettingsNow();
			// LT-6440
			// In the case of a shutdown while the parser is starting, we were getting
			// into a situation where the main window was disposed of while the parser
			// thread was trying to execute com calls on the UI thread and using the
			// main form as the Invoke point.
			m_mediator.SendMessageToAllNow("StopParser", null);
			base.XWindow_Closing(sender, e);
		}

		public override void SaveSettings()
		{
			GlobalSettingServices.SaveSettings(Cache.ServiceLocator, m_mediator.PropertyTable);
			// first save global settings, ignoring database specific ones.
			m_mediator.PropertyTable.SaveGlobalSettings();
			// now save database specific settings.
			m_mediator.PropertyTable.SaveLocalSettings();
		}

		/// <summary>Keyman select language message</summary>
		protected static uint s_wm_kmselectlang =
			Win32.RegisterWindowMessage("WM_KMSELECTLANG");

		/// <summary>
		/// Keyman's select language message must be forwarded to the focus window to take useful effect.
		/// </summary>
		protected override void WndProc(ref Message m)
		{
			if (IsDisposed)
				return;

			if (m.Msg == s_wm_kmselectlang)
			{
				IntPtr focusWnd = Win32.GetFocus();
				if (focusWnd != IntPtr.Zero)
				{
					Win32.SendMessage(focusWnd, m.Msg, m.WParam, m.LParam);
					focusWnd = IntPtr.Zero;
					return; // No need to pass it on to the superclass, since we dealt with it.
				}
			}
			base.WndProc (ref m);
			// In Mono, closing a dialog invokes WM_ACTIVATE on the active form, which then selects
			// its active control.  This swallows keyboard input.  To prevent this, we select the
			// desired control if one has been established so that keyboard input can still be seen
			// by that control.  (See FWNX-785.)
			if (MiscUtils.IsMono && m.Msg == (int)Win32.WinMsgs.WM_ACTIVATE && m.HWnd == this.Handle &&
				DesiredControl != null && !DesiredControl.IsDisposed && DesiredControl.Visible && DesiredControl.Enabled)
			{
				DesiredControl.Select();
			}
		}

		/// <summary>
		/// Gets or sets the control that needs keyboard input.  (See FWNX-785.)
		/// </summary>
		public Control DesiredControl { get; set; }

		#region ISettings implementation

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Registry key for settings for this window.
		/// </summary>
		/// <remarks>Part of the ISettings interface.</remarks>
		/// -----------------------------------------------------------------------------------
		virtual public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				return m_app.SettingsKey;
			}
		}

		/// <summary>
		/// Save the persisted settings now.
		/// </summary>
		public void SaveSettingsNow()
		{
			CheckDisposed();

			try
			{
				RegistryKey key = SettingsKey;
				key.SetValue("LatestConfigurationFile", m_configFile);
			}
			catch (Exception e)
			{
				// just ignore any exceptions. It really doesn't matter if SaveSettingsNow() fail.
				Console.WriteLine("FwXWindow.SaveSettingsNow: Exception caught: {0}", e.Message);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the FwMainWnd has been created as a copy of
		/// another FwMainWnd.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool WindowIsCopy
		{
			get
			{
				CheckDisposed();

				return m_fWindowIsCopy;
			}
		}

		#endregion // ISettings implementation

		#region IFwMainWnd implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnFinishedInit()
		{
			CheckDisposed();

			if (m_startupLink != null)
				m_mediator.SendMessage("FollowLink", m_startupLink);
			UpdateControls();
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the data objects cache.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();

				return (FdoCache)m_mediator.PropertyTable.GetValue("cache", null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the application to which this main winodw belongs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwApp App
		{
			get { return m_app; }
		}

		/// <summary></summary>
		public override void OnPropertyChanged(string name)
		{
			CheckDisposed();

			// When switching tools, the Cache should be saved.  Persisting the Undo/Redo buffer between tools can be confusing
			// Fixes (LT-4650)
			if (name == "currentContentControl")
			{
				Cache.DomainDataByFlid.GetActionHandler().Commit();
				// If we change tools, the FindReplaceDlg is no longer valid, as its rootsite
				// is part of the previous tool, and will thus be disposed.  See FWR-2080.
				if (this.OwnedForms.Length > 0 && this.OwnedForms[0] is FwFindReplaceDlg)
					this.OwnedForms[0].Close();
			}

			base.OnPropertyChanged(name);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Returns the NormalStateDesktopBounds property from the persistence object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Rectangle NormalStateDesktopBounds
		{
			get
			{
				CheckDisposed();

				object loc = m_mediator.PropertyTable.GetValue("windowLocation");
				Debug.Assert(loc != null);

				object size = m_mediator.PropertyTable.GetValue("windowSize", /*hack*/new Size(400,400));
				Debug.Assert(size != null);
				return new Rectangle((Point)loc, (Size)size);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add corresponding stuff to the sidebar, View menu,
		/// etc.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void InitAndShowClient()
		{
			CheckDisposed();

			Show();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Enable or disable this window.
		/// </summary>
		///
		/// <param name="fEnable">Enable (true) or disable (false).</param>
		/// -----------------------------------------------------------------------------------
		public void EnableWindow(bool fEnable)
		{
			CheckDisposed();

			Enabled = fEnable;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PreSynchronize(SyncMsg sync)
		{
			CheckDisposed();
			// TODO: Implement it. This is copied from TE.
		}

		/// <summary>
		/// If a property requests it, do a db sync.
		/// </summary>
		public virtual void OnIdle(object sender)
		{
			CheckDisposed();

			/* Bad things happen, when this is done and the parser is running.
			 * TODO: Figure out how they can co-exist.
			if (Mediator.PropertyTable.GetBoolProperty("SyncOnIdle", false) && FwApp.App != null
				&& FwApp.App.SyncGuid != Guid.Empty)
			{
				FwApp.App.SyncFromDb();
			}
			*/
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// <returns>True if the sync message was handled; false, indicating that the
		/// application should refresh all windows. </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool Synchronize(SyncMsg sync)
		{
			CheckDisposed();

			if (sync == SyncMsg.ksyncStyle)
			{
				// force our stylesheet to resync (LT-7382).
				ResyncStylesheet();
				ResyncRootboxStyles();
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get all the rootboxes we know about and reinitialize their stylesheets.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ResyncRootboxStyles()
		{
			FwStyleSheet fssPrev = null;	// this is used to minimize reloads via Init().
			foreach (IVwRootBox rootb in FindAllRootBoxes(this))
			{
				FwStyleSheet fss = rootb.Stylesheet as FwStyleSheet;
				if (fss != null && fss.Cache != null)
				{
					Debug.Assert(fss.Cache == Cache);
					if (fss != fssPrev)
					{
						Debug.Assert(fss.RootObjectHvo != 0);
						fss.Init(Cache, fss.RootObjectHvo, fss.StyleListTag);
					}
					rootb.OnStylesheetChange();
					fssPrev = fss;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// JohnT: this might be a poorly named or obsolete message. Kept because there are
		/// some callers and I don't have time to analyze them all. Generally better to use
		/// RefreshDisplay().
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshAllViews()
		{
			CheckDisposed();

			// We don't want to clear the cache... just update the view.
			m_mediator.SendMessage("Refresh", this);
			//OnMasterRefresh(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the currently active view (client window).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IRootSite ActiveView
		{
			get
			{
				CheckDisposed();
				return m_viewHelper.ActiveView;
			}
		}
		#endregion // IFwMainWnd implementation

		#region IRecordListOwner implementation
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the IRecordListUpdater object named by the argument.
		/// </summary>
		/// <remarks>Part of the IRecordListOwner interface.</remarks>
		/// -----------------------------------------------------------------------------------
		virtual public IRecordListUpdater FindRecordListUpdater(string name)
		{
			CheckDisposed();

			return Mediator.PropertyTable.GetValue(name, null) as IRecordListUpdater;
		}
		#endregion // IRecordListOwner implementation

		#region implementation of (some of) IMainWindowDelegateCallbacks
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style list helper for the paragraph styles combo box on the formatting
		/// toolbar.
		/// (FwXWindow doesn't (yet) have one. I haven't researched what it is good for.
		/// Possibly it relates mainly to the more complex set of styles (with structural
		/// meaning) maintained by TE.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StyleComboListHelper ParaStyleListHelper
		{
			get
			{
				CheckDisposed();
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style list helper for the character styles combo box on the formatting toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StyleComboListHelper CharStyleListHelper
		{
			get
			{
				CheckDisposed();
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the combo box used to select writing systems.
		/// Enhance JohnT: to really use the writing system common code we'll have to implement this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox WritingSystemSelector
		{
			get
			{
				CheckDisposed();
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the EditingHelper cast as an FwEditingHelper. (Is this ever non-null in Harvest??)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwEditingHelper FwEditingHelper
		{
			get
			{
				CheckDisposed();
				return (ActiveView == null) ? null : (ActiveView.EditingHelper as FwEditingHelper);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the EditingHelper cast as an FwEditingHelper. (Is this ever non-null in Harvest??)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();
				return (ActiveView == null) ? null : (ActiveView.EditingHelper);
			}
		}

		/// <summary>
		/// Gets the stylesheet being used by the active view, if possible, otherwise,
		/// the window's own stylesheet.
		/// </summary>
		public FwStyleSheet ActiveStyleSheet
		{
			get
			{
				var view = ActiveView as SimpleRootSite;
				if (view == null || (view.StyleSheet as FwStyleSheet) == null)
					return StyleSheet;
				return (FwStyleSheet)view.StyleSheet;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the style sheet. If the getter is called first, it gets the
		/// LexDb stylesheet. (not yet implemented)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual FwStyleSheet StyleSheet
		{
			get
			{
				CheckDisposed();

				FdoCache cache = Cache;
				if (m_StyleSheet == null && cache != null)
					ResyncStylesheet();
				return m_StyleSheet;
			}

			set
			{
				CheckDisposed();
				m_StyleSheet = value;
			}
		}

		/// <summary>
		/// We need to resync the windows StyleSheet object during a MasterRefresh and Synchronize()
		/// LT-7382.
		/// </summary>
		private void ResyncStylesheet()
		{
			if (m_StyleSheet == null)
				m_StyleSheet = new FwStyleSheet();
			m_StyleSheet.Init(Cache, Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
			if (m_rebarAdapter is IUIAdapterForceRegenerate)
				((IUIAdapterForceRegenerate)m_rebarAdapter).ForceFullRegenerate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the style type combo box
		/// in the styles dialog where the user can select the type of styles to show
		/// (all, basic, or custom styles).  False indicates a FLEx style type combo box
		/// (all, basic, dictionary, or custom styles).
		/// </summary>
		/// <value>The default implementation always returns <c>false</c></value>
		/// ------------------------------------------------------------------------------------
		public virtual bool ShowTEStylesComboInStylesDialog
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the user can select a background color on the
		/// paragraph tab in the styles dialog. This is possible in all apps except TE.
		/// </summary>
		/// <value>The default implementation always return <c>true</c>.</value>
		/// ------------------------------------------------------------------------------------
		public virtual bool CanSelectParagraphBackgroundColor
		{
			get { return true; }
		}
		#endregion implementation of (some of) IMainWindowDelegateCallbacks

		/// <summary>
		/// Get a special-case find dialog help ID. We attempt to retrieve it from our active view or its parent.
		/// (The only current actual implementation is XmlDocView.)
		/// </summary>
		public string FindTabHelpId
		{
			get
			{
				if (ActiveView is IFindAndReplaceContext)
					return ((IFindAndReplaceContext)ActiveView).FindTabHelpId;
				if (ActiveView is Control && ((Control)ActiveView).Parent is IFindAndReplaceContext)
					return ((IFindAndReplaceContext)((Control)ActiveView).Parent).FindTabHelpId;
				return null;
			}
		}

		/// <summary>
		/// Mediator message handling Priority.
		/// To fix LT-13375, this needs to have a slightly higher priority than normal.
		/// </summary>
		public override int Priority
		{
			get { return ((int)ColleaguePriority.Medium) - 1; }
		}
	}

	/// <summary>
	/// This interface marks a cache that can do something meaningful in the way of Refresh
	/// (that is, it doesn't just contain raw data).
	/// </summary>
	internal interface IRefreshCache : IRefreshable
	{
		new void Refresh();
	}
}

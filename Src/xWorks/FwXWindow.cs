// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwXWindow.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
//	This just wraps the FieldWorks-agnostic XWindow in a form that FwApp can swallow.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;	// for [Browsable] attribute
using Microsoft.Win32;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FwCoreDlgControls;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.XWorks
{

	/// <summary>
	/// Summary description for FwXWindow.
	/// </summary>
	public class FwXWindow : XWindow, IFwMainWnd, ISettings, IRecordListOwner,
		IMainWindowDelegatedFunctions, IMainWindowDelegateCallbacks
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
		protected string m_startupUrl = null;

		/// <summary>
		/// list of the virtual handlers we loaded.
		/// </summary>
		protected List<IVwVirtualHandler> m_installedVirtualHandlers;

		static bool m_fInUndoRedo; // true while executing an Undo/Redo command.

		/// <summary>
		/// The stylesheet used for all views in this window.
		/// </summary>
		protected FwStyleSheet m_StyleSheet;

		public delegate void LinkDelegate(Object link);
		public LinkDelegate IncomingLinkHandler;
		#endregion

		public void OnIncomingLink(Object link)
		{
			CheckDisposed();

			((FwXApp)FwApp.App).HandleIncomingLink(link as FwLink);
		}

		#region Overridden properties
		/// <summary>
		/// Gets an offset from the system's local application data folder.
		/// Subclasses should override this property
		/// if they want a folder within the base folder.
		/// </summary>
		protected override string LocalApplicationDataOffset
		{
			get { return @"SIL\FieldWorks"; }
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
				return (int)LexDb.LexDbTags.kflidStyles;
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
				return Cache.LangProject.LexDbOA.Hvo;
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
			Cache.ClearAllData();
			FinishRefresh();
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
			BasicInit();
		}

		private void BasicInit()
		{
			m_delegate = new MainWindowDelegate(this);
			IncomingLinkHandler = new LinkDelegate(OnIncomingLink);
		}

		/// <summary>
		/// Construct a new form
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wndCopyFrom">Source window to copy from</param>
		/// <param name="iconStream"></param>
		/// <param name="configFile"></param>
		/// <param name="app"></param>
		/// <param name="inAutomatedTest"></param>
		public FwXWindow(FdoCache cache, Form wndCopyFrom, Stream iconStream,
			string configFile, bool inAutomatedTest)
		{
			BasicInit();

			int dbversion, cobjects, mdfSize, ldfSize;
			DbOps.ReadOneIntFromCommand(cache, "select DbVer from version$", null, out dbversion);
			DbOps.ReadOneIntFromCommand(cache, "select count(id) from CmObject", null, out cobjects);
			DbOps.ReadOneIntFromCommand(cache, "select size from dbo.sysfiles where fileid = 1", null, out mdfSize);
			DbOps.ReadOneIntFromCommand(cache, "select size from dbo.sysfiles where fileid = 2", null, out ldfSize);

			// REVIEW (EberhardB): This doesn't work if you have multiple databases open because
			// the properties are stored in static variables. Opening a second database overwrites
			// the values from the first database!
			// DB: TestLangProj, Ver: 200129, CmObjects: 55105, Mdf size: 5256, Ldf size: 1280
			SIL.Utils.ErrorReporter.AddProperty("DB", cache.DatabaseName + ", Ver: " + dbversion + ", CmObjects: "
				+ cobjects + ", Mdf size: " + (mdfSize * 8) + "K, Ldf size: " + (ldfSize * 8) + "K");
			SIL.Utils.ErrorReporter.AddProperty("LangProject", cache.LangProject.Name.AnalysisDefaultWritingSystem);

			Debug.Assert(File.Exists(configFile));
			m_configFile = configFile;

			Init(iconStream, wndCopyFrom, cache);
			if(inAutomatedTest)
			{
				//this should cause (well behaved) code to avoid bringing up dialogs
				// that we cannot respond to (like confirmation and error dialogs)
				//and it should cause the system to avoid saving the settings to a file (or at least
				//saving them in some special way so as not to mess up the user.)
				Mediator.PropertyTable.SetProperty("DoingAutomatedTest", true);
				Mediator.PropertyTable.SetPropertyPersistence("DoingAutomatedTest", false);
			}

			LoadUI(configFile);
			// Reload additional property settings that depend on knowing the database name.
			m_viewHelper = new ActiveViewHelper(this);
		}

		/// <summary>
		/// Virtuals are the virtual handlers (IVwVirtualHandler implementors) in an XWorks app.
		/// </summary>
		/// <param name="virtualsNode">An XmlNode whose name is 'virtuals', and which is to have 'virtual' children.</param>
		protected override void LoadVirtuals(XmlNode virtualsNode)
		{
			if (virtualsNode == null)
				return;
			m_installedVirtualHandlers = PropertyTableVirtualHandler.InstallVirtuals(virtualsNode, Cache, Mediator);
		}

		/// <summary>
		/// FwXWindow also restores database-specific properties.
		/// </summary>
		protected override void RestoreProperties()
		{
			base.RestoreProperties();
			m_mediator.PropertyTable.RestoreFromFile(m_mediator.PropertyTable.LocalSettingsId);
			if (m_installedVirtualHandlers != null)
			{
				foreach (IVwVirtualHandler vh in m_installedVirtualHandlers)
				{
					if (vh is FDOSequencePropertyTableVirtualHandler)
					{
						(vh as FDOSequencePropertyTableVirtualHandler).NeedToReloadSettings = true;
						(vh as FDOSequencePropertyTableVirtualHandler).LoadSettings(m_mediator.PropertyTable);
					}
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Construct a new form
		/// </summary>
		/// <param name="cache">FDO cache.</param>
		/// <param name="wndCopyFrom">Source window to copy from.</param>
		/// <param name="iconStream"></param>
		/// <param name="configStream"></param>
		/// <param name="app"></param>
		/// -----------------------------------------------------------------------------------
		public FwXWindow(FdoCache cache, Form wndCopyFrom, Stream iconStream,
			Stream configStream) : this()
		{
			m_configFile = null;
			Init(iconStream, wndCopyFrom, cache);
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

			FdoCache wndCache = null;
			if (disposing)
			{
				if (m_delegate != null)
					m_delegate.Dispose();
				if (m_viewHelper != null)
					m_viewHelper.Dispose();
				if (FwApp.App != null)
				{
					FwApp.App.OkToCloseApp = false;
					FwApp.App.RemoveWindow(this, out wndCache);
				}
			}

			// NOTE: base.Dispose() may need the FdoCache which RemoveWindow() wants to delete.
			base.Dispose(disposing);

			if (disposing)
			{
				if (FwApp.App != null)
				{
					FwApp.App.OkToCloseApp = true;
					if (wndCache != null)
						FwApp.App.RemoveFdoCache(wndCache);
				}
			}
			m_viewHelper = null;
			m_delegate = null;
			IncomingLinkHandler = null;
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
		}


		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Logger.WriteEvent(WindowHandleInfo("Closed window"));
		}

		private string WindowHandleInfo(string eventMsg)
		{
			return String.Format(eventMsg + " [{0}] handle: [0x{1}]",
				Cache.DatabaseName, this.Handle.ToString("x"));
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
			Mediator.PropertyTable.LocalSettingsId = cache.DatabaseName;
			Mediator.PropertyTable.SetProperty("cache", cache);
			Mediator.PropertyTable.SetPropertyPersistence("cache", false);
			Mediator.PropertyTable.SetProperty("DocumentName", GetMainWindowCaption(cache));
			Mediator.PropertyTable.SetPropertyPersistence("DocumentName", false);
			Mediator.PathVariables["{DISTFILES}"] = DirectoryFinder.FWCodeDirectory;
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

			PredictiveProgressState s = new PredictiveProgressState(panel, taskLabel);
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
				return m_viewHelper.ActiveView.EditingHelper.CutSelection();
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
				m_viewHelper.ActiveView.EditingHelper.PasteClipboard(false);
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

			if (m_viewHelper.ActiveView != null)
				return m_viewHelper.ActiveView.EditingHelper.PasteUrl(StyleSheet);
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

			display.Enabled = m_viewHelper.ActiveView != null &&
				m_viewHelper.ActiveView.EditingHelper.CanPasteUrl();
			return true;
		}

		public bool OnInsertExternalLink(object arg)
		{
			CheckDisposed();

			if (m_viewHelper.ActiveView != null)
			{
				// JohnT: don't use m_StyleSheet, no guarantee it has been created (see LT-7034)
				return m_viewHelper.ActiveView.EditingHelper.InsertExternalLink(/*this, */StyleSheet);
			}
			return false;
		}

		/// <summary>
		/// Enable the InsertExternalLink command
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertExternalLink(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			bool fAllow = Mediator.PropertyTable.GetBoolProperty("AllowInsertExternalLink", true);

			if (fAllow)
			{
				display.Enabled = m_viewHelper.ActiveView != null &&
					m_viewHelper.ActiveView.EditingHelper.CanInsertExternalLink();
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

			display.Enabled = m_viewHelper.ActiveView != null &&
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

			using (SIL.FieldWorks.Common.Controls.UtilityDlg dlg = new UtilityDlg(FwApp.App))
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
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnDisplayShowCharMap(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

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
		/// <param name="command"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnShowCharMap(object command)
		{
			CheckDisposed();

			Process prc;
			prc = new Process();
			prc.StartInfo.FileName = "charmap.exe ";
			prc.Start();
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
			this.SaveSettings();
			FwApp.App.NewMainWindow(this, false);
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
			SIL.FieldWorks.Common.Utils.ScriptMaker sm =
				new SIL.FieldWorks.Common.Utils.ScriptMaker(ActiveForm);
			LinkListener ll = (LinkListener)m_mediator.PropertyTable.GetValue("LinkListener",
				null);
			if (ll == null)
				return true;
			sm.GoTo(ll.CurrentContext.ToString());
			return true;
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

			Cache.Save();
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
			bool balloonActive=m_mediator.PropertyTable.GetBoolProperty("ShowBalloonHelp", false);
			m_mediator.PropertyTable.SetProperty("ShowBalloonHelp", false);
			if (FwApp.App is FwXApp)
				((FwXApp)FwApp.App).ChooseLangProject(this);
			else
			{
				FwApp.App.OpenProjectDialog(this, Cache.ServerName,
					Cache.LanguageWritingSystemFactoryAccessor);
			}
			m_mediator.PropertyTable.SetProperty("ShowBalloonHelp", balloonActive);
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
			//			FwHelpAbout fHelpAboutWnd = new FwHelpAbout(FwApp.App.DropDeadDate);
			using (FwHelpAbout fHelpAboutWnd = new FwHelpAbout())
			{
				fHelpAboutWnd.ShowDialog();
			}
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
			FdoCache cache = Cache;
			bool fDbRenamed = false;
			string sServer = cache.ServerName;
			string sDatabase = cache.DatabaseName;
			string sProject = cache.LangProject.Name.UserDefaultWritingSystem;
			if (sProject != sDatabase)
			{
				cache.LangProject.Name.UserDefaultWritingSystem = sDatabase;
				sProject = sDatabase;
			}
			string sExtLinkRootDir = cache.LangProject.ExternalLinkRootDir;
			using (FwProjPropertiesDlg dlg = new FwProjPropertiesDlg(cache, FwApp.App, FwApp.App,
				FwApp.App, Logger.Stream, cache.LangProject.Hvo, HvoAppRootObject,
				cache.DefaultUserWs, FontHeightAdjuster.StyleSheetFromMediator(Mediator)))
			{
				if (startOnWSPage)
					dlg.StartWithWSPage();
				if (dlg.ShowDialog(this) != DialogResult.Abort)
				{
					fDbRenamed = dlg.ProjectNameChanged();
					if (fDbRenamed)
					{
						sProject = cache.LangProject.Name.UserDefaultWritingSystem;
					}
					bool fFilesMoved = false;
					if (dlg.ExternalLinkChanged())
					{
						fFilesMoved = FwApp.UpdateExternalLinks(sExtLinkRootDir, cache.LangProject);
					}
					// no need for any of these refreshes if entire window has been/will be
					// destroyed and recreated.
					if (!fDbRenamed && !fFilesMoved)
					{
						bool fDidRefresh = false;
						if (dlg.NewRenderingNeeded() && FwApp.App is FwXApp)
						{
							Mediator.BroadcastMessage("MasterRefresh", null); // Part of fixing LT-2339: redisplay with changes.
							fDidRefresh = true;
						}
						if (dlg.WritingSystemsChanged())
						{
							// Since we are going to do a full refresh, we don't want to sync the writing systems here as that
							// adds several more seconds updating everything prior to doing the full refresh. If we really need
							// to notify other apps of this change, we need some better approach, or sync a full refresh (but I
							// (KZ) tried this and it runs into the same huge delay as calling OnMasterRefresh directly. Also, it
							// doesn't appear that a call to Synchronize gets a sync message to the database anyway. It just
							// does the work immediately on the local copy).
							//FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncWs, 0, 0), cache);
							// JohnT: So many things depend on the current list of writing systems...for example,
							// everywhere we display all writing systems, or best writing system, or default writing system...
							if (!fDidRefresh && FwApp.App is FwXApp)
							{
								Mediator.BroadcastMessage("MasterRefresh", null);
								// Note: calling OnMasterRefresh directly takes many minutes reloading and painting
								// everything in the process of removing the browse control in WarmBootPart1.
								//(FwApp.App as FwXApp).OnMasterRefresh(null);
								fDidRefresh = true;
							}
						}
						if (!fDidRefresh && dlg.SortChanged() && FwApp.App is FwXApp)
							Mediator.BroadcastMessage("MasterRefresh", null);
						Mediator.PropertyTable.SetProperty("DocumentName", cache.ProjectName());
						Mediator.PropertyTable.SetPropertyPersistence("DocumentName", false);
						SetWindowLabel();
					}
				}
			}
			if (fDbRenamed)
			{

				string oldFilenamePrefix = String.Format("db${0}$", sDatabase);
				string newFilenamePrefix = String.Format("db${0}$", sProject);

				//Make a copy all the settings files to match the new project name.
				DirectoryInfo di = new DirectoryInfo(Mediator.PropertyTable.UserSettingDirectory);
				string findOldFilenames = String.Format("db${0}$*", sDatabase);
				System.IO.FileInfo[] fi = di.GetFiles(findOldFilenames);
				foreach (System.IO.FileInfo f in fi)
				{
					string newname = CreateNewFileName(f.FullName, oldFilenamePrefix, newFilenamePrefix);
					File.Copy(f.FullName, newname, true);
				}

				// The file db$ProjectName$Settings.xml cannot just be copied because the PropertyTable
				// settings have to be renamed to reflect the new project name.
				// Therefore before renaming the database we need to save the project specific PropertyTable settings
				// to a new file using the new project name in the filename and property names.
				Mediator.PropertyTable.SaveLocalSettingsForNewProjectName(sDatabase, sProject);

				bool fRenameSucceeded = false;
				fRenameSucceeded = FwApp.App.RenameProject(sServer, sDatabase, sProject);
				if (fRenameSucceeded)
				{
					//Now delete all old settings files.
					foreach (System.IO.FileInfo f in fi)
					{
						if (File.Exists(f.FullName))
							File.Delete(f.FullName);
					}
				}
				else
				{
					//delete the new settings files since the database rename failed
					foreach (System.IO.FileInfo f in fi)
					{
						string newname = CreateNewFileName(f.FullName, oldFilenamePrefix, newFilenamePrefix);
						if (File.Exists(newname))
							File.Delete(newname);
					}
				}
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
		protected bool OnBackupRestore(object arg)
		{
			// This method used to create a dummy window to be the parent of the Backup/Restore
			// dialog. Then there was LT-8752. However, the dialog ought to be modal, so now
			// we pass the true parent window's handle to the dialog.
			// [The dummy window was set to be topmost so that the new scheduler dialog could
			// not get above it.]
			DIFwBackupDb backupSystem = FwBackupClass.Create();
			backupSystem.Init(FwApp.App, this.Handle.ToInt32());
			backupSystem.UserConfigure(FwApp.App, false);
			backupSystem.Close();

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/"Project Management"/Delete... menu command
		/// </summary>
		/// <param name="arg"></param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDeleteProject(object args)
		{
			FwDeleteProjectDlg deleteDialog = new FwDeleteProjectDlg();
			deleteDialog.SetDialogProperties(FwApp.App);

			deleteDialog.ShowDialog(this);
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
			Control focusControl = null;
			object control = m_mediator.PropertyTable.GetValue("FirstControlToHandleMessages");
			if (control != null)
				focusControl = control as Control;
			if (focusControl == null || focusControl.IsDisposed)
				focusControl = XWindow.FocusedControl();
			if (focusControl != null && focusControl != this)
			{
				MethodInfo mi = focusControl.GetType().GetMethod(methodName,
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (mi != null)
					return (bool)mi.Invoke(focusControl, args);
			}
			return false;
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
			if (FocusControlHandlesMessage("OnUndo", new object[] { args }))
				return true;

			if (Cache.CanUndo)
			{
				// start hour glass
				using (new WaitCursor(this))
				{
					// Are changes private to a TssEdit field editor or similar?
					bool fPrivate = Cache.ActionHandlerAccessor.get_TasksSinceMark(true);

					try
					{
						m_fInUndoRedo = true;
						UndoResult ures;
						Cache.Undo(out ures);
						HandleUndoResult(ures, fPrivate);
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

		private void HandleUndoResult(UndoResult ures, bool fPrivate)
		{
			// Enhance JohnT: may want to display messages for kuresFailed, kuresError
			if (ures != UndoResult.kuresSuccess)
			{

				if (!fPrivate && FwApp.App != null)
				{
					// currently implemented, this will cause this app to do a master refresh,
					FwApp.App.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);
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
			bool canUndo = Cache.CanUndo;
			display.Enabled = canUndo;
			string sUndo = canUndo ? Cache.UndoText : xWorksStrings.Undo;
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
			if (FocusControlHandlesMessage("OnRedo", new object[] { args }))
				return true;

			if (Cache.CanRedo)
			{
				// start hour glass
				using (new WaitCursor(this))
				{
					// Are changes private to a TssEdit field editor or similar?
					bool fPrivate = Cache.ActionHandlerAccessor.get_TasksSinceMark(false);
					try
					{
						m_fInUndoRedo = true;
						UndoResult ures;
						Cache.Redo(out ures);
						HandleUndoResult(ures, fPrivate);
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

			// Collect other Mediator values that are needed later.
			string areaName = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
			string startingAreaName = m_mediator.PropertyTable.GetStringProperty("InitialArea", null);
			string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", "");
			string toolName2 = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_" + areaName, toolName);
			XmlNode currentContentControlParameters = (XmlNode)m_mediator.PropertyTable.GetValue("currentContentControlParameters");

			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			// force our stylesheet to resync (LT-7382).
			ResyncStylesheet();
			WarmBootPart1();
			m_mainContentControl = null;
			// Add stuff to Mediator and/or its PropertyTable.
			InitMediatorValues(cache);

			// Add non-common properties.
			m_mediator.PropertyTable.SetProperty("ToolForAreaNamed_" + areaName, toolName2, false);

			WarmBootPart2();

			// Spin through child controls and bring all to front, except main control.
			Control[] controls = new Control[Controls.Count];
			for (int i = 0; i < Controls.Count; i++)
				controls[i] = Controls[i];
			Controls.Clear();

			Controls.AddRange(controls);

			m_mediator.MainWindow = this;
			// JohnT: NO! NO! NO! This means our mediator never get its broadcasted messages unless we happen to be
			// the active window at the time the message is broadcast!
			//m_mediator.SpecificToOneMainWindow = false;
			// JohnT: Since the new approach to Refresh has made a new window, and it is SpecificToOneMainWindow, we MUST
			// call this to start getting mediator messages. Note sure this is the optimal time, but it replaces the
			// disastrous call making SpecificToOneMainWindow false.
			m_mediator.BroadcastPendingItems();

			// Reset current control properties.
			m_mediator.PropertyTable.SetProperty("areaChoice", areaName, false);
			m_mediator.PropertyTable.SetProperty("InitialArea", startingAreaName, false);
			m_mediator.PropertyTable.SetProperty("currentContentControlParameters", currentContentControlParameters, false);
			m_mediator.PropertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_mediator.PropertyTable.SetProperty("currentContentControl", toolName);
			m_mediator.PropertyTable.SetPropertyPersistence("currentContentControl", false);

			ResumeLayout(true);

			return true;
		}

		/// <summary>
		/// Call RefreshDisplay for every child control (recursively) which implements it.
		/// </summary>
		public void RefreshDisplay()
		{
			foreach (Control c in Controls)
				ReconstructViews(c);
		}

		/// <summary>
		/// Walk your stack of subcontrols and send RefreshDisplay to each control that
		/// has a public (no argument) method of that name.
		/// </summary>
		/// <param name="control"></param>
		private void ReconstructViews(Control control)
		{
			Type type = control.GetType();
			MethodInfo mi=type.GetMethod("RefreshDisplay", BindingFlags.Public|BindingFlags.Instance);
			if (mi != null)
				mi.Invoke(control, null);

			foreach (Control c in control.Controls)
				ReconstructViews(c);
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
			bool canRedo = Cache.CanRedo;
			display.Enabled = canRedo;
			string sRedo = canRedo ? Cache.RedoText : xWorksStrings.Redo;
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
				CharStyleListHelper != null ? CharStyleListHelper.SelectedStyle.Name : null);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required interface member to launch the style dialog.
		/// </summary>
		/// <param name="paraStyleName">Name of the initially selected paragraph style.</param>
		/// <param name="charStyleName">Name of the initially selected character style.</param>
		/// <returns>true if refresh should be called to reload the cache</returns>
		/// ------------------------------------------------------------------------------------
		public bool ShowStylesDialog(string paraStyleName, string charStyleName)
		{
			CheckDisposed();

			if (m_delegate.ShowStylesDialog(paraStyleName, charStyleName))
			{
				// Need to refresh to reload the cache.  See LT-6265.
				(FwApp.App as FwXApp).OnMasterRefresh(null);
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
			if(FwApp.App is IxCoreColleague)
				return new IxCoreColleague[]{this, FwApp.App as IxCoreColleague};
			else
				return new IxCoreColleague[]{this};
		}
		#endregion // XCore Message Handlers

		/// <summary>
		/// Either enable bulk loading and disable idle processing, or disable bulk loading and
		/// enable idle processing.
		/// </summary>
		/// <param name="fEnable"></param>
		public void EnableBulkLoadingDisableIdleProcessing(bool fEnable)
		{
			Cache.EnableBulkLoadingIfPossible(fEnable);
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
			// find virtual properties that depend upon property table, to save their settings.
			if (m_installedVirtualHandlers != null)
			{
				foreach (IVwVirtualHandler vh in m_installedVirtualHandlers)
				{
					if (vh is FDOSequencePropertyTableVirtualHandler)
						(vh as FDOSequencePropertyTableVirtualHandler).StoreSettings(m_mediator.PropertyTable);
				}
			}
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
		/// <param name="m"></param>
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
		}

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
				return FwApp.App.SettingsKey;
			}
		}

		//set by the application when it reads a command line parameter which is a URL
		public void StartupAtURL(string URL)
		{
			CheckDisposed();

			m_startupUrl = URL;
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

				// Save Database Settings
				FdoCache cache = Cache;
				if(cache != null)
				{
					key.SetValue("LatestDatabaseName", cache.DatabaseName);
					key.SetValue("LatestDatabaseServer", cache.ServerName);
				}
				key.SetValue("LatestConfigurationFile", m_configFile);
			}
			catch (Exception e)
			{
				// just ignore any exceptions. It really doesn't matter if SaveSettingsNow() fail.
				Console.WriteLine("FwXWindow.SaveSettingsNow: Exception caught: {0}", e.Message);
			}
		}

		///***********************************************************************************
		/// <summary>
		/// Gets a window creation option.
		/// </summary>
		/// <value>Returns true if this window is to be a copy of another OysterMainWnd, in
		/// which case we calculate a new window size and position, and ignore the
		/// size and position values persisted in the registry.</value>
		///***********************************************************************************
		[Browsable(false)]
		public bool KeepWindowSizePos
		{
			get
			{
				CheckDisposed();

				return WindowIsCopy;
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

			if (m_startupUrl != null)
				m_mediator.SendMessage("FollowLink", new FwLink(m_startupUrl));
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string ApplicationName
		{
			get
			{
				CheckDisposed();
				return string.Empty;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the data objects cache.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual FdoCache Cache
		{
			get
			{
				CheckDisposed();

				return (FdoCache)m_mediator.PropertyTable.GetValue("cache", null);
			}
			set
			{
				CheckDisposed();

				// Shouldn't reset the cache.
				Debug.Assert(false);
			}
		}

		public override void OnPropertyChanged(string name)
		{
			CheckDisposed();

			// When switching tools, the Cache should be saved.  Persisting the Undo/Redo buffer between tools can be confusing
			// Fixes (LT-4650)
			if (name == "currentContentControl")
				Cache.Save();

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

				object size = m_mediator.PropertyTable.GetValue("windowSize", /*hack*/new System.Drawing.Size(400,400));
				Debug.Assert(size != null);
				return new Rectangle((Point)loc, (System.Drawing.Size)size);
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

			this.Enabled = fEnable;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save all data in this window, ending the current transaction.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveData()
		{
			CheckDisposed();

			Cache.Save();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization information record</param>
		/// ------------------------------------------------------------------------------------
		public virtual bool PreSynchronize(SyncInfo sync)
		{
			CheckDisposed();

			// TODO: Implement it. This is copied from TE.
			return true;
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
		/// <param name="sync">syncronization information record</param>
		/// <returns>True if the sync message was handled; false, indicating that the
		/// application should refresh all windows. </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool Synchronize(SyncInfo sync)
		{
			CheckDisposed();

			if (sync.msg == SyncMsg.ksyncStyle)
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
						fss.Init(Cache, Cache.LangProject.LexDbOA.Hvo,
							(int)FDO.Ling.LexDb.LexDbTags.kflidStyles);
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
				{
					ResyncStylesheet();
				}
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
			m_StyleSheet.Init(Cache, Cache.LangProject.LexDbOA.Hvo,
				(int)LexDb.LexDbTags.kflidStyles, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the style type combo box
		/// in the styles dialog where the user can select the type of styles to show
		/// (all, basic, or custom styles). This combo box is shown in TE but not in the other
		/// apps.
		/// </summary>
		/// <value>The default implementation always returns <c>false</c></value>
		/// ------------------------------------------------------------------------------------
		public virtual bool ShowSelectStylesComboInStylesDialog
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

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwXWindow));
			this.SuspendLayout();
			//
			// FwXWindow
			//
			resources.ApplyResources(this, "$this");
			this.Name = "FwXWindow";
			this.ResumeLayout(false);

		}
	}
}

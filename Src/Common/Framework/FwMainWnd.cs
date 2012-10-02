//---------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002-2010, SIL International. All Rights Reserved.
// <copyright from='2002' to='2010' company='SIL International'>
//    Copyright (c) 2010, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: FwMainWnd.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Resources;
using XCore;

namespace SIL.FieldWorks.Common.Framework
{
	/// -------------------------------------------------------------------------------------------
	/// <summary>Base class for all top level application windows for FieldWorks apps.</summary>
	/// <remarks>
	/// FwMainWnd is the base class for all top-level application windows for FieldWorks apps.
	/// Technically, it should be abstract, but that makes it so it won't work in Designer.
	/// </remarks>
	/// -------------------------------------------------------------------------------------------
	public abstract class FwMainWnd : Form, IFWDisposable, ISettings, IFwMainWnd, IxCoreColleague,
		IFwMainWndSettings, IMainWindowDelegateCallbacks
	{
		#region Events and Delegates
		/// <summary></summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public delegate void ZoomPercentageChangedHandler(object sender, ZoomPercentageChangedEventArgs args);
		/// <summary></summary>
		public event ZoomPercentageChangedHandler ZoomPercentageChanged;
		#endregion

		#region Data members
		/// <summary></summary>
		protected bool m_fFullWindow;
		/// <summary></summary>
		protected ActiveViewHelper m_viewHelper;
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary>All the client windows available in this main window.</summary>
		protected Dictionary<string, IRootSite> m_rgClientViews; // Control objects get added, but they must implement two interfaces.
		/// <summary>The selected client window in this main window.</summary>
		protected ISelectableView m_selectedView;
		/// <summary></summary>
		protected FwStyleSheet m_StyleSheet;
		/// <summary>The drop down list box for undo/redo</summary>
		protected UndoRedoDropDown m_UndoRedoDropDown;
		/// <summary></summary>
		protected ComboBox m_writingSystemSelector;
		/// <summary>
		/// Flag indicating whether or not this instance of FwMainWnd is a copy of
		/// another FwMainWnd (i.e. created by choosing the "Window/New Window" menu).
		/// </summary>
		protected bool m_fWindowIsCopy;
		/// <summary>The current app</summary>
		protected FwApp m_app;
		/// <summary>
		/// Shared functionality of FwXWindow and FwMainWnd may be delegated here.
		/// </summary>
		protected MainWindowDelegate m_delegate;
		private IContainer components;
		/// <summary></summary>
		protected InformationBarButton informationBarOverlaysButton;
		/// <summary></summary>
		protected Persistence m_persistence;

		// Status Bar

//		/// <summary></summary>
//		private ResourceManager m_Resources;

		/// <summary></summary>
		protected ITMAdapter m_tmAdapter;

		/// <summary></summary>
		protected ISIBInterface m_sibAdapter;

		/// <summary></summary>
		protected ComboBox m_cboZoomPercent = null;

		/// <summary></summary>
		protected ComboBox m_paraStylesComboBox = null;

		/// <summary></summary>
		protected ComboBox m_charStylesComboBox = null;

		private StyleComboListHelper m_paraStyleListHelper = null;
		private StyleComboListHelper m_charStyleListHelper = null;
		/// <summary></summary>
		protected Panel m_sideBarContainer;
		/// <summary></summary>
		protected Panel m_infoBarContainer;

		private StatusStrip statusStrip;
		private ToolStripStatusLabel statusBarFwPanel1;
		private ToolStripProgressBar ProgressPanel;
		private ToolStripStatusLabel statusBarFilterPanel;
		private StatusBarProgressHandler m_progressHandler;
		/// <summary></summary>
		protected FwSplitContainer splitContainer;
		/// <summary></summary>
		protected bool m_beingDisposed = false;
		private Mediator m_mediator;

		/// <summary>The default width of the side bar</summary>
		protected const int kDefaultSideBarWidth = 90;
		#endregion	// FwMainWnd data members

		#region Constructors, Initializers, Cleanup
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor for FwMainWnd.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected FwMainWnd()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_mediator = new Mediator();
			m_delegate = new MainWindowDelegate(this);
//			m_Resources = new System.Resources.ResourceManager(typeof(FwMainWnd));

			m_rgClientViews = new Dictionary<string, IRootSite>();
			m_viewHelper = new ActiveViewHelper(this);

			m_progressHandler = new StatusBarProgressHandler(statusBarFwPanel1, ProgressPanel);

			// Set the width of the side bar
			splitContainer.MaxFirstPanePercentage = 0.5f;
			splitContainer.SplitterDistance = kDefaultSideBarWidth;
			splitContainer.SettingsKey = SettingsKey;

			Debug.Assert(components is FwContainer, "Member variable components should be of " +
				"type FwContainer. If the Designer changed it, please change it back!");
			if (!(components is FwContainer))
				components = new FwContainer(components);

			// Add this as a component so that children can retrieve services
			components.Add(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwMainWnd"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		protected FwMainWnd(FdoCache cache) : this()
		{
			m_cache = cache;
			m_cache.ProjectNameChanged += ProjectNameChanged;
			m_mediator.PropertyTable.LocalSettingsId = "local";
			m_mediator.PropertyTable.SetProperty("cache", cache);
			m_mediator.PropertyTable.SetPropertyPersistence("cache", false);
			AccessibleName = "FwMainWnd on " + cache.ProjectId.Name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwMainWnd"/> class.
		/// </summary>
		/// <param name="app">The app.</param>
		/// <param name="wndCopyFrom">The Window to copy from.</param>
		/// ------------------------------------------------------------------------------------
		public FwMainWnd(FwApp app, Form wndCopyFrom) : this(app.Cache)
		{
			m_app = app;
			m_delegate.App = app;
			m_fWindowIsCopy = (wndCopyFrom != null);

			CreateMenusAndToolBars();
			CreateSideBarInfoBarAdapter();

			m_mediator.HelpTopicProvider = app;
			m_mediator.FeedbackInfoProvider = app;
			m_mediator.PropertyTable.SetProperty("App", app);
			m_mediator.PropertyTable.SetPropertyPersistence("App", false);
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");

			// Must not be run more than once.
			if (IsDisposed || m_beingDisposed || Disposing)
				return;
			m_beingDisposed = true;
			if (disposing)
			{
				foreach (Control ctrl in m_rgClientViews.Values)
				{
					// Dispose of any views that aren't in the form currently.
					if (ctrl.Parent == null)
						ctrl.Dispose();
				}

				if (m_mediator != null)
				{
					m_mediator.ProcessMessages = false;
					m_mediator.RemoveColleague(this);
				}

				if (m_progressHandler != null)
					m_progressHandler.Dispose();
				if (m_writingSystemSelector != null)
					m_writingSystemSelector.Dispose();
				if (m_UndoRedoDropDown != null)
					m_UndoRedoDropDown.Dispose();
				if (components != null)
					components.Dispose();
				// no need to explicitly call Dispose on m_persistence - it's part of
				// components collection and gets disposed there.
				if (m_rgClientViews != null)
					m_rgClientViews.Clear();
				if (m_tmAdapter != null)
				{
					m_tmAdapter.LoadControlContainerItem -= LoadCustomToolBarControls;
					m_tmAdapter.InitializeComboItem -= InitializeToolBarCombos;
					m_tmAdapter.Dispose();
				}
				if (m_cache != null)
					m_cache.ProjectNameChanged -= ProjectNameChanged;
				if (m_app != null)
				{
					// The removing of the window from the app's collection needs to happen later, after
					// this main window is already disposed of. This is needed for side-effects
					// that require a running message loop (such as closing the TE notes view
					// which would normally happen at this call without a running message loop)
					m_app.FwManager.ExecuteAsync(m_app.RemoveWindow, this);
				}
			}
			m_delegate = null;
			m_tmAdapter = null;
			m_UndoRedoDropDown = null;
			m_writingSystemSelector = null;
			m_StyleSheet = null;
			m_selectedView = null;
			m_rgClientViews = null;

#if !__MonoCS__
			base.Dispose(disposing);
#else
			try
			{
				base.Dispose(disposing);
			}
			catch(System.ArgumentOutOfRangeException)
			{
				// TODO-Linux: examine ToolStrip disposal in UIAdapter
				// is ToolStrip (from UIAdapter?) being Disposed multiple times?
			}
#endif

			if (disposing)
			{
				if (m_viewHelper != null)
					m_viewHelper.Dispose();
				if (m_mediator != null)
					m_mediator.Dispose();
			}

			m_cache = null;
			m_viewHelper = null;
			m_mediator = null;
			m_app = null;
			m_beingDisposed = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This should be called by derived classes, usually in the OnLoad event and also as
		/// part of synchronization when styles have been changed by another client.
		/// </summary>
		/// <param name="hvoStylesOwner">the owning object</param>
		/// <param name="tagStylesList">the owner(hvoStylesOwner)'s field ID which holds the
		/// collection of StStyle objects</param>
		/// ------------------------------------------------------------------------------------
		protected void InitStyleSheet(int hvoStylesOwner, int tagStylesList)
		{
			if (m_cache == null)
				throw new Exception("Cache not yet intialized.");

			StyleSheet.Init(m_cache, hvoStylesOwner, tagStylesList);
			InitStyleComboBox();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This should be called by derived classes, usually as part of synchronization when
		/// styles have been changed by another client.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ReSynchStyleSheet()
		{
			Debug.Assert(StyleSheet != null);
			StyleSheet.Init(m_cache, StyleSheet.RootObjectHvo, StyleSheet.StyleListTag);
			InitStyleComboBox();
		}
		#endregion

		#region Menu and Toolbar Setup
//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Returns the full path (including filename) of the ToolBar settings file.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		private string ToolBarSettingsFile
//		{
//			get
//			{
//				string appPrgName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
//				string appPath = Path.GetDirectoryName(Application.ExecutablePath);
//				return Path.Combine(appPath, appPrgName + ".ToolBarDef.xml");
//			}
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create and initializes a new sidebar/info. bar adapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateSideBarInfoBarAdapter()
		{
			m_sibAdapter = AdapterHelper.CreateSideBarInfoBarAdapter();

			if (m_sibAdapter != null)
				m_sibAdapter.Initialize(m_sideBarContainer, m_infoBarContainer, m_mediator);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct the default menus and toolbars
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CreateMenusAndToolBars()
		{
			m_tmAdapter = AdapterHelper.CreateTMAdapter();

			// This will always be null when running tests. Otherwise, it will only be null if
			// The UIAdapter.dll couldn't be found... or null for some other reason. :o)
			if (m_tmAdapter == null)
			{
				// Do this for the sake of the tests.
				InitializeToolBarCombos("tbbParaStylesCombo", new ComboBox());
				InitializeToolBarCombos("tbbCharStylesCombo", new ComboBox());
				InitializeToolBarCombos("tbbZoom", new ComboBox());
				return;
			}

			// Use this to deliver controls to control container toolbar items.
			m_tmAdapter.LoadControlContainerItem +=	LoadCustomToolBarControls;

			// Use this to initialize combo box items.
			m_tmAdapter.InitializeComboItem += InitializeToolBarCombos;

			string sMenuToolBarDefinition = Path.Combine(DirectoryFinder.FWCodeDirectory, "FwTMDefinition.xml");

			m_tmAdapter.Initialize(this, AdapterContentControl, m_mediator, m_app.ProjectSpecificSettingsKey.ToString(),
				new string[] { sMenuToolBarDefinition, GetAppSpecificMenuToolBarDefinition() });

			InitializeContextMenus(sMenuToolBarDefinition, GetAppSpecificMenuToolBarDefinition());

			m_tmAdapter.AllowUpdates = true;
			m_mediator.AddColleague(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual Control AdapterContentControl
		{
			get { return splitContainer; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store any data needed for creating context menus.
		/// </summary>
		/// <param name="generalFile">The general file.</param>
		/// <param name="specificFile">The specific file.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void InitializeContextMenus(string generalFile, string specificFile)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the desired context menu.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual ContextMenuStrip CreateContextMenu(string name)
		{
			return new ContextMenuStrip();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This delegate will return the appropriate control when control container items
		/// request the control to contain.
		/// </summary>
		/// <param name="name">Name of toolbar item.</param>
		/// <param name="tooltip">The tooltip text to assign to the custom control.</param>
		/// ------------------------------------------------------------------------------------
		private Control LoadCustomToolBarControls(string name, string tooltip)
		{
			return LoadAppSpecificCustomToolBarControls(name, tooltip);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This delegate gets called by the toolbar adapter in order to allow us to initialize
		/// combo boxes. Use it to initialize the zoom and styles combos.
		/// </summary>
		/// <param name="name">Name of toolbar item.</param>
		/// <param name="combo">ComboBox to initialize.</param>
		/// ------------------------------------------------------------------------------------
		private void InitializeToolBarCombos(string name, ComboBox combo)
		{
			if (name == "tbbZoom")
			{
				m_cboZoomPercent = combo;

				m_cboZoomPercent.KeyPress +=
					new System.Windows.Forms.KeyPressEventHandler(this.ZoomKeyPress);
				m_cboZoomPercent.LostFocus +=
					new System.EventHandler(this.ZoomLostFocus);
				m_cboZoomPercent.SelectionChangeCommitted +=
					new System.EventHandler(this.ZoomSelectionChanged);

				m_cboZoomPercent.DropDownStyle = ComboBoxStyle.DropDown;

				m_cboZoomPercent.Items.Add(PercentString(50));
				m_cboZoomPercent.Items.Add(PercentString(75));
				m_cboZoomPercent.Items.Add(PercentString(100));
				m_cboZoomPercent.Items.Add(PercentString(150));
				m_cboZoomPercent.Items.Add(PercentString(200));
				m_cboZoomPercent.Items.Add(PercentString(300));
				m_cboZoomPercent.Items.Add(PercentString(400));
			}
			else if (name == "tbbParaStylesCombo")
			{
				m_paraStylesComboBox = combo;
				m_paraStylesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
				m_paraStyleListHelper = new StyleComboListHelper(m_paraStylesComboBox);
				m_paraStyleListHelper.StyleChosen +=
					new StyleChosenHandler(StyleChosenFromStylesComboBox);
				m_paraStyleListHelper.GetCurrentStyleName +=
					new GetCurrentStyleNameHandler(GiveStyleComboCurrStyleName);
			}
			else if (name == "tbbCharStylesCombo")
			{
				m_charStylesComboBox = combo;
				m_charStylesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
				m_charStyleListHelper = new StyleComboListHelper(m_charStylesComboBox);
				m_charStyleListHelper.StyleChosen +=
					new StyleChosenHandler(StyleChosenFromStylesComboBox);
				m_charStyleListHelper.GetCurrentStyleName +=
					new GetCurrentStyleNameHandler(GiveStyleComboCurrStyleName);
			}
			else if (name == "tbbWSCombo")
			{
				m_writingSystemSelector = combo;
				m_writingSystemSelector.AccessibleName = "WritingSystemSelector";
				m_writingSystemSelector.DropDownStyle = ComboBoxStyle.DropDownList;
				m_writingSystemSelector.SelectionChangeCommitted +=
					new EventHandler(m_writingSystemSelector_SelectedIndexChanged);
			}
			else
				InitializeAppSpecificToolBarCombos(name, combo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a string to represent a percentage (50 -> 50%).
		/// </summary>
		/// <param name="percent">percent value to build a string for</param>
		/// <returns>a string representation of the percentage</returns>
		/// ------------------------------------------------------------------------------------
		private string PercentString(int percent)
		{
			return String.Format(ResourceHelper.GetResourceString("ksPercentage"),
				percent.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applications containing derivations of this class implement this if they have their
		/// own toolbar combo boxes they want to initialize.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="cboItem"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void InitializeAppSpecificToolBarCombos(string name, ComboBox cboItem)
		{
			return;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applications containing derivations of this class implement this if they have their
		/// own custom control they want on a toolbar.
		/// </summary>
		/// <param name="name">Name of the toolbar container item for which a control is
		/// requested.</param>
		/// <param name="tooltip">The tooltip to assign to the custom control.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual Control LoadAppSpecificCustomToolBarControls(string name, string tooltip)
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applications containing derivations of this class implement this if they have their
		/// own toolbars they want added to the toolbar container.
		/// </summary>
		/// <returns>An array of XML strings containing toolbar definition.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual string GetAppSpecificMenuToolBarDefinition()
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds tab items to the sidebar (if the adapter is not null) via the sidebar/info.
		/// bar adapter. When running tests, the adapter will be null.
		/// </summary>
		/// <param name="tab"></param>
		/// <param name="itemProps"></param>
		/// ------------------------------------------------------------------------------------
		protected void AddSideBarTabItem(string tab, SBTabItemProperties itemProps)
		{
			if (SIBAdapter != null)
				SIBAdapter.AddTabItem(tab, itemProps);
		}
		#endregion

		#region IFwMainWnd implementation

		private bool m_fInRefreshAll = false;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all the views in this Main Window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshAllViews()
		{
			CheckDisposed();
			if (m_fInRefreshAll)
				return; //don't recurse, it tends to go on forever.

			m_fInRefreshAll = true;
			try
			{

				using (new WaitCursor(this))
				{
					bool prevParaIgnoreValue = false;
					bool prevCharIgnoreValue = false;
					if (ParaStyleListHelper != null)
					{
						prevParaIgnoreValue = ParaStyleListHelper.IgnoreListRefresh;
						prevCharIgnoreValue = CharStyleListHelper.IgnoreListRefresh;

						ParaStyleListHelper.IgnoreListRefresh = true;
						CharStyleListHelper.IgnoreListRefresh = true;
					}

					foreach (IRootSite view in m_rgClientViews.Values)
					{
						if (view != null)
							view.RefreshDisplay();
					}

					if (ParaStyleListHelper != null)
					{
						ParaStyleListHelper.IgnoreListRefresh = prevParaIgnoreValue;
						CharStyleListHelper.IgnoreListRefresh = prevCharIgnoreValue;
					}

					if (!prevParaIgnoreValue || !prevCharIgnoreValue)
						UpdateStyleComboBoxValue(ActiveView);
				}
			}
			finally
			{
				m_fInRefreshAll = false;
			}
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
				return m_cache;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add correspnding stuff to the sidebar, View menu,
		/// etc. Subclasses must override this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void InitAndShowClient()
		{
			CheckDisposed();

			throw new Exception("This is a base class. You must override InitAndShowClient.");
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
				return m_persistence.NormalStateDesktopBounds;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window is finished being created and completely initialized. We use
		/// this to set the new owner of the Find/Replace dialog (OnActivated gets called after
		/// the main window is created, but at that point we don't have any views yet).
		/// </summary>
		/// <returns>True (indicates success)</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnFinishedInit()
		{
			CheckDisposed();

			if (m_app != null)
			{
				// If this registry key exists then we know we're in full window mode.
				m_fFullWindow = RegistryHelper.KeyExists(m_app.ProjectSpecificSettingsKey, "FullWindowSettings");

				if (m_fFullWindow)
				{
					statusStrip.Visible = false;
					if (SIBAdapter != null)
						SIBAdapter.SideBarVisible = false;
				}
				else if (m_app != null)
				{
					statusStrip.Visible = m_app.RegistrySettings.ShowStatusBarSetting;
					if (SIBAdapter != null)
						SIBAdapter.SideBarVisible = m_app.RegistrySettings.ShowSideBarSetting;
				}
			}
			splitContainer.Panel1Collapsed = (SIBAdapter == null) ? true : !SIBAdapter.SideBarVisible;
			OnActivated(new EventArgs());
			return true;
		}

		#region Syncronization Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// ------------------------------------------------------------------------------------
		public abstract void PreSynchronize(SyncMsg sync);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// <returns>true if successful (false results in RefreshAll)</returns>
		/// ------------------------------------------------------------------------------------
		public abstract bool Synchronize(SyncMsg sync);

		#endregion // Syncronization Methods

		#endregion // IFwMainWnd implementation

		#region ISettings implementation
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Registry key for user settings for this window.
		/// </summary>
		/// <remarks>Part of the ISettings interface.</remarks>
		/// -----------------------------------------------------------------------------------
		public virtual RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();

				if (m_app != null)
					return m_app.SettingsKey;
				return FwRegistryHelper.FieldWorksRegistryKey;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Save the persisted settings now.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();

			m_persistence.SaveSettingsNow(this);
		}

		#endregion // ISettings implementation

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new FwContainer();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwMainWnd));
			this.splitContainer = new SIL.FieldWorks.Common.Controls.FwSplitContainer();
			this.m_sideBarContainer = new System.Windows.Forms.Panel();
			this.m_infoBarContainer = new System.Windows.Forms.Panel();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.statusBarFwPanel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.ProgressPanel = new System.Windows.Forms.ToolStripProgressBar();
			this.statusBarFilterPanel = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_persistence = new SIL.FieldWorks.Common.Controls.Persistence(this.components);
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.statusStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).BeginInit();
			this.SuspendLayout();
			//
			// splitContainer
			//
			this.splitContainer.DesiredFirstPanePercentage = 0F;
			resources.ApplyResources(this.splitContainer, "splitContainer");
			this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer.MaxFirstPanePercentage = 0F;
			this.splitContainer.Name = "splitContainer";
			//
			// splitContainer.Panel1
			//
			this.splitContainer.Panel1.Controls.Add(this.m_sideBarContainer);
			//
			// splitContainer.Panel2
			//
			this.splitContainer.Panel2.Controls.Add(this.m_infoBarContainer);
			this.splitContainer.PanelToActivate = this.splitContainer.Panel2;
			this.splitContainer.SettingsKey = null;
			//
			// m_sideBarContainer
			//
			resources.ApplyResources(this.m_sideBarContainer, "m_sideBarContainer");
			this.m_sideBarContainer.Name = "m_sideBarContainer";
			//
			// m_infoBarContainer
			//
			resources.ApplyResources(this.m_infoBarContainer, "m_infoBarContainer");
			this.m_infoBarContainer.Name = "m_infoBarContainer";
			//
			// statusStrip
			//
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.statusBarFwPanel1,
			this.ProgressPanel,
			this.statusBarFilterPanel});
			resources.ApplyResources(this.statusStrip, "statusStrip");
			this.statusStrip.Name = "statusStrip";
			//
			// statusBarFwPanel1
			//
			this.statusBarFwPanel1.Name = "statusBarFwPanel1";
			resources.ApplyResources(this.statusBarFwPanel1, "statusBarFwPanel1");
			this.statusBarFwPanel1.Spring = true;
			//
			// ProgressPanel
			//
			this.ProgressPanel.Maximum = 5000;
			this.ProgressPanel.Name = "ProgressPanel";
			resources.ApplyResources(this.ProgressPanel, "ProgressPanel");
			this.ProgressPanel.Step = 500;
			//
			// statusBarFilterPanel
			//
			resources.ApplyResources(this.statusBarFilterPanel, "statusBarFilterPanel");
			this.statusBarFilterPanel.Name = "statusBarFilterPanel";
			//
			// m_persistence
			//
			this.m_persistence.Parent = this;
			this.m_persistence.SaveSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(this.SaveSettings);
			//
			// FwMainWnd
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.splitContainer);
			this.Controls.Add(this.statusStrip);
			this.Name = "FwMainWnd";
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			this.splitContainer.ResumeLayout(false);
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the messgage mediator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Mediator Mediator
		{
			get
			{
				CheckDisposed();
				return m_mediator;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sidebar/info. bar adapter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ISIBInterface SIBAdapter
		{
			get
			{
				CheckDisposed();
				return m_sibAdapter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the combo box used to select writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox WritingSystemSelector
		{
			get
			{
				CheckDisposed();
				return m_writingSystemSelector;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the toolbar/menu adapter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITMAdapter TMAdapter
		{
			get
			{
				CheckDisposed();
				return m_tmAdapter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a new FwMainWnd is a copy of another FwMainWnd and the new FwMainWnd's Show()
		/// method is called, the .net framework will mysteriously add the height of the menu
		/// bar (and border) to the preset window height.
		/// Aaarghhh! So we intercept the CreateParams in order to set it back to the desired
		/// height.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				if (WindowIsCopy)
					cp.Height = this.Height;
				return cp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the window's status bar control.
		/// </summary>
		/// <remarks>Note: although this property is currently never used in release code,
		/// we want to keep it since it is useful to output debug information, e.g.
		/// from TE's DraftView.</remarks>
		/// ------------------------------------------------------------------------------------
		public StatusStrip StatusStrip
		{
			get
			{
				CheckDisposed();
				return statusStrip;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a reference to the paragraph styles combo box on the toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox ParaStylesComboBox
		{
			get
			{
				CheckDisposed();
				return m_paraStylesComboBox;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a reference to the character styles combo box on the toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox CharStylesComboBox
		{
			get
			{
				CheckDisposed();
				return m_charStylesComboBox;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a reference to the zoom combo box on the toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox ZoomComboBox
		{
			get
			{
				CheckDisposed();
				return m_cboZoomPercent;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the style sheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual FwStyleSheet StyleSheet
		{
			get
			{
				CheckDisposed();

				if (m_StyleSheet == null)
					m_StyleSheet = new FwStyleSheet();
				return m_StyleSheet;
			}
			set	{m_StyleSheet = value;}
		}

		/// <summary>
		/// Get the stylesheet used in the active view. For FwMainWnd this is (so far) always
		/// the same as the main stylesheet
		/// </summary>
		public FwStyleSheet ActiveStyleSheet { get { return StyleSheet;} }

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
				return LangProjectTags.kflidStyles;
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
		/// Gets the style list helper for the paragraph styles combo box on the formatting toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StyleComboListHelper ParaStyleListHelper
		{
			get
			{
				CheckDisposed();
				return m_paraStyleListHelper;
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
				return m_charStyleListHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the maximum style level to be displayed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MaxStyleLevel
		{
			set
			{
				CheckDisposed();

				if (m_paraStyleListHelper != null)
					m_paraStyleListHelper.MaxStyleLevel = value;
				if (m_charStyleListHelper != null)
					m_charStyleListHelper.MaxStyleLevel = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text in the main window's information bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string InformationBarText
		{
			get
			{
				CheckDisposed();
				return (SIBAdapter != null) ? SIBAdapter.InformationBarText : string.Empty;
			}
			set
			{
				CheckDisposed();

				if (SIBAdapter != null && SIBAdapter.InformationBarText != value)
					SIBAdapter.InformationBarText = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the client controls. This is the control collection where you should add views
		/// and so on. It is basically the remaining space between the sidebar, toolbar and
		/// status bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected Control.ControlCollection ClientControls
		{
			get
			{
				CheckDisposed();
				return splitContainer.Panel2.Controls;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the active client window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite ActiveView
		{
			get
			{
				CheckDisposed();
				return m_viewHelper.ActiveView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ActiveViewHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ActiveViewHelper ActiveViewHelper
		{
			get
			{
				CheckDisposed();
				return m_viewHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the EditingHelper cast as an FwEditingHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwEditingHelper FwEditingHelper
		{
			get
			{
				CheckDisposed();
				if (ActiveView == null || ActiveView.EditingHelper == null)
					return null;
				return ActiveView.EditingHelper.CastAs<FwEditingHelper>();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the EditingHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();
				return ActiveView == null ? null : ActiveView.EditingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the main "root object" associated with the application to which this
		/// main window belongs. For example, for TE, this would be the HVO of Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int HvoAppRootObject
		{
			get
			{
				throw new NotImplementedException("Derived class has to implement HvoAppRootObject");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets publication used by active view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual IPublication CurrentPublication
		{
			get { return null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current publication control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual IPublicationView CurrentPublicationView
		{
			get { return ActiveView as IPublicationView; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the publication associated with the current view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string CurrentPublicationName
		{
			get { return null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The current view can be printed or has an associated view that can be printed. (Used
		/// for enabling File Page Setup command)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool CanPrintFromView
		{
			get { return CurrentPublicationView != null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Current view (or its associated printable view) has at least one page of printable
		/// material. (Used for enabling the File Print command)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool HaveSomethingToPrint
		{
			get
			{
				IPublicationView pubView = CurrentPublicationView;
				return (pubView != null && pubView.PageCount > 0);
			}
		}
		#endregion

		#region Load/Save registry settings
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Save UI settings specific to this window.
		/// </summary>
		///
		/// <param name='key'>The Registry Key.</param>
		/// -----------------------------------------------------------------------------------
		protected virtual void SaveSettings(RegistryKey key)
		{
			try
			{

				if (m_tmAdapter != null)
					m_tmAdapter.SaveBarSettings();

				if (m_sibAdapter != null)
					m_sibAdapter.SaveSettings(MainWndSettingsKey);
			}
			catch (Exception e)
			{
				// just ignore any exceptions. It really doesn't matter if SaveSettings() fail
				Console.WriteLine("FwMainWnd.SaveSettings: Exception caught: {0}", e.Message);
			}
		}
		#endregion

		#region Miscellaneous methods
		private delegate void VoidBoolInvoke(bool f);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows or hides the status bar message displayed when a filter is active
		/// </summary>
		/// <param name="fShow"><c>true</c> to display the filter message, <c>false</c>
		/// otherwise</param>
		/// ------------------------------------------------------------------------------------
		public void ShowFilterStatusBarMessage(bool fShow)
		{
			if (InvokeRequired)
				Invoke(new VoidBoolInvoke(ShowFilterStatusBarMessage), fShow);
			else
			{
				CheckDisposed();

				if (fShow)
				{
					statusBarFilterPanel.Text = ResourceHelper.GetResourceString("kstidFilterStatusMsg");
					statusBarFilterPanel.BackColor = Color.Yellow;
				}
				else
				{
					statusBarFilterPanel.Text = string.Empty;
					statusBarFilterPanel.BackColor = SystemColors.Control;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the application name and project name in the caption bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateCaptionBar()
		{
			CheckDisposed();
			string sCaption = m_delegate.GetMainWindowCaption(m_cache);
			if (sCaption != null)
				Text = sCaption;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches the main window (and all it's child controls) for a view with the
		/// specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns>The view with the specified name.</returns>
		/// ------------------------------------------------------------------------------------
		public Control GetNamedView(string name)
		{
			CheckDisposed();

			return GetNamedView(name, this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches a specified control (and all it's child controls) for a view with the
		/// specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="startCtrl"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Control GetNamedView(string name, Control startCtrl)
		{
			CheckDisposed();

			if (startCtrl == null)
				startCtrl = this;

			foreach (Control ctrl in startCtrl.Controls)
			{
				if (ctrl.Name == name)
					return ctrl;

				Control tmpCtrl = GetNamedView(name, ctrl);
				if (tmpCtrl != null)
					return tmpCtrl;
			}

			return null;
		}
		#endregion

		#region Update handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the enabled state of the File Project Sharing Location menu item
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileProjectSharingLocation(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.CanWriteKey();
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the value in the zoom combo box with the active view's zoom percentage.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateViewZoom(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && m_cboZoomPercent != null)
			{
				if (!m_cboZoomPercent.Focused)
				{
					if (ActiveView is SimpleRootSite)
					{
						itemProps.Text = m_cboZoomPercent.Text = ZoomString;
						itemProps.Enabled = ZoomEnabledForView(ActiveView as SimpleRootSite);
					}
					else
					{
						itemProps.Text = m_cboZoomPercent.Text = m_app.GetResourceString("kstidPageWidth");
						itemProps.Enabled = false;
					}
					m_cboZoomPercent.Enabled = itemProps.Enabled;
					itemProps.Update = true;
				}
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Derived classes may override this in case they contain SimpleRootSites that cannot
		/// be zoomed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool ZoomEnabledForView(SimpleRootSite view)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Edit/Delete menu item
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditDelete(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = EditingHelper != null && EditingHelper.CanDelete();
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Edit/Copy menu item
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditCopy(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = EditingHelper != null && EditingHelper.CanCopy();
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Edit/Cut menu item
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnUpdateEditCut(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = EditingHelper != null && EditingHelper.CanCut();
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Edit/Paste menu item
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditPaste(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = EditingHelper != null && EditingHelper.CanPaste();
				itemProps.Update = true;
				return true;
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
		protected bool OnUpdateFileDelete(object arg)
		{
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the checked status of the toolbar menu items off the view menu.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateToggleToolBarVisiblilty(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = !m_fFullWindow;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the status bar menu item
		/// </summary>
		/// <param name="arg">The menu or toolbar item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateStatusBar(object arg)
		{
			TMItemProperties itemProps = arg as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = !m_fFullWindow;
				itemProps.Checked = statusStrip.Visible;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the side bar menu item
		/// </summary>
		/// <param name="arg">The menu or toolbar item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateSideBar(object arg)
		{
			TMItemProperties itemProps = arg as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = !m_fFullWindow;
				itemProps.Checked = SIBAdapter.SideBarVisible;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If there is nothing to save then disable the File/Save menu item and toolbar
		/// button.
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileSave(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				// We never have open transactions, except at the moment of saving.
				itemProps.Enabled = (m_cache != null &&
					m_cache.ServiceLocator.GetInstance<IUndoStackManager>().HasUnsavedChanges);
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the File/Print menu item. We disable it for views
		/// that have no corresponding publication.
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnUpdateFilePrint(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = (HaveSomethingToPrint);
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Undo menu item
		/// </summary>
		/// <param name="args">Menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditUndo(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = (Cache != null &&
					Cache.DomainDataByFlid.GetActionHandler().UndoableSequenceCount > 0);
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Redo menu item
		/// </summary>
		/// <param name="args">Menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditRedo(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = (Cache != null &&
					Cache.DomainDataByFlid.GetActionHandler().RedoableSequenceCount > 0);
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the initialization of the Window menu, before it is displayed.
		/// We must establish menu items for each current main window of our app.
		/// </summary>
		/// <param name="args">ignored</param>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateWindowActivate(object args)
		{
			WindowListInfo wndListInfo = args as WindowListInfo;
			if (wndListInfo == null)
				return false;

			int checkedIndex = 0;
			// This can't really be a generic,
			// as it gets handed off to wndListInfo.WindowListItemProperties.List, below,
			// and that 'List' property needs to hold most any type of object,
			// depending on who uses it.
			ArrayList menuText = new ArrayList(m_app.MainWindows.Count);

			for (int i = 0; i < m_app.MainWindows.Count; i++)
			{
				IFwMainWnd wnd = m_app.MainWindows[i];
				string format = wndListInfo.WindowListItemProperties.OriginalText;
				string projName = wnd.Cache.ProjectId.Name;

				if (wnd is FwMainWnd)
				{
					string text = string.Format(format,
						((FwMainWnd)wnd).SIBAdapter.InformationBarText, projName);
					menuText.Add(text);
				}
				else if (wnd is Form)
				{
					string text = string.Format(format, ((Form)wnd).Text, projName);
					menuText.Add(text);
				}

				if (wnd == this)
					checkedIndex = i;
			}

			wndListInfo.CheckedItemIndex = checkedIndex;
			wndListInfo.WindowListItemProperties.List = menuText;
			wndListInfo.WindowListItemProperties.Update = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables the User properties dialog because that doesn't really do anything
		/// </summary>
		/// <param name="args">The menu or toolbar item</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateToolsUserProperties(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = false;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		///
//		/// </summary>
//		/// <param name="args"></param>
//		/// <returns></returns>
//		/// ------------------------------------------------------------------------------------
//		protected bool OnUpdateFormatBorders(object args)
//		{
//			TMItemProperties itemProps = args as TMItemProperties;
//			if (itemProps != null)
//			{
//				itemProps.Enabled = false;
//				itemProps.Update = true;
//				return true;
//			}
//
//			return false;
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		///
//		/// </summary>
//		/// <param name="args"></param>
//		/// <returns></returns>
//		/// ------------------------------------------------------------------------------------
//		protected bool OnUpdateFormatBackgroundColor(object args)
//		{
//			TMItemProperties itemProps = args as TMItemProperties;
//			if (itemProps != null)
//			{
//				itemProps.Enabled = false;
//				itemProps.Update = true;
//				return true;
//			}
//
//			return false;
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		///
//		/// </summary>
//		/// <param name="args"></param>
//		/// <returns></returns>
//		/// ------------------------------------------------------------------------------------
//		protected bool OnUpdateFormatForegroundColor(object args)
//		{
//			TMItemProperties itemProps = args as TMItemProperties;
//			if (itemProps != null)
//			{
//				itemProps.Enabled = false;
//				itemProps.Update = true;
//				return true;
//			}
//
//			return false;
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the Enabled state of the Format Apply Style dialog menu option
		/// </summary>
		/// <param name="args">The menu/toolbar item</param>
		/// <returns>true</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateFormatApplyStyle(object args)
		{
			CheckDisposed();

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = m_delegate.CanApplyStyle;
				itemProps.Update = true;
				return true;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Edit/Copy menu item
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnUpdatePageSetup(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = CanPrintFromView;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles side bar and toolbar's visibility
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateViewFullWindow(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Checked = m_fFullWindow;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if Edit Find menu item should be enabled.
		/// </summary>
		/// <param name="args">The menu item properties</param>
		/// <returns><c>true</c> if a valid menu item</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditFind(object args)
		{
			return UpdateMenuRequiringValidView(args as TMItemProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if Edit Replace menu item should be enabled.
		/// </summary>
		/// <param name="args">The menu item properties</param>
		/// <returns><c>true</c> if a valid menu item</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditReplace(object args)
		{
			return UpdateMenuRequiringValidView(args as TMItemProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if Edit Find Next menu item should be enabled.
		/// </summary>
		/// <param name="args">The menu item properties</param>
		/// <returns><c>true</c> if a valid menu item</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditFindNext(object args)
		{
			return UpdateMenuRequiringValidView(args as TMItemProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if Edit Find Prev menu item should be enabled.
		/// </summary>
		/// <param name="args">The menu item properties</param>
		/// <returns><c>true</c> if a valid menu item</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditFindPrev(object args)
		{
			return UpdateMenuRequiringValidView(args as TMItemProperties);
		}
		#endregion

		#region Message handlers
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
		/// Handle the Replace menu command.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnEditReplace(object args)
		{
			return m_app.ShowFindReplaceDialog(true, ActiveView as RootSite);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Find Next menu command.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnEditFindNext(object args)
		{
			if (m_app.FindReplaceDialog == null)
				return m_app.ShowFindReplaceDialog(false, ActiveView as RootSite);

			m_app.FindReplaceDialog.FindNext();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Find Next menu command.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnEditFindPrev(object args)
		{
			if (m_app.FindReplaceDialog == null)
				return m_app.ShowFindReplaceDialog(false, ActiveView as RootSite);

			m_app.FindReplaceDialog.FindPrevious();
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Edit/Copy menu command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CmdEditCopy1</c></remarks>
		/// -----------------------------------------------------------------------------------
		protected bool OnEditCopy(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return true;

			using (new WaitCursor(this)) // creates a wait cursor and makes it active until the end of the method.
			{
				return EditingHelper.CopySelection();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Edit/Cut menu command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CmdEditCut1</c></remarks>
		/// -----------------------------------------------------------------------------------
		protected bool OnEditCut(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return false;

			string undo, redo;
			ResourceHelper.MakeUndoRedoLabels("kstidEditCut", out undo, out redo);

			using (UndoTaskHelper undoHelper = new UndoTaskHelper(ActiveView.CastAsIVwRootSite(), undo, redo))
			using (new DataUpdateMonitor(this, "EditCut"))
			{
				bool result = EditingHelper.CutSelection();
				undoHelper.RollBack = !result;
				return result;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Edit/Delete menu command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CmdEditDel1</c></remarks>
		/// -----------------------------------------------------------------------------------
		protected virtual bool OnEditDelete(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return true; //discard this event

			string undo, redo;
			ResourceHelper.MakeUndoRedoLabels("kstidEditDelete", out undo, out redo);
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(ActiveView.CastAsIVwRootSite(), undo, redo))
			using (new DataUpdateMonitor(this, "DeleteSelection"))
			{
				EditingHelper.DeleteSelection();

				undoHelper.RollBack = false;
				return true;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Edit/Paste menu command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// <remarks>Formerly <c>AfVwRootSite::CmdEditPaste1</c></remarks>
		/// -----------------------------------------------------------------------------------
		protected bool OnEditPaste(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress() || EditingHelper == null || !EditingHelper.CanPaste())
				return true; //discard this event

			string stUndo, stRedo;
			ResourceHelper.MakeUndoRedoLabels("kstidEditPaste", out stUndo, out stRedo);
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(ActiveView.CastAsIVwRootSite(), stUndo, stRedo))
			using (new DataUpdateMonitor(this, "EditPaste"))
			{
				if (EditingHelper.PasteClipboard())
					undoHelper.RollBack = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnFilePrint(object args)
		{
			IPublicationView pubView = CurrentPublicationView;
			if (pubView == null)
				return false;

			pubView.OnFilePrint();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close this window
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileClose(object args)
		{
			Logger.WriteEvent("Closing " + Name + ", " + Text);

			Close();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close this window
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileDelete(object args)
		{
			m_app.FwManager.DeleteProject(m_app, this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save to the database; does NOT force clearing undo stack.
		/// </summary>
		/// <param name="args">Menu item or toolbar item</param>
		/// <returns><c>true</c> because we handled this command</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileSave(object args)
		{
			try
			{
				if (m_cache != null)
					m_cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
			}
			catch (NonRecoverableConnectionLostException e)
			{
				// any changes have NOT been saved.
				Logger.WriteEvent("Got non-recoverable error while saving:");
				Logger.WriteError(e);
				m_delegate.FileExit();
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The "New FieldWorks Project" menu option has been selected. Display the New
		/// FieldWorks Project dialog.
		/// </summary>
		/// <param name="args">ignored</param>
		/// ------------------------------------------------------------------------------------
		public bool OnFileNew(object args)
		{
			CheckDisposed();
			m_delegate.FileNew(this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display a dialog to allow the user to open a new language project.
		/// </summary>
		/// <param name="args">ignored</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileOpen(object args)
		{
			CheckDisposed();
			m_delegate.FileOpen(this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new window
		/// </summary>
		/// <param name="args">The menu or toolbar item</param>
		/// <returns><c>true</c> because we handled this message.</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnNewWindow(object args)
		{
			CheckDisposed();
			// First save all persisted registry settings for this current window,
			// sidebar, etc,
			// so new window will load and use the same settings when it is opened.
			SaveSettingsNow();
			m_app.FwManager.OpenNewWindowForApp(m_app, this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cascade all the windows
		/// </summary>
		/// <param name="sender"></param>
		/// <returns><c>true</c> because we handled this message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnWindowCascade(object sender)
		{
			m_app.CascadeWindows(this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tile the windows from side to side
		/// </summary>
		/// <param name="sender"></param>
		/// <returns><c>true</c> because we handled this message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnWindowTileSideBySide(object sender)
		{
			m_app.TileWindows(this, WindowTiling.SideBySide);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tile the windows from top to bottom
		/// </summary>
		/// <param name="sender"></param>
		/// <returns><c>true</c> because we handled this message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnWindowTileStacked(object sender)
		{
			m_app.TileWindows(this, WindowTiling.Stacked);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For handling clicks on the dynamically added list of windows in the window menu.
		/// Changes focus to the window associated with the sender.
		/// </summary>
		/// <param name="sender">the WindowMenuItem that was clicked on</param>
		/// ------------------------------------------------------------------------------------
		protected bool OnWindowActivate(object sender)
		{
			if (sender is TMItemProperties)
			{
				int i = (int)((TMItemProperties)sender).Tag;
				m_app.ActivateWindow(i);
			}
			else if (sender is int) // for testing
				m_app.ActivateWindow((int)sender);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="args">the item that was clicked on</param>
		/// ------------------------------------------------------------------------------------
		protected bool OnMoreWindows(object args)
		{
			using (MoreWindowsForm moreWindows = new MoreWindowsForm(m_app.MainWindows))
			{
				if (ShowTestableDialog(moreWindows) == DialogResult.OK)
					m_app.ActivateWindow(moreWindows.SelectedWindow);

				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The purpose for this method is to deal with modal dialogs that need to be tested
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual DialogResult ShowTestableDialog(Form dialog)
		{
			return dialog.ShowDialog();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close all windows and shut down the application
		/// </summary>
		/// <param name="sender"></param>
		/// <returns><c>true</c> because we handled this message.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnFileExit(object sender)
		{
			CheckDisposed();
			m_delegate.FileExit();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Backup menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackupProject(object arg)
		{
			m_app.FwManager.BackupProject(m_app, this);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Restore menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnRestoreProject(object arg)
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
			m_app.FwManager.FileProjectSharingLocation(m_app, this);
			return true;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles the visibility of a toolbar.
		/// </summary>
		/// <param name="args">Menu item indicating what toolbar's visibility will be toggled.
		/// </param>
		/// <returns><c>true</c> because we handled this command</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnToggleToolBarVisiblilty(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;

			if (itemProps == null || m_tmAdapter == null)
				return false;

			// Get the toolbar name associated with the item.
			string barName = itemProps.Tag as string;

			if (barName == null)
				return false;

			// If the menu item is checked it means the toolbar is currently visible.
			// Therefore, hide it. Otherwise, show it.
			if (itemProps.Checked)
				m_tmAdapter.HideToolBar(barName);
			else
				m_tmAdapter.ShowToolBar(barName);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles the status bar's visibility.
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnStatusBar(object args)
		{
			if (!DesignMode)
			{
				statusStrip.Visible = !statusStrip.Visible;
				m_app.RegistrySettings.ShowStatusBarSetting = statusStrip.Visible;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles the side bar's visibility.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnSideBar(object args)
		{
			if (!DesignMode)
			{
				if (SIBAdapter.SideBarVisible) // SideBar must be invisible before collapsing
				{
					SIBAdapter.SideBarVisible = false;
					splitContainer.Panel1Collapsed = true;
				}
				else // SideBar must be uncollapsed before being made visible
				{
					splitContainer.Panel1Collapsed = false;
					SIBAdapter.SideBarVisible = true;
				}
				m_app.RegistrySettings.ShowSideBarSetting = SIBAdapter.SideBarVisible;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles side bar, status bar and toolbar's visibility
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnViewFullWindow(object args)
		{
			// If this registry key exists then we know we're in full window mode.
			m_fFullWindow = RegistryHelper.KeyExists(m_app.ProjectSpecificSettingsKey, "FullWindowSettings");

			using (RegistryGroup regGroup = new RegistryGroup(m_app.ProjectSpecificSettingsKey, "FullWindowSettings"))
			{
				if (!m_fFullWindow)
				{
					m_fFullWindow = true;
					regGroup.SetBoolValue("SideBarVisible", SIBAdapter.SideBarVisible);
					regGroup.SetBoolValue("StatusBarVisible", statusStrip.Visible);
					SIBAdapter.SideBarVisible = false;
					splitContainer.Panel1Collapsed = true;
					statusStrip.Visible = false;

					TMBarProperties[] toolbars = m_tmAdapter.BarInfoForViewMenu;
					foreach (TMBarProperties barProps in toolbars)
					{
						regGroup.SetBoolValue(barProps.Name, barProps.Visible);
						m_tmAdapter.HideToolBar(barProps.Name);
					}
				}
				else
				{
					m_fFullWindow = false;

					// If side bar was already not visible, then it was also collapsed.
					// there's nothing we need to do for the side bar.
					if (regGroup.GetBoolValue("SideBarVisible", true))
					{
						// SideBar must be uncollapsed before being made visible
						splitContainer.Panel1Collapsed = false;
						SIBAdapter.SideBarVisible = true;
					}

					statusStrip.Visible = regGroup.GetBoolValue("StatusBarVisible", true);

					TMBarProperties[] toolbars = m_tmAdapter.BarInfoForViewMenu;
					foreach (TMBarProperties barProps in toolbars)
					{
						if (regGroup.GetBoolValue(barProps.Name, true))
							m_tmAdapter.ShowToolBar(barProps.Name);
					}

					try
					{
						regGroup.Delete();
					}
					catch
					{
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will undo the last changes done to the project.
		/// This function is executed when the user clicks the undo menu item.
		/// </summary>
		/// <param name="args">If this command is being called directly from a menu command,
		/// this parameter is the adapter menu item; otherwise, if this is being called as part
		/// of a batch, pass null. If this is not null, then in the case of a failure result
		/// from the undo, a message will be displayed. If this is null, then this method will
		/// return false, so the caller can display an error message if appropriate.</param>
		/// <returns><c>true</c> if handled successfully, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnEditUndo(object args)
		{
			if (!Cache.DomainDataByFlid.GetActionHandler().CanUndo())
			{
				if (Cache.ActionHandlerAccessor.UndoableSequenceCount > 0)
				{
					MessageBox.Show(this, ResourceHelper.GetResourceString("kstidCannotUndo"),
						m_app.ApplicationName);
				}
				return false;
			}

			bool fResult = true;
			// start hour glass
			using (new WaitCursor(this))
			{
				m_app.SuppressSynchronize();
				m_app.SuppressPaint(); // to prevent re-entrancy during PropChanges. (FWR-1752)
				try
				{
					try
					{
						Logger.WriteEvent(m_cache.DomainDataByFlid.GetActionHandler().GetUndoTextN(
							m_cache.DomainDataByFlid.GetActionHandler().UndoableSequenceCount - 1));
					}
					catch
					{
						// Diagnostic code to try to track down TE-6409
						Logger.WriteEvent("Undo Failed (exception details follow)");
						Logger.WriteEvent("m_cache.ActionHandlerAccessor.UndoableSequenceCount = " +
							m_cache.DomainDataByFlid.GetActionHandler().UndoableSequenceCount);
						Logger.WriteEvent("m_cache.ActionHandlerAccessor.UndoableActionCount = " +
							m_cache.DomainDataByFlid.GetActionHandler().UndoableActionCount);
						Logger.WriteEvent("Undo stack items:");
						try
						{
							for (int i = 0; i < m_cache.DomainDataByFlid.GetActionHandler().UndoableSequenceCount - 1; i++)
							{
								Logger.WriteEvent(m_cache.DomainDataByFlid.GetActionHandler().GetUndoTextN(i));
							}
						}
						catch
						{
							// Ignore error caught while generating diagnostic messages
						}
						throw;
					}

					// Are changes private to a TssEdit field editor or similar?
					bool fPrivate = Cache.DomainDataByFlid.GetActionHandler().get_TasksSinceMark(true);

					UndoResult ures = Cache.DomainDataByFlid.GetActionHandler().Undo();
					// Enhance JohnT: handle ures errors?

					if (ures == UndoResult.kuresError || ures == UndoResult.kuresFailed || ures == UndoResult.kuresRefresh)
					{
						Debug.Fail("Undo failed!");
						if (args != null)
						{
							// This command is being called as a stand-alone operation, so tell the user we failed.
							MessageBox.Show(ResourceHelper.GetResourceString("kstidUndoFailed"),
								m_app.ApplicationName);
						}
						else
							fResult = false;
					}
					if (!fPrivate)
					{
						if (ures == UndoResult.kuresSuccess)
							m_app.Synchronize(SyncMsg.ksyncUndoRedo);
						else
							m_app.Synchronize(SyncMsg.ksyncFullRefresh);
					}
				}
				finally
				{
					m_app.ResumeSynchronize();
					m_app.ResumePaint();
				}
			}
			return fResult;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will drop down the undo list box
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDropDownEditUndo(object args)
		{
			// TE-6534: There may be some cases in which the user clicks on the Undo drop-down
			// error as the main window is being displayed but before the OnUpdate handler has
			// a chance to make the undo button disabled. Therefore we need to make sure that
			// before proceeding here, make sure we have a cache and an ActionHandlerAccessor.
			if (m_cache == null || m_cache.DomainDataByFlid.GetActionHandler() == null ||
				m_cache.DomainDataByFlid.GetActionHandler().UndoableSequenceCount == 0)
			{
				return false;
			}

			ToolBarPopupInfo popupInfo = args as ToolBarPopupInfo;
			if (popupInfo == null)
				return false;

			InitializeUndoRedoDropDown(popupInfo, m_app.GetResourceString("kstidUndo1Action"),
				m_app.GetResourceString("kstidUndoMultipleActions"),
				m_app.GetResourceString("kstidUndoRedoCancel"));

			for (int i = m_cache.DomainDataByFlid.GetActionHandler().UndoableSequenceCount - 1; i >= 0; i--)
			{
				string undoText = m_cache.DomainDataByFlid.GetActionHandler().GetUndoTextN(i);
				Debug.Assert(!String.IsNullOrEmpty(undoText));
				m_UndoRedoDropDown.Actions.Add(undoText.Replace("&", String.Empty));
			}

			m_UndoRedoDropDown.ItemClick += OnUndoDropDownClicked;

			m_UndoRedoDropDown.AdjustHeight();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will drop down the redo list box
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDropDownEditRedo(object args)
		{
			// TE-6534: There may be some cases in which the user clicks on the Redo drop-down
			// error as the main window is being displayed but before the OnUpdate handler has
			// a chance to make the redo button disabled. Therefore we need to make sure that
			// before proceeding here, make sure we have a cache and an ActionHandlerAccessor.
			if (m_cache == null || m_cache.DomainDataByFlid.GetActionHandler() == null ||
				m_cache.DomainDataByFlid.GetActionHandler().RedoableSequenceCount == 0)
			{
				return false;
			}

			ToolBarPopupInfo popupInfo = args as ToolBarPopupInfo;
			if (popupInfo == null)
				return false;
			InitializeUndoRedoDropDown(popupInfo, m_app.GetResourceString("kstidRedo1Action"),
				m_app.GetResourceString("kstidRedoMultipleActions"),
				m_app.GetResourceString("kstidUndoRedoCancel"));

			// JohnT: can't reproduce a state in which either m_cache or m_cache.ActionHandlerAccessor is null,
			// but it happened at least once (TE-6544) so rule it out.
			if (m_cache != null && m_cache.DomainDataByFlid.GetActionHandler() != null)
			{
				for (int i = 0; i < m_cache.DomainDataByFlid.GetActionHandler().RedoableSequenceCount; i++)
				{
					string redoText = m_cache.DomainDataByFlid.GetActionHandler().GetRedoTextN(i);
					Debug.Assert(!String.IsNullOrEmpty(redoText));
					m_UndoRedoDropDown.Actions.Add(redoText.Replace("&", String.Empty));
				}
			}

			m_UndoRedoDropDown.ItemClick +=
				new UndoRedoDropDown.ClickEventHandler(OnRedoDropDownClicked);

			m_UndoRedoDropDown.AdjustHeight();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="popupInfo"></param>
		/// <param name="singleAction"></param>
		/// <param name="multipleActions"></param>
		/// <param name="cancel"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void InitializeUndoRedoDropDown(ToolBarPopupInfo popupInfo,
			string singleAction, string multipleActions, string cancel)
		{
			m_UndoRedoDropDown = new UndoRedoDropDown(singleAction, multipleActions, cancel);
			popupInfo.Control = m_UndoRedoDropDown;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This function will redo the last changes undone to the project.
		/// This function is executed when the user clicks the redo menu item.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnEditRedo(object args)
		{
			if (!Cache.DomainDataByFlid.GetActionHandler().CanRedo())
			{
				if (Cache.ActionHandlerAccessor.RedoableSequenceCount > 0)
				{
					MessageBox.Show(this, ResourceHelper.GetResourceString("kstidCannotRedo"),
						m_app.ApplicationName);
				}
				return false;
			}

			// start hour glass
			using(new WaitCursor(this))
			{
				m_app.SuppressSynchronize();
				m_app.SuppressPaint(); // to prevent re-entrancy during PropChanges. (FWR-1752)
				try
				{
					Logger.WriteEvent(m_cache.DomainDataByFlid.GetActionHandler().GetRedoTextN(0));

					// Are changes private to a TssEdit field editor or similar?
					bool fPrivate = Cache.DomainDataByFlid.GetActionHandler().get_TasksSinceMark(false);

					UndoResult ures = Cache.DomainDataByFlid.GetActionHandler().Redo();
					// Enhance JohnT: handle ures errors?
					Debug.Assert(ures == UndoResult.kuresSuccess ||
						ures == UndoResult.kuresRefresh, "Redo failed!");

					if (!fPrivate)
					{
						if (ures == UndoResult.kuresSuccess)
							m_app.Synchronize(SyncMsg.ksyncUndoRedo);
						else
							m_app.Synchronize(SyncMsg.ksyncFullRefresh);
					}
				}
				finally
				{
					m_app.ResumeSynchronize();
					m_app.ResumePaint();
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnFormatBorders(object args)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnFormatBackgroundColor(object args)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnDropDownFormatBackgroundColor(object args)
		{
			if (m_tmAdapter == null)
				return false;

			// TODO: Show color picker.
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDropDownFormatForegroundColor(object args)
		{
			if (m_tmAdapter == null)
				return false;

			// TODO: Show color picker.
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the project properties dialog
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnFileProjectProperties(object args)
		{
			// Disabling the main window circumvents the usual mechanisms for switching keyboards,
			// so we manually switch to the default keyboard here so that the Project Properties
			// dialog displays the default keyboard. When we're all done we switch back to the
			// keyboard we had recently. (TE-4683)
			int oldWs = 0;
			if (EditingHelper != null) // JohnT: guard against e.g. TE-6543.
				oldWs = EditingHelper.SetKeyboardForWs(-1);
			// Disable windows on cache to prevent painting when fonts for writing system are changed
			m_app.EnableMainWindows(false);
			bool fDbRenamed = false;
			bool fFilesMoved = false;
			string sProject = m_cache.ProjectId.Name;
			string sOrigLinkedFilesRootDir = m_cache.LangProject.LinkedFilesRootDir;
			try
			{
				using (var dlg = new FwProjPropertiesDlg(m_cache, m_app, m_app, m_StyleSheet))
				{
					if (dlg.ShowDialog(this) != DialogResult.OK)
						return true;
					using (new WaitCursor(this))
					{
						fDbRenamed = dlg.ProjectNameChanged();
						if (fDbRenamed)
							sProject = dlg.ProjectName;
						if (dlg.LinkedFilesChanged())
							fFilesMoved = m_app.UpdateExternalLinks(sOrigLinkedFilesRootDir);
						if (!fDbRenamed)
						{
							// rename works only if other programs (like Flex) are not
							// running.  In which case, the Sync operation isn't needed.
							// Note: We handle this here since Flex does full refresh and we don't want
							// this happening first.
							if (dlg.WritingSystemsChanged())
							{
								using (var undoHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor, null,
									"kstidUndoRedoProjectProperties"))
								{
									var undoAction = new SyncUndoAction(m_app, SyncMsg.ksyncWs);
									undoAction.Do();
									m_cache.DomainDataByFlid.GetActionHandler().AddAction(undoAction);
									undoHelper.RollBack = false;
								}
							}
						}
					}
				}
			}
			finally
			{
				m_app.EnableMainWindows(true);
				if (!fDbRenamed && !fFilesMoved)	// no need for refresh when total shutdown & reopen
				{
					// Make sure windows for this cache are now enabled.
					// if the dialog merged two writing systems it will close/dispose this FwMainWnd.
					// if so, don't try to access any properties like EditingHelper or we'll crash (TE-7297).
					if (!IsDisposed)
					{
						// Restore the previous keyboard
						if (oldWs != 0 && EditingHelper != null)
							EditingHelper.SetKeyboardForWs(oldWs);
					}
				}
			}
			if (fDbRenamed)
				m_app.FwManager.RenameProject(sProject, m_app);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the dialog to allow users to customize toolbars.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnCustomizeToolBars(object args)
		{
			m_tmAdapter.ShowCustomizeDialog();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show About box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnHelpAbout(object args)
		{
			return m_delegate.ShowHelpAbout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show User Properties dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnToolsUserProperties(object args)
		{
			using (FwUserProperties propDlg = new FwUserProperties(m_cache, m_app.GetAppFeatures()))
			{
				propDlg.SetDialogProperties(m_app);
				propDlg.ShowDialog();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will get called when the current tab in the sidebar changes. When that
		/// happens, we need to make sure the view changes to the one associated with the new
		/// tab's current item. If the new tab doesn't have a current item, then we need
		/// to set one.
		/// </summary>
		/// <param name="args">SBTabProperties</param>
		/// <returns>true if handled</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnSideBarTabClicked(object args)
		{
			SBTabProperties tabProps = args as SBTabProperties;
			if (tabProps == null)
				return false;

			try
			{
				string currItem = tabProps.CurrentTabItem;

				// If the tab already has a current item, then activate it's view.
				if (currItem != null)
				{
					OnSwitchActiveView(SIBAdapter.GetTabItemProperties(tabProps.Name, currItem));
					return true;
				}

				// At this point, the tab must not have a current item, so set one.
				currItem = GetDefaultItemForTab(tabProps.Name);

				SIBAdapter.SetCurrentTabItem(tabProps.Name, currItem, true);
				return true;
			}
			catch
			{
				// This can happen if we got the message first but do not handle it.
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the default sidebar item for the given tab. Applications should
		/// override this if they want to work well.
		/// </summary>
		/// <param name="tabName">Name of the sidebar tab</param>
		/// <returns>null</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual string GetDefaultItemForTab(string tabName)
		{
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the styles dialog
		/// </summary>
		/// <param name="args">ignored</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFormatStyle(object args)
		{
			string charStyleName;
			string paraStyleName;
			GetCurrentStyleNames(out paraStyleName, out charStyleName);

			if (m_delegate.ShowStylesDialog(paraStyleName, charStyleName, SetPropsToFactorySettings))
				m_app.Synchronize(SyncMsg.ksyncStyle);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the properties of a StyleInfo to the factory default settings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract void SetPropsToFactorySettings(StyleInfo styleInfo);

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
			string paraStyleName;
			string charStyleName;
			GetCurrentStyleNames(out paraStyleName, out charStyleName);
			ShowApplyStyleDialog(paraStyleName, charStyleName);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh the current view
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnViewRefresh(object args)
		{
			RefreshAllViews();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given menu item should be enabled, based on whether there is a
		/// valid view and constructed rootbox.
		/// </summary>
		/// <param name="itemProps">The menu item properties</param>
		/// <returns><c>true</c> if a valid menu item</returns>
		/// ------------------------------------------------------------------------------------
		private bool UpdateMenuRequiringValidView(TMItemProperties itemProps)
		{
			if (itemProps == null)
				return false;

			RootSite rs = ActiveView as RootSite;
			bool enable = (rs != null && rs.RootBox != null);

			if (itemProps.Enabled != enable)
			{
				itemProps.Update = true;
				itemProps.Enabled = enable;
			}

			return true;
		}
		#endregion

		#region Menu event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Page Setup menu option.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if this event is handled</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnPageSetup(object args)
		{
			CheckDisposed();

			IPublication pub;
			IPubDivision div;
			IPubPageLayout pgl;

			if (!PageSetupPreparePublication(out pub, out div, out pgl))
				return false;

			using (IPageSetupDialog dlg = CreatePageSetupDialog(pgl, pub, div))
			{
				// The PageSetup dialog is view specific, so we add the view name to the title.
				if (CurrentPublicationName != null)
				{
					dlg.Text = string.Format(ResourceHelper.GetResourceString(
						"kstidPageSetupWithViewName"), CurrentPublicationName);
				}

				using (UndoTaskHelper undoHelper =
					new UndoTaskHelper(m_cache.ActionHandlerAccessor, null, "kstidUndoPageSetupChanges"))
				{
					if (dlg.ShowDialog() == DialogResult.OK && CurrentPublicationView != null)
					{
						CurrentPublicationView.ApplyPubOverrides(dlg.BaseCharacterSize,
																 -dlg.BaseLineSpacing);
						CurrentPublicationView.RefreshDisplay();
					}
					undoHelper.RollBack = false;
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the page setup dialog.
		/// </summary>
		/// <param name="pgl">The PubPageLayout object</param>
		/// <param name="pub">The Publication object</param>
		/// <param name="div">The PubDivision object</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual IPageSetupDialog CreatePageSetupDialog(IPubPageLayout pgl,
			IPublication pub, IPubDivision div)
		{
			throw new NotImplementedException("Subclass must override CreatePageSetupDialog.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a publication for OnPageSetup.
		/// </summary>
		/// <param name="publication">A publication to use for page setup.</param>
		/// <param name="division">Publication Division for pub</param>
		/// <param name="pageLayout">A publication page layout for pub</param>
		/// <returns>returns <c>false</c> if there is no current publication</returns>
		/// <remarks>This was the top half of OnPageSetup originally.
		/// It was refactored to avoid duplicating code in an override of
		/// OnPageSetup in TeMainWind.</remarks>
		/// ------------------------------------------------------------------------------------
		protected bool PageSetupPreparePublication(out IPublication publication,
			out IPubDivision division, out IPubPageLayout pageLayout)
		{
			publication = null;
			division = null;
			pageLayout = null;

			// Get a publication to use for page setup.
			publication = CurrentPublication;
			if (publication == null)
				return false;

			// Get a publication division for given publication.  If none, create one.
			int chvoDiv = publication.DivisionsOS.Count;
			int hvoDiv;
			if (chvoDiv < 1)
			{
				division = m_cache.ServiceLocator.GetInstance<IPubDivisionFactory>().Create();
				publication.DivisionsOS.Add(division);
				division.StartAt = DivisionStartOption.NewPage;
				division.DifferentEvenHF = false;
				division.DifferentFirstHF = false;
			}
			else
			{
				division = publication.DivisionsOS[0];
			}
			hvoDiv = division.Hvo;

			// Get publication page layout for given division.  If none create one.
			if (division.PageLayoutOA == null)
			{
				pageLayout = m_cache.ServiceLocator.GetInstance<IPubPageLayoutFactory>().Create();
				division.PageLayoutOA = pageLayout;
				pageLayout.MarginTop = 72000;
				pageLayout.MarginBottom = 72000;
				pageLayout.MarginInside = 72000;
				pageLayout.MarginOutside = 72000;
				pageLayout.PosHeader = 36000;
				pageLayout.PosFooter = 36000;
			}
			else
			{
				pageLayout = division.PageLayoutOA;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Create Shortcut on Desktop menu/toolbar item.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnCreateShortcut(object args)
		{
			return m_delegate.OnCreateShortcut(args);
		}
		#endregion

		#region Other event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked an item in the Undo drop down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="iClicked">Zero-based index of clicked item</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnUndoDropDownClicked(object sender, int iClicked)
		{
			if (m_tmAdapter != null)
			{
				m_tmAdapter.HideBarItemsPopup("tbbUndo");
				Application.DoEvents();
			}

			m_app.SuppressSynchronize();
			try
			{
				for (int i = 0; i <= iClicked; i++)
				{
					if (!OnEditUndo(null))
					{
						if (i == 0)
						{
							MessageBox.Show(ResourceHelper.GetResourceString("kstidUndoFailed"),
								m_app.ApplicationName);
						}
						else
						{
							MessageBox.Show(ResourceHelper.FormatResourceString("kstidUndoFailureReport",
								iClicked + 1, i), m_app.ApplicationName);
						}
						break;
					}
				}
			}
			finally
			{
				using (new WaitCursor(this))
				{
					m_app.ResumeSynchronize();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked an item in the Redo drop down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="iClicked">Zero-based index of clicked item</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnRedoDropDownClicked(object sender, int iClicked)
		{
			if (m_tmAdapter != null)
			{
				m_tmAdapter.HideBarItemsPopup("tbbRedo");
				Application.DoEvents();
			}

			m_app.SuppressSynchronize();
			try
			{
				for (int i = 0; i <= iClicked; i++)
				{
					if (!OnEditRedo(null))
					{
						if (i == 0)
						{
							MessageBox.Show(ResourceHelper.GetResourceString("kstidRedoFailed"),
								m_app.ApplicationName);
						}
						else
						{
							MessageBox.Show(ResourceHelper.FormatResourceString("kstidRedoFailureReport",
								iClicked + 1, i), m_app.ApplicationName);
						}
						break;
					}
				}
			}
			finally
			{
				using (new WaitCursor(this))
				{
					m_app.ResumeSynchronize();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hide the previously active client window and activate the new one
		/// </summary>
		/// <param name="selectedView">client window to switch to</param>
		/// ------------------------------------------------------------------------------------
		public virtual bool SwitchActiveView(ISelectableView selectedView)
		{
			CheckDisposed();

			using (new WaitCursor(this))
			{
				// If the user minimizes the app during startup, we need to restore it before
				// activating the view so it gets laid out correctly.
				if (WindowState == FormWindowState.Minimized)
					WindowState = FormWindowState.Normal;

				if (selectedView == null || selectedView == m_selectedView)
					return true;
				if (selectedView is Control && ((Control)selectedView).FindForm() != this)
				{
					// we don't want to deactivate a view of another main window! Fix for TE-3305
					return false;
				}

				// deactivate the old view if it is not the same as the new one
				if (m_selectedView != null && selectedView != m_selectedView)
					m_selectedView.DeactivateView();

				InformationBarText = selectedView.BaseInfoBarCaption;

				// There is a strange quirk (perhaps bug) where a view's window first appears
				// at 0,0 relative to the FwMainWnd's client area before it pops into its
				// container control. Therefore, we set the default location of the view's
				// window to somewhere outside the viewable area so the user won't see the
				// strange behavior.
				Control view = selectedView as Control;
				if (view != null && view.Location == new Point(0, 0))
					view.Location = new Point(this.Width + 10, 10);

				Logger.WriteEvent("Switching view to " + view.Name + " (" + Text + ")");

				// activate the new view, even if it is the same view so that the focus will come back
				selectedView.ActivateView();
				m_selectedView = selectedView;

				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hide the previously active view and activate the new one (corresponding to the
		/// button clicked in the sidebar (or the menu item chosen).
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual bool OnSwitchActiveView(object args)
		{
			SBTabItemProperties itemProps = args as SBTabItemProperties;
			if (itemProps == null)
				return false;

			ISelectableView selectableView;
			if (itemProps.Tag is ISelectableViewFactory)
				selectableView = ((ISelectableViewFactory)itemProps.Tag).Create(itemProps);
			else
				selectableView = itemProps.Tag as ISelectableView;

			return SwitchActiveView(selectableView);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This event is called by the styles combo box when the user chooses a style from the
		/// combo box.
		/// </summary>
		/// <param name="prevStyle">The previously selected style (not used)</param>
		/// <param name="newStyle">The new style</param>
		/// ------------------------------------------------------------------------------------
		public void StyleChosenFromStylesComboBox(StyleListItem prevStyle, StyleListItem newStyle)
		{
			CheckDisposed();

			// See TE-4675 for some reasons why newStyle might be null.
			if (newStyle == null)
				return;

			Logger.WriteEvent(string.Format("Applying style {0}", newStyle.Name));
			IRootSite rootSite = ActiveView;
			if (rootSite != null)
			{
				Debug.Assert(rootSite.EditingHelper != null);

				Control ctrl = rootSite as Control;
				if (ctrl != null)
					ctrl.Focus();

				if (rootSite.EditingHelper.GetParaStyleNameFromSelection() == newStyle.Name)
					return;

				using (UndoTaskHelper undoHelper = new UndoTaskHelper(rootSite.CastAsIVwRootSite(),
					"kstidUndoStyleChanges"))
				{
					if (newStyle.IsDefaultParaCharsStyle)
						rootSite.EditingHelper.RemoveCharFormatting();
					else
						rootSite.EditingHelper.ApplyStyle(newStyle.Name);
					undoHelper.RollBack = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This event is called by the styles combo box when it needs to know what styles are
		/// in the current selection.
		/// </summary>
		/// <param name="type">Type of style to return</param>
		/// <returns>The name of the style in the current selection or an empty string if there
		/// is more than one style</returns>
		/// ------------------------------------------------------------------------------------
		private string GiveStyleComboCurrStyleName(StyleType type)
		{
			string styleName;
			IRootSite rootSite = ActiveView;
			if (rootSite != null)
			{
				if (type == StyleType.kstCharacter)
				{
					styleName = rootSite.EditingHelper.GetCharStyleNameFromSelection();
					if (styleName == null)
						return string.Empty;
					else if (styleName == string.Empty)
						return ResourceHelper.DefaultParaCharsStyleName;
				}
				else
				{
					styleName = rootSite.EditingHelper.GetParaStyleNameFromSelection();
					if (styleName == null)
						return string.Empty;
				}

				return styleName;
			}
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_writingSystemSelector_SelectedIndexChanged(object sender, EventArgs e)
		{
			var box = sender as ComboBox;
			if (box == null || ActiveView == null)
				return;
			IWritingSystem ws = box.SelectedItem as IWritingSystem;
			if (ws == null)
				return;

			Logger.WriteEvent(string.Format("Applying writing system {0} to current selection", ws.DisplayLabel));
			ActiveView.EditingHelper.ApplyWritingSystem(ws.Handle);

			if (ActiveView is Control)
				((Control)ActiveView).Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a project name change.
		/// </summary>
		/// <param name="sender">The FDO cache (should be the same as our member).</param>
		/// ------------------------------------------------------------------------------------
		private void ProjectNameChanged(FdoCache sender)
		{
			Debug.Assert(sender == m_cache);
			UpdateCaptionBar();
		}
		#endregion

		#region overriden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:Layout"/> event.
		/// </summary>
		/// <param name="levent">The <see cref="T:System.Windows.Forms.LayoutEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);

			// The window's size and location used to be set in the persistence object's
			// EndInit method (which gets called before PerformLayout). However, that was
			// too early because when PerformLayout is called, auto scaling takes place
			// which adjusts the size and position if the form was designed in a different
			// screen DPI from that in which the program is running, thus causing the
			// persisted (in the registry) window size and location to be changed every
			// time the application starts up (See TE-6698).
			if (m_persistence != null &&
				levent.AffectedControl == this && levent.AffectedProperty == "Visible")
			{
				m_persistence.LoadWindowPosition();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cancels the closing if there is some data update in progress.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			// Even with backup/restore in progress, we may want to close this window.
			// See TE-7713.
			e.Cancel = DataUpdateMonitor.IsUpdateInProgress();

			if (!e.Cancel)
				base.OnClosing(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This main window is now the active one, so he has to take over ownership of the
		/// find/replace dialog. Also a good time to handle any data changes made in other
		/// applications. Also makes our undo stack active.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnActivated(EventArgs e)
		{
			// We've had some crashes on shutdown because of events being processed after this
			// has been disposed. Debated about calling base method, but wasn't sure about
			// other side effects that may happen. May get this call when Dispose is in
			// progress, so added test for m_delegate also.
			if (IsDisposed || m_delegate == null)
				return;

			base.OnActivated(e);

			m_delegate.OnActivated();

			if (m_app != null && Visible)
				HandleActivation();
			else
			{
				//TE-4914, et al: This code caused the shortcut keys in TE to quit working
				//after a modal dialog was brought up. If you manually set focus to another view
				//and then set it back again, the short cut keys would work again.
				//The reason it quit working is that we were returning before calling
				//base.OnActivated, which was called as a result of a closing modal dialg.
				//We could find no way to detect the case of a closing modal dialog (when we
				//actually didn't want to activate the child).
				//Calling base.OnActivated in addition to calling child.Activate(), caused an
				//annoying blinking when the child was activated, it caused the main window to get
				//re-activated, which then tried to activate the child, etc.

				//// When a main window is activated and another window has a modal dialog open,
				//// then activate the modal dialog instead of this window. LT-1627, TE-3307
				//foreach (Form form in m_app.MainWindows)
				//{
				//    foreach (Form child in form.OwnedForms)
				//    {
				//        // If the child window is in the process of closing, we're getting
				//        // activated. If we now activate the child again this results in the
				//        // main window being activated twice which is distracting. A solution
				//        // is to disable the child window in its OnClosing() method and check
				//        // for that condition here.
				//        if (child.Modal && child.Enabled)
				//        {
				//            child.Activate();
				//            return;
				//        }
				//    }
				//}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void HandleActivation()
		{
			FwFindReplaceDlg findReplaceDlg = m_app.FindReplaceDialog;
			if (findReplaceDlg != null && ActiveView != null)
				findReplaceDlg.SetOwner(ActiveView.CastAsIVwRootSite(), this, m_app.FindPattern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			using (new WaitCursor(this))
			{
				base.OnResize(e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (DesignMode)
				return;

			if (m_cache == null)
				throw new InvalidOperationException("No database connection exists");

			if (m_cboZoomPercent != null)
				m_cboZoomPercent.Text = ZoomString;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disable the selection in the current view.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDeactivate(EventArgs e)
		{
			base.OnDeactivate(e);

			// FWR-1686: Added check to see whether the active view is/has a rootsite yet in order
			// to prevent debug assertion failures due to re-entrancy during initial layout.
			if (ActiveView != null && ActiveView.CastAsIVwRootSite() != null)
			{
				// Comment recovered from old version (seems to still be relevant -- see TE-3977):
				// If any of the combo boxes in the toolbar has focus we want to put the focus
				// to the active view again before we get deactivated, so that the user can
				// continue to work in the view when he returns to the app. This mimics the
				// behavior of Word.
				if (!(ActiveControl is SimpleRootSite))
				{
					Control mainView = ActiveView as Control;
					if (mainView != null)
						mainView.Focus();
				}
			}
		}
		#endregion

		#region Zoom ComboBox handling stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the zoom multiplier
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float ZoomMultiplier
		{
			get { return ActiveView is SimpleRootSite ? ((SimpleRootSite)ActiveView).Zoom : 1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the zoom percentage
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ZoomPercentage
		{
			get
			{
				CheckDisposed();
				return (int)Math.Round(ZoomMultiplier * 100);
			}
			set
			{
				CheckDisposed();

				SimpleRootSite rootSite = ActiveView as SimpleRootSite;
				float oldZoom = 1;
				float zoom = (float)value / 100;
				if (rootSite != null)
				{
					oldZoom = rootSite.Zoom;
					rootSite.Zoom = zoom;

					if (m_cboZoomPercent != null)
						m_cboZoomPercent.Text = ZoomString;
				}
				else
					Debug.WriteLine("Don't know how to get or set zoom factor for this kind of view");
				OnZoomPercentageChanged(oldZoom, zoom);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the zoom percentage changed.
		/// </summary>
		/// <param name="oldZoomPercentage">The previous zoom percentage.</param>
		/// <param name="newZoomPercentage">The new zoom percentage.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnZoomPercentageChanged(float oldZoomPercentage,
			float newZoomPercentage)
		{
			if (ZoomPercentageChanged != null)
			{
				ZoomPercentageChanged(this, new ZoomPercentageChangedEventArgs(
					new SizeF(oldZoomPercentage, oldZoomPercentage),
					new SizeF(newZoomPercentage, newZoomPercentage)));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string that should be displayed in the zoom combo box (i.e. has the
		/// zoom percentage number with the following percent sign, or whatever the localized
		/// version of that is).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ZoomString
		{
			get
			{
				CheckDisposed();
				return String.Format(ResourceHelper.GetResourceString("ksPercentage"),
					ZoomPercentage.ToString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User has changed the zoom percentage with the drop down list.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">An <see cref="T:EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void ZoomSelectionChanged(object sender, EventArgs e)
		{
			ZoomChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User has pressed a key in the zoom percentage control
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">An <see cref="T:EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void ZoomKeyPress(object sender, KeyPressEventArgs e)
		{
			// Check if the enter key was pressed.
			if (e.KeyChar == '\r')
				ZoomChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The zoom control has lost focus
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">An <see cref="T:EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void ZoomLostFocus(object sender, EventArgs e)
		{
			ZoomChanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ZoomChanged()
		{
			int newZoomValue;

			if (ZoomTextValid(out newZoomValue) && newZoomValue != ZoomPercentage)
			{
				ZoomPercentage = newZoomValue;
				if (ActiveView != null)
					((Control)ActiveView).Focus();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validate the zoom text
		/// </summary>
		/// <param name="newVal">The new value if valid; otherwise the previous value</param>
		/// <returns><c>true</c> if valid; otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		private bool ZoomTextValid(out int newVal)
		{
			// This should only be null during test running.
			if (m_cboZoomPercent == null)
			{
				newVal = 100;
				return true;
			}

			// If the text is typed from the keyboard then use the Text property. Otherwise
			// use the SelectedItem property since it will reflect the item chosen from the
			// drop-down portion of the combo box.
			string s = (m_cboZoomPercent.SelectedItem == null ?
				m_cboZoomPercent.Text :	((string)m_cboZoomPercent.SelectedItem));

			s = s.Trim(new char[] {' ', '%'});

			try
			{
				newVal = Convert.ToInt32(s);

				// Limit the valid zoom percentage to between 25% and 1000%
				if (newVal < 25)
					newVal = 25;
				if (newVal > 1000)
					newVal = 1000;
			}
			catch
			{
				newVal = ZoomPercentage;
				return false;
			}

			return true;
		}
		#endregion

		#region Styles/WS handling stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reloads all the writing systems in the writing system selector combo box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateWritingSystemsInCombobox()
		{
			CheckDisposed();

			if(m_writingSystemSelector == null)
				return;
			m_writingSystemSelector.Items.Clear();
			m_writingSystemSelector.Items.AddRange(m_cache.ServiceLocator.WritingSystems.AllWritingSystems.ToArray());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reload the styles combo box and update it's current value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitStyleComboBox()
		{
			CheckDisposed();

			m_delegate.InitStyleComboBox();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the styles combo boxes on the formatting toolbar, with the correct style name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateStyleComboBoxValue(IRootSite rootsite)
		{
			CheckDisposed();

			m_delegate.UpdateStyleComboBoxValue(rootsite);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the Writiing System selector combo box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateWritingSystemSelectorForSelection(IVwRootBox rootbox)
		{
			CheckDisposed();

			m_delegate.UpdateWritingSystemSelectorForSelection(rootbox);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when styles are renamed or deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void OnStylesRenamedOrDeleted()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the Format Apply Style dialog
		/// </summary>
		/// <param name="paraStyleName">The currently-selected Paragraph style name</param>
		/// <param name="charStyleName">The currently-selected Character style name</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void ShowApplyStyleDialog(string paraStyleName, string charStyleName)
		{
			m_delegate.ShowApplyStyleDialog(paraStyleName, charStyleName, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current style names from the selected text
		/// </summary>
		/// <param name="paraStyleName">Name of the para style.</param>
		/// <param name="charStyleName">Name of the char style.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void GetCurrentStyleNames(out string paraStyleName, out string charStyleName)
		{
			paraStyleName = m_paraStyleListHelper == null ? null : m_paraStyleListHelper.SelectedStyleName;
			charStyleName = m_charStyleListHelper == null ? null : m_charStyleListHelper.SelectedStyleName;
		}
		#endregion

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Not used
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configurationParameters">The configuration parameters.</param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the possible message targets, i.e. the view(s) we are showing
		/// </summary>
		/// <returns>Message targets</returns>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			// return list of view windows with focused window being the first one
			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			foreach (Control control in m_rgClientViews.Values)
			{
				IxCoreColleague view = control as IxCoreColleague;
				if (view != null && control != null)
				{
					if (control.Focused || control == ActiveView)
						targets.InsertRange(0, view.GetMessageTargets());
					else if (control.Visible || control.ContainsFocus)
						targets.AddRange(view.GetMessageTargets());
				}
			}
			targets.Add(this);
			return targets.ToArray();
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}
		#endregion

		#region IFwMainWndSettings Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a registry key where user settings can be saved/loaded which are specific to
		/// the main window and the current project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegistryKey MainWndSettingsKey
		{
			get
			{
				CheckDisposed();
				if (m_app != null)
					return m_app.ProjectSpecificSettingsKey.CreateSubKey(Name);

				Debug.Assert(MiscUtils.RunningTests);
				return FwRegistryHelper.FieldWorksRegistryKey;
			}
		}

		#endregion
	}

	#region ISelectableViewFactory interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Allows the creation of an ISelectableView.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ISelectableViewFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an ISelectableView
		/// </summary>
		/// <param name="tabItem">The tab item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		ISelectableView Create(SBTabItemProperties tabItem);
	}
	#endregion

	#region ISelectableView interface
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface defines the common behaviors (in addition to being some kind
	/// of UserControl) that are required for windows which can function as the main
	/// data panel (client window) of an FwMainWindow.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public interface ISelectableView
	{
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Activate the view" make it the current one...includes at least showing it.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		void ActivateView();

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Deactivate the view...app is closing or some other view is becoming active...
		/// at least, hide it.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		void DeactivateView();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the base caption string for the info bar. Individual views may want
		/// to get this property to build the caption for their info bar. For example, they may
		/// want to add a Scripture reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string BaseInfoBarCaption
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Name { get; }
	}
	#endregion

	#region ZoomPercentageChangedEventArgs class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ZoomPercentageChangedEventArgs : EventArgs
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ZoomPercentageChangedEventArgs"/> class.
		/// </summary>
		/// <param name="oldZoomFactor">The previous zoom factor.</param>
		/// <param name="newZoomFactor">The new zoom factor.</param>
		/// ------------------------------------------------------------------------------------
		public ZoomPercentageChangedEventArgs(SizeF oldZoomFactor, SizeF newZoomFactor)
		{
			OldZoomFactor = oldZoomFactor;
			NewZoomFactor = newZoomFactor;
		}

		/// <summary>Gets the previous zoom factor</summary>
		public SizeF OldZoomFactor;
		/// <summary>Gets the new zoom factor</summary>
		public SizeF NewZoomFactor;
	}
	#endregion
}

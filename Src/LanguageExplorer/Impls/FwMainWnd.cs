// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using LanguageExplorer.Archiving;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.Controls.SilSidePane;
using LanguageExplorer.SendReceive;
using LanguageExplorer.UtilityTools;
using LanguageExplorer.Works;
using SIL.Collections;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.IO;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Reporting;
using SIL.Utils;
using File = System.IO.File;
using FileUtils = SIL.LCModel.Utils.FileUtils;
using Win32 = SIL.FieldWorks.Common.FwUtils.Win32;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Main window class for FW/FLEx.
	/// </summary>
	/// <remarks>
	/// The main control for this window is an instance of CollapsingSplitContainer (mainContainer data member).
	/// "mainContainer" holds the SidePane (_sidePane) in its Panel1/FirstControl (left side).
	/// It holds a tool-specific control in its right side (Panel2/SecondControl).
	/// </remarks>
	[Export(typeof(IFwMainWnd))]
	internal sealed partial class FwMainWnd : Form, IFwMainWnd
	{
		[Import]
		private IAreaRepository _areaRepository;
		[Import]
		private IPropertyTable _propertyTable;
		[Import]
		private IPublisher _publisher;
		[Import]
		private ISubscriber _subscriber;
		[Import]
		private IFlexApp _flexApp;
		static bool _inUndoRedo; // true while executing an Undo/Redo command.
		private bool _windowIsCopy;
		private FwLinkArgs _startupLink;
		// Used to count the number of times we've been asked to suspend Idle processing.
		private int _countSuspendIdleProcessing;
		/// <summary>
		///  Web browser to use in Linux
		/// </summary>
		private string _webBrowserProgramLinux = "firefox";
		private IRecordListRepositoryForTools _recordListRepositoryForTools;
		private ActiveViewHelper _viewHelper;
		private IArea _currentArea;
		private ITool _currentTool;
		private LcmStyleSheet _stylesheet;
		private DateTime _lastToolChange = DateTime.MinValue;
		private readonly HashSet<string> _toolsReportedToday = new HashSet<string>();
		private bool _persistWindowSize;
		private ParserMenuManager _parserMenuManager;
		private DataNavigationManager _dataNavigationManager;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private SendReceiveMenuManager _sendReceiveMenuManager;
		private WritingSystemListHandler _writingSystemListHandler;
		private CombinedStylesListHandler _combinedStylesListHandler;
		private LinkHandler _linkHandler;

		/// <summary>
		/// Create new instance of window.
		/// </summary>
		public FwMainWnd()
		{
			InitializeComponent();
		}

		private static bool SetupCrashDetectorFile()
		{
			if (File.Exists(CrashOnStartupDetectorPathName))
			{
				return true;
			}

			// Create the crash detector file for next time.
			// Make sure the folder exists first.
			Directory.CreateDirectory(CrashOnStartupDetectorPathName.Substring(0, CrashOnStartupDetectorPathName.LastIndexOf(Path.DirectorySeparatorChar)));
			using (var writer = File.CreateText(CrashOnStartupDetectorPathName))
			{
				writer.Close();
			}
			return false;
		}

		private void SetupWindowSizeIfNeeded(Size restoreSize)
		{
			if (restoreSize == Size)
			{
				return;
			}

			// It will be the same as what is now in the file and the prop table,
			// so skip updating the table.
			_persistWindowSize = false;
			Size = restoreSize;
			_persistWindowSize = true;
		}

		private void SetupStylesheet()
		{
			_stylesheet = new LcmStyleSheet();
			_stylesheet.Init(Cache, Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
		}

		private void SetupParserMenuItems()
		{
			var parserMenuItems = new Dictionary<string, ToolStripMenuItem>(12)
			{
				{"parser", _parserToolStripMenuItem},
				{"parseAllWords", _parseAllWordsToolStripMenuItem},
				{"reparseAllWords", _reparseAllWordsToolStripMenuItem},
				{"reloadGrammarLexicon", _reloadGrammarLexiconToolStripMenuItem},
				{"stopParser", _stopParserToolStripMenuItem},
				{"tryAWord", _tryAWordToolStripMenuItem},
				{"parseWordsInText", _parseWordsInTextToolStripMenuItem},
				{"parseCurrentWord", _parseCurrentWordToolStripMenuItem},
				{"clearCurrentParserAnalyses", _clearCurrentParserAnalysesToolStripMenuItem},
				{"defaultParserXAmple", _defaultParserXAmpleToolStripMenuItem},
				{"phonologicalRulebasedParserHermitCrab", _phonologicalRulebasedParserHermitCrabNETToolStripMenuItem},
				{"editParserParameters", _editParserParametersToolStripMenuItem}
			};
			_parserMenuManager = new ParserMenuManager(_statusbar.Panels[LanguageExplorerConstants.StatusBarPanelProgress], parserMenuItems);
		}

		/// <summary>
		/// Gets the pathname of the "crash detector" file.
		/// The file is created at app startup, and deleted at app exit,
		/// so if it exists before being created, the app didn't close properly,
		/// and will start without using the saved settings.
		/// </summary>
		private static string CrashOnStartupDetectorPathName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"SIL",
			"FieldWorks",
			"CrashOnStartupDetector.tmp");

		private void RestoreWindowSettings(bool wasCrashDuringPreviousStartup)
		{
			const string id = "Language Explorer";
			var useExtantPrefs = ModifierKeys != Keys.Shift; // Holding shift key means don't use extant preference file, no matter what.
			if (useExtantPrefs)
			{
				// Tentatively RestoreProperties, but first check for a previous crash...
				if (wasCrashDuringPreviousStartup && (Environment.ExpandEnvironmentVariables("%FwSkipSafeMode%") == "%FwSkipSafeMode%"))
				{
					// Note: The environment variable will not exist at all to get here.
					// A non-developer user.
					// Display a message box asking users if they
					// want to  revert to factory settings.
					ShowInTaskbar = true;
					var activeForm = ActiveForm;
					if (activeForm == null)
						useExtantPrefs = (MessageBox.Show(ActiveForm, LanguageExplorerResources.SomethingWentWrongLastTime,
							id,
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Question) != DialogResult.Yes);
					else
					{
						// Make sure as far as possible it comes up in front of any active window, including the splash screen.
						activeForm.Invoke((Action)(() =>
							useExtantPrefs = (MessageBox.Show(LanguageExplorerResources.SomethingWentWrongLastTime,
								id,
								MessageBoxButtons.YesNo,
								MessageBoxIcon.Question) != DialogResult.Yes)));
					}
				}
			}
			if (useExtantPrefs)
			{
				RestoreProperties();
			}
			else
			{
				DiscardProperties();
			}

			FormWindowState state;
			if (PropertyTable.TryGetValue("windowState", SettingsGroup.GlobalSettings, out state) && state != FormWindowState.Minimized)
			{
				WindowState = state;
			}

			Point persistedLocation;
			if (PropertyTable.TryGetValue("windowLocation", SettingsGroup.GlobalSettings, out persistedLocation))
			{
				Location = persistedLocation;
				//the location restoration only works if the window startposition is set to "manual"
				//because the window is not visible yet, and the location will be changed
				//when it is Show()n.
				StartPosition = FormStartPosition.Manual;
			}

			Size persistedSize;
			if (!PropertyTable.TryGetValue("windowSize", SettingsGroup.GlobalSettings, out persistedSize))
			{
				return;
			}
			_persistWindowSize = false;
			try
			{
				Size = persistedSize;
			}
			finally
			{
				_persistWindowSize = true;
			}
		}

		private void SaveWindowSettings()
		{
			if (!_persistWindowSize)
				return;
			// Don't bother storing anything if we are maximized or minimized.
			// if we did, then when the user exits the application and then runs it again,
			//then switches to the normal state, we would be switching to 0,0 or something.
			if (WindowState != FormWindowState.Normal)
			{
				return;
			}

			PropertyTable.SetProperty("windowState", WindowState, SettingsGroup.GlobalSettings, true, false);
			PropertyTable.SetProperty("windowLocation", Location, SettingsGroup.GlobalSettings, true, false);
			PropertyTable.SetProperty("windowSize", Size, SettingsGroup.GlobalSettings, true, false);
		}

		/// <summary>
		/// Restore properties persisted for the mediator.
		/// </summary>
		private void RestoreProperties()
		{
			PropertyTable.RestoreFromFile(PropertyTable.GlobalSettingsId);
			PropertyTable.RestoreFromFile(PropertyTable.LocalSettingsId);
			var hcSettings = PropertyTable.GetValue<string>("HomographConfiguration");
			if (string.IsNullOrEmpty(hcSettings))
			{
				return;
			}
			var hc = Cache.ServiceLocator.GetInstance<HomographConfiguration>();
			hc.PersistData = hcSettings;
		}

		/// <summary>
		/// If we are discarding saved settings, we must not keep any saved sort sequences,
		/// as they may represent a filter we are not restoring (LT-11647)
		/// </summary>
		private void DiscardProperties()
		{
			var tempDirectory = Path.Combine(Cache.ProjectId.ProjectFolder, LcmFileHelper.ksSortSequenceTempDir);
			DirectoryUtilities.DeleteDirectoryRobust(tempDirectory);
		}

		private void SetupOutlookBar()
		{
			mainContainer.SuspendLayout();

			mainContainer.Tag = "SidebarWidthGlobal";
			mainContainer.Panel1MinSize = CollapsingSplitContainer.kCollapsedSize;
			mainContainer.Panel1Collapsed = false;
			mainContainer.Panel2Collapsed = false;
			var sd = PropertyTable.GetValue<int>("SidebarWidthGlobal", SettingsGroup.GlobalSettings);
			if (!mainContainer.Panel1Collapsed)
			{
				SetSplitContainerDistance(mainContainer, sd);
			}
			_sidePane.Initalize(_areaRepository);

			mainContainer.ResumeLayout(false);
		}

		private void Tool_Clicked(Item itemClicked)
		{
			var clickedTool = (ITool)itemClicked.Tag;
			if (_currentTool == clickedTool)
			{
				_currentArea.ActiveTool = clickedTool;
				return;  // Not much else to do.
			}

			ClearDuringTransition();
			_currentArea.ActiveTool = null;
			_currentTool?.Deactivate(_majorFlexComponentParameters);
			_currentTool = clickedTool;
			_currentArea.ActiveTool = clickedTool;
			var areaName = _currentArea.MachineName;
			var toolName = _currentTool.MachineName;
			PropertyTable.SetProperty($"{AreaServices.ToolForAreaNamed_}{areaName}", toolName, SettingsGroup.LocalSettings, true, false);
			PropertyTable.SetProperty(AreaServices.ToolChoice, _currentTool.MachineName, SettingsGroup.LocalSettings, true, false);

			// Do some logging.
			Logger.WriteEvent("Switched to " + _currentTool.MachineName);
			// Should we report a tool change?
			if (_lastToolChange.Date != DateTime.Now.Date)
			{
				// New day has dawned (or just started up). Reset tool reporting.
				_toolsReportedToday.Clear();
				_lastToolChange = DateTime.Now;
			}
			if (!_toolsReportedToday.Contains(toolName))
			{
				_toolsReportedToday.Add(toolName);
				UsageReporter.SendNavigationNotice("SwitchToTool/{0}/{1}", areaName, toolName);
			}

			_currentTool.Activate(_majorFlexComponentParameters);
		}

		private void Area_Clicked(Tab tabClicked)
		{
			var clickedArea = (IArea)tabClicked.Tag;
			if (_currentArea == clickedArea)
			{
				_currentArea.ActiveTool = null;
				return; // Not much else to do.
			}

			ClearDuringTransition();

			if (_currentArea != null)
			{
				_currentArea.Deactivate(_majorFlexComponentParameters);
				_currentArea.ActiveTool = null;
			}
			_currentArea = clickedArea;
			PropertyTable.SetProperty(AreaServices.AreaChoice, _currentArea.MachineName, SettingsGroup.LocalSettings, true, false);
			_currentArea.Activate(_majorFlexComponentParameters);
		}

		private void ClearDuringTransition()
		{
			// NB: If you are ever tempted to not set the record list to null, then be prepared to have each area/tool clear it.
			RecordListServices.SetRecordList(null);
			StatusBarPanelServices.ClearBasicStatusBars(_statusbar);
		}

		private void SetupPropertyTable()
		{
			LoadPropertyTable();
			SetDefaultProperties();
			SetTemporaryProperties();
			RemoveObsoleteProperties();
		}

		private void LoadPropertyTable()
		{
			PropertyTable.UserSettingDirectory = LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
			PropertyTable.LocalSettingsId = "local";

			if (!Directory.Exists(PropertyTable.UserSettingDirectory))
			{
				Directory.CreateDirectory(PropertyTable.UserSettingDirectory);
			}
			PropertyTable.RestoreFromFile(PropertyTable.GlobalSettingsId);
			PropertyTable.RestoreFromFile(PropertyTable.LocalSettingsId);
		}

		private void ConvertOldPropertiesToNewIfPresent()
		{
			const int currentPropertyTableVersion = 10;
			if (PropertyTable.GetValue<int>("PropertyTableVersion") == currentPropertyTableVersion)
			{
				return;
			}
			string oldStringValue;
			if (PropertyTable.TryGetValue("currentContentControl", out oldStringValue))
			{
				PropertyTable.RemoveProperty("currentContentControl");
				PropertyTable.SetProperty(AreaServices.ToolChoice, oldStringValue, SettingsGroup.LocalSettings, true, false);
			}
			PropertyTable.SetProperty("PropertyTableVersion", currentPropertyTableVersion, SettingsGroup.GlobalSettings, true, false);
		}

		/// <summary>
		/// Just in case some expected property hasn't been reloaded,
		/// see that they are loaded. "SetDefault" doesn't mess with them if they are restored.
		/// </summary>
		/// <remarks>NB: default properties of interest to areas/tools are handled by them.</remarks>
		private void SetDefaultProperties()
		{
			// This "PropertyTableVersion" property will control the beahvior of the "ConvertOldPropertiesToNewIfPresent" method.
			PropertyTable.SetDefault("PropertyTableVersion", int.MinValue, SettingsGroup.GlobalSettings, true, false);
			// This is the splitter distance for the sidebar/secondary splitter pair of controls.
			PropertyTable.SetDefault("SidebarWidthGlobal", 140, SettingsGroup.GlobalSettings, true, false);
			// This is the splitter distance for the record list/main content pair of controls.
			PropertyTable.SetDefault("RecordListWidthGlobal", 200, SettingsGroup.GlobalSettings, true, false);

			PropertyTable.SetDefault(AreaServices.InitialArea, AreaServices.InitialAreaMachineName, SettingsGroup.LocalSettings, true, false);
			// Set these properties so they don't get set the first time they're accessed in a browse view menu. (cf. LT-2789)
			PropertyTable.SetDefault("SortedFromEnd", false, SettingsGroup.LocalSettings, true, false);
			PropertyTable.SetDefault("SortedByLength", false, SettingsGroup.LocalSettings, true, false);
			PropertyTable.SetDefault("CurrentToolbarVersion", 1, SettingsGroup.GlobalSettings, true, false);
			PropertyTable.SetDefault("SuspendLoadingRecordUntilOnJumpToRecord", string.Empty, SettingsGroup.LocalSettings, false, false);
			// This property can be used to set the settingsGroup for context dependent properties. No need to persist it.
			PropertyTable.SetDefault("SliceSplitterBaseDistance", -1, SettingsGroup.LocalSettings, false, false);
			// Common to several tools in various areas, so set them here.
			PropertyTable.SetDefault("AllowInsertLinkToFile", false, SettingsGroup.LocalSettings, false, false);
			PropertyTable.SetDefault("AllowShowNormalFields", false, SettingsGroup.LocalSettings, false, false);
#if RANDYTODO
// TODO DataTree processes both of these:
// TODO:	1. "ShowHiddenFields" is used by a View menu item.
// TODO:	2. "ShowHiddenFields-someToolName" is used by PanelButton.
// TODO: Remove this property, when the menu is set up.
#endif
			PropertyTable.SetDefault("ShowHiddenFields", false, SettingsGroup.LocalSettings, false, false);
		}

		private void RemoveObsoleteProperties()
		{
			// Get rid of obsolete properties, if they were restored.
			PropertyTable.RemoveProperty("ShowRecordList", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("PreferredUILibrary");
			PropertyTable.RemoveProperty("ShowSidebar", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("SidebarLabel", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("DoingAutomatedTest", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("RecordListLabel", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("MainContentLabel", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("StatusPanelRecordNumber");
			PropertyTable.RemoveProperty("StatusPanelMessage");
			PropertyTable.RemoveProperty("StatusBarPanelFilter");
			PropertyTable.RemoveProperty("StatusBarPanelSort");
			PropertyTable.RemoveProperty("DialogFilterStatus");
			PropertyTable.RemoveProperty("IgnoreStatusPanel");
			PropertyTable.RemoveProperty("DoLog");
			PropertyTable.RemoveProperty("ShowBalloonHelp");
			PropertyTable.RemoveProperty("ShowMorphBundles");
			PropertyTable.RemoveProperty("ActiveClerk");
			PropertyTable.RemoveProperty("SelectedWritingSystemHvosForCurrentContextMenu");

			ConvertOldPropertiesToNewIfPresent();
		}

		private void SetTemporaryProperties()
		{
			_temporaryPropertyNames.Clear();
			// Not persisted, but needed at runtime.
			PropertyTable.SetProperty("window", this, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("App", _flexApp, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("cache", Cache, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("HelpTopicProvider", _flexApp, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("FlexStyleSheet", _stylesheet, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("LinkHandler", _linkHandler, SettingsGroup.BestSettings, false, false);
			_temporaryPropertyNames.AddRange(new[] { "window", "App", "cache", "HelpTopicProvider", "FlexStyleSheet", "LinkHandler" });
		}
		private readonly HashSet<string> _temporaryPropertyNames = new HashSet<string>();

		private void RemoveTemporaryProperties()
		{
			foreach (var temporaryPropertyName in _temporaryPropertyNames)
			{
				PropertyTable.RemoveProperty(temporaryPropertyName);
			}
			_temporaryPropertyNames.Clear();
		}

		private void RegisterSubscriptions()
		{
			Subscriber.Subscribe("MigrateOldConfigurations", MigrateOldConfigurations);
		}

		private void MigrateOldConfigurations(object newValue)
		{
			var configMigrator = new DictionaryConfigurationMigrator();
			configMigrator.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
			configMigrator.MigrateOldConfigurationsIfNeeded();
		}

		private void SetupCustomStatusBarPanels()
		{
			_statusbar.SuspendLayout();
			// Insert first, so it ends up last in the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarTextBox(_statusbar)
			{
				TextForReal = "Filter",
				Name = LanguageExplorerConstants.StatusBarPanelFilter,
				MinWidth = 40,
				AutoSize = StatusBarPanelAutoSize.Contents
			});

			// Insert second, so it ends up in the middle of the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarTextBox(_statusbar)
			{
				TextForReal = "Sort",
				Name = LanguageExplorerConstants.StatusBarPanelSort,
				MinWidth = 40,
				AutoSize = StatusBarPanelAutoSize.Contents
			});

			// Insert last, so it ends up first in the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarProgressPanel(_statusbar)
			{
				Name = LanguageExplorerConstants.StatusBarPanelProgressBar,
				MinWidth = 150,
				AutoSize = StatusBarPanelAutoSize.Contents
			});
			_statusbar.ResumeLayout(true);
		}

		private static void SetSplitContainerDistance(SplitContainer splitCont, int pixels)
		{
			if (splitCont.SplitterDistance != pixels)
			{
				splitCont.SplitterDistance = pixels;
			}
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (!_persistWindowSize)
				return;

			// Don't bother storing the size if we are maximized or minimized.
			// If we did, then when the user exits the application and then runs it again,
			//then switches to the normal state, we would be switching to a bizarre size.
			if (WindowState == FormWindowState.Normal)
			{
				PropertyTable.SetProperty("windowSize", Size, SettingsGroup.GlobalSettings, true, false);
			}
		}

		protected override void OnMove(EventArgs e)
		{
			base.OnMove(e);

			if (!_persistWindowSize)
				return;
			// Don't bother storing the location if we are maximized or minimized.
			// if we did, then when the user exits the application and then runs it again,
			//then switches to the normal state, we would be switching to 0,0 or something.
			if (WindowState == FormWindowState.Normal)
			{
				PropertyTable.SetProperty("windowLocation", Location, SettingsGroup.GlobalSettings, true, false);
			}
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		public void SaveSettings()
		{
			SaveWindowSettings();

			// Have current IArea put any needed properties into the table.
			_currentArea.EnsurePropertiesAreCurrent();

			// Have current ITool put any needed properties into the table.
			_currentTool.EnsurePropertiesAreCurrent();

			// first save global settings, ignoring database specific ones.
			PropertyTable.SaveGlobalSettings();

			// now save database specific settings.
			PropertyTable.SaveLocalSettings();
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call PropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable => _propertyTable;

		#endregion

		#region Implementation of IRecordListOwner

		/// <summary>Find the IRecordListUpdater object with the given name.</summary>
		public IRecordListUpdater FindRecordListUpdater(string name)
		{
			return PropertyTable.GetValue<IRecordListUpdater>(name, null);
		}

		#endregion

		#region Implementation of IFwMainWnd

		/// <summary>
		/// Gets the active view of the window
		/// </summary>
		public IRootSite ActiveView => _viewHelper.ActiveView;

		/// <summary>
		/// Gets the focused control of the window
		/// </summary>
		public Control FocusedControl
		{
			get
			{
				Control focusControl = null;
				if (PropertyTable.PropertyExists("FirstControlToHandleMessages"))
				{
					focusControl = PropertyTable.GetValue<Control>("FirstControlToHandleMessages");
				}
				if (focusControl == null || focusControl.IsDisposed)
				{
					focusControl = FromHandle(Win32.GetFocus());
				}
				return focusControl;
			}
		}

		/// <summary>
		/// Gets the data object cache.
		/// </summary>
		public LcmCache Cache => _flexApp.Cache;

		/// <summary>
		/// Get the specified main menu
		/// </summary>
		/// <param name="menuName"></param>
		/// <returns>Return the specified main menu, or null if not found.</returns>
		public ToolStripMenuItem GetMainMenu(string menuName)
		{
			ToolStripMenuItem retval = null;
			if (_menuStrip.Items.ContainsKey(menuName))
			{
				retval = (ToolStripMenuItem)_menuStrip.Items[menuName];
			}
			return retval;
		}

		/// <summary>
		/// Initialize the window, before being shown.
		/// </summary>
		/// <remarks>
		/// This allows for creating all sorts of things used by the implementation.
		/// </remarks>
		public void Initialize(bool windowIsCopy = false, FwLinkArgs linkArgs = null)
		{
			_windowIsCopy = windowIsCopy;
			_startupLink = linkArgs; // May be null.

			var wasCrashDuringPreviousStartup = SetupCrashDetectorFile();

			var flexComponentParameters = new FlexComponentParameters(PropertyTable, Publisher, Subscriber);
			SetupCustomStatusBarPanels();

			projectLocationsToolStripMenuItem.Enabled = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.CanWriteKey();
			archiveWithRAMPSILToolStripMenuItem.Enabled = ReapRamp.Installed;

			_viewHelper = new ActiveViewHelper(this);
			_linkHandler = new LinkHandler(this, Cache, toolStripButtonHistoryBack, toolStripButtonHistoryForward, copyLocationAsHyperlinkToolStripMenuItem);
			_linkHandler.InitializeFlexComponent(flexComponentParameters);

			SetupStylesheet();
			SetupPropertyTable();
			RegisterSubscriptions();

			_dataNavigationManager = new DataNavigationManager(
				new Dictionary<Navigation, Tuple<ToolStripMenuItem, ToolStripButton>>
				{
					{Navigation.First, new Tuple<ToolStripMenuItem, ToolStripButton>(_data_First, _tsbFirst)},
					{Navigation.Previous, new Tuple<ToolStripMenuItem, ToolStripButton>(_data_Previous, _tsbPrevious)},
					{Navigation.Next, new Tuple<ToolStripMenuItem, ToolStripButton>(_data_Next, _tsbNext)},
					{Navigation.Last, new Tuple<ToolStripMenuItem, ToolStripButton>(_data_Last, _tsbLast)}
				});

			RestoreWindowSettings(wasCrashDuringPreviousStartup);
			var restoreSize = Size;

			_recordListRepositoryForTools = new RecordListRepository(Cache, flexComponentParameters);
			_writingSystemListHandler = new WritingSystemListHandler(this, Cache, Subscriber, toolStripComboBoxWritingSystem, writingSystemToolStripMenuItem);
			_combinedStylesListHandler = new CombinedStylesListHandler(this, Subscriber, _stylesheet, toolStripComboBoxStyles);

			SetupParserMenuItems();

			_majorFlexComponentParameters = new MajorFlexComponentParameters(
				mainContainer,
				_menuStrip,
				toolStripContainer,
				_statusbar,
				_parserMenuManager,
				_dataNavigationManager,
				_recordListRepositoryForTools,
				flexComponentParameters,
				Cache,
				_flexApp,
				this,
				_sidePane);

			RecordListServices.Setup(_majorFlexComponentParameters);

			// Most tools show it, but let them deal with it and its event handler.
			var fileExportMenu = MenuServices.GetFileExportMenu(_majorFlexComponentParameters.MenuStrip);
			fileExportMenu.Visible = false;
			fileExportMenu.Enabled = false;

			_parserMenuManager.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);

			IdleQueue = new IdleQueue();

			_sendReceiveMenuManager = new SendReceiveMenuManager(IdleQueue, this, _flexApp, Cache, _sendReceiveToolStripMenuItem, toolStripButtonFlexLiftBridge);
			_sendReceiveMenuManager.InitializeFlexComponent(flexComponentParameters);

			SetupOutlookBar();

			SetWindowTitle();

			SetupWindowSizeIfNeeded(restoreSize);

			if (File.Exists(CrashOnStartupDetectorPathName)) // Have to check again, because unit test check deletes it in the RestoreWindowSettings method.
			{
				File.Delete(CrashOnStartupDetectorPathName);
			}

			Application.Idle += Application_Idle;
		}

		/// <summary>
		/// Gets a Rectangle representing the position and size of the window in its
		/// normal (non-minimized, non-maximized) state.
		/// </summary>
		public Rectangle NormalStateDesktopBounds { get; private set; }

		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		public void PreSynchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		public bool Synchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise.  False should keep the main window
		/// from being shown/initialized (maybe even close the window if false is returned)
		/// </returns>
		public bool OnFinishedInit()
		{
			CheckDisposed();

			if (_startupLink == null)
			{
				return true;
			}
			var commands = new List<string>
			{
				"AboutToFollowLink",
				"FollowLink" // LinkHandler is only thing that cares about this message, and it (or its replacement) isn't listening yet.
			};
			var parms = new List<object>
			{
				null,
				_startupLink
			};
			_startupLink = null;
			Publisher.Publish(commands, parms);

			return true;
		}

		/// <summary>
		/// Handle the Find menu command.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		public bool OnEditFind(object args)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Prepare to refresh the main window and its IAreas and ITools.
		/// </summary>
		public void PrepareToRefresh()
		{
			_currentArea.PrepareToRefresh();
		}

		/// <summary>
		/// Finish refreshing the main window and its IAreas and ITools.
		/// </summary>
		/// <remarks>
		/// This should call Refresh on real window implementations,
		/// after everything else is done.</remarks>
		public void FinishRefresh()
		{
			Cache.ServiceLocator.GetInstance<IUndoStackManager>().Refresh();
			_currentArea.FinishRefresh();
			Refresh();
		}

		/// <summary>
		/// Refreshes all the views in this window and in all others in the app.
		/// </summary>
		public void RefreshAllViews()
		{
			// Susanna asked that refresh affect only the currently active project, which is
			// what the string and List variables below attempt to handle.  See LT-6444.
			var activeWnd = ActiveForm as IFwMainWnd;

			var allMainWindowsExceptActiveWindow = new List<IFwMainWnd>();
			foreach (var otherMainWindow in _flexApp.MainWindows.Where(mw => mw != activeWnd))
			{
				otherMainWindow.PrepareToRefresh();
				allMainWindowsExceptActiveWindow.Add(otherMainWindow);
			}

			// Now that all IFwMainWnds except currently active one have done basic refresh preparation,
			// have them all finish refreshing.
			foreach (var otherMainWindow in allMainWindowsExceptActiveWindow)
			{
				otherMainWindow.FinishRefresh();
			}

			// LT-3963: active IFwMainWnd changes as a result of a refresh.
			// Make sure focus doesn't switch to another FLEx application / window also
			// make sure the application focus isn't lost all together.
			// ALSO, after doing a refresh with just a single application / window,
			// the application would loose focus and you'd have to click into it to
			// get that back, this will reset that too.
			if (activeWnd == null)
			{
				return;
			}
			// Refresh it last, so its saved settings get restored.
			activeWnd.FinishRefresh();
			var activeForm = activeWnd as Form;
			activeForm?.Activate();
		}

		/// <summary>
		/// Call this for the duration of a block of code where we don't want idle events.
		/// (Note that various things outside our control may pump events and cause the
		/// timer that fires the idle events to be triggered when we are not idle, even in the
		/// middle of processing another event.) Call ResumeIdleProcessing when done.
		/// </summary>
		public void SuspendIdleProcessing()
		{
			CheckDisposed();

			_countSuspendIdleProcessing++;
		}

		/// <summary>
		/// See SuspendIdleProcessing.
		/// </summary>
		public void ResumeIdleProcessing()
		{
			CheckDisposed();

			if (_countSuspendIdleProcessing > 0)
			{
				_countSuspendIdleProcessing--;
			}
		}

		/// <summary>
		/// Get the RecordBar (as a Control), or null if not present.
		/// </summary>
		public IRecordBar RecordBarControl => (mainContainer.SecondControl as CollapsingSplitContainer)?.FirstControl as IRecordBar;

		/// <summary>
		/// Get the TreeView of RecordBarControl, or null if not present, or it is not showng a tree.
		/// </summary>
		public TreeView TreeStyleRecordList
		{
			get
			{
				var recordBarControl = RecordBarControl;
				if (recordBarControl == null)
				{
					return null;
				}
				var retVal = recordBarControl.TreeView;
				if (!retVal.Visible)
				{
					retVal = null;
				}
				return retVal;
			}
		}

		/// <summary>
		/// Get the ListView of RecordBarControl, or null if not present, or it is not showing a list.
		/// </summary>
		public ListView ListStyleRecordList
		{
			get
			{
				var recordBarControl = RecordBarControl;
				if (recordBarControl == null)
				{
					return null;
				}
				var retVal = recordBarControl.ListView;
				if (!retVal.Visible)
				{
					retVal = null;
				}
				return retVal;
			}
		}

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher => _publisher;
#endregion

#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber => _subscriber;
#endregion

#region IIdleQueueProvider implementation

		/// <summary>
		/// Get the singleton (per window) instance of IdleQueue.
		/// </summary>
		public IdleQueue IdleQueue { get; private set; }

#endregion

#region Implementation of IDisposable

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				Application.Idle -= Application_Idle;

				// Quit responding to messages early on.
				Subscriber.Unsubscribe("MigrateOldConfigurations", MigrateOldConfigurations);
				RecordListServices.TearDown(_majorFlexComponentParameters);
				_currentArea?.Deactivate(_majorFlexComponentParameters);
				_currentTool?.Deactivate(_majorFlexComponentParameters);
				_sendReceiveMenuManager?.Dispose();
				_parserMenuManager?.Dispose();
				_dataNavigationManager?.Dispose();
				IdleQueue?.Dispose();
				_writingSystemListHandler?.Dispose();
				_combinedStylesListHandler?.Dispose();
				_linkHandler?.Dispose();

				// Get rid of known, temporary, properties
				RemoveTemporaryProperties();

				components?.Dispose();

				// TODO: Is this comment still relevant?
				// TODO: Seems like FLEx worked well with it in this place (in the original window) for a long time.
				// The removing of the window needs to happen later; after this main window is
				// already disposed of. This is needed for side-effects that require a running
				// message loop.
				_flexApp.FwManager.ExecuteAsync(_flexApp.RemoveWindow, this);

				_dataNavigationManager?.Dispose();
				_sidePane?.Dispose();
				_viewHelper?.Dispose();
			}

			_sendReceiveMenuManager = null;
			_parserMenuManager = null;
			_dataNavigationManager = null;
			_sidePane = null;
			_viewHelper = null;
			_currentArea = null;
			_stylesheet = null;
			_publisher = null;
			_subscriber = null;
			_areaRepository = null;
			IdleQueue = null;
			_majorFlexComponentParameters = null;
			_writingSystemListHandler = null;
			_combinedStylesListHandler = null;
			_linkHandler = null;

			base.Dispose(disposing);

			if (disposing && _recordListRepositoryForTools != null)
			{
				_recordListRepositoryForTools.Dispose();
				_recordListRepositoryForTools = null;
			}

			// Leave the PropertyTable for last, since the above stuff may still want to access it, while shutting down.
			if (disposing)
			{
				PropertyTable?.Dispose();
			}
			_propertyTable = null;
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FwMainWnd()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
		}

		#endregion

		/// <summary>
		/// Layout toolstrips by size. (Assume all toolstrips are same height.)
		///
		/// The Designer has a hard time keeping the toolbars in the right place, so this puts them where they belong.
		/// </summary>
		private void LayoutToolStrips()
		{
			SuspendLayout();

			var containerPosition = new Point(0, 0);
			var containerHeight = 0;
			// Row[0] is the menu bar.
			// Row[1] is the toolbar.
			foreach (var row in toolStripContainer.TopToolStripPanel.Rows)
			{
				// Start each row at top and left.
				if (row.Controls[0].Name == "_menuStrip")
				{
					continue;
				}
				var currentPosition = new Point(0, 0);
				for (var idx = 0; idx < row.Controls.Length; idx++)
				{
					var currentToolbar = row.Controls[idx];
					var previousToolbar = idx == 0 ? null : row.Controls[idx - 1];
					currentPosition = idx == 0 ? currentToolbar.Location : new Point(previousToolbar.Location.Y, currentToolbar.Width);
					currentToolbar.Location = currentPosition;
				}
			}
			toolStripContainer.Height = toolStripContainer.TopToolStripPanel.Height + containerPosition.Y + containerHeight;
			ResumeLayout();
		}

		private void File_CloseWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void Help_LanguageExplorer(object sender, EventArgs e)
		{
			var helpFile = _flexApp.HelpFile;
			try
			{
				// When the help window is closed it will return focus to the window that opened it (see MSDN
				// documentation for HtmlHelp()). We don't want to use the main window as the parent, because if
				// a modal dialog is visible, it will still return focus to the main window, allowing the main window
				// to perform some behaviors (such as refresh by pressing F5) while the modal dialog is visible,
				// which can be bad. So, we just create a dummy control and pass that in as the parent.
				Help.ShowHelp(new Control(), helpFile);
			}
			catch (Exception)
			{
				MessageBox.Show(this, string.Format(LanguageExplorerResources.ksCannotLaunchX, helpFile),
					LanguageExplorerResources.ksError);
			}
		}

		private void Help_Training(object sender, EventArgs e)
		{
			using (var process = new Process())
			{
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.FileName = "http://wiki.lingtransoft.info/doku.php?id=tutorials:student_manual";
				process.Start();
				process.Close();
			}
		}

		private void Help_DemoMovies(object sender, EventArgs e)
		{
			try
			{
				var pathMovies = string.Format(FwDirectoryFinder.CodeDirectory +
					"{0}Language Explorer{0}Movies{0}Demo Movies.html",
					Path.DirectorySeparatorChar);

				OpenDocument<Win32Exception>(pathMovies, win32err =>
				{
					if (win32err.NativeErrorCode == 1155)
					{
						// The user has the movie files, but does not have a file association for .html files.
						// Try to launch Internet Explorer directly:
						using (Process.Start("IExplore.exe", pathMovies))
						{
						}
					}
					else
					{
						// User probably does not have movies. Try to launch the "no movies" web page:
						var pathNoMovies = string.Format(FwDirectoryFinder.CodeDirectory +
							"{0}Language Explorer{0}Movies{0}notfound.html",
							Path.DirectorySeparatorChar);

						OpenDocument<Win32Exception>(pathNoMovies, win32err2 =>
						{
							if (win32err2.NativeErrorCode == 1155)
							{
								// The user does not have a file association for .html files.
								// Try to launch Internet Explorer directly:
								using (Process.Start("IExplore.exe", pathNoMovies))
								{
								}
							}
							else
								throw win32err2;
						});
					}
				});
			}
			catch (Exception)
			{
				// Some other unforeseen error:
				MessageBox.Show(null, string.Format(FrameworkStrings.ksErrorCannotLaunchMovies,
					string.Format(FwDirectoryFinder.CodeDirectory + "{0}Language Explorer{0}Movies", Path.DirectorySeparatorChar)), LanguageExplorerResources.ksError);
			}
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
					using (Process.Start(_webBrowserProgramLinux, Enquote(path)))
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
				exceptionHandler?.Invoke(e);
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

		private void Help_Technical_Notes_on_FieldWorks_Send_Receive(object sender, EventArgs e)
		{
			var path = string.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on FieldWorks Send-Receive.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, err =>
			{
				MessageBox.Show(null, string.Format(LanguageExplorerResources.ksCannotLaunchX, path),
					LanguageExplorerResources.ksError);
			});
		}

		private void Help_ReportProblem(object sender, EventArgs e)
		{
			ErrorReporter.ReportProblem(FwRegistryHelper.FieldWorksRegistryKey, _flexApp.SupportEmailAddress, this);
		}

		private void Help_Make_a_Suggestion(object sender, EventArgs e)
		{
			ErrorReporter.MakeSuggestion(FwRegistryHelper.FieldWorksRegistryKey, "FLExDevteam@sil.org", this);
		}

		private void Help_About_Language_Explorer(object sender, EventArgs e)
		{
			using (var helpAboutWnd = new FwHelpAbout())
			{
				helpAboutWnd.ProductExecutableAssembly = Assembly.LoadFile(_flexApp.ProductExecutableFile);
				helpAboutWnd.ShowDialog();
			}
		}

		private void File_New_FieldWorks_Project(object sender, EventArgs e)
		{
			if (_flexApp.ActiveMainWindow != this)
				throw new InvalidOperationException("Unexpected active window for app.");
			_flexApp.FwManager.CreateNewProject();
		}

		private void File_Open(object sender, EventArgs e)
		{
			_flexApp.FwManager.ChooseLangProject();
		}

		private void File_FieldWorks_Project_Properties(object sender, EventArgs e)
		{
			// 'true' for either of these two menus,
			// but 'false' for fieldWorksProjectPropertiesToolStripMenuItem on the File menu.
			LaunchProjPropertiesDlg(sender == setUpWritingSystemsToolStripMenuItem || sender == setUpWritingSystemsToolStripMenuItem1);
		}

		/// <summary>
		/// Launches the proj properties DLG.
		/// </summary>
		/// <param name="startOnWSPage">if set to <c>true</c> [start on WS page].</param>
		private void LaunchProjPropertiesDlg(bool startOnWSPage)
		{
			var cache = _flexApp.Cache;
			if (!SharedBackendServicesHelper.WarnOnOpeningSingleUserDialog(cache))
				return;

			var fDbRenamed = false;
			var sProject = cache.ProjectId.Name;
			var sLinkedFilesRootDir = cache.LangProject.LinkedFilesRootDir;
			using (var dlg = new FwProjPropertiesDlg(cache, _flexApp, _flexApp))
			{
				dlg.ProjectPropertiesChanged += ProjectProperties_Changed;
				if (startOnWSPage)
				{
					dlg.StartWithWSPage();
				}
				if (dlg.ShowDialog(this) != DialogResult.Abort)
				{
					// NOTE: This code is called, even if the user cancelled the dlg.
					fDbRenamed = dlg.ProjectNameChanged();
					if (fDbRenamed)
					{
						sProject = dlg.ProjectName;
					}
					var fFilesMoved = false;
					if (dlg.LinkedFilesChanged())
					{
						fFilesMoved = _flexApp.UpdateExternalLinks(sLinkedFilesRootDir);
					}
					// no need for any of these refreshes if entire window has been/will be
					// destroyed and recreated.
					if (!fDbRenamed && !fFilesMoved)
					{
						SetWindowTitle();
					}
				}
			}
			if (fDbRenamed)
			{
				_flexApp.FwManager.RenameProject(sProject);
			}
		}

		private void SetWindowTitle()
		{
			Text = $@"{_flexApp.Cache.ProjectId.UiName} - {FwUtils.ksSuiteName}";
		}

		private void ProjectProperties_Changed(object sender, EventArgs eventArgs)
		{
			// this event is fired before the Project Properties dialog is closed, so that we have a chance
			// to refresh everything before Paint events start getting fired, which can cause problems if
			// any writing systems are removed that a rootsite is currently displaying
			var dlg = (FwProjPropertiesDlg)sender;
			if (dlg.WritingSystemsChanged())
			{
				View_Refresh(sender, eventArgs);
			}
		}

		/// <summary>
		/// This is the one (and should be only) handler for the user Refresh command.
		/// Refresh wants to first clean up the cache, then give things like record lists a
		/// chance to reload stuff (calling the old OnRefresh methods), then give
		/// windows a chance to redisplay themselves.
		/// </summary>
		/// <remarks>
		/// Areas/Tools can decide to disable the F5  menu and toolbar refresh, if they wish. One (GrammarSketchTool) is known to do that.
		/// </remarks>
		private void View_Refresh(object sender, EventArgs e)
		{
			RefreshAllViews();
		}

		private void File_Back_up_this_Project(object sender, EventArgs e)
		{
			SaveSettings();

			_flexApp.FwManager.BackupProject(this);
		}

		private void File_Restore_a_Project(object sender, EventArgs e)
		{
			_flexApp.FwManager.RestoreProject(_flexApp, this);
		}

		private void File_Project_Location(object sender, EventArgs e)
		{
			_flexApp.FwManager.FileProjectLocation(_flexApp, this);
		}

		private void File_Delete_Project(object sender, EventArgs e)
		{
			_flexApp.FwManager.DeleteProject(_flexApp, this);
		}

		private void File_Create_Shortcut_on_Desktop(object sender, EventArgs e)
		{
			var directory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			if (!FileUtils.DirectoryExists(directory))
			{
				MessageBoxUtils.Show($"Error: Cannot create project shortcut because destination directory '{directory}' does not exist.");
				return;
			}

			var applicationArguments = "-" + FwAppArgs.kProject + " \"" + _flexApp.Cache.ProjectId.Handle + "\"";
			var description = ResourceHelper.FormatResourceString(
				"kstidCreateShortcutLinkDescription", _flexApp.Cache.ProjectId.UiName,
				_flexApp.ApplicationName);

			if (MiscUtils.IsUnix)
			{
				var projectName = _flexApp.Cache.ProjectId.UiName;
				const string pathExtension = ".desktop";
				var launcherPath = Path.Combine(directory, projectName + pathExtension);

				// Choose a different name if already in use
				var tailNumber = 2;
				while (FileUtils.SimilarFileExists(launcherPath))
				{
					var tail = "-" + tailNumber;
					launcherPath = Path.Combine(directory, projectName + tail + pathExtension);
					tailNumber++;
				}

				const string applicationExecutablePath = "fieldworks-flex";
				const string iconPath = "fieldworks-flex";
				if (string.IsNullOrEmpty(applicationExecutablePath))
					return;
				var content = string.Format(
					"[Desktop Entry]{0}" +
					"Version=1.0{0}" +
					"Terminal=false{0}" +
					"Exec=" + applicationExecutablePath + " " + applicationArguments + "{0}" +
					"Icon=" + iconPath + "{0}" +
					"Type=Application{0}" +
					"Name=" + projectName + "{0}" +
					"Comment=" + description + "{0}", Environment.NewLine);

				// Don't write a BOM
				using (var launcher = FileUtils.OpenFileForWrite(launcherPath, new UTF8Encoding(false)))
				{
					launcher.Write(content);
					FileUtils.SetExecutable(launcherPath);
				}
			}
			else
			{
				WshShell shell = new WshShellClass();

				var filename = _flexApp.Cache.ProjectId.UiName;
				filename = Path.ChangeExtension(filename, "lnk");
				var linkPath = Path.Combine(directory, filename);

				var link = (IWshShortcut)shell.CreateShortcut(linkPath);
				if (link.FullName != linkPath)
				{
					var msg = string.Format(FrameworkStrings.ksCannotCreateShortcut,
						_flexApp.ProductExecutableFile + " " + applicationArguments);
					MessageBox.Show(ActiveForm, msg,
						FrameworkStrings.ksCannotCreateShortcutCaption, MessageBoxButtons.OK,
						MessageBoxIcon.Asterisk);
					return;
				}
				link.TargetPath = _flexApp.ProductExecutableFile;
				link.Arguments = applicationArguments;
				link.Description = description;
				link.IconLocation = link.TargetPath + ",0";
				link.Save();
			}
		}

		private void File_Archive_With_RAMP(object sender, EventArgs e)
		{
			List<string> filesToArchive = null;
			using (var dlg = new ArchiveWithRamp(Cache, _flexApp))
			{
				// prompt the user to select or create a FieldWorks backup
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					filesToArchive = dlg.FilesToArchive;
				}
			}

			// if there are no files to archive, return now.
			if ((filesToArchive == null) || (filesToArchive.Count == 0))
			{
				return;
			}

			// show the RAMP dialog
			var ramp = new ReapRamp();
			ramp.ArchiveNow(this, MainMenuStrip.Font, Icon, filesToArchive, PropertyTable, _flexApp, Cache);
		}

		private void UploadToWebonary_Click(object sender, EventArgs e)
		{
			var publications = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Select(p => p.Name.BestAnalysisAlternative.Text).ToList();
			var projectConfigDir = DictionaryConfigurationListener.GetProjectConfigurationDirectory(PropertyTable, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var defaultConfigDir = DictionaryConfigurationListener.GetDefaultConfigurationDirectory(DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var configurations = DictionaryConfigurationController.GetDictionaryConfigurationLabels(Cache, defaultConfigDir, projectConfigDir);
			// Now collect all the reversal configurations into the reversals variable
			projectConfigDir = DictionaryConfigurationListener.GetProjectConfigurationDirectory(PropertyTable, DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName);
			defaultConfigDir = DictionaryConfigurationListener.GetDefaultConfigurationDirectory(DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName);
			var reversals = DictionaryConfigurationController.GetDictionaryConfigurationLabels(Cache, defaultConfigDir, projectConfigDir);

			// show dialog
			var model = new UploadToWebonaryModel(PropertyTable)
			{
				Reversals = reversals,
				Configurations = configurations,
				Publications = publications
			};
			using (var controller = new UploadToWebonaryController(Cache, PropertyTable, Publisher, _majorFlexComponentParameters.Statusbar))
			using (var dialog = new UploadToWebonaryDlg(controller, model, PropertyTable))
			{
				dialog.ShowDialog();
				RefreshAllViews();
			}
		}

		private void File_Translated_List_Content(object sender, EventArgs e)
		{
			string filename;
			// ActiveForm can go null (see FWNX-731), so cache its value, and check whether
			// we need to use 'this' instead (which might be a better idea anyway).
			var form = ActiveForm ?? this;
			using (var dlg = new OpenFileDialogAdapter())
			{
				dlg.CheckFileExists = true;
				dlg.RestoreDirectory = true;
				dlg.Title = ResourceHelper.GetResourceString("kstidOpenTranslatedLists");
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = ResourceHelper.FileFilter(FileFilterType.FieldWorksTranslatedLists);
				if (dlg.ShowDialog(form) != DialogResult.OK)
				{
					return;
				}
				filename = dlg.FileName;
			}
			using (new WaitCursor(form, true))
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
					() =>
					{
						using (var dlg = new ProgressDialogWithTask(this))
						{
							dlg.AllowCancel = true;
							dlg.Maximum = 200;
							dlg.Message = filename;
							dlg.RunTask(true, LcmCache.ImportTranslatedLists, filename, Cache);
						}
					});
			}
		}

		private void NewWindow_Clicked(object sender, EventArgs e)
		{
			SaveSettings();
			_flexApp.FwManager.OpenNewWindowForApp(this);
		}

		private void Help_Training_Writing_Systems(object sender, EventArgs e)
		{
			var pathnameToWritingSystemHelpFile = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Training", "Technical Notes on Writing Systems.pdf");
			OpenDocument(pathnameToWritingSystemHelpFile, err =>
			{
				MessageBox.Show(this, string.Format(FrameworkStrings.ksCannotShowX, pathnameToWritingSystemHelpFile), LanguageExplorerResources.ksError);
			});
		}

		private void Help_XLingPaper(object sender, EventArgs e)
		{
			var xLingPaperPathname = Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps", "XLingPap", "UserDoc.htm");

			OpenDocument(xLingPaperPathname, err =>
			{
				MessageBox.Show(this, string.Format(FrameworkStrings.ksCannotShowX, xLingPaperPathname), LanguageExplorerResources.ksError);
			});
		}

		private void Edit_Cut(object sender, EventArgs e)
		{
			using (new DataUpdateMonitor(this, "EditCut"))
			{
				_viewHelper.ActiveView.EditingHelper.CutSelection();
			}
		}

		private void Edit_Copy(object sender, EventArgs e)
		{
			_viewHelper.ActiveView.EditingHelper.CopySelection();
		}

		private void Edit_Paste(object sender, EventArgs e)
		{
			string stUndo, stRedo;
			ResourceHelper.MakeUndoRedoLabels("kstidEditPaste", out stUndo, out stRedo);
			using (var undoHelper = new UndoableUnitOfWorkHelper(Cache.ServiceLocator.GetInstance<IActionHandler>(), stUndo, stRedo))
			using (new DataUpdateMonitor(this, "EditPaste"))
			{
				if (_viewHelper.ActiveView.EditingHelper.PasteClipboard())
				{
					undoHelper.RollBack = false;
				}
			}
		}

		private void SetupEditUndoAndRedoMenus()
		{
			// Set basic values on the pessimistic side.
			var ah = Cache.DomainDataByFlid.GetActionHandler();
			string rawUndoText;
			var undoEnabled = ah.UndoableSequenceCount > 0;
			var redoEnabled = ah.RedoableSequenceCount > 0;
			string rawRedoText;
			// Q: Is the focused control an instance of IUndoRedoHandler
			var asIUndoRedoHandler = FocusedControl as IUndoRedoHandler;
			if (asIUndoRedoHandler != null)
			{
				// A1: Yes: Let it set up the text and enabled state for both menus.
				// The handler may opt to not fret about, or or the other,
				// so this method may need to deal with in the end, after all.
				rawUndoText = asIUndoRedoHandler.UndoText;
				undoEnabled = asIUndoRedoHandler.UndoEnabled(undoEnabled);
				rawRedoText = asIUndoRedoHandler.RedoText;
				redoEnabled = asIUndoRedoHandler.RedoEnabled(redoEnabled);
			}
			else
			{
				// A2: No: Deal with it here.
				// Normal undo processing.
				var baseUndo = undoEnabled ? ah.GetUndoText() : LanguageExplorerResources.Undo;
				rawUndoText = string.IsNullOrEmpty(baseUndo) ? LanguageExplorerResources.Undo : baseUndo;

				// Normal Redo processing.
				var baseRedo = redoEnabled ? ah.GetRedoText() : LanguageExplorerResources.Redo;
				rawRedoText = string.IsNullOrEmpty(baseRedo) ? LanguageExplorerResources.Redo : baseRedo;
			}
			undoToolStripMenuItem.Text = FwUtils.ReplaceUnderlineWithAmpersand(rawUndoText);
			undoToolStripMenuItem.Enabled = undoEnabled;
			redoToolStripMenuItem.Text = FwUtils.ReplaceUnderlineWithAmpersand(rawRedoText);
			redoToolStripMenuItem.Enabled = redoEnabled;
			// Standard toolstrip buttons.
			undoToolStripButton.Enabled = undoEnabled;
			redoToolStripButton.Enabled = redoEnabled;
		}

		private void Edit_Paste_Hyperlink(object sender, EventArgs e)
		{
			((RootSiteEditingHelper)_viewHelper.ActiveView.EditingHelper).PasteUrl(_stylesheet);
		}

		private void Edit_Select_All(object sender, EventArgs e)
		{
#if RANDYTODO
/*
	Jason Naylor's expanded comment on potential issues.
	Things to keep in mind:

	"I think if anything my comment regards a potential design improvement that is
beyond the scope of this current change. You might want to have a quick look at the
LanguageExplorer.Areas.TextsAndWords.Tools.CorpusStatistics.StatisticsView and how it will play into this. You may find nothing
that needs to change. I wrote that class way back before I understood how many tentacles
the xWindow and other 'x' classes had. We needed a very simple view of data that wasn't
in any of our blessed RecordLists. I wrote this view with the idea that it would be tied
into the xBeast as little as possible. This was years ago.

	After your changes I expect that something like this would be easier to do, and easy to
make more functional. The fact that the ActiveView is an IRootSite factors in to what kind
of views we are able to have.

	Just giving you more to think about while you're working on these
very simple minor adjustments. ;)"
*/
#endif
			using (new WaitCursor(this))
			{
				_viewHelper.ActiveView.EditingHelper.SelectAll();
			}
		}

		private void utilitiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new UtilityDlg(_flexApp))
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ShowDialog(this);
			}
		}

		#region Overrides of Form

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data. </param>
		protected override void OnLoad(EventArgs e)
		{
			if (_recordListRepositoryForTools != RecordList.ActiveRecordListRepository)
			{
				RecordList.ActiveRecordListRepository = _recordListRepositoryForTools;
			}

			// Insert toolbar is not visible at the start. Tools make it visible as needed.
			InsertToolbarManager.DeactivateInsertToolbar(_majorFlexComponentParameters);

			base.OnLoad(e);

			LayoutToolStrips();

			var currentArea = _areaRepository.PersistedOrDefaultArea;
			var currentTool = currentArea.PersistedOrDefaultTool;
			_sidePane.TabClicked += Area_Clicked;
			_sidePane.ItemClicked += Tool_Clicked;
			// This call fires Area_Clicked and then Tool_Clicked to make sure the provided tab and item are both selected in the end.
			_sidePane.SelectItem(_sidePane.GetTabByName(currentArea.MachineName), currentTool.MachineName);
		}

		#endregion

		#region Handle record list repository and last known active record list
		/// <summary>
		/// This window is being activated, so (re)-set static stuff to what we want.
		/// </summary>
		private void FwMainWnd_Activated(object sender, EventArgs e)
		{
			if (_recordListRepositoryForTools != RecordList.ActiveRecordListRepository)
			{
				RecordList.ActiveRecordListRepository = _recordListRepositoryForTools;
		}
		}

		#endregion

		private void File_Import_Standard_Format_Marker_Click(object sender, EventArgs e)
		{
			using (var importWizard = new LexImportWizard())
			{
				var wndActive = _majorFlexComponentParameters.MainWindow;
				((IFwExtension)importWizard).Init(_majorFlexComponentParameters.LcmCache, wndActive.PropertyTable, wndActive.Publisher);
				importWizard.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
			}
		}

		private void Tools_Options_Click(object sender, EventArgs e)
		{
			using (var optionsDlg = new LexOptionsDlg())
			{
				AreaServices.HandleDlg(optionsDlg, Cache, _flexApp, this, PropertyTable, Publisher);
			}
		}

		private void Edit_Undo_Click(object sender, EventArgs e)
		{
			var focusedControl = FocusedControl;
			var asIUndoRedoHandler = focusedControl as IUndoRedoHandler;
			if (asIUndoRedoHandler != null)
			{
				if (asIUndoRedoHandler.HandleUndo(sender, e))
				{
					return;
				}
			}

			// For all the bother, the IUndoRedoHandler impl couldn't be bothered, so do it here.
			var ah = Cache.DomainDataByFlid.GetActionHandler();
			using (new WaitCursor(this))
			{
				try
				{
					_inUndoRedo = true;
					ah.Undo();
				}
				finally
				{
					_inUndoRedo = false;
				}
			}
			// Trigger a selection changed, to force updating of controls like the writing system combo
			// that might be affected, if relevant.
			var focusRootSite = focusedControl as SimpleRootSite;
			if (focusRootSite != null && !focusRootSite.IsDisposed && focusRootSite.RootBox?.Selection != null)
			{
				focusRootSite.SelectionChanged(focusRootSite.RootBox, focusRootSite.RootBox.Selection);
			}
		}

		private void Edit_Redo_Click(object sender, EventArgs e)
		{
			var focusedControl = FocusedControl;
			var asIUndoRedoHandler = focusedControl as IUndoRedoHandler;
			if (asIUndoRedoHandler != null)
			{
				if (asIUndoRedoHandler.HandleRedo(sender, e))
				{
					return;
				}
			}

			// For all the bother, the IUndoRedoHandler impl couldn't be bothered, so do it here.
			var ah = Cache.DomainDataByFlid.GetActionHandler();
			using (new WaitCursor(this))
			{
				try
				{
					_inUndoRedo = true;
					ah.Redo();
				}
				finally
				{
					_inUndoRedo = false;
				}
			}
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			_dataNavigationManager.SetEnabledStateForWidgets();

			var hasActiveView = _viewHelper.ActiveView != null;
			selectAllToolStripMenuItem.Enabled = hasActiveView;
			cutToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper.CanCut());
			copyToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper.CanCopy());
			pasteToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper.CanPaste());
			pasteHyperlinkToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper is RootSiteEditingHelper && ((RootSiteEditingHelper)_viewHelper.ActiveView.EditingHelper).CanPasteUrl());
			applyStyleToolStripMenuItem.Enabled = hasActiveView && CanApplyStyle;
			pasteHyperlinkToolStripMenuItem.Enabled = hasActiveView && _viewHelper.ActiveView.EditingHelper is RootSiteEditingHelper && ((RootSiteEditingHelper)_viewHelper.ActiveView.EditingHelper).CanPasteUrl();
			selectAllToolStripMenuItem.Enabled = hasActiveView && !DataUpdateMonitor.IsUpdateInProgress();

			// Enable/disable toolbar buttons.
			SetupEditUndoAndRedoMenus();
		}

		private bool CanApplyStyle
		{
			get
			{
				var selectionHelper = _viewHelper.ActiveView?.EditingHelper.CurrentSelection;
				if (selectionHelper == null)
				{
					return false;
				}
				var rootSite = selectionHelper.RootSite as RootSite;
				if (rootSite != null)
				{
					return rootSite.CanApplyStyle;
				}

				var selection = selectionHelper.Selection;
				return selection != null && selection.IsEditable && (selection.CanFormatChar || selection.CanFormatPara);
			}
		}

		private void Format_Styles_Click(object sender, EventArgs e)
		{
			string paraStyleName = null;
			string charStyleName = null;
			bool refreshAllViews;
			var stylesXmlAccessor = new FlexStylesXmlAccessor(Cache.LanguageProject.LexDbOA);
			StVc vc = null;
			IVwRootSite activeViewSite = null;
			if (ActiveView != null)
			{
				if (ActiveView.EditingHelper == null) // If the calling location doesn't provide this just bring up the dialog with normal selected
				{
					paraStyleName = "Normal";
				}
				vc = ActiveView.EditingHelper.ViewConstructor as StVc;
				activeViewSite = ActiveView.CastAsIVwRootSite();
				if (paraStyleName == null && ActiveView.EditingHelper.CurrentSelection?.Selection != null)
				{
					// If the caller didn't know the default style, try to figure it out from // the selection.
					GetStyleNames((SimpleRootSite)ActiveView, ActiveView.EditingHelper.CurrentSelection.Selection, ref paraStyleName, ref charStyleName);
				}
			}
			using (var stylesDlg = new FwStylesDlg(activeViewSite,
				Cache, _stylesheet, vc?.RightToLeft ?? false,
				Cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				_stylesheet.GetDefaultBasedOnStyleName(),
				int.MaxValue, _flexApp.MeasurementSystem, paraStyleName, charStyleName,
				Cache.LanguageProject.LexDbOA.Hvo, _flexApp, _flexApp))
			{
				stylesDlg.SetPropsToFactorySettings = stylesXmlAccessor.SetPropsToFactorySettings;
				stylesDlg.StylesRenamedOrDeleted += OnStylesRenamedOrDeleted;
				stylesDlg.AllowSelectStyleTypes = true;
				stylesDlg.ShowTEStyleTypes = false;
				stylesDlg.CanSelectParagraphBackgroundColor = true;
				refreshAllViews = (stylesDlg.ShowDialog(this) == DialogResult.OK &&
				        ((stylesDlg.ChangeType & StyleChangeType.DefChanged) > 0 ||
				         (stylesDlg.ChangeType & StyleChangeType.Added) > 0));
			}

			if (refreshAllViews)
			{
				RefreshAllViews();
			}
		}

		private void GetStyleNames(IVwRootSite rootsite, IVwSelection sel, ref string paraStyleName, ref string charStyleName)
		{
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(sel, out vttp, out vvps, out cttp);
			var fSingleStyle = true;
			string sStyle = null;
			for (var ittp = 0; ittp < cttp; ++ittp)
			{
				var style = vttp[ittp].Style();
				if (ittp == 0)
				{
					sStyle = style;
				}
				else if (sStyle != style)
				{
					fSingleStyle = false;
				}
			}
			if (fSingleStyle && !string.IsNullOrEmpty(sStyle))
			{
				if (_stylesheet.GetType(sStyle) == (int)StyleType.kstCharacter)
				{
					if (sel.CanFormatChar)
					{
						charStyleName = sStyle;
					}
				}
				else
				{
					if (sel.CanFormatPara)
					{
						paraStyleName = sStyle;
					}
				}
			}
			if (paraStyleName != null)
			{
				return;
			}
			// Look at the paragraph (if there is one) to get the paragraph style.
			var helper = SelectionHelper.GetSelectionInfo(sel, rootsite);
			var info = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
			if (info.Length == 0)
			{
				return;
			}
			var hvo = info[0].hvo;
			if (hvo == 0)
			{
				return;
			}
			var cmObjectRepository = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (!cmObjectRepository.IsValidObjectId(hvo))
			{
				return;
			}
			var cmo = cmObjectRepository.GetObject(hvo);
			if (cmo is IStPara)
			{
				paraStyleName = (cmo as IStPara).StyleName;
			}
		}

		/// <summary>
		/// Called when styles are renamed or deleted.
		/// </summary>
		private void OnStylesRenamedOrDeleted()
		{
			PrepareToRefresh();
			Refresh();
		}

		private void applyStyleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Disabled if there is no ActiveView and it cannot appy the styles.
			SimpleRootSite rootsite = null;
			try
			{
				string paraStyleName = null;
				string charStyleName = null;
				rootsite = ActiveView as SimpleRootSite;
				if (rootsite != null)
				{
					rootsite.ShowRangeSelAfterLostFocus = true;
				}

				var sel = _viewHelper.ActiveView.EditingHelper.CurrentSelection.Selection;
				if (paraStyleName == null && charStyleName == null)
				{
					// Try to get the style names from the selection.
					GetStyleNames(rootsite, sel, ref paraStyleName, ref charStyleName);
				}
				int hvoRoot, frag;
				IVwViewConstructor vc;
				IVwStylesheet ss;
				ActiveView.CastAsIVwRootSite().RootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
				using (var applyStyleDlg = new FwApplyStyleDlg(Cache, _stylesheet, paraStyleName, charStyleName, _flexApp))
				{
					applyStyleDlg.ApplicableStyleContexts = new List<ContextValues>(new[] { ContextValues.General });
					applyStyleDlg.CanApplyCharacterStyle = sel.CanFormatChar;
					applyStyleDlg.CanApplyParagraphStyle = sel.CanFormatPara;

					if (applyStyleDlg.ShowDialog(this) != DialogResult.OK)
					{
						return;
					}
					string sUndo, sRedo;
					ResourceHelper.MakeUndoRedoLabels("kstidUndoApplyStyle", out sUndo, out sRedo);
					using (var helper = new UndoTaskHelper(Cache.ActionHandlerAccessor, ActiveView.CastAsIVwRootSite(), sUndo, sRedo))
					{
						_viewHelper.ActiveView.EditingHelper.ApplyStyle(applyStyleDlg.StyleChosen);
						helper.RollBack = false;
					}
				}
			}
			finally
			{
				if (rootsite != null)
					rootsite.ShowRangeSelAfterLostFocus = false;
			}
		}

		/// <summary>
		/// When a new FwMainWnd is a copy of another FwMainWnd and the new FwMainWnd's Show()
		/// method is called, the .net framework will mysteriously add the height of the menu
		/// bar (and border) to the preset window height.
		/// Aaarghhh! So we intercept the CreateParams in order to set it back to the desired
		/// height.
		/// </summary>
		protected override CreateParams CreateParams
		{
			get
			{
				var cp = base.CreateParams;
				if (_windowIsCopy)
				{
					cp.Height = Height;
				}
				return cp;
			}
		}
	}
}
// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using LanguageExplorer.Archiving;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Controls;
using SIL.Code;
using LanguageExplorer.Controls.SilSidePane;
using LanguageExplorer.UtilityTools;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;
using SIL.IO;
using SIL.Reporting;
using SIL.Utils;
using File = System.IO.File;
using FileUtils = SIL.Utils.FileUtils;
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
#if RANDYTODO
	// TODO: These comments come from the original xWindow class
	/// Event handlers expected to be managed by areas/tools that are ostensibly global:
	///		1. printToolStripMenuItem : the active tool can enable this and add an event handler, if needed.
	///		2. exportToolStripMenuItem : the active tool can enable this and add an event handler, if needed.
#endif
	/// </remarks>
	internal sealed partial class FwMainWnd : Form, IFwMainWnd
	{
		// Used to count the number of times we've been asked to suspend Idle processing.
		private int _countSuspendIdleProcessing = 0;
		/// <summary>
		///  Web browser to use in Linux
		/// </summary>
		private string _webBrowserProgramLinux = "firefox";
		private IAreaRepository _areaRepository;
		private IToolRepository _toolRepository;
		private ActiveViewHelper _viewHelper;
		private IArea _currentArea;
		private ITool _currentTool;
		private FwStyleSheet _stylesheet;
		private IPublisher _publisher;
		private ISubscriber _subscriber;
		private readonly IFlexApp _flexApp;
		private DateTime _lastToolChange = DateTime.MinValue;
		private readonly HashSet<string> _toolsReportedToday = new HashSet<string>();
		private bool _persistWindowSize;
		private ParserMenuManager _parserMenuManager;
		private DataNavigationManager _dataNavigationManager;
		private readonly MajorFlexComponentParameters _majorFlexComponentParameters;

		/// <summary>
		/// Create new instance of window.
		/// </summary>
		public FwMainWnd()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Create new instance of window.
		/// </summary>
		public FwMainWnd(IFlexApp flexApp, FwMainWnd wndCopyFrom, FwLinkArgs linkArgs)
			: this()
		{
			Guard.AssertThat(wndCopyFrom == null, "Support for the 'wndCopyFrom' is not yet implemented.");
			Guard.AssertThat(linkArgs == null, "Support for the 'linkArgs' is not yet implemented.");

			var wasCrashDuringPreviousStartup = SetupCrashDetectorFile();

			_flexApp = flexApp;

			SetupCustomStatusBarPanels();

			_sendReceiveToolStripMenuItem.Enabled = FLExBridgeHelper.IsFlexBridgeInstalled();
			projectLocationsToolStripMenuItem.Enabled = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.CanWriteKey();
			archiveWithRAMPSILToolStripMenuItem.Enabled = ReapRamp.Installed;

			_viewHelper = new ActiveViewHelper(this);

			SetupStylesheet();
			PubSubSystemFactory.CreatePubSubSystem(out _publisher, out _subscriber);
			SetupPropertyTable();
			RegisterSubscriptions();

			_dataNavigationManager = new DataNavigationManager(Subscriber, new Dictionary<string, Tuple<ToolStripMenuItem, ToolStripButton>>
			{
				{DataNavigationManager.First, new Tuple<ToolStripMenuItem, ToolStripButton>(_data_First, _tsbFirst)},
				{DataNavigationManager.Previous, new Tuple<ToolStripMenuItem, ToolStripButton>(_data_Previous, _tsbPrevious)},
				{DataNavigationManager.Next, new Tuple<ToolStripMenuItem, ToolStripButton>(_data_Next, _tsbNext)},
				{DataNavigationManager.Last, new Tuple<ToolStripMenuItem, ToolStripButton>(_data_Last, _tsbLast)}
			});

			RestoreWindowSettings(wasCrashDuringPreviousStartup);
			var restoreSize = Size;

			_majorFlexComponentParameters = new MajorFlexComponentParameters(
				mainContainer,
				_menuStrip,
				toolStripContainer,
				_statusbar,
				_parserMenuManager,
				_dataNavigationManager,
				new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			SetupRepositories();

			SetupParserMenuItems();

			SetupOutlookBar();

			SetWindowTitle();

			SetupWindowSizeIfNeeded(restoreSize);

			if (File.Exists(CrashOnStartupDetectorPathName)) // Have to check again, because unit test check deletes it in the RestoreWindowSettings method.
			{
				File.Delete(CrashOnStartupDetectorPathName);
			}

			IdleQueue = new IdleQueue();
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

		private void SetupRepositories()
		{
			_toolRepository = new ToolRepository();
			_areaRepository = new AreaRepository(_toolRepository);
			_areaRepository.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		private void SetupStylesheet()
		{
			_stylesheet = new FwStyleSheet();
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
			_parserMenuManager = new ParserMenuManager(_statusbar.Panels["statusBarPanelProgress"], parserMenuItems);
			_parserMenuManager.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
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
			var tempDirectory = Path.Combine(Cache.ProjectId.ProjectFolder, FdoFileHelper.ksSortSequenceTempDir);
			DirectoryUtilities.DeleteDirectoryRobust(tempDirectory);
		}

		private void SetupOutlookBar()
		{
			mainContainer.SuspendLayout();
			_sidePane.TabStop = true;
			_sidePane.TabIndex = 0;
			_sidePane.ItemAreaStyle = SidePaneItemAreaStyle.List;

			mainContainer.Tag = "SidebarWidthGlobal";
			mainContainer.Panel1MinSize = CollapsingSplitContainer.kCollapsedSize;
			mainContainer.Panel1Collapsed = false;
			mainContainer.Panel2Collapsed = false;
			var sd = PropertyTable.GetValue<int>("SidebarWidthGlobal", SettingsGroup.GlobalSettings);
			if (!mainContainer.Panel1Collapsed)
			{
				SetSplitContainerDistance(mainContainer, sd);
			}
			// Add areas and tools to "_sidePane";
			foreach (var area in _areaRepository.AllAreasInOrder())
			{
				var tab = new Tab(StringTable.Table.LocalizeLiteralValue(area.UiName))
				{
					Icon = area.Icon,
					Tag = area,
					Name = area.MachineName
				};

				_sidePane.AddTab(tab);

				// Add tools for area.
				foreach (var tool in area.AllToolsInOrder)
				{
					var item = new Item(StringTable.Table.LocalizeLiteralValue(tool.UiName))
					{
						Icon = tool.Icon,
						Tag = tool,
						Name = tool.MachineName
					};
					_sidePane.AddItem(tab, item);
				}
			}

			// TODO: If no tool has been persisted, or persisted tool is not in persisted area, pick the default for persisted area.
			mainContainer.ResumeLayout(false);
		}

		private void Tool_Clicked(Item itemClicked)
		{
			var clickedTool = (ITool)itemClicked.Tag;
			if (_currentTool == clickedTool)
			{
				return;  // Nothing to do.
			}
			_dataNavigationManager.Clerk = null;
			_currentTool?.Deactivate(_majorFlexComponentParameters);
			_currentTool = clickedTool;
			var areaName = _currentArea.MachineName;
			var toolName = _currentTool.MachineName;
			PropertyTable.SetProperty($"ToolForAreaNamed_{areaName}", toolName, SettingsGroup.LocalSettings, true, false);
			PropertyTable.SetProperty("toolChoice", _currentTool.MachineName, SettingsGroup.LocalSettings, true, false);

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
				return; // Nothing to do.
			}
			_currentArea?.Deactivate(_majorFlexComponentParameters);
			_currentArea = clickedArea;
			PropertyTable.SetProperty("areaChoice", _currentArea.MachineName, SettingsGroup.LocalSettings, true, false);
			_currentArea.Activate(_majorFlexComponentParameters);
		}

		private void SetupPropertyTable()
		{
			PropertyTable = PropertyTableFactory.CreatePropertyTable(_publisher);
			LoadPropertyTable();
			SetDefaultProperties();
			SetTemporaryProperties();
			RemoveObsoleteProperties();
		}

		private void LoadPropertyTable()
		{
			PropertyTable.UserSettingDirectory = FdoFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
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
				PropertyTable.SetProperty("toolChoice", oldStringValue, true, false);
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

			PropertyTable.SetDefault("InitialArea", "lexicon", SettingsGroup.LocalSettings, true, false);
			// Set these properties so they don't get set the first time they're accessed in a browse view menu. (cf. LT-2789)
			PropertyTable.SetDefault("SortedFromEnd", false, SettingsGroup.LocalSettings, true, false);
			PropertyTable.SetDefault("SortedByLength", false, SettingsGroup.LocalSettings, true, false);
			PropertyTable.SetDefault("CurrentToolbarVersion", 1, SettingsGroup.GlobalSettings, true, false);
			PropertyTable.SetDefault("SuspendLoadListUntilOnChangeFilter", string.Empty, SettingsGroup.LocalSettings, false, false);
			PropertyTable.SetDefault("SuspendLoadingRecordUntilOnJumpToRecord", string.Empty, SettingsGroup.LocalSettings, false, false);
			PropertyTable.SetDefault("SelectedWritingSystemHvosForCurrentContextMenu", string.Empty, SettingsGroup.LocalSettings, false, false);
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

			ConvertOldPropertiesToNewIfPresent();
		}

		private void SetTemporaryProperties()
		{
			// Not persisted, but needed at runtime.
			PropertyTable.SetProperty("window", this, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("App", _flexApp, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("cache", Cache, SettingsGroup.BestSettings, false, false);
			PropertyTable.SetProperty("HelpTopicProvider", _flexApp, false, false);
			PropertyTable.SetProperty("FwStyleSheet", _stylesheet, false, false);
		}

		private void RegisterSubscriptions()
		{
			Subscriber.Subscribe("MigrateOldConfigurations", MigrateOldConfigurations);
			Subscriber.Subscribe("StatusPanelRecordNumber", StatusPanelRecordNumber);
			Subscriber.Subscribe("StatusPanelMessage", StatusPanelMessage);
			Subscriber.Subscribe("StatusBarPanelFilter", StatusBarPanelFilter);
			Subscriber.Subscribe("StatusBarPanelSort", StatusBarPanelSort);
		}

		private void MigrateOldConfigurations(object newValue)
		{
			var configMigrator = new DictionaryConfigurationMigrator();
			configMigrator.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
			configMigrator.MigrateOldConfigurationsIfNeeded();
		}

		private void StatusBarPanelFilter(object newValue)
		{
			var newTextMsg = (string)newValue;
			var statusBarPanelFilter = (StatusBarTextBox)_statusbar.Panels["statusBarPanelFilter"];
			statusBarPanelFilter.TextForReal = newTextMsg;
			statusBarPanelFilter.BackBrush = string.IsNullOrEmpty(newTextMsg) ? Brushes.Transparent : Brushes.Yellow;
		}

		private void StatusBarPanelSort(object newValue)
		{
			var newTextMsg = (string)newValue;
			var statusBarPanelSort = (StatusBarTextBox)_statusbar.Panels["statusBarPanelSort"];
			statusBarPanelSort.TextForReal = newTextMsg;
			statusBarPanelSort.BackBrush = string.IsNullOrEmpty(newTextMsg) ? Brushes.Transparent : Brushes.Lime;
		}

		private void StatusPanelMessage(object newValue)
		{
			_statusbar.Panels["statusBarPanelMessage"].Text = (string)newValue;
		}

		private void StatusPanelRecordNumber(object newValue)
		{
			_statusbar.Panels["statusBarPanelRecordNumber"].Text = (string)newValue;
		}

		private void SetupCustomStatusBarPanels()
		{
			_statusbar.SuspendLayout();
			// Insert first, so it ends up last in the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarTextBox(_statusbar)
			{
				TextForReal = "Filter",
				Name = "statusBarPanelFilter",
				MinWidth = 40,
				AutoSize = StatusBarPanelAutoSize.Contents
			});

			// Insert second, so it ends up in the middle of the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarTextBox(_statusbar)
			{
				TextForReal = "Sort",
				Name = "statusBarPanelSort",
				MinWidth = 40,
				AutoSize = StatusBarPanelAutoSize.Contents
			});

			// Insert last, so it ends up first in the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarProgressPanel(_statusbar)
			{
				Name = "statusBarPanelProgressBar",
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
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IRecordListOwner

		/// <summary>Find the IRecordListUpdater object with the given name.</summary>
		public IRecordListUpdater FindRecordListUpdater(string name)
		{
			return PropertyTable.GetValue<IRecordListUpdater>(name, null);
		}

		#endregion

		#region Implementation of IFwMainWnd

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active view of the window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite ActiveView { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the focused control of the window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Control FocusedControl => FromHandle(Win32.GetFocus());

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the data object cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache => _flexApp.Cache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add correspnding stuff to the sidebar, View menu,  etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitAndShowClient()
		{
			CheckDisposed();

			Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a Rectangle representing the position and size of the window in its
		/// normal (non-minimized, non-maximized) state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Rectangle NormalStateDesktopBounds { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// ------------------------------------------------------------------------------------
		public void PreSynchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		/// ------------------------------------------------------------------------------------
		public bool Synchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise.  False should keep the main window
		/// from being shown/initialized (maybe even close the window if false is returned)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool OnFinishedInit()
		{
			CheckDisposed();

#if RANDYTODO
			if (m_startupLink != null)
			{
				var commands = new List<string>
						{
							"AboutToFollowLink",
							"FollowLink"
						};
				var parms = new List<object>
						{
							null,
							m_startupLink
						};
				Publisher.Publish(commands, parms);
			}
			UpdateControls();
#endif
			return true;
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
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare to refresh the main window and its IAreas and ITools.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PrepareToRefresh()
		{
			_currentArea.PrepareToRefresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finish refreshing the main window and its IAreas and ITools.
		/// </summary>
		/// <remarks>
		/// This should call Refresh on real window implementations,
		/// after everything else is done.</remarks>
		/// ------------------------------------------------------------------------------------
		public void FinishRefresh()
		{
#if RANDYTODO
			// TODO: Decide which is right.
			Cache.ServiceLocator.GetInstance<IUndoStackManager>().Refresh();
#else
			// This is from my fork:
			//ZAP?? Cache.ServiceLocator.GetInstance<IUndoStackManager>().Refresh();
#endif
			_currentArea.FinishRefresh();
			Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all the views in this window and in all others in the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
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
		public TreeView TreeStyleRecordList => RecordBarControl?.TreeView;

		/// <summary>
		/// Get the ListView of RecordBarControl, or null if not present, or it is not showing a list.
		/// </summary>
		public ListView ListStyleRecordList => RecordBarControl?.ListView;
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
				_parserMenuManager.Dispose();
				_dataNavigationManager.Dispose();
				IdleQueue.Dispose();

				Subscriber.Unsubscribe("MigrateOldConfigurations", MigrateOldConfigurations);
				Subscriber.Unsubscribe("StatusPanelRecordNumber", StatusPanelRecordNumber);
				Subscriber.Unsubscribe("StatusPanelMessage", StatusPanelRecordNumber);
				Subscriber.Unsubscribe("StatusBarPanelFilter", StatusBarPanelFilter);
				Subscriber.Unsubscribe("StatusBarPanelSort", StatusBarPanelSort);

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

			base.Dispose(disposing);

			if (disposing && PropertyTable != null)
			{
				// Leave the PropertyTable for last, since the above stuff may still want to access it, while shutting down.
				PropertyTable.Dispose();
				PropertyTable = null;
			}
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
				MessageBox.Show(this, string.Format(FrameworkStrings.ksCannotLaunchX, helpFile),
					FrameworkStrings.ksError);
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
					string.Format(FwDirectoryFinder.CodeDirectory + "{0}Language Explorer{0}Movies", Path.DirectorySeparatorChar)), FrameworkStrings.ksError);
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
				MessageBox.Show(null, string.Format(FrameworkStrings.ksCannotLaunchX, path),
					FrameworkStrings.ksError);
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
		/// Refresh wants to first clean up the cache, then give things like Clerks a
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
			// prompt the user to select or create a FieldWorks backup
			var filesToArchive = _flexApp.FwManager.ArchiveProjectWithRamp(_flexApp, this);

			// if there are no files to archive, return now.
			if((filesToArchive == null) || (filesToArchive.Count == 0))
				return;

			// show the RAMP dialog
			var ramp = new ReapRamp();
			ramp.ArchiveNow(this, MainMenuStrip.Font, Icon, filesToArchive, PropertyTable, _flexApp, Cache);
		}

		private void File_Page_Setup(object sender, EventArgs e)
		{
			throw new NotSupportedException("There was no code to support this menu in the original system.");
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
							dlg.RunTask(true, FdoCache.ImportTranslatedLists, filename, Cache);
						}
					});
			}
		}

		private void NewWindow_Clicked(object sender, EventArgs e)
		{
			SaveSettings();
			_flexApp.FwManager.OpenNewWindowForApp();
		}

		private void Help_Training_Writing_Systems(object sender, EventArgs e)
		{
			var pathnameToWritingSystemHelpFile = string.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Language Explorer{0}Training{0}Technical Notes on Writing Systems.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(pathnameToWritingSystemHelpFile, err =>
			{
				MessageBox.Show(null, string.Format(FrameworkStrings.ksCannotShowX, pathnameToWritingSystemHelpFile),
					FrameworkStrings.ksError);
			});
		}

		private void Help_XLingPaper(object sender, EventArgs e)
		{
			var xLingPaperPathname = string.Format(FwDirectoryFinder.CodeDirectory + "{0}Helps{0}XLingPap{0}UserDoc.htm",
				Path.DirectorySeparatorChar);

			OpenDocument(xLingPaperPathname, err =>
			{
				MessageBox.Show(null, string.Format(FrameworkStrings.ksCannotShowX, xLingPaperPathname),
					FrameworkStrings.ksError);
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

		private void EditMenu_Opening(object sender, EventArgs e)
		{
			var hasActiveView = _viewHelper.ActiveView != null;
			selectAllToolStripMenuItem.Enabled = hasActiveView;
			cutToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper.CanCut());
			copyToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper.CanCopy());
			pasteToolStripMenuItem.Enabled = (hasActiveView && _viewHelper.ActiveView.EditingHelper.CanPaste());
			pasteHyperlinkToolStripMenuItem.Enabled = (hasActiveView
				&& _viewHelper.ActiveView.EditingHelper is RootSiteEditingHelper
				&& ((RootSiteEditingHelper)_viewHelper.ActiveView.EditingHelper).CanPasteUrl());
#if RANDYTODO
			// TODO: Handle enabling/disabling other Edit menu/toolbar items, such as Undo & Redo.
#else
			// TODO: In the meantime, just go with disabled.
			undoToolStripMenuItem.Enabled = false;
			redoToolStripMenuItem.Enabled = false;
			undoToolStripButton.Enabled = false;
			redoToolStripButton.Enabled = false;
#endif
		}

		private void File_Export_Global(object sender, EventArgs e)
		{
			// This handles the general case if nobody else is handling it.
			// Other handlers:
			//		A. The notebook area does its version for all of its tools.
			//		B. The "grammarSketch" tool in the 'grammar" area does its own thing. (Same as below, but without AreCustomFieldsAProblem and ActivateUI.)
			// Not visible and thus, not enabled:
			//		A. Tools in "textsWords": complexConcordance, concordance, corpusStatistics, and interlinearEdit
			// Stuff that uses this code:
			//		A. lexicon area: all 8 tools
			//		B. textsWords area: Analyses, bulkEditWordforms, wordListConcordance (all use "concordanceWords" clerk, so can do export)
			//		C. grammar area: all tools, except grammarSketch,, which goes its own way
			//		D. lists area: all 27 tools
#if RANDYTODO
			// TODO: RecordClerk's "AreCustomFieldsAProblem" method will also need a new home: maybe FDO is a better place for it.
			// It's somewhat unfortunate that this bit of code knows what classes can have custom fields.
			// However, we put in code to prevent punctuation in custom field names at the same time as this check (which is therefore
			// for the benefit of older projects), so it should not be necessary to check any additional classes we allow to have them.
			if (AreCustomFieldsAProblem(new int[] { LexEntryTags.kClassId, LexSenseTags.kClassId, LexExampleSentenceTags.kClassId, MoFormTags.kClassId }))
				return true;
			using (var dlg = new ExportDialog())
			{
				dlg.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
				dlg.ShowDialog();
			}
#if RANDYTODO
			// TODO: This method is on RecordClerk, so figure out how to call it from here, if it is still needed at all.
			ActivateUI(true);
#endif
#else
			MessageBox.Show(this, "Export not yet implemented. Stay tuned.", "Export not ready", MessageBoxButtons.OK);
#endif
		}

		private void Edit_Paste_Hyperlink(object sender, EventArgs e)
		{
			if (_stylesheet == null)
			{
				_stylesheet = new FwStyleSheet();
				_stylesheet.Init(Cache, Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
#if RANDYTODO
				// TODO: I (RandyR) don't think there is a reason to do this now,
				// unless there is some style UI widget on the toolbar (menu?) that needs to be updated.
				if (m_rebarAdapter is IUIAdapterForceRegenerate)
				{
					((IUIAdapterForceRegenerate)m_rebarAdapter).ForceFullRegenerate();
				}
#endif
			}
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
				if (DataUpdateMonitor.IsUpdateInProgress())
				{
					return;
				}
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
			base.OnLoad(e);

			var currentArea = _areaRepository.GetPersistedOrDefaultArea();
			var currentTool = currentArea.GetPersistedOrDefaultToolForArea();
			_sidePane.TabClicked += Area_Clicked;
			_sidePane.ItemClicked += Tool_Clicked;
			// This call fires Area_Clicked and then Tool_Clicked to make sure the provided tab and item are both selected in the end.
			_sidePane.SelectItem(_sidePane.GetTabByName(currentArea.MachineName), currentTool.MachineName);
		}

#endregion
	}
}

// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.DictionaryConfiguration;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.Dictionary
{
	/// <summary>
	/// ITool implementation for the "lexiconDictionary" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class LexiconDictionaryTool : ITool
	{
		private LexiconDictionaryToolMenuHelper _toolMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private IRecordList _recordList;
		private XhtmlDocView DocView { get; set; }
		[Import(AreaServices.LexiconAreaMachineName)]
		private IArea _area;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// This will also remove any event handlers set up by the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

			DocView = null;
			_toolMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LanguageExplorerConstants.Entries, majorFlexComponentParameters.StatusBar, LexiconArea.EntriesFactoryMethod);
			}
			var root = XDocument.Parse(LexiconResources.LexiconDictionaryToolParameters).Root;
			_toolMenuHelper = new LexiconDictionaryToolMenuHelper(majorFlexComponentParameters, this, _recordList, root.Attribute("configureObjectName").Value);
			DocView = new XhtmlDocView(root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_toolMenuHelper.DocView = DocView;
			var docViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var rightSpacer = new Spacer
			{
				Width = 10,
				Dock = DockStyle.Right
			};
			var rightPanelMenu = new PanelMenu(_toolMenuHelper.MainPanelMenuContextMenuFactory, AreaServices.RightPanelMenuId)
			{
				Dock = DockStyle.Right,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			var leftSpacer = new Spacer
			{
				Width = 10,
				Dock = DockStyle.Left
			};
			var leftPanelMenu = new PanelMenu(_toolMenuHelper.MainPanelMenuContextMenuFactory, AreaServices.LeftPanelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			docViewPaneBar.AddControls(new List<Control> { leftPanelMenu, leftSpacer, rightPanelMenu, rightSpacer });
			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer,
				DocView, docViewPaneBar);

			_paneBarContainer.ResumeLayout(true);
			DocView.FinishInitialization();
			DocView.SelectedPublication = "DictionaryPublicationLayout";
			_paneBarContainer.PostLayoutInit();
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			DocView.PublicationDecorator.Refresh();
			_recordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordList.VirtualListPublisher).Refresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.LexiconDictionaryMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.LexiconDictionaryUiName);

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.DocumentView.SetBackgroundColor(Color.Magenta);

		#endregion

		private sealed class LexiconDictionaryToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private IPropertyTable _propertyTable;
			private IFwMainWnd _fwMainWnd;
			private IRecordList _recordList;
			internal XhtmlDocView DocView { private get; set; }
			internal PanelMenuContextMenuFactory MainPanelMenuContextMenuFactory { get; private set; }
			private SharedLexiconToolsUiWidgetHelper _sharedLexiconToolsUiWidgetHelper;
			private string _configureObjectName;

			internal LexiconDictionaryToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, IRecordList recordList, string configureObjectName)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNullOrEmptyString(configureObjectName, nameof(configureObjectName));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordList = recordList;
				_configureObjectName = configureObjectName;
				_propertyTable = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
				_fwMainWnd = _majorFlexComponentParameters.MainWindow;
				MainPanelMenuContextMenuFactory = new PanelMenuContextMenuFactory();
				_sharedLexiconToolsUiWidgetHelper = new SharedLexiconToolsUiWidgetHelper(_majorFlexComponentParameters, _recordList);

				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				SetupUiWidgets(toolUiWidgetParameterObject);
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(AreaServices.LeftPanelMenuId, CreateLeftMainPanelContextMenuStrip);
				MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(AreaServices.RightPanelMenuId, CreateRightMainPanelContextMenuStrip);
			}

			private void SetupUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
			{
				toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Edit].Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdFindAndReplaceText_Click, () => UiWidgetServices.CanSeeAndDo));
				toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert].Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdFindAndReplaceText_Click, () => UiWidgetServices.CanSeeAndDo));
				_sharedLexiconToolsUiWidgetHelper.SetupToolUiWidgets(toolUiWidgetParameterObject, new HashSet<Command> { Command.CmdGoToEntry, Command.CmdInsertLexEntry, Command.CmdConfigureDictionary });
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateLeftMainPanelContextMenuStrip(string panelMenuId)
			{
				Require.That(panelMenuId == AreaServices.LeftPanelMenuId, $"I don't know how to create a context menu with an ID of '{panelMenuId}', as I can only create one with an id of '{AreaServices.LeftPanelMenuId}'.");

				var currentPublication = _propertyTable.GetValue<string>(LanguageExplorerConstants.SelectedPublication);
				var contextMenuStrip = new ContextMenuStrip();

				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
				var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
				// <item command="CmdShowAllPublications" />
				var currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ShowAllPublications_Clicked, LexiconResources.AllEntries);
				currentToolStripMenuItem.Tag = "All Entries";
				currentToolStripMenuItem.Checked = (currentPublication == "All Entries");
				var pubName = DocView.GetCurrentPublication();
				currentToolStripMenuItem.Checked = (LanguageExplorerResources.AllEntriesPublication == pubName);
				// // <menu list="Publications" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="SelectedPublication" />
				List<string> inConfig;
				List<string> notInConfig;
				DocView.SplitPublicationsByConfiguration(_majorFlexComponentParameters.LcmCache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS, DocView.GetCurrentConfiguration(false), out inConfig, out notInConfig);
				foreach (var pub in inConfig)
				{
					currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Publication_Clicked, pub);
					currentToolStripMenuItem.Tag = pub;
					currentToolStripMenuItem.Checked = (currentPublication == pub);
				}
				if (notInConfig.Any())
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
					foreach (var pub in notInConfig)
					{
						currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Publication_Clicked, pub);
						currentToolStripMenuItem.Tag = pub;
						currentToolStripMenuItem.Checked = (currentPublication == pub);
					}
				}

				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <item command="CmdPublicationsJumpToDefault" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, EditPublications_Clicked, LexiconResources.EditPublications);

				return retVal;
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateRightMainPanelContextMenuStrip(string panelMenuId)
			{
				Require.That(panelMenuId == AreaServices.RightPanelMenuId, $"I don't know how to create a context menu with an ID of '{panelMenuId}', as I can only create one with an id of '{AreaServices.RightPanelMenuId}'.");

				var contextMenuStrip = new ContextMenuStrip();

				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
				var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
				ToolStripMenuItem currentToolStripMenuItem;

				// 1. <menu list="Configurations" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="DictionaryPublicationLayout"/>
				IDictionary<string, string> hasPub;
				IDictionary<string, string> doesNotHavePub;
				var allConfigurations = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(_propertyTable.GetValue<LcmCache>(FwUtils.cache), _configureObjectName);
				DocView.SplitConfigurationsByPublication(allConfigurations, DocView.GetCurrentPublication(), out hasPub, out doesNotHavePub);
				// Add menu items that display the configuration name and send PropChanges with
				// the configuration path.
				var currentPublication = _propertyTable.GetValue<string>("DictionaryPublicationLayout");
				foreach (var config in hasPub)
				{
					// Key is label, value is Tag for config pathname.
					currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Configuration_Clicked, config.Key);
					currentToolStripMenuItem.Tag = config.Value;
					currentToolStripMenuItem.Checked = (currentPublication == config.Value);

				}
				if (doesNotHavePub.Any())
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
					foreach (var config in doesNotHavePub)
					{
						currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Configuration_Clicked, config.Key);
						currentToolStripMenuItem.Tag = config.Value;
						currentToolStripMenuItem.Checked = (currentPublication == config.Value);
					}
				}

				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ConfigureDictionary_Clicked, LexiconResources.ConfigureDictionary);

				return retVal;
			}

			private void Configuration_Clicked(object sender, EventArgs e)
			{
				var clickedToolStripMenuItem = (ToolStripMenuItem)sender;
				if (clickedToolStripMenuItem.Checked)
				{
					return; // No change.
				}
				var newValue = (string)clickedToolStripMenuItem.Tag;
				_propertyTable.SetProperty("DictionaryPublicationLayout", newValue, true, settingsGroup: SettingsGroup.LocalSettings);
				DocView.SelectedPublication = "DictionaryPublicationLayout";
			}

			private void ConfigureDictionary_Clicked(object sender, EventArgs e)
			{
				if (DictionaryConfigurationDlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters, (Form)_fwMainWnd, _recordList?.CurrentObject, "khtpConfigureDictionary", LanguageExplorerResources.Dictionary))
				{
					_fwMainWnd.RefreshAllViews();
				}
			}

			private void EditPublications_Clicked(object sender, EventArgs e)
			{
				/*
			<command id="CmdPublicationsJumpToDefault" label="Edit Publications" message="JumpToTool">
				<parameters tool="publicationsEdit" className="ICmPossibility" ownerClass="LangProject" ownerField="PublicationTypes" />
			</command>
				 */
				MessageBox.Show((Form)_fwMainWnd, @"Stay tuned for jump to: 'publicationsEdit' in list 'LangProject->PublicationTypes'");
			}

			private void Publication_Clicked(object sender, EventArgs e)
			{
				var clickedToolStripMenuItem = (ToolStripMenuItem)sender;
				if (clickedToolStripMenuItem.Checked)
				{
					return; // No change.
				}
				var newValue = (string)clickedToolStripMenuItem.Tag;
				_propertyTable.SetProperty(LanguageExplorerConstants.SelectedPublication, newValue, true, settingsGroup: SettingsGroup.LocalSettings);
				DocView.SelectedPublication = LanguageExplorerConstants.SelectedPublication;
			}

			private void ShowAllPublications_Clicked(object sender, EventArgs e)
			{
				_propertyTable.SetProperty(LanguageExplorerConstants.SelectedPublication, LanguageExplorerResources.AllEntriesPublication, true);
				DocView.SelectedPublication = LanguageExplorerConstants.SelectedPublication;
			}

			private void CmdFindAndReplaceText_Click(object sender, EventArgs e)
			{
				DocView.FindText();
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~LexiconDictionaryToolMenuHelper()
			{
				// The base class finalizer is called automatically.
				Dispose(false);
			}

			/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SuppressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (_isDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					_sharedLexiconToolsUiWidgetHelper.Dispose();
					MainPanelMenuContextMenuFactory.Dispose();
				}
				_majorFlexComponentParameters = null;
				MainPanelMenuContextMenuFactory = null;
				_sharedLexiconToolsUiWidgetHelper = null;
				DocView = null;
				_propertyTable = null;
				_fwMainWnd = null;
				_recordList = null;
				_configureObjectName = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
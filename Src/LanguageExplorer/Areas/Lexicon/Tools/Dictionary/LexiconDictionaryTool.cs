// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.Dictionary
{
	/// <summary>
	/// ITool implementation for the "lexiconDictionary" tool in the "lexicon" area.
	/// </summary>
	internal sealed class LexiconDictionaryTool : ITool
	{
		private string _configureObjectName;
		private IFwMainWnd _fwMainWnd;
		private LcmCache _cache;
		private PaneBarContainer _paneBarContainer;
		private RecordClerk _recordClerk;
		private XhtmlDocView _xhtmlDocView;
		private SliceContextMenuFactory _sliceContextMenuFactory;
		private const string leftPanelMenuId = "left";
		private const string rightPanelMenuId = "right";


		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_sliceContextMenuFactory.Dispose(); // No Data Tree in this tool to dispose of it for us.
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);
			_xhtmlDocView = null;
			_fwMainWnd = null;
			_cache = null;
			_sliceContextMenuFactory = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_cache = majorFlexComponentParameters.LcmCache;
			_sliceContextMenuFactory = new SliceContextMenuFactory(); // Make our own, since the tool has no data tree.
			RegisterContextMenuMethods();
			_fwMainWnd = majorFlexComponentParameters.MainWindow;
			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(LexiconArea.Entries, majorFlexComponentParameters.Statusbar, LexiconArea.EntriesFactoryMethod);
			}
			var root = XDocument.Parse(LexiconResources.LexiconDictionaryToolParameters).Root;
			_configureObjectName = root.Attribute("configureObjectName").Value;
			_xhtmlDocView = new XhtmlDocView(root, majorFlexComponentParameters.LcmCache, _recordClerk);
			var docViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);

			var rightSpacer = new Spacer
			{
				Width = 10,
				Dock = DockStyle.Right
			};
			var rightPanelMenu = new PanelMenu(_sliceContextMenuFactory, rightPanelMenuId)
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
			var leftPanelMenu = new PanelMenu(_sliceContextMenuFactory, leftPanelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			docViewPaneBar.AddControls(new List<Control> { leftPanelMenu, leftSpacer, rightPanelMenu, rightSpacer });

			_paneBarContainer = PaneBarContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				docViewPaneBar,
				_xhtmlDocView);

			_paneBarContainer.ResumeLayout(true);
			_xhtmlDocView.FinishInitialization();
			_xhtmlDocView.OnPropertyChanged("DictionaryPublicationLayout");
			_paneBarContainer.PostLayoutInit();
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
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
			_xhtmlDocView.PublicationDecorator.Refresh();
			_recordClerk.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordClerk.VirtualListPublisher).Refresh();
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
		public string MachineName => "lexiconDictionary";

#if RANDYTODO
		// TODO: It displays fine the first time it is selected, but the second time PropertyTable is upset in a multi-thread context.
		// TODO: Feed all expected stuff from the PropertyTable into the threaded context, but not the table itself.
#endif
		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "BUGGY: Dictionary";
#endregion

#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "lexicon";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.DocumentView.SetBackgroundColor(Color.Magenta);

#endregion

		private void RegisterContextMenuMethods()
		{
			_sliceContextMenuFactory.RegisterPanelMenuCreatorMethod(leftPanelMenuId, CreateLeftContextMenuStrip);
			_sliceContextMenuFactory.RegisterPanelMenuCreatorMethod(rightPanelMenuId, CreateRightContextMenuStrip);
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateRightContextMenuStrip(string panelMenuId)
		{
			var contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Opening += RightContextMenuStrip_Opening;

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, RightContextMenuStrip_Opening, menuItems);
			ToolStripMenuItem currentToolStripMenuItem;

			// 1. <menu list="Configurations" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="DictionaryPublicationLayout"/>
			IDictionary<string, string> hasPub;
			IDictionary<string, string> doesNotHavePub;
			var allConfigurations = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(PropertyTable.GetValue<LcmCache>("cache"), _configureObjectName);
			_xhtmlDocView.SplitConfigurationsByPublication(allConfigurations, _xhtmlDocView.GetCurrentPublication(), out hasPub, out doesNotHavePub);
			// Add menu items that display the configuration name and send PropChanges with
			// the configuration path.
			var currentPublication = PropertyTable.GetValue<string>("DictionaryPublicationLayout");
			foreach (var config in hasPub)
			{
				// Key is label, value is Tag for config pathname.
				currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Configuration_Clicked, string.Empty, config.Key);
				currentToolStripMenuItem.Tag = config.Value;
				currentToolStripMenuItem.Checked = (currentPublication == config.Value);

			}
			if (doesNotHavePub.Count > 0)
			{
				contextMenuStrip.Items.Add(new ToolStripSeparator());
				foreach (var config in doesNotHavePub)
				{
					currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Configuration_Clicked, string.Empty, config.Key);
					currentToolStripMenuItem.Tag = config.Value;
					currentToolStripMenuItem.Checked = (currentPublication == config.Value);
				}
			}

			contextMenuStrip.Items.Add(new ToolStripSeparator());
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ConfigureDictionary_Clicked, "Configure Dictionary");

			return retVal;
		}

		private void RightContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			var rightContextMenuStrip = (ContextMenuStrip)sender;
			var currentlayout = PropertyTable.GetValue<string>("DictionaryPublicationLayout", SettingsGroup.LocalSettings);
			// Make sure matching menu is checked, but none of the others are checked
			foreach (var toolStripMenuItem in rightContextMenuStrip.Items.OfType<ToolStripMenuItem>())
			{
				toolStripMenuItem.Checked = (string)toolStripMenuItem.Tag == currentlayout;
			}
		}

		private void Configuration_Clicked(object sender, EventArgs e)
		{
			var clickedToolStripMenuItem = (ToolStripMenuItem)sender;
			if (clickedToolStripMenuItem.Checked)
			{
				return; // No change.
			}
			var newValue = (string)clickedToolStripMenuItem.Tag;
			PropertyTable.SetProperty("DictionaryPublicationLayout", newValue, SettingsGroup.LocalSettings, true, false);
			_xhtmlDocView.OnPropertyChanged("DictionaryPublicationLayout");
		}

		private void ConfigureDictionary_Clicked(object sender, EventArgs e)
		{
			bool refreshNeeded;
			using (var dlg = new DictionaryConfigurationDlg(PropertyTable))
			{
				var controller = new DictionaryConfigurationController(dlg, _recordClerk?.CurrentObject);
				controller.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.Text = string.Format(xWorksStrings.ConfigureTitle, xWorksStrings.Dictionary);
				dlg.HelpTopic = "khtpConfigureDictionary";
				dlg.ShowDialog((IWin32Window)_fwMainWnd);
				refreshNeeded = controller.MasterRefreshRequired;
			}
			if (refreshNeeded)
			{
				_fwMainWnd.RefreshAllViews();
			}
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateLeftContextMenuStrip(string panelMenuId)
		{
			var currentPublication = PropertyTable.GetValue<string>("SelectedPublication");
			var contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Opening += LeftContextMenuStrip_Opening;

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, LeftContextMenuStrip_Opening, menuItems);

			var currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ShowAllPublications_Clicked, "All Entries");
			currentToolStripMenuItem.Tag = "All Entries";
			currentToolStripMenuItem.Checked = (currentPublication == "All Entries");
			var pubName = _xhtmlDocView.GetCurrentPublication();
			currentToolStripMenuItem.Checked = (xWorksStrings.AllEntriesPublication == pubName);

			contextMenuStrip.Items.Add(new ToolStripSeparator());

			List<string> inConfig;
			List<string> notInConfig;
			_xhtmlDocView.SplitPublicationsByConfiguration(_cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
			_xhtmlDocView.GetCurrentConfiguration(false), out inConfig, out notInConfig);
			foreach (var pub in inConfig)
			{
				currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Publication_Clicked, pub);
				currentToolStripMenuItem.Tag = pub;
				currentToolStripMenuItem.Checked = (currentPublication == pub);
			}
			if (notInConfig.Any())
			{
				contextMenuStrip.Items.Add(new ToolStripSeparator());
				foreach (var pub in notInConfig)
				{
					currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Publication_Clicked, pub);
					currentToolStripMenuItem.Tag = pub;
					currentToolStripMenuItem.Checked = (currentPublication == pub);
				}
			}

			contextMenuStrip.Items.Add(new ToolStripSeparator());
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, EditPublications_Clicked, "Edit Publications");

			return retVal;
		}

		private void LeftContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
			var leftContextMenuStrip = (ContextMenuStrip)sender;
			var currentPublication = PropertyTable.GetValue<string>("SelectedPublication", SettingsGroup.LocalSettings);
			// Make sure matching menu is checked, but none of the others are checked
			foreach (var toolStripMenuItem in leftContextMenuStrip.Items.OfType<ToolStripMenuItem>())
			{
				toolStripMenuItem.Checked = (string)toolStripMenuItem.Tag == currentPublication;
			}
		}

		private void EditPublications_Clicked(object sender, EventArgs e)
		{
			/*
		<command id="CmdPublicationsJumpToDefault" label="Edit Publications" message="JumpToTool">
			<parameters tool="publicationsEdit" className="ICmPossibility" ownerClass="LangProject" ownerField="PublicationTypes" />
		</command>
			 */
			MessageBox.Show((Form)_fwMainWnd, "Stay tuned for jump to: 'publicationsEdit' in list 'LangProject->PublicationTypes'");
		}

		private void Publication_Clicked(object sender, EventArgs e)
		{
			var clickedToolStripMenuItem = (ToolStripMenuItem)sender;
			if (clickedToolStripMenuItem.Checked)
			{
				return; // No change.
			}
			var newValue = (string)clickedToolStripMenuItem.Tag;
			PropertyTable.SetProperty("SelectedPublication", newValue, SettingsGroup.LocalSettings, true, false);
			_xhtmlDocView.OnPropertyChanged("SelectedPublication");
		}

		private void ShowAllPublications_Clicked(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("SelectedPublication", xWorksStrings.AllEntriesPublication, true, false);
			_xhtmlDocView.OnPropertyChanged("SelectedPublication");
		}
	}
}
// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lexicon.Tools.Dictionary
{
	/// <summary>
	/// ITool implementation for the "lexiconDictionary" tool in the "lexicon" area.
	/// </summary>
	internal sealed class LexiconDictionaryTool : ITool
	{
		private string _configureObjectName;
		private PaneBarContainer _paneBarContainer;
		private RecordClerk _recordClerk;
		private XhtmlDocView _xhtmlDocView;
		private ContextMenuStrip _leftContextMenuStrip;
		private readonly HashSet<Tuple<ToolStripMenuItem, EventHandler>> _leftMenusAndHandlers = new HashSet<Tuple<ToolStripMenuItem, EventHandler>>();
		private ContextMenuStrip _rightContextMenuStrip;
		private readonly HashSet<Tuple<ToolStripMenuItem, EventHandler>> _rightMenusAndHandlers = new HashSet<Tuple<ToolStripMenuItem, EventHandler>>();


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
			PropertyTable.RemoveProperty("ActiveClerk");
			_leftContextMenuStrip.Opening -= LeftContextMenuStrip_Opening;
			_leftContextMenuStrip.Dispose();
			_leftContextMenuStrip = null;

			_rightContextMenuStrip.Opening -= RightContextMenuStrip_Opening;
			_rightContextMenuStrip.Dispose();
			_rightContextMenuStrip = null;

			PaneBarContainerFactory.RemoveFromParentAndDispose(
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				ref _paneBarContainer,
				ref _recordClerk);
			_xhtmlDocView = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var cache = PropertyTable.GetValue<FdoCache>("cache");
			var root = XDocument.Parse(LexiconResources.LexiconDictionaryToolParameters).Root;
			_configureObjectName = root.Attribute("configureObjectName").Value;
			var flexComponentParameters = new FlexComponentParameters(PropertyTable, Publisher, Subscriber);
			_recordClerk = LexiconArea.CreateBasicClerkForLexiconArea(cache);
			PropertyTable.SetProperty("ActiveClerk", _recordClerk, false, false);
			_recordClerk.InitializeFlexComponent(flexComponentParameters);
			_xhtmlDocView = new XhtmlDocView(root, _recordClerk);
			var docViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);

			var rightSpacer = new Spacer
			{
				Width = 10,
				Dock = DockStyle.Right
			};
			var rightPanelMenu = new PanelMenu
			{
				Dock = DockStyle.Right,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center,
				ContextMenuStrip = CreateRightContextMenuStrip()
			};

			var leftSpacer = new Spacer
			{
				Width = 10,
				Dock = DockStyle.Left
			};
			var leftPanelMenu = new PanelMenu
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center,
				ContextMenuStrip = CreateLeftContextMenuStrip()
			};
			docViewPaneBar.AddControls(new List<Control> { leftPanelMenu, leftSpacer, rightPanelMenu, rightSpacer });

			_paneBarContainer = PaneBarContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				docViewPaneBar,
				_xhtmlDocView);

			_xhtmlDocView.FinishInitialization();
			_xhtmlDocView.OnPropertyChanged("DictionaryPublicationLayout");
			_paneBarContainer.PostLayoutInit();
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerk;
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

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Dictionary";
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

		private ContextMenuStrip CreateRightContextMenuStrip()
		{
			_rightContextMenuStrip = new ContextMenuStrip();
			_rightContextMenuStrip.Opening += RightContextMenuStrip_Opening;

			return _rightContextMenuStrip;
		}

		private void RightContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_rightContextMenuStrip.Items.Count > 0)
			{
				foreach (var menuTuple in _rightMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_rightContextMenuStrip.Items.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_rightMenusAndHandlers.Clear();
				_rightContextMenuStrip.Items.Clear();
			}

			// Create new menus each time, and put them in _rightMenusAndHandlers.
			ToolStripMenuItem currentToolStripMenuItem;

			// 1. <menu list="Configurations" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="DictionaryPublicationLayout"/>
			IDictionary<string, string> hasPub;
			IDictionary<string, string> doesNotHavePub;
			var allConfigurations = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(PropertyTable.GetValue<FdoCache>("cache"), _configureObjectName);
			_xhtmlDocView.SplitConfigurationsByPublication(allConfigurations,
														_xhtmlDocView.GetCurrentPublication(),
														out hasPub, out doesNotHavePub);
			// Add menu items that display the configuration name and send PropChanges with
			// the configuration path.
			var currentPublication = PropertyTable.GetValue<string>("DictionaryPublicationLayout");
			foreach (var config in hasPub)
			{
				// Key is label, value is Tag for config pathname.
				currentToolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(_rightContextMenuStrip, config.Key, null, Configuration_Clicked, null);
				currentToolStripMenuItem.Tag = config.Value;
				currentToolStripMenuItem.Checked = (currentPublication == config.Value);
				_rightMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(currentToolStripMenuItem, Configuration_Clicked));

			}
			if (doesNotHavePub.Count > 0)
			{
				_rightContextMenuStrip.Items.Add(new ToolStripMenuItem("-"));
				foreach (var config in doesNotHavePub)
				{
					currentToolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(_rightContextMenuStrip, config.Key, null, Configuration_Clicked, null);
					currentToolStripMenuItem.Tag = config.Value;
					currentToolStripMenuItem.Checked = (currentPublication == config.Value);
					_rightMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(currentToolStripMenuItem, Configuration_Clicked));
				}
			}

			_rightContextMenuStrip.Items.Add(new ToolStripMenuItem("-"));
			currentToolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(_rightContextMenuStrip, "Configure Dictionary", null, ConfigureDictionary_Clicked, null);
			_rightMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(currentToolStripMenuItem, ConfigureDictionary_Clicked));
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
			foreach (var menuItemTuple in _rightMenusAndHandlers)
			{
				clickedToolStripMenuItem.Checked = (((string)menuItemTuple.Item1.Tag)== newValue);
			}
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
				dlg.ShowDialog(PropertyTable.GetValue<IWin32Window>("window"));
				refreshNeeded = controller.MasterRefreshRequired;
			}
			if (refreshNeeded)
			{
				Publisher.Publish("MasterRefresh", null);
			}
		}

		private ContextMenuStrip CreateLeftContextMenuStrip()
		{
			_leftContextMenuStrip = new ContextMenuStrip();
			_leftContextMenuStrip.Opening += LeftContextMenuStrip_Opening;

			return _leftContextMenuStrip;
		}

		private void LeftContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Create new menus each time, and put them in _leftMenusAndHandlers
			if (_leftContextMenuStrip.Items.Count > 0)
			{
				foreach (var menuTuple in _leftMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_leftContextMenuStrip.Items.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_leftMenusAndHandlers.Clear();
				_leftContextMenuStrip.Items.Clear();
			}

			var currentPublication = PropertyTable.GetValue<string>("SelectedPublication");
			ToolStripMenuItem currentToolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(_leftContextMenuStrip, "All Entries", null, ShowAllPublications_Clicked, null);
			currentToolStripMenuItem.Tag = "All Entries";
			currentToolStripMenuItem.Checked = (currentPublication == "All Entries");
			var pubName = _xhtmlDocView.GetCurrentPublication();
			currentToolStripMenuItem.Checked = (xWorksStrings.AllEntriesPublication == pubName);
			_leftMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(currentToolStripMenuItem, ShowAllPublications_Clicked));

			_leftContextMenuStrip.Items.Add(new ToolStripMenuItem("-"));

			var cache = PropertyTable.GetValue<FdoCache>("cache");
			List<string> inConfig;
			List<string> notInConfig;
			_xhtmlDocView.SplitPublicationsByConfiguration(cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
			_xhtmlDocView.GetCurrentConfiguration(false),
																   out inConfig, out notInConfig);
			foreach (var pub in inConfig)
			{
				currentToolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(_leftContextMenuStrip, pub, null, Publication_Clicked, null);
				currentToolStripMenuItem.Tag = pub;
				currentToolStripMenuItem.Checked = (currentPublication == pub);
				_leftMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(currentToolStripMenuItem, Publication_Clicked));
			}
			if (notInConfig.Any())
			{
				_leftContextMenuStrip.Items.Add(new ToolStripMenuItem("-"));
				foreach (var pub in notInConfig)
				{
					currentToolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(_leftContextMenuStrip, pub, null, Publication_Clicked, null);
					currentToolStripMenuItem.Tag = pub;
					currentToolStripMenuItem.Checked = (currentPublication == pub);
					_leftMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(currentToolStripMenuItem, Publication_Clicked));
				}
			}

			_leftContextMenuStrip.Items.Add(new ToolStripMenuItem("-"));
			currentToolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(_leftContextMenuStrip, "Edit Publications", null, EditPublications_Clicked, null);
			_leftMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(currentToolStripMenuItem, EditPublications_Clicked));
		}

		private void EditPublications_Clicked(object sender, EventArgs e)
		{
			/*
		<command id="CmdPublicationsJumpToDefault" label="Edit Publications" message="JumpToTool">
			<parameters tool="publicationsEdit" className="ICmPossibility" ownerClass="LangProject" ownerField="PublicationTypes" />
		</command>
			 */
			MessageBox.Show(PropertyTable.GetValue<Form>("window"), "Stay tuned for jump to: 'publicationsEdit' in list 'LangProject->PublicationTypes'");
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
			foreach (var menuItemTuple in _leftMenusAndHandlers)
			{
				clickedToolStripMenuItem.Checked = (((string)menuItemTuple.Item1.Tag) == newValue);
			}
			_xhtmlDocView.OnPropertyChanged("SelectedPublication");
		}

		private void ShowAllPublications_Clicked(object sender, EventArgs e)
		{
			PropertyTable.SetProperty("SelectedPublication", xWorksStrings.AllEntriesPublication, true, false);
			_xhtmlDocView.OnPropertyChanged("SelectedPublication");
		}
	}
}
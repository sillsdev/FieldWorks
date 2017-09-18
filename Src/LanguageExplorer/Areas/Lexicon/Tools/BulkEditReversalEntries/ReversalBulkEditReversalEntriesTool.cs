// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.LcmUi;
using LanguageExplorer.Works;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.BulkEditReversalEntries
{
	/// <summary>
	/// ITool implementation for the "reversalBulkEditReversalEntries" tool in the "lexicon" area.
	/// </summary>
	internal sealed class ReversalBulkEditReversalEntriesTool : ITool
	{
		private PaneBarContainer _paneBarContainer;
		private RecordBrowseView _recordBrowseView;
		private RecordClerk _recordClerk;
		private IReversalIndexRepository _reversalIndexRepository;
		private IReversalIndex _currentReversalIndex;
		private LcmCache _cache;
		private SliceContextMenuFactory _sliceContextMenuFactory;
		private const string panelMenuId = "left";

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
			_sliceContextMenuFactory.Dispose();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);
			_reversalIndexRepository = null;
			_currentReversalIndex = null;
			_recordBrowseView = null;
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
			_sliceContextMenuFactory = new SliceContextMenuFactory();
			_cache = majorFlexComponentParameters.LcmCache;
			var currentGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
			if (currentGuid != Guid.Empty)
			{
				_currentReversalIndex = (IReversalIndex)majorFlexComponentParameters.LcmCache.ServiceLocator.GetObject(currentGuid);
			}
			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(LexiconArea.AllReversalEntries, majorFlexComponentParameters.Statusbar, LexiconArea.AllReversalEntriesFactoryMethod);
			}
			_sliceContextMenuFactory.RegisterPanelMenuCreatorMethod(panelMenuId, CreatePanelContextMenuStrip);
			_recordBrowseView = new RecordBrowseView(XDocument.Parse(LexiconResources.ReversalBulkEditReversalEntriesToolParameters).Root, majorFlexComponentParameters.LcmCache, _recordClerk);
			var browseViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var panelMenu = new PanelMenu(_sliceContextMenuFactory, panelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			browseViewPaneBar.AddControls(new List<Control> { panelMenu });

			_paneBarContainer = PaneBarContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				browseViewPaneBar,
				_recordBrowseView);
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_recordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
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
		public string MachineName => "reversalBulkEditReversalEntries";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Bulk Edit Reversal Entries";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "lexicon";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.BrowseView.SetBackgroundColor(Color.Magenta);

		#endregion

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> CreatePanelContextMenuStrip(string panelMenuId)
		{
			if (_reversalIndexRepository == null)
			{
				_reversalIndexRepository = _cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			}

			var contextMenuStrip = new ContextMenuStrip();

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MainPanelContextMenuStrip_Opening, menuItems);

			// If allInstancesinRepository has any remaining instances, then they are not in the menu. Add them.
			foreach (var rei in _reversalIndexRepository.AllInstances())
			{
				var newMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItem(contextMenuStrip, rei.ChooserNameTS.Text, null, ReversalIndex_Menu_Clicked, null);
				newMenuItem.Tag = rei;
				if (rei == _currentReversalIndex)
				{
					SetCheckedState(newMenuItem);
				}
				menuItems.Add(new Tuple<ToolStripMenuItem, EventHandler>(newMenuItem, ReversalIndex_Menu_Clicked));
			}

			contextMenuStrip.Items.Add(new ToolStripSeparator());
			var currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItem(contextMenuStrip, "Configure Dictionary", null, ConfigureDictionary_Clicked, null);
			menuItems.Add(new Tuple<ToolStripMenuItem, EventHandler>(currentToolStripMenuItem, ConfigureDictionary_Clicked));

			return retVal;
		}

		private void MainPanelContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
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

		private void ReversalIndex_Menu_Clicked(object sender, EventArgs e)
		{
			var contextMenuItem = (ToolStripMenuItem)sender;
			_currentReversalIndex = (IReversalIndex)contextMenuItem.Tag;
			PropertyTable.SetProperty("ReversalIndexGuid", _currentReversalIndex.Guid.ToString(), SettingsGroup.LocalSettings, true, false);
			((ReversalClerk)_recordClerk).ChangeOwningObjectIfPossible();
			SetCheckedState(contextMenuItem);
		}

		private void SetCheckedState(ToolStripMenuItem reversalToolStripMenuItem)
		{
			var currentTag = (IReversalIndex)reversalToolStripMenuItem.Tag;
			reversalToolStripMenuItem.Checked = (currentTag.Guid.ToString() == PropertyTable.GetValue<string>("ReversalIndexGuid"));
		}
	}
}
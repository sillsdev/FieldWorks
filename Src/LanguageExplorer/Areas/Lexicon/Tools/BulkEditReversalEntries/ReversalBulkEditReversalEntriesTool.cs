// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;

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
		private ContextMenuStrip _contextMenuStrip;
		private IReversalIndexRepository _reversalIndexRepository;
		private IReversalIndex _currentReversalIndex;
		private readonly HashSet<Tuple<ToolStripMenuItem, EventHandler>> _newMenusAndHandlers = new HashSet<Tuple<ToolStripMenuItem, EventHandler>>();

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
			_contextMenuStrip.Opening -= ContextMenuStrip_Opening;
			_contextMenuStrip = null;

			foreach (var menuTuple in _newMenusAndHandlers)
			{
				menuTuple.Item1.Click -= menuTuple.Item2;
			}
			_newMenusAndHandlers.Clear();

			PaneBarContainerFactory.RemoveFromParentAndDispose(
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				ref _paneBarContainer,
				ref _recordClerk);
			_reversalIndexRepository = null;
			_currentReversalIndex = null;
			_recordBrowseView = null;
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
			var currentGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
			if (currentGuid != Guid.Empty)
			{
				_currentReversalIndex = (IReversalIndex)cache.ServiceLocator.GetObject(currentGuid);
			}
			_recordClerk = new ReversalEntryClerk(cache.ServiceLocator, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), _currentReversalIndex);
			_recordClerk.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
			_recordBrowseView = new RecordBrowseView(XDocument.Parse(LexiconResources.ReversalBulkEditReversalEntriesToolParameters).Root, _recordClerk);
			var browseViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var panelMenu = new PanelMenu
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center,
				ContextMenuStrip = CreateContextMenuStrip()
			};
			browseViewPaneBar.AddControls(new List<Control> { panelMenu });

			_paneBarContainer = PaneBarContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				browseViewPaneBar,
				_recordBrowseView);
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerk;
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

		private ContextMenuStrip CreateContextMenuStrip()
		{
			_contextMenuStrip = new ContextMenuStrip();

			_contextMenuStrip.Opening += ContextMenuStrip_Opening;

			return _contextMenuStrip;
		}

		private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (_reversalIndexRepository == null)
			{
				var cache = PropertyTable.GetValue<FdoCache>("cache");
				_reversalIndexRepository = cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			}
			if (_contextMenuStrip.Items.Count > 0)
			{
				foreach (var menuTuple in _newMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_contextMenuStrip.Items.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newMenusAndHandlers.Clear();
				_contextMenuStrip.Items.Clear();
			}

			// If allInstancesinRepository has any remaining instances, then they are not in the menu. Add them.
			foreach (var rei in _reversalIndexRepository.AllInstances())
			{
				var newMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(_contextMenuStrip, rei.ChooserNameTS.Text, null, ReversalIndex_Menu_Clicked, null);
				newMenuItem.Tag = rei;
				if (rei == _currentReversalIndex)
				{
					SetCheckedState(newMenuItem);
				}
				_newMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(newMenuItem, ReversalIndex_Menu_Clicked));
			}

			_contextMenuStrip.Items.Add(new ToolStripMenuItem("-"));
			var currentToolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(_contextMenuStrip, "Configure Dictionary", null, ConfigureDictionary_Clicked, null);
			_newMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(currentToolStripMenuItem, ConfigureDictionary_Clicked));
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
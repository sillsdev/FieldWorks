// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.BulkEditReversalEntries
{
	/// <summary>
	/// ITool implementation for the "reversalBulkEditReversalEntries" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class ReversalBulkEditReversalEntriesTool : ITool
	{
		private LexiconAreaMenuHelper _lexiconAreaMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
		private IReversalIndexRepository _reversalIndexRepository;
		private IReversalIndex _currentReversalIndex;
		private SliceContextMenuFactory _sliceContextMenuFactory;
		private LcmCache _cache;
		private const string panelMenuId = "left";
		[Import(AreaServices.LexiconAreaMachineName)]
		private IArea _area;
		[Import]
		private IPropertyTable _propertyTable;
		[Import]
		private IPublisher _publisher;
		[Import]
		private ISubscriber _subscriber;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_lexiconAreaMenuHelper.Dispose();
			_sliceContextMenuFactory.Dispose(); // No Data Tree in this tool to dispose of it for us.
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);
			_reversalIndexRepository = null;
			_currentReversalIndex = null;
			_recordBrowseView = null;
			_cache = null;
			_sliceContextMenuFactory = null;
			_lexiconAreaMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_sliceContextMenuFactory = new SliceContextMenuFactory(); // Make our own, since the tool has no data tree.
			_cache = majorFlexComponentParameters.LcmCache;
			var currentGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(_propertyTable, "ReversalIndexGuid");
			if (currentGuid != Guid.Empty)
			{
				_currentReversalIndex = (IReversalIndex)majorFlexComponentParameters.LcmCache.ServiceLocator.GetObject(currentGuid);
			}
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(LexiconArea.AllReversalEntries, majorFlexComponentParameters.Statusbar, LexiconArea.AllReversalEntriesFactoryMethod);
			}
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper(majorFlexComponentParameters, _recordList);

			_sliceContextMenuFactory.RegisterPanelMenuCreatorMethod(panelMenuId, CreatePanelContextMenuStrip);
			_recordBrowseView = new RecordBrowseView(XDocument.Parse(LexiconResources.ReversalBulkEditReversalEntriesToolParameters).Root, majorFlexComponentParameters.LcmCache, _recordList);
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
			_lexiconAreaMenuHelper.Initialize();
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
		public string MachineName => AreaServices.ReversalBulkEditReversalEntriesMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Bulk Edit Reversal Entries";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

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
				var newMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ReversalIndex_Menu_Clicked, rei.ChooserNameTS.Text);
				newMenuItem.Tag = rei;
				if (rei == _currentReversalIndex)
				{
					SetCheckedState(newMenuItem);
				}
			}

			contextMenuStrip.Items.Add(new ToolStripSeparator());
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ConfigureDictionary_Clicked, "Configure Dictionary");

			return retVal;
		}

		private void MainPanelContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
		}

		private void ConfigureDictionary_Clicked(object sender, EventArgs e)
		{
			var mainWindow = _propertyTable.GetValue<IFwMainWnd>("window");
			if (DictionaryConfigurationDlg.ShowDialog(new FlexComponentParameters(_propertyTable, _publisher, _subscriber), (Form)mainWindow, _recordList?.CurrentObject, "khtpConfigureReversalIndex", LanguageExplorerResources.Dictionary))
			{
				mainWindow.RefreshAllViews();
			}
		}

		private void ReversalIndex_Menu_Clicked(object sender, EventArgs e)
		{
			var contextMenuItem = (ToolStripMenuItem)sender;
			_currentReversalIndex = (IReversalIndex)contextMenuItem.Tag;
			_propertyTable.SetProperty("ReversalIndexGuid", _currentReversalIndex.Guid.ToString(), true, settingsGroup: SettingsGroup.LocalSettings);
			((ReversalListBase)_recordList).ChangeOwningObjectIfPossible();
			SetCheckedState(contextMenuItem);
		}

		private void SetCheckedState(ToolStripMenuItem reversalToolStripMenuItem)
		{
			var currentTag = (IReversalIndex)reversalToolStripMenuItem.Tag;
			reversalToolStripMenuItem.Checked = (currentTag.Guid.ToString() == _propertyTable.GetValue<string>("ReversalIndexGuid"));
		}
	}
}
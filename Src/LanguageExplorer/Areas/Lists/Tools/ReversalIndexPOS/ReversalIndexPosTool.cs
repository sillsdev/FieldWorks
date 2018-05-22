// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lists.Tools.ReversalIndexPOS
{
	/// <summary>
	/// ITool implementation for the "reversalToolReversalIndexPOS" tool in the "lists" area.
	/// </summary>
	[Export(AreaServices.ListsAreaMachineName, typeof(ITool))]
	internal sealed class ReversalIndexPosTool : ITool
	{
		private ListsAreaMenuHelper _listsAreaMenuHelper;
		private const string panelMenuId = "left";
		private LcmCache _cache;
		private MultiPane _multiPane;
		private IRecordList _recordList;
		private RecordBrowseView _recordBrowseView;
		private IReversalIndexRepository _reversalIndexRepository;
		private IReversalIndex _currentReversalIndex;
		[Import(AreaServices.ListsAreaMachineName)]
		private IArea _area;
		[Import]
		private IPropertyTable _propertyTable;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_listsAreaMenuHelper.Dispose();
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);
			_cache = null;
			_recordBrowseView = null;
			_reversalIndexRepository = null;
			_currentReversalIndex = null;
			_listsAreaMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var currentGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(_propertyTable, "ReversalIndexGuid");
			if (currentGuid != Guid.Empty)
			{
				_currentReversalIndex = (IReversalIndex)majorFlexComponentParameters.LcmCache.ServiceLocator.GetObject(currentGuid);
			}

			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(ReversalIndexPOSRecordList.ReversalEntriesPOS, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}
			_listsAreaMenuHelper = new ListsAreaMenuHelper(majorFlexComponentParameters, (IListArea)_area, _recordList);

			_recordBrowseView = new RecordBrowseView(XDocument.Parse(ListResources.ReversalToolReversalIndexPOSBrowseViewParameters).Root, majorFlexComponentParameters.LcmCache, _recordList);
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var dataTree = new DataTree();
			dataTree.SliceContextMenuFactory.RegisterPanelMenuCreatorMethod(panelMenuId, CreateMainPanelContextMenuStrip);
			var recordEditView = new RecordEditView(XDocument.Parse(ListResources.ReversalToolReversalIndexPOSRecordEditViewParameters).Root, XDocument.Parse(AreaResources.HideAdvancedListItemFields), majorFlexComponentParameters.LcmCache, _recordList, dataTree, MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "RevEntryPOSesAndDetailMultiPane",
				ToolMachineName = MachineName
			};
			var browseViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var panelMenu = new PanelMenu(dataTree.SliceContextMenuFactory, panelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			browseViewPaneBar.AddControls(new List<Control> { panelMenu });

			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters,
				_recordBrowseView, "Browse", browseViewPaneBar,
				recordEditView, "Details", recordEditViewPaneBar);

			panelButton.MyDataTree = recordEditView.MyDataTree;
			// Too early before now.
			recordEditView.FinishInitialization();
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void PrepareToRefresh()
		{
			_recordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
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
		public string MachineName => AreaServices.ReversalToolReversalIndexPOSMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Reversal Index Categories";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
		{
			var contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Opening += MainPanelContextMenuStrip_Opening;
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MainPanelContextMenuStrip_Opening, menuItems);

			if (_reversalIndexRepository == null)
			{
				_reversalIndexRepository = _cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			}
			var allInstancesinRepository = _reversalIndexRepository.AllInstances().ToDictionary(rei => rei.Guid);
			foreach (var rei in allInstancesinRepository.Values)
			{
				var newMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ReversalIndex_Menu_Clicked, rei.ChooserNameTS.Text);
				newMenuItem.Tag = rei;
			}

			return retVal;
		}

		private void MainPanelContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
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

		private static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == ReversalIndexPOSRecordList.ReversalEntriesPOS, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{ReversalIndexPOSRecordList.ReversalEntriesPOS}'.");
			/*
            <clerk id="ReversalEntriesPOS">
              <dynamicloaderinfo assemblyPath="LexEdDll.dll" class="SIL.FieldWorks.XWorks.LexEd.ReversalEntryPOSClerk" />
              <recordList owner="ReversalIndex" property="PartsOfSpeech">
                <dynamicloaderinfo assemblyPath="LexEdDll.dll" class="SIL.FieldWorks.XWorks.LexEd.ReversalIndexPOSRecordList" />
              </recordList>
              <filters />
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
              <!--<recordFilterListProvider assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.WfiRecordFilterListProvider"/>-->
            </clerk>
			*/
			IReversalIndex currentReversalIndex = null;
			var currentReversalIndexGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(flexComponentParameters.PropertyTable, "ReversalIndexGuid");
			if (currentReversalIndexGuid != Guid.Empty)
			{
				currentReversalIndex = (IReversalIndex)cache.ServiceLocator.GetObject(currentReversalIndexGuid);
			}

			// NB: No need to pass 'recordListId' to the constructor, since it supplies ReversalIndexPOSRecordList.ReversalEntriesPOS for the id.
			return new ReversalIndexPOSRecordList(statusBar, cache.ServiceLocator, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), currentReversalIndex);
		}
	}
}

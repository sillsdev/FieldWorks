// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Application;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookEdit
{
	/// <summary>
	/// ITool implementation for the "notebookEdit" tool in the "notebook" area.
	/// </summary>
	[Export(AreaServices.NotebookAreaMachineName, typeof(ITool))]
	internal sealed class NotebookEditTool : ITool
	{
		private NotebookEditToolMenuHelper _notebookEditToolMenuHelper;
		private BrowseViewContextMenuFactory _browseViewContextMenuFactory;
		private DataTree MyDataTree { get; set; }
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
		[Import(AreaServices.NotebookAreaMachineName)]
		private IArea _area;
		private ISharedEventHandlers _sharedEventHandlers;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

			// Dispose after the main UI stuff.
			_browseViewContextMenuFactory.Dispose();
			_notebookEditToolMenuHelper.Dispose();

			_recordBrowseView = null;
			_notebookEditToolMenuHelper = null;
			_browseViewContextMenuFactory = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			majorFlexComponentParameters.FlexComponentParameters.PropertyTable.SetDefault($"{AreaServices.ToolForAreaNamed_}{_area.MachineName}", MachineName, true);
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(NotebookArea.Records, majorFlexComponentParameters.StatusBar, NotebookArea.NotebookFactoryMethod);
			}
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			_browseViewContextMenuFactory = new BrowseViewContextMenuFactory();
			_browseViewContextMenuFactory.RegisterBrowseViewContextMenuCreatorMethod(AreaServices.mnuBrowseView, BrowseViewContextMenuCreatorMethod);

			var showHiddenFieldsPropertyName = PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName);
			MyDataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers);
			_recordBrowseView = new RecordBrowseView(NotebookArea.LoadDocument(NotebookResources.NotebookEditBrowseParameters).Root, _browseViewContextMenuFactory, majorFlexComponentParameters.LcmCache, _recordList);
			_notebookEditToolMenuHelper = new NotebookEditToolMenuHelper(majorFlexComponentParameters, this, _recordList, MyDataTree, _recordBrowseView);
			var recordEditView = new RecordEditView(XElement.Parse(NotebookResources.NotebookEditRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordList, MyDataTree, MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "RecordBrowseAndDetailMultiPane",
				ToolMachineName = MachineName,
				DefaultPrintPane = "RecordDetailPane"
			};
			var paneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var panelMenu = new PanelMenu(MyDataTree.DataTreeStackContextMenuFactory.MainPanelMenuContextMenuFactory, AreaServices.PanelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			paneBar.AddControls(new List<Control> { panelMenu, panelButton });

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters,
				_recordBrowseView, "Browse", new PaneBar(),
				recordEditView, "Details", paneBar);

			using (var gr = _multiPane.CreateGraphics())
			{
				_multiPane.Panel2MinSize = Math.Max((int)(162000 * gr.DpiX) / MiscUtils.kdzmpInch, CollapsingSplitContainer.kCollapseZone);
			}

			panelButton.MyDataTree = recordEditView.MyDataTree;
			// Too early before now.
			recordEditView.FinishInitialization();
			_notebookEditToolMenuHelper.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false, SettingsGroup.LocalSettings))
			{
				majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish("ShowHiddenFields", true);
			}
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
		public string MachineName => AreaServices.NotebookEditToolMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Record Edit";
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

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> BrowseViewContextMenuCreatorMethod(IRecordList recordList, string browseViewMenuId)
		{
			// The actual menu declaration has a gazillion menu items, but only two of them are seen in this tool (plus the separator).
			// Start: <menu id="mnuBrowseView" (partial) >
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = AreaServices.mnuBrowseView
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.CmdDeleteSelectedObject), string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("RnGenericRec", "ClassNames")));
			var currentSlice = MyDataTree.CurrentSlice;
			if (currentSlice == null)
			{
				MyDataTree.GotoFirstSlice();
			}
			menu.Tag = MyDataTree.CurrentSlice;

			// End: <menu id="mnuBrowseView" (partial) >

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}
	}
}

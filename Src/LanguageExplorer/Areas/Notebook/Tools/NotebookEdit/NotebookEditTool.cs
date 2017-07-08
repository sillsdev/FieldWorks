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
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;
using SIL.LCModel.Application;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookEdit
{
	/// <summary>
	/// ITool implementation for the "notebookEdit" tool in the "notebook" area.
	/// </summary>
	internal sealed class NotebookEditTool : ITool
	{
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private RecordClerk _recordClerk;
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

			PropertyTable.SetDefault($"ToolForAreaNamed_{AreaMachineName}", MachineName, SettingsGroup.LocalSettings, true, false);
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
			foreach (var menuTuple in _newMenusAndHandlers)
			{
				menuTuple.Item1.Click -= menuTuple.Item2;
			}
			_newMenusAndHandlers.Clear();

			MultiPaneFactory.RemoveFromParentAndDispose(
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				majorFlexComponentParameters.DataNavigationManager,
				majorFlexComponentParameters.RecordClerkRepository,
				ref _multiPane);
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
			if (_recordClerk == null)
			{
				_recordClerk = NotebookArea.CreateRecordClerkForAllNotebookAreaTools(majorFlexComponentParameters.LcmCache);
				_recordClerk.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
				majorFlexComponentParameters.RecordClerkRepository.AddRecordClerk(_recordClerk);
			}

			_recordBrowseView = new RecordBrowseView(NotebookArea.LoadDocument(NotebookResources.NotebookEditBrowseParameters).Root, majorFlexComponentParameters.LcmCache, _recordClerk);
#if RANDYTODO
			// TODO: Set up 'dataTreeMenuHandler' to handle menu events.
			// TODO: Install menus and connect them to event handlers. (See "CreateContextMenuStrip" method for where the menus are.)
#endif
			var recordEditView = new RecordEditView(XElement.Parse(NotebookResources.NotebookEditRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordClerk);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
				Id = "RecordBrowseAndDetailMultiPane",
				ToolMachineName = MachineName,
				DefaultPrintPane = "RecordDetailPane"
			};
			var paneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var panelMenu = new PanelMenu
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center,
				ContextMenuStrip = CreateContextMenuStrip()
			};
			var panelButton = new PanelButton(PropertyTable, null, PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), LanguageExplorerResources.ksHideFields, LanguageExplorerResources.ksShowHiddenFields)
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
				_multiPane.Panel2MinSize = Math.Max((int)(162000 * gr.DpiX) / MiscUtils.kdzmpInch,
						CollapsingSplitContainer.kCollapseZone);
			}

			panelButton.DatTree = recordEditView.DatTree;
			// Too early before now.
			recordEditView.FinishInitialization();
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerk;
			majorFlexComponentParameters.RecordClerkRepository.ActiveRecordClerk = _recordClerk;
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
		public string MachineName => "notebookEdit";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Record Edit";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "notebook";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion

		private ContextMenuStrip CreateContextMenuStrip()
		{
			var contextMenuStrip = new ContextMenuStrip();

			// Insert_Subrecord menu item.
			/*
<item label="Insert _Subrecord" command="CmdDataTree-Insert-Subrecord"/>
<command id="CmdDataTree-Insert-Subrecord" label="Insert _Subrecord" message="InsertItemInVector">
	<parameters className="RnGenericRec" subrecord="true"/>
</command>
			*/
			var contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, NotebookResources.Insert_Subrecord, null, Insert_Subrecord_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			// Insert Insert S_ubrecord of Subrecord menu item. (CmdInsertSubsense->msg: DataTreeInsert, also on Insert menu)
			/*
				<item label="Insert S_ubrecord of Subrecord" command="CmdDataTree-Insert-Subsubrecord" defaultVisible="false"/>
<command id="CmdDataTree-Insert-Subsubrecord" label="Insert S_ubrecord of Subrecord" message="InsertItemInVector">
	<parameters className="RnGenericRec" subrecord="true" subsubrecord="true"/>
</command>
			*/
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, NotebookResources.Insert_Subrecord_of_Subrecord, null, Insert_Subsubrecord_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif
			// Demote Record... menu item. (CmdDemoteRecord).
			/*
<item command="CmdDemoteRecord"/>
<command id="CmdDemoteRecord" label="Demote Record..." message="DemoteItemInVector">
	<parameters className="RnGenericRec"/>
</command>
			*/
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, NotebookResources.Demote_Record, null, Demote_Record_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			return contextMenuStrip;
		}

		private void Demote_Record_Clicked(object sender, EventArgs e)
		{
		}

		private void Insert_Subsubrecord_Clicked(object sender, EventArgs e)
		{
		}

		private void Insert_Subrecord_Clicked(object sender, EventArgs e)
		{
		}

		private ToolStripMenuItem GetItemForItemText(string menuText)
		{
			return _newMenusAndHandlers.First(t => t.Item1.Text == FwUtils.ReplaceUnderlineWithAmpersand(menuText)).Item1;
		}

		private ToolStripMenuItem CreateToolStripMenuItem(ContextMenuStrip contextMenuStrip, string menuText, string menuTooltip, EventHandler eventHandler)
		{
			var toolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(contextMenuStrip, menuText, null, eventHandler, menuTooltip);
			_newMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(toolStripMenuItem, eventHandler));
			return toolStripMenuItem;
		}
	}
}

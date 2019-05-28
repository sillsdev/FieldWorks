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
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
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
		[Import(AreaServices.NotebookAreaMachineName)]
		private IArea _area;
		private NotebookEditToolMenuHelper _toolMenuHelper;
		private DataTree _dataTree;
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;

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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

			_recordBrowseView = null;
			_toolMenuHelper = null;
			_dataTree = null;
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

			var showHiddenFieldsPropertyName = PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName);
			_dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers);
			_recordBrowseView = new RecordBrowseView(NotebookArea.LoadDocument(NotebookResources.NotebookEditBrowseParameters).Root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			// NB: The constructor will create the ToolUiWidgetParameterObject instance and register events.
			_toolMenuHelper = new NotebookEditToolMenuHelper(majorFlexComponentParameters, this, _recordList, _dataTree, _recordBrowseView);
			var recordEditView = new RecordEditView(XElement.Parse(NotebookResources.NotebookEditRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordList, _dataTree, majorFlexComponentParameters.UiWidgetController);
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
			var panelMenu = new PanelMenu(_toolMenuHelper.MainPanelMenuContextMenuFactory, AreaServices.LeftPanelMenuId)
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

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters, _recordBrowseView, "Browse", new PaneBar(), recordEditView, "Details", paneBar);

			using (var gr = _multiPane.CreateGraphics())
			{
				_multiPane.Panel2MinSize = Math.Max((int)(162000 * gr.DpiX) / MiscUtils.kdzmpInch, CollapsingSplitContainer.kCollapseZone);
			}

			// Too early before now.
			recordEditView.FinishInitialization();
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false, SettingsGroup.LocalSettings))
			{
				majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish(LanguageExplorerConstants.ShowHiddenFields, true);
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

		/// <summary>
		/// This class handles all interaction for the NotebookEditTool for its menus, tool bars, plus all context menus that are used in Slices and PaneBars.
		/// </summary>
		private sealed class NotebookEditToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private ITool _tool;
			private SharedNotebookToolsUiWidgetMenuHelper _sharedNotebookToolsUiWidgetMenuHelper;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;
			private RightClickContextMenuManager _rightClickContextMenuManager;
			private DataTree _dataTree;
			private IRecordList _recordList;
			private RecordBrowseView _recordBrowseView;
			private ISharedEventHandlers _sharedEventHandlers;
			internal PanelMenuContextMenuFactory MainPanelMenuContextMenuFactory { get; private set; }

			internal NotebookEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, IRecordList recordList, DataTree dataTree, RecordBrowseView recordBrowseView)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(dataTree, nameof(dataTree));
				Require.That(tool.MachineName == AreaServices.NotebookEditToolMachineName);

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_tool = tool;
				_recordList = recordList;
				_dataTree = dataTree;
				_recordBrowseView = recordBrowseView;
				_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
				MainPanelMenuContextMenuFactory = new PanelMenuContextMenuFactory();
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(_tool);
				SetupToolUiWidgets(toolUiWidgetParameterObject);
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				_recordBrowseView.ContextMenuStrip = _sharedNotebookToolsUiWidgetMenuHelper.CreateBrowseViewContextMenu();
			}

			private void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
			{
				_sharedNotebookToolsUiWidgetMenuHelper = new SharedNotebookToolsUiWidgetMenuHelper(_majorFlexComponentParameters, _recordList);
				_sharedNotebookToolsUiWidgetMenuHelper.SetupToolUiWidgets(toolUiWidgetParameterObject, new HashSet<Command>{ Command.CmdExport, Command.CmdInsertRecord, Command.CmdInsertSubrecord, Command.CmdInsertSubsubrecord });
				_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(_majorFlexComponentParameters, _recordList);
				_rightClickContextMenuManager = new RightClickContextMenuManager(_majorFlexComponentParameters, _tool, _dataTree, _recordList);
				// <item command="CmdConfigureColumns" defaultVisible="false" />
				MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(AreaServices.LeftPanelMenuId, CreateMainPanelContextMenuStrip);

				_partiallySharedForToolsWideMenuHelper.StartSharing(Command.CmdAddToLexicon, () => CanCmdAddToLexicon);
				_partiallySharedForToolsWideMenuHelper.SetupAddToLexicon(toolUiWidgetParameterObject, _dataTree);
				var menuItem = _majorFlexComponentParameters.UiWidgetController.InsertMenuDictionary[Command.CmdAddToLexicon];
				menuItem.Tag = _dataTree;

				// <item command="CmdLexiconLookup" defaultVisible="false" />
				toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Tools].Add(Command.CmdLexiconLookup, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdLexiconLookup_Click, () => CanCmdLexiconLookup));
				toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert].Add(Command.CmdLexiconLookup, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdLexiconLookup_Click, () => CanCmdLexiconLookup));

				// Slice menus don't need to add anything to: toolUiWidgetParameterObject.
				SetupSliceMenus();
			}

			private Tuple<bool, bool> CanCmdAddToLexicon
			{
				get
				{
					var currentSliceAsStTextSlice = _dataTree?.CurrentSliceAsStTextSlice;
					var enabled = currentSliceAsStTextSlice != null;
					IVwSelection currentSelection = null;
					if (currentSliceAsStTextSlice != null)
					{
						currentSelection = currentSliceAsStTextSlice.RootSite.RootBox.Selection;
						enabled = currentSelection != null && currentSelection.CanLookupLexicon();
					}
					SetTagsToSelection(currentSelection);
					return new Tuple<bool, bool>(true, enabled);
				}
			}

			private Tuple<bool, bool> CanCmdLexiconLookup
			{
				get
				{
					var currentSliceAsStTextSlice = _dataTree?.CurrentSliceAsStTextSlice;
					var enabled = currentSliceAsStTextSlice != null;
					IVwSelection currentSelection = null;
					if (currentSliceAsStTextSlice != null)
					{
						currentSelection = currentSliceAsStTextSlice.RootSite.RootBox.Selection;
						enabled = PartiallySharedForToolsWideMenuHelper.Set_CmdInsertFoo_Enabled_State(_majorFlexComponentParameters.LcmCache, currentSelection);
					}
					SetTagsToSelection(currentSelection);
					return new Tuple<bool, bool>(true, enabled);
				}
			}

			private void CmdLexiconLookup_Click(object sender, EventArgs e)
			{
				var currentSliceAsStTextSlice = _dataTree.CurrentSliceAsStTextSlice;
				int ichMin, ichLim, hvo, tag, ws;
				if (currentSliceAsStTextSlice.RootSite.RootBox.Selection.GetSelectedWordPos(out hvo, out tag, out ws, out ichMin, out ichLim))
				{
					LexEntryUi.DisplayOrCreateEntry(_majorFlexComponentParameters.LcmCache, hvo, tag, ws, ichMin, ichLim, currentSliceAsStTextSlice, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher, _majorFlexComponentParameters.FlexComponentParameters.Subscriber, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), "UserHelpFile");
				}
			}

			private void SetTagsToSelection(IVwSelection currentSelection)
			{
				// 'currentSelection' may be null, which  is fine.
				_majorFlexComponentParameters.UiWidgetController.ToolsMenuDictionary[Command.CmdLexiconLookup].Tag = currentSelection;
				_majorFlexComponentParameters.UiWidgetController.InsertToolBarDictionary[Command.CmdLexiconLookup].Tag = currentSelection;
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
			{
				var contextMenuStrip = new ContextMenuStrip();
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
				var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);

				// <item label="Insert _Subrecord" command="CmdDataTree_Insert_Subrecord"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.GetEventHandler(Command.CmdInsertSubrecord), NotebookResources.Insert_Subrecord);

				// <item label="Insert S_ubrecord of Subrecord" command="CmdDataTree_Insert_Subsubrecord" defaultVisible="false"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.GetEventHandler(Command.CmdInsertSubsubrecord), NotebookResources.Insert_Subrecord_of_Subrecord);

				/*
					<command id="CmdDemoteRecord" label="Demote Record..." message="DemoteItemInVector">
					  <parameters className="RnGenericRec" />
					</command>
				*/
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Demote_Record_Clicked, NotebookResources.Demote_Record);
				menu.Enabled = CanDemoteItemInVector;

				return retVal;
			}

			private bool CanDemoteItemInVector
			{
				get
				{
					var root = _dataTree.Root;
					return root.Owner is IRnResearchNbk && (root.Owner as IRnResearchNbk).RecordsOC.Count > 1;
				}
			}

			private void Demote_Record_Clicked(object sender, EventArgs e)
			{
				/*
					<command id="CmdDemoteRecord" label="Demote Record..." message="DemoteItemInVector">
						<parameters className="RnGenericRec"/>
					</command>
				*/
				var cache = _majorFlexComponentParameters.LcmCache;
				var record = (IRnGenericRec)_dataTree.Root;
				IRnGenericRec newOwner;
				if (record.Owner is IRnResearchNbk)
				{
					var notebook = (IRnResearchNbk)record.Owner;
					var owners = notebook.RecordsOC.Where(recT => recT != record).ToList();
					newOwner = owners.Count == 1 ? owners[0] : ChooseNewOwner(owners.ToArray(), NotebookResources.Choose_Owner_of_Demoted_Record);
				}
				else
				{
					return;
				}
				if (newOwner == null)
				{
					return;
				}
				if (newOwner == record)
				{
					throw new InvalidOperationException("RnGenericRec cannot own itself!");
				}
				UowHelpers.UndoExtensionUsingNewOrCurrentUOW(NotebookResources.Demote_SansDots, cache.ActionHandlerAccessor, () =>
				{
					newOwner.SubRecordsOS.Insert(0, record);
				});
			}

			private Tuple<bool, bool> CanCmdGoToRecord => new Tuple<bool, bool>(true, _dataTree?.CurrentSliceAsStTextSlice != null);

			private void SetupSliceMenus()
			{
				#region Left edge context menus

				// <menu id="mnuDataTree_Participants">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Participants, Create_mnuDataTree_Participants);

				// <menu id="mnuDataTree_SubRecords">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_SubRecords, Create_mnuDataTree_SubRecords);

				// <menu id="mnuDataTree_SubRecordSummary">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_SubRecordSummary, Create_mnuDataTree_SubRecordSummary);

				#endregion Left edge context menus

				#region Hotlinks menus

				// <menu id="mnuDataTree_Subrecord_Hotlinks">
				_dataTree.DataTreeSliceContextMenuParameterObject.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_Subrecord_Hotlinks, Create_mnuDataTree_Subrecord_Hotlinks);

				// <menu id="mnuDataTree_SubRecords_Hotlinks">
				_dataTree.DataTreeSliceContextMenuParameterObject.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_SubRecords_Hotlinks, Create_mnuDataTree_SubRecords_Hotlinks);

				#endregion Hotlinks menus
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Participants(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Participants, $"Expected argument value of '{ContextMenuName.mnuDataTree_Participants.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Participants">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Participants.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <command id="CmdDataTree_Delete_Participants" label="Delete Participants" message="DeleteParticipants" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_Participants_Clicked, NotebookResources.Delete_Participants);

				// End: <menu id="mnuDataTree_Participants">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Delete_Participants_Clicked(object sender, EventArgs e)
			{
				var slice = _dataTree.CurrentSlice;
				var parentSlice = slice.ParentSlice;
				var roledPartic = slice.MyCmObject as IRnRoledPartic ?? parentSlice.MyCmObject as IRnRoledPartic;
				if (roledPartic == null)
				{
					// Just give up.
					return;
				}
				UowHelpers.UndoExtension(NotebookResources.Delete_Participants, roledPartic.Cache.ActionHandlerAccessor, () =>
				{
					roledPartic.Delete();
				});
				parentSlice.Collapse();
				parentSlice.Expand();
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubRecords(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_SubRecords, $"Expected argument value of '{ContextMenuName.mnuDataTree_SubRecords.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_SubRecords">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_SubRecords.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_Subrecord" /> // Shared locally
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.GetEventHandler(Command.CmdInsertSubrecord), NotebookResources.Insert_Subrecord);

				// End: <menu id="mnuDataTree_SubRecords">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubRecordSummary(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_SubRecordSummary, $"Expected argument value of '{ContextMenuName.mnuDataTree_SubRecordSummary.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_SubRecordSummary">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Participants.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

				// <item command="CmdDataTree_Insert_Subrecord" /> // Shared locally
				var eventHandler = _sharedEventHandlers.GetEventHandler(Command.CmdInsertSubrecord);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, eventHandler, NotebookResources.Insert_Subrecord);

				// <item command="CmdDataTree_Insert_Subsubrecord" /> // Shared locally
				eventHandler = _sharedEventHandlers.GetEventHandler(Command.CmdInsertSubsubrecord);
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, eventHandler, NotebookResources.Insert_Subrecord_of_Subrecord);

				/*
					  <item command="CmdMoveRecordUp" />
						<command id="CmdMoveRecordUp" label="Move Up" message="MoveItemUpInVector">
						  <parameters className="RnGenericRec" />
						</command>
				*/
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveRecordUp_Clicked, LanguageExplorerResources.MoveUp);
				menu.Enabled = CanMoveRecordUp;

				/*
					  <item command="CmdMoveRecordDown" />
						<command id="CmdMoveRecordDown" label="Move Down" message="MoveItemDownInVector">
						  <parameters className="RnGenericRec" />
						</command>
				*/
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveRecordDown_Clicked, LanguageExplorerResources.MoveDown);
				menu.Enabled = CanMoveRecordDown;

				/*
					  <item command="CmdPromoteSubrecord" />
						<command id="CmdPromoteSubrecord" label="Promote" message="PromoteSubitemInVector">
						  <parameters className="RnGenericRec" />
						</command>
				*/
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, PromoteSubitemInVector_Clicked, AreaResources.Promote);
				menu.Enabled = CanPromoteSubitemInVector;

				/*
					  <item command="CmdDemoteSubrecord" />
						<command id="CmdDemoteSubrecord" label="Demote..." message="DemoteSubitemInVector">
						  <parameters className="RnGenericRec" />
						</command>
				*/
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, DemoteSubrecord_Clicked, NotebookResources.Demote_WithDots);
				menu.Enabled = CanDemoteSubitemInVector;

				// End: <menu id="mnuDataTree_SubRecordSummary">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			/// <summary>
			/// See if it makes sense to provide the "Move Up" command.
			/// </summary>
			private bool CanMoveRecordUp
			{
				get
				{
					var currentSliceObject = _dataTree.CurrentSlice?.MyCmObject as IRnGenericRec;
					var recordOwner = currentSliceObject?.Owner as IRnGenericRec;
					return currentSliceObject != null && recordOwner != null && currentSliceObject.OwnOrd > 0;
				}
			}

			private void MoveRecordUp_Clicked(object sender, EventArgs e)
			{
				var record = (IRnGenericRec)_dataTree.CurrentSlice.MyCmObject;
				var recordOwner = (IRnGenericRec)record.Owner;
				var idxOrig = record.OwnOrd;
				UowHelpers.UndoExtensionUsingNewOrCurrentUOW(LanguageExplorerResources.MoveUp, record.Cache.ActionHandlerAccessor, () =>
				{
					recordOwner.SubRecordsOS.MoveTo(idxOrig, idxOrig, recordOwner.SubRecordsOS, idxOrig - 1);
				});
			}

			/// <summary>
			/// See if it makes sense to provide the "Move Down" command.
			/// </summary>
			private bool CanMoveRecordDown
			{
				get
				{
					var currentSliceObject = _dataTree.CurrentSlice?.MyCmObject as IRnGenericRec;
					var recordOwner = currentSliceObject?.Owner as IRnGenericRec;
					return currentSliceObject != null && recordOwner != null && currentSliceObject.OwnOrd < recordOwner.SubRecordsOS.Count - 1;
				}
			}

			private void MoveRecordDown_Clicked(object sender, EventArgs e)
			{
				var record = (IRnGenericRec)_dataTree.CurrentSlice.MyCmObject;
				var recordOwner = (IRnGenericRec)record.Owner;
				var idxOrig = record.OwnOrd;
				UowHelpers.UndoExtensionUsingNewOrCurrentUOW(LanguageExplorerResources.MoveDown, record.Cache.ActionHandlerAccessor, () =>
				{
					// idxOrig + 2 looks strange, but it's the correct value to make this work.
					recordOwner.SubRecordsOS.MoveTo(idxOrig, idxOrig, recordOwner.SubRecordsOS, idxOrig + 2);
				});
			}

			private bool CanPromoteSubitemInVector
			{
				get
				{
					var slice = _dataTree.CurrentSlice;
					var currentSliceObject = slice.MyCmObject as IRnGenericRec;
					return currentSliceObject != null && currentSliceObject.Owner is IRnGenericRec;
				}
			}

			private void PromoteSubitemInVector_Clicked(object sender, EventArgs e)
			{
				var record = (IRnGenericRec)_dataTree.CurrentSlice.MyCmObject;
				var recordOwner = record.Owner as IRnGenericRec;
				UowHelpers.UndoExtensionUsingNewOrCurrentUOW(AreaResources.Promote, record.Cache.ActionHandlerAccessor, () =>
				{
					if (recordOwner.Owner is IRnGenericRec)
					{
						(recordOwner.Owner as IRnGenericRec).SubRecordsOS.Insert(recordOwner.OwnOrd + 1, record);
					}
					else if (recordOwner.Owner is IRnResearchNbk)
					{
						(recordOwner.Owner as IRnResearchNbk).RecordsOC.Add(record);
					}
					else
					{
						throw new Exception("RnGenericRec object not owned by either RnResearchNbk or RnGenericRec??");
					}
				});
				if (recordOwner.Owner is IRnResearchNbk)
				{
					// If possible, jump to the newly promoted record.
					_majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish("JumpToRecord", record.Hvo);
				}
			}

			/// <summary>
			/// See if it makes sense to provide the "Demote..." command.
			/// </summary>
			private bool CanDemoteSubitemInVector
			{
				get
				{
					var currentSliceObject = _dataTree.CurrentSlice?.MyCmObject as IRnGenericRec;
					return currentSliceObject?.Owner is IRnGenericRec && (currentSliceObject.Owner as IRnGenericRec).SubRecordsOS.Count > 1;
				}
			}

			private void DemoteSubrecord_Clicked(object sender, EventArgs e)
			{
				var cache = _majorFlexComponentParameters.LcmCache;
				var record = (IRnGenericRec)_dataTree.CurrentSlice.MyCmObject;
				IRnGenericRec newOwner;
				var recordOwner = record.Owner as IRnGenericRec;
				if (recordOwner.SubRecordsOS.Count == 2)
				{
					newOwner = record.OwnOrd == 0 ? recordOwner.SubRecordsOS[1] : recordOwner.SubRecordsOS[0];
				}
				else
				{
					newOwner = ChooseNewOwner(recordOwner.SubRecordsOS.Where(recT => recT != record).ToArray(), NotebookResources.Choose_Owner_of_Demoted_Subrecord);
				}
				if (newOwner == null)
				{
					return;
				}
				if (newOwner == record)
				{
					throw new InvalidOperationException("RnGenericRec cannot own itself!");
				}

				UowHelpers.UndoExtensionUsingNewOrCurrentUOW(NotebookResources.Demote_SansDots, cache.ActionHandlerAccessor, () =>
				{
					newOwner.SubRecordsOS.Insert(0, record);
				});
			}

			private IRnGenericRec ChooseNewOwner(IRnGenericRec[] records, string sTitle)
			{
				var cache = _majorFlexComponentParameters.LcmCache;
				using (var dlg = new ReallySimpleListChooser(PersistenceProviderFactory.CreatePersistenceProvider(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable), ObjectLabel.CreateObjectLabels(cache, records, "ShortName", cache.WritingSystemFactory.GetStrFromWs(cache.DefaultAnalWs)),
					string.Empty, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
				{
					dlg.Text = sTitle;
					dlg.SetHelpTopic("khtpDataNotebook-ChooseOwnerOfDemotedRecord");
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						return (IRnGenericRec)dlg.SelectedObject;
					}
				}
				return null;
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Subrecord_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_Subrecord_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_Subrecord_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				/*
					<command id="CmdDataTree_Insert_Subrecord" label="Insert _Subrecord" message="InsertItemInVector">
					  <parameters className="RnGenericRec" subrecord="true" />
					</command>
				*/
				return CommonHotlinksCreator();
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_SubRecords_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_SubRecords_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_SubRecords_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				/*
					<command id="CmdDataTree_Insert_Subrecord" label="Insert _Subrecord" message="InsertItemInVector"> // Shared locally
					  <parameters className="RnGenericRec" subrecord="true" />
					</command>
				*/
				return CommonHotlinksCreator();
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> CommonHotlinksCreator()
			{
				var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				/*
					<command id="CmdDataTree_Insert_Subrecord" label="Insert _Subrecord" message="InsertItemInVector"> // Shared locally
					  <parameters className="RnGenericRec" subrecord="true" />
					</command>
				*/
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedEventHandlers.GetEventHandler(Command.CmdInsertSubrecord), NotebookResources.Insert_Subrecord);

				return hotlinksMenuItemList;
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~NotebookEditToolMenuHelper()
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
					MainPanelMenuContextMenuFactory.Dispose();
					_sharedNotebookToolsUiWidgetMenuHelper?.Dispose();
					_partiallySharedForToolsWideMenuHelper.Dispose();
					_rightClickContextMenuManager?.Dispose();
					_recordBrowseView.ContextMenuStrip?.Dispose();
					_recordBrowseView.ContextMenuStrip = null;
				}
				MainPanelMenuContextMenuFactory = null;
				_majorFlexComponentParameters = null;
				_tool = null;
				_sharedNotebookToolsUiWidgetMenuHelper = null;
				_partiallySharedForToolsWideMenuHelper = null;
				_rightClickContextMenuManager = null;
				_dataTree = null;
				_recordList = null;
				_sharedEventHandlers = null;
				_recordBrowseView = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
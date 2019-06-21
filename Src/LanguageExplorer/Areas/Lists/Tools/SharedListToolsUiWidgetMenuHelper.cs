// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lists.Tools
{
	internal sealed class SharedListToolsUiWidgetMenuHelper : IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ITool _tool;
		private ICmPossibilityList _list;
		private IRecordList _recordList;
		private DataTree _dataTree;
		private ISharedEventHandlers _sharedEventHandlers;
		private FileExportMenuHelper _fileExportMenuHelper;
		private IListArea Area => (IListArea)_tool.Area;
		private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;

		internal SharedListToolsUiWidgetMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, ICmPossibilityList list, IRecordList recordList, DataTree dataTree)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(tool, nameof(tool));
			Guard.AgainstNull(list, nameof(list));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNull(dataTree, nameof(dataTree));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_fileExportMenuHelper = new FileExportMenuHelper(majorFlexComponentParameters);
			_tool = tool;
			_list = list;
			_recordList = recordList;
			_dataTree = dataTree;
			_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
			_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(_majorFlexComponentParameters, _recordList);
			Register_PossibilityList_Slice_Context_Menus();
		}

		internal void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject, Dictionary<string, string> names = null, HashSet<Command> commands = null)
		{
			if (names == null)
			{
				names = new Dictionary<string, string>()
				{
					{ AreaServices.List_Item, ListResources.Item },
					{ AreaServices.Subitem, ListResources.Subitem }
				};
			}
			if (commands == null)
			{
				// NB: Only the lists that contain CmPossibility instances (e.g., no sub-classes) can have a null option for 'commands'.
				var plainVanillaToolNames = new HashSet<string>
				{
					AreaServices.ChartmarkEditMachineName,
					AreaServices.CharttempEditMachineName,
					AreaServices.ConfidenceEditMachineName,
					AreaServices.DialectsListEditMachineName,
					AreaServices.DomainTypeEditMachineName,
					AreaServices.EducationEditMachineName,
					AreaServices.ExtNoteTypeEditMachineName,
					AreaServices.GenresEditMachineName,
					AreaServices.LanguagesListEditMachineName,
					AreaServices.PositionsEditMachineName,
					AreaServices.PublicationsEditMachineName,
					AreaServices.RecTypeEditMachineName,
					AreaServices.RestrictionsEditMachineName,
					AreaServices.RoleEditMachineName,
					AreaServices.SenseTypeEditMachineName,
					AreaServices.StatusEditMachineName,
					AreaServices.TextMarkupTagsEditMachineName,
					AreaServices.TimeOfDayEditMachineName,
					AreaServices.TranslationTypeEditMachineName,
					AreaServices.UsageTypeEditMachineName
				};
				Require.That(plainVanillaToolNames.Contains(_tool.MachineName));
				commands = new HashSet<Command>
				{
					Command.CmdAddToLexicon, Command.CmdExport, Command.CmdConfigureList, Command.CmdInsertPossibility, Command.CmdDataTree_Insert_Possibility
				};
			}
			var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
			var toolsMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Tools];
			var insertToolbarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];
			// Goes in Insert menu for all honest List Area tools.
			// <item command="CmdAddCustomList" defaultVisible="false" />
			insertMenuDictionary.Add(Command.CmdAddCustomList, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(AddCustomList_Click, () => CanCmdAddCustomList));
			foreach (var command in commands)
			{
				switch (command)
				{
					case Command.CmdAddToLexicon:
						_partiallySharedForToolsWideMenuHelper.SetupCmdAddToLexicon(toolUiWidgetParameterObject, _dataTree, () => CanCmdAddToLexicon);
						break;
					case Command.CmdExport:
						// Set up File->Export menu, which is visible and enabled in all list area tools, using the default event handler.
						_fileExportMenuHelper.SetupFileExportMenu(toolUiWidgetParameterObject);
						break;
					case Command.CmdConfigureList:
						// <command id = "CmdConfigureList" label="List..." message="ConfigureList" />
						toolsMenuDictionary.Add(Command.CmdConfigureList, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ConfigureList_Click, () => CanCmdConfigureList));
						break;
					case Command.CmdInsertPossibility: // Add to Hashset
						// <command id="CmdInsertPossibility" label="_Item" message="InsertItemInVector" icon="AddItem">
						insertMenuDictionary.Add(Command.CmdInsertPossibility, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertPossibility_Click, () => CanCmdInsertPossibility));
						insertToolbarDictionary.Add(Command.CmdInsertPossibility, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertPossibility_Click, () => CanCmdInsertPossibility));
						ResetMainPossibilityInsertUiWidgetsText(_majorFlexComponentParameters.UiWidgetController, names[AreaServices.List_Item]);
						break;
					case Command.CmdDataTree_Insert_Possibility: // Add to Hashset
						// <command id="CmdDataTree_Insert_Possibility" label="Insert subitem" message="DataTreeInsert" icon="AddSubItem">
						insertMenuDictionary.Add(Command.CmdDataTree_Insert_Possibility, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdDataTree_Insert_Possibility_Click, () => CanCmdDataTree_Insert_Possibility));
						insertToolbarDictionary.Add(Command.CmdDataTree_Insert_Possibility, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdDataTree_Insert_Possibility_Click, () => CanCmdDataTree_Insert_Possibility));
						ResetSubitemPossibilityInsertUiWidgetsText(_majorFlexComponentParameters.UiWidgetController, names[AreaServices.Subitem]);
						break;
					default:
						throw new ArgumentOutOfRangeException($"Don't know how to process command: '{command.ToString()}'");
				}
			}
		}

		internal void ResetMainPossibilityInsertUiWidgetsText(UiWidgetController uiWidgetController, string newText)
		{
			ResetInsertUiWidgetsText(_majorFlexComponentParameters.UiWidgetController.InsertMenuDictionary[Command.CmdInsertPossibility],
				_majorFlexComponentParameters.UiWidgetController.InsertToolBarDictionary[Command.CmdInsertPossibility], newText);
		}

		internal void ResetSubitemPossibilityInsertUiWidgetsText(UiWidgetController uiWidgetController, string newText)
		{
			ResetInsertUiWidgetsText(_majorFlexComponentParameters.UiWidgetController.InsertMenuDictionary[Command.CmdDataTree_Insert_Possibility],
				_majorFlexComponentParameters.UiWidgetController.InsertToolBarDictionary[Command.CmdDataTree_Insert_Possibility], newText);
		}

		private static void ResetInsertUiWidgetsText(ToolStripItem menu, ToolStripItem toolBarButton, string newText)
		{
			menu.Text = newText;
			toolBarButton.ToolTipText = newText;
		}

		private static Tuple<bool, bool> CanCmdAddCustomList => new Tuple<bool, bool>(true, true);

		private void AddCustomList_Click(object sender, EventArgs e)
		{
			using (var dlg = new AddCustomListDlg(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher, _majorFlexComponentParameters.LcmCache))
			{
				if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK)
				{
					Area.OnAddCustomList(dlg.NewList);
				}
			}
		}

		private void Register_PossibilityList_Slice_Context_Menus()
		{
			/*
			 <part ref="Summary" label="Complex Form Type" param="PossibilityName"  menu="mnuDataTree_DeletePossibility"/> class="LexEntryType"
			 <part ref="Summary" label="Variant Type" param="PossibilityName"  menu="mnuDataTree_DeletePossibility"/> class="LexEntryType"
			 <part ref="Summary" label="Irr. Inflected Form" param="PossibilityName"  menu="mnuDataTree_DeletePossibility"/> class="LexEntryInflType"
			 <part ref="Summary" label="Subitem" param="PossibilityName"  menu="mnuDataTree_DeletePossibility"/> class="CmPossibility"
			 <part ref="Summary" label="Subdomain" param="PossibilityName"  menu="mnuDataTree_DeletePossibility"/> class="CmSemanticDomain"
			 <part ref="Summary" label="Subcategory" param="PossibilityName" menu="mnuDataTree_DeletePossibility"/> class="CmAnthroItem"
			*/
			_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_DeletePossibility, Create_mnuDataTree_DeletePossibility);

			/*
				<slice label="Subitems" menu="mnuDataTree_SubPossibilities">
					<seq field="SubPossibilities"/>
				</slice>
			*/
			// All except FeatureTypesAdvancedEditTool, which isn't a real list anyway.
			_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_SubPossibilities, Create_mnuDataTree_SubPossibilities);

			// <menu id="mnuDataTree_POS_SubPossibilities">
			// Shared Reversal (Lists) and Morphology (Grammar) worlds.
			_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_SubPossibilities, Create_mnuDataTree_POS_SubPossibilities);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_DeletePossibility(Slice slice, ContextMenuName contextMenuId)
		{
			Require.That(contextMenuId == ContextMenuName.mnuDataTree_DeletePossibility, $"Expected argument value of '{ContextMenuName.mnuDataTree_DeletePossibility.ToString()}', but got '{contextMenuId.ToString()}' instead.");

			// Start: <menu id="mnuDataTree_DeletePossibility">
			// This menu and its commands are shared
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ContextMenuName.mnuDataTree_DeletePossibility.ToString()
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			    <command id="CmdDataTree_Delete_Possibility" label="Delete subitem and its subitems" message="DataTreeDelete" icon="Delete">
			      <parameters field="SubPossibilities" className="CmPossibility" />
			    </command>
			*/
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, ListResources.Delete_subitem_and_its_subitems, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// End: <menu id="mnuDataTree_DeletePossibility">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubPossibilities(Slice slice, ContextMenuName contextMenuId)
		{
			Require.That(contextMenuId == ContextMenuName.mnuDataTree_SubPossibilities, $"Expected argument value of '{ContextMenuName.mnuDataTree_SubPossibilities.ToString()}', but got '{contextMenuId.ToString()}' instead.");

			// Start: <menu id="mnuDataTree_SubPossibilities">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ContextMenuName.mnuDataTree_SubPossibilities.ToString()
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			      <item command="CmdDataTree_Insert_Possibility" /> // Shared
			*/
			var currentPossibility = _recordList.CurrentObject as ICmPossibility; // this will be null for the features 'list', but not to worry, since the menu won't be built for that tool.
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Insert_Possibility_Click, ListResources.Insert_Subitem, image: AreaResources.AddSubItem.ToBitmap());
			//menu.Tag = new List<object> { currentPossibility, _dataTree, _recordList, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, AreaServices.PopulateForSubitemInsert(_list, currentPossibility, ListResources.Insert_Subitem) };

			// End: <menu id="mnuDataTree_SubPossibilities">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_SubPossibilities(Slice slice, ContextMenuName contextMenuId)
		{
			Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_SubPossibilities, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_SubPossibilities.ToString()}', but got '{contextMenuId.ToString()}' instead.");

			// Start: <menu id="mnuDataTree_POS_SubPossibilities">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ContextMenuName.mnuDataTree_POS_SubPossibilities.ToString()
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			      <item command="CmdDataTree_Insert_POS_SubPossibilities" />
				    <command id="CmdDataTree_Insert_POS_SubPossibilities" label="Insert Subcategory..." message="DataTreeInsert" icon="AddSubItem">
				      <parameters field="SubPossibilities" className="PartOfSpeech" slice="owner" />
				    </command>
			*/
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, InsertCategory_Clicked, AreaResources.Insert_Subcategory, image: AreaResources.AddSubItem.ToBitmap());

			// End: <menu id="mnuDataTree_POS_SubPossibilities">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void InsertCategory_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new MasterCategoryListDlg())
			{
				var selectedCategoryOwner = _recordList.CurrentObject?.Owner;
				var propertyTable = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
				dlg.SetDlginfo(_list, propertyTable, true, selectedCategoryOwner as IPartOfSpeech);
				dlg.ShowDialog(propertyTable.GetValue<Form>(FwUtils.window));
			}
		}

		private Tuple<bool, bool> CanCmdAddToLexicon
		{
			get
			{
				var currentSliceAsStTextSlice = _dataTree?.CurrentSliceAsStTextSlice;
				var visible = currentSliceAsStTextSlice != null;
				var enabled = false;
				if (currentSliceAsStTextSlice != null)
				{
					var currentSelection = currentSliceAsStTextSlice.RootSite.RootBox.Selection;
					enabled = currentSelection != null && PartiallySharedForToolsWideMenuHelper.Set_CmdInsertFoo_Enabled_State(_majorFlexComponentParameters.LcmCache, currentSelection) && currentSelection.CanLookupLexicon();
				}
				return new Tuple<bool, bool>(visible, enabled);
			}
		}

		private static Tuple<bool, bool> CanCmdInsertPossibility => new Tuple<bool, bool>(true, true);

		private void CmdInsertPossibility_Click(object sender, EventArgs e)
		{
			ICmPossibility newPossibility = null;
			UowHelpers.UndoExtension(ListResources.Item, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
			{
				newPossibility = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), _list);
			});
			if (newPossibility != null)
			{
				_recordList.UpdateRecordTreeBar();
			}
		}

		private Tuple<bool, bool> CanCmdDataTree_Insert_Possibility => new Tuple<bool, bool>(true, _list.Depth > 1 && _recordList.CurrentObject != null);

		private void CmdDataTree_Insert_Possibility_Click(object sender, EventArgs e)
		{
			ICmPossibility newSubPossibility = null;
			UowHelpers.UndoExtension(ListResources.Item, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
			{
				newSubPossibility = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), (ICmPossibility)_recordList.CurrentObject);
			});
			if (newSubPossibility != null)
			{
				_recordList.UpdateRecordTreeBar();
			}
		}

		private static Tuple<bool, bool> CanCmdConfigureList => new Tuple<bool, bool>(true, true);

		private void ConfigureList_Click(object sender, EventArgs e)
		{
			var originalUiName = _list.Name.BestAnalysisAlternative.Text;
			using (var dlg = new ConfigureListDlg(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher, _majorFlexComponentParameters.LcmCache, _list))
			{
				if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK && originalUiName != _list.Name.BestAnalysisAlternative.Text)
				{
					Area.OnUpdateListDisplayName(_tool, _list);
				}
			}
		}

		#region Implementation of IDisposable
		private bool _isDisposed;

		~SharedListToolsUiWidgetMenuHelper()
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
				_fileExportMenuHelper.Dispose();
				_partiallySharedForToolsWideMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			_tool = null;
			_list = null;
			_recordList = null;
			_dataTree = null;
			_fileExportMenuHelper = null;
			_partiallySharedForToolsWideMenuHelper = null;
			_sharedEventHandlers = null;


			_isDisposed = true;
		}
		#endregion
	}
}

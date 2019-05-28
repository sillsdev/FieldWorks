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
	/// <summary>
	/// This is shared among all List area tools, EXCEPT FeatureTypesAdvancedEditTool (which isn't actually a possibility list tool).
	/// Closed lists may not want any of this stuff either. (Major Entry Types seems to be the only closed one, and it has no tool.)
	/// </summary>
	/// <remarks>
	/// Tools that only contain instances of ICmPossibility should create "SharedForPlainVanillaListToolMenuHelper", which  creates one of these.
	/// </remarks>
	internal sealed class PartiallySharedListToolMenuHelper : IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ICmPossibilityList _list;
		private IRecordList _recordList;
		private DataTree _dataTree;
		private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;
		private ISharedEventHandlers _sharedEventHandlers;

		internal PartiallySharedListToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, PartiallySharedForToolsWideMenuHelper partiallySharedForToolsWideMenuHelper, ICmPossibilityList list, IRecordList recordList, DataTree dataTree)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(partiallySharedForToolsWideMenuHelper, nameof(partiallySharedForToolsWideMenuHelper));
			Guard.AgainstNull(list, nameof(list));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNull(dataTree, nameof(dataTree));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_list = list;
			_recordList = recordList;
			_dataTree = dataTree;
			_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
			_partiallySharedForToolsWideMenuHelper = partiallySharedForToolsWideMenuHelper;
			Register_PossibilityList_Slice_Context_Menus();
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
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.GetEventHandler(Command.AddNewSubPossibilityListItem), ListResources.Insert_Subitem, image: AreaResources.AddSubItem.ToBitmap());
			menu.Tag = new List<object> { currentPossibility, _dataTree, _recordList, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, AreaServices.PopulateForSubitemInsert(_list, currentPossibility, ListResources.Insert_Subitem) };

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

		/// <summary>
		/// Call method if list tool supports a CmdDuplicateFoo command on the insert tool bar
		/// </summary>
		internal void SetupSharedDuplicateMainPossibility(ToolUiWidgetParameterObject toolUiWidgetParameterObject, Command command)
		{
#if RANDYTODO
			// TODO: Needs to wait until I can actually see what is happening with these in 9.0.7, before I can enable them.

		private void AddInsertToolbarItems()
		{
			const string duplicateMainItem = "duplicateMainItem";
			var toolsThatSupportItemDuplication = new HashSet<string>
			{
				AreaServices.ChartmarkEditMachineName,
				AreaServices.CharttempEditMachineName,
				AreaServices.TextMarkupTagsEditMachineName
			};
			// Default in switch
			if (toolsThatSupportItemDuplication.Contains(activeListTool.MachineName) || activeListTool.MachineName.StartsWith("CustomList"))
			{
				// Add support for the duplicate main item button.
				toolbarButtonCreationData.Add(duplicateMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(Duplicate_Item_Clicked, ListResources.Duplicate_Item, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Duplicate_Item)));
			}
		}

		private void Duplicate_Item_Clicked(object sender, EventArgs e)
		{
			// NB: This will not be enabled if a sub-item is the current record in the record list.
			var tag = (List<object>)((ToolStripItem)sender).Tag;
			var possibilityList = (ICmPossibilityList)tag[0];
			var recordList = (IRecordList)tag[2];
			var otherOptions = (Dictionary<string, string>)tag[4];
			var currentPossibility = (ICmPossibility)recordList.CurrentObject;
			UowHelpers.UndoExtension(otherOptions[AreaServices.BaseUowMessage], possibilityList.Cache.ActionHandlerAccessor, () =>
			{
				if (currentPossibility is ICmCustomItem)
				{
					((ICmCustomItem)currentPossibility).Clone();
				}
				else
				{
					// NB: This will throw, if 'currentPossibility' is a subclass of ICmPossibility, since we don't support duplicating those.
					currentPossibility.Clone();
				}
			});
		}

		private void ApplicationOnIdle(object sender, EventArgs e)
		{
			if (_duplicateItemToolStripButton != null)
			{
				_duplicateItemToolStripButton.Enabled = MyRecordList.CurrentObject != null && MyRecordList.CurrentObject.Owner.ClassID == CmPossibilityListTags.kClassId;
			}
		}
#else
			MessageBox.Show("One of these days, I can get this to work.");
#endif
		}

		internal void SetupToolUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			_partiallySharedForToolsWideMenuHelper.StartSharing(Command.CmdAddToLexicon, ()=> CanCmdAddToLexicon);
			_partiallySharedForToolsWideMenuHelper.SetupAddToLexicon(toolUiWidgetParameterObject, _dataTree);
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

		#region Implementation of IDisposable
		private bool _isDisposed;

		~PartiallySharedListToolMenuHelper()
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
				_partiallySharedForToolsWideMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			_list = null;
			_recordList = null;
			_dataTree = null;
			_partiallySharedForToolsWideMenuHelper = null;
			_sharedEventHandlers = null;

			_isDisposed = true;
		}
		#endregion
	}
}
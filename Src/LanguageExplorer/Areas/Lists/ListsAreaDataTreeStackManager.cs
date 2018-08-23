// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lists
{
	internal sealed class ListsAreaDataTreeStackManager : IToolUiWidgetManager
	{
		private DataTree MyDataTree { get; set; }
		private IRecordList MyRecordList { get; set; }
		private ISharedEventHandlers _sharedEventHandlers;
		private IListArea _listArea;
		private IPropertyTable _propertyTable;

		internal ListsAreaDataTreeStackManager(DataTree dataTree, IListArea listArea)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNull(listArea, nameof(listArea));

			MyDataTree = dataTree;
			_listArea = listArea;
		}

		#region Implementation of IToolUiWidgetManager

		/// <inheritdoc />
		public void Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			MyRecordList = recordList;
			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;

			if (_listArea.ActiveTool.MachineName != AreaServices.FeatureTypesAdvancedEditMachineName && !ListsAreaMenuHelper.GetPossibilityList(MyRecordList).IsClosed)
			{
				// These all deal with insertion/deletion, so are not used in a closed list.
				// Nor are they used in the AreaServices.FeatureTypesAdvancedEditMachineName tool, which isn't a possibility list at all.
				Register_PossibilityList_Slice_Context_Menus();
			}
		}

		/// <inheritdoc />
		public void UnwireSharedEventHandlers()
		{
		}

		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~ListsAreaDataTreeStackManager()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}


		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
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
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
			}
			MyDataTree = null;
			MyRecordList = null;
			_sharedEventHandlers = null;
			_listArea = null;

			_isDisposed = true;
		}

		#endregion

		private void Register_PossibilityList_Slice_Context_Menus()
		{
			/*
			 <part ref="Summary" label="Complex Form Type" param="PossibilityName"  menu="mnuDataTree-DeletePossibility"/>
			 <part ref="Summary" label="Variant Type" param="PossibilityName"  menu="mnuDataTree-DeletePossibility"/>
			 <part ref="Summary" label="Irr. Inflected Form" param="PossibilityName"  menu="mnuDataTree-DeletePossibility"/>
			 <part ref="Summary" label="Subitem" param="PossibilityName"  menu="mnuDataTree-DeletePossibility"/>
			 <part ref="Summary" label="Subdomain" param="PossibilityName"  menu="mnuDataTree-DeletePossibility"/>
			 <part ref="Summary" label="Subcategory" param="PossibilityName" menu="mnuDataTree-DeletePossibility"/>
			*/
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_DeletePossibility, Create_mnuDataTree_DeletePossibility);

			/*
				<part id="CmSemanticDomain-Detail-Questions" type="Detail">
					<slice label="Questions" menu="mnuDataTree-InsertQuestion">
					  <seq field="Questions"/>
					</slice>
				</part>
			*/
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_InsertQuestion, Create_mnuDataTree_InsertQuestion);

			/*
				<part id="CmDomainQ-Detail-QuestionAllA" type="Detail">
					<slice field="Question" label="Question" editor="multistring" ws="all analysis" menu="mnuDataTree-DeleteQuestion">
					</slice>
				</part>
			*/
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_DeleteQuestion, Create_mnuDataTree_DeleteQuestion);

			/*
				<slice label="Subitems" menu="mnuDataTree-SubPossibilities">
					<seq field="SubPossibilities"/>
				</slice>
			*/
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_SubPossibilities, Create_mnuDataTree_SubPossibilities);

			/*
				// Used for CmLocation, but, unexpectedly, also for: LexEntryType
				// I'm not sure how one can reasonable insert an instance of CmLocation into a list of LexEntryType instance, given that the list should prevent that.
			    <menu id="mnuDataTree-SubLocation">
			*/
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_SubLocation, Create_mnuDataTree_SubLocation);

			// <menu id="mnuDataTree-SubAnthroCategory">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_SubAnthroCategory, Create_mnuDataTree_SubAnthroCategory);

			// <menu id="mnuDataTree-SubSemanticDomain">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_SubSemanticDomain, Create_mnuDataTree_SubSemanticDomain);

			// <menu id="mnuDataTree-SubComplexEntryType">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_SubComplexEntryType, Create_mnuDataTree_SubComplexEntryType);

			// <menu id="mnuDataTree-SubVariantEntryType">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_SubVariantEntryType, Create_mnuDataTree_SubVariantEntryType);

			// <menu id="mnuDataTree-MoveMainReversalPOS">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_MoveMainReversalPOS, Create_mnuDataTree_MoveMainReversalPOS);

			// <menu id="mnuDataTree-MoveReversalPOS">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_MoveReversalPOS, Create_mnuDataTree_MoveReversalPOS);

			// <menu id="mnuDataTree-POS-SubPossibilities">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ListsAreaConstants.mnuDataTree_POS_SubPossibilities, Create_mnuDataTree_POS_SubPossibilities);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_DeletePossibility(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_DeletePossibility, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_DeletePossibility}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-DeletePossibility">
			// This menu and its commands are shared
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_DeletePossibility
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			    <command id="CmdDataTree-Delete-Possibility" label="Delete subitem and its subitems" message="DataTreeDelete" icon="Delete">
			      <parameters field="SubPossibilities" className="CmPossibility" />
			    </command>
			*/
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, ListResources.Delete_subitem_and_its_subitems, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// End: <menu id="mnuDataTree-DeletePossibility">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_InsertQuestion(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_InsertQuestion, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_InsertQuestion}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-InsertQuestion">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_InsertQuestion
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			    <command id="CmdDataTree-Insert-Question" label="Insert Question" message="DataTreeInsert" icon="AddSubItem">
			      <parameters field="Questions" className="CmDomainQ" />
			    </command> // Insert_Question
			*/
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, InsertQuestion_Clicked, ListResources.Insert_Question, image: AreaResources.AddSubItem.ToBitmap());

			// End: <menu id="mnuDataTree-InsertQuestion">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void InsertQuestion_Clicked(object sender, EventArgs e)
		{
			var currentSemanticDomain = (ICmSemanticDomain)MyRecordList.CurrentObject;
			var cache = currentSemanticDomain.Cache;
			AreaServices.UndoExtension(ListResources.Insert_Question, cache.ActionHandlerAccessor, () =>
			{
				currentSemanticDomain.QuestionsOS.Add(cache.ServiceLocator.GetInstance<ICmDomainQFactory>().Create());
			});
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_DeleteQuestion(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_DeleteQuestion, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_DeleteQuestion}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-Delete-Question">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_DeleteQuestion
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			    <command id="CmdDataTree-Delete-Question" label="Delete Question" message="DataTreeDelete" icon="Delete">
			      <parameters field="Questions" className="CmDomainQ" />
			    </command>
			*/
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, ListResources.Delete_Question, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// End: <menu id="mnuDataTree-Delete-Question">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubPossibilities(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_SubPossibilities, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_SubPossibilities}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-SubPossibilities">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_SubPossibilities
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			      <item command="CmdDataTree-Insert-Possibility" /> // Shared
			*/
			var currentPossibility = MyRecordList.CurrentObject as ICmPossibility; // this will be null for the features 'list', but not to worry, since the menu won't be built for that tool.
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Insert_Subitem, image: AreaResources.AddSubItem.ToBitmap());
			menu.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _propertyTable, AreaServices.PopulateForSubitemInsert(ListsAreaMenuHelper.GetPossibilityList(MyRecordList), currentPossibility, ListResources.Insert_Subitem) };

			// End: <menu id="mnuDataTree-SubPossibilities">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubLocation(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_SubLocation, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_SubLocation}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-SubLocation">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_SubLocation
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			      <item command="CmdDataTree-Insert-Location" /> // Shared
				    <command id="CmdDataTree-Insert-Location" label="Insert subitem" message="DataTreeInsert" icon="AddSubItem">
				      <parameters field="SubPossibilities" className="CmLocation" />
				    </command>
			*/
			var currentPossibility = MyRecordList.CurrentObject as ICmPossibility; // this will be null for the features 'list', but not to worry, since the menu won't be built for that tool.
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Insert_Subitem, image: AreaResources.AddSubItem.ToBitmap());
			menu.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _propertyTable, AreaServices.PopulateForSubitemInsert(ListsAreaMenuHelper.GetPossibilityList(MyRecordList), currentPossibility, ListResources.Insert_Subitem) };

			// End: <menu id="mnuDataTree-SubLocation">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubAnthroCategory(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_SubAnthroCategory, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_SubAnthroCategory}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-SubAnthroCategory">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_SubAnthroCategory
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
				<item command="CmdDataTree-Insert-AnthroCategory" /> // Shared
			*/
			var currentPossibility = MyRecordList.CurrentObject as ICmPossibility; // this will be null for the features 'list', but not to worry, since the menu won't be built for that tool.
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Insert_Subcategory, image: AreaResources.AddSubItem.ToBitmap());
			menu.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _propertyTable, AreaServices.PopulateForSubitemInsert(ListsAreaMenuHelper.GetPossibilityList(MyRecordList), currentPossibility, ListResources.Insert_Subcategory) };

			// End: <menu id="mnuDataTree-SubAnthroCategory">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubSemanticDomain(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_SubSemanticDomain, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_SubSemanticDomain}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-SubSemanticDomain">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_SubSemanticDomain
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
				<item command="CmdDataTree-Insert-SemanticDomain" />
			*/
			var currentPossibility = MyRecordList.CurrentObject as ICmPossibility; // this will be null for the features 'list', but not to worry, since the menu won't be built for that tool.
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Insert_Subdomain, image: AreaResources.AddSubItem.ToBitmap());
			menu.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _propertyTable, AreaServices.PopulateForSubitemInsert(ListsAreaMenuHelper.GetPossibilityList(MyRecordList), currentPossibility, ListResources.Insert_Subdomain) };

			// End: <menu id="mnuDataTree-SubSemanticDomain">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubComplexEntryType(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_SubComplexEntryType, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_SubComplexEntryType}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-SubComplexEntryType">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_SubComplexEntryType
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
				<item command="CmdDataTree-Insert-LexEntryType" />
			*/
			var currentPossibility = MyRecordList.CurrentObject as ICmPossibility; // this will be null for the features 'list', but not to worry, since the menu won't be built for that tool.
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Insert_Subtype, image: AreaResources.AddSubItem.ToBitmap());
			menu.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _propertyTable, AreaServices.PopulateForSubitemInsert(ListsAreaMenuHelper.GetPossibilityList(MyRecordList), currentPossibility, ListResources.Insert_Subtype) };

			// End: <menu id="mnuDataTree-SubComplexEntryType">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubVariantEntryType(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_SubVariantEntryType, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_SubVariantEntryType}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-SubVariantEntryType">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_SubVariantEntryType
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
				<item command="CmdDataTree-Insert-LexEntryType" />
			*/
			var currentPossibility = MyRecordList.CurrentObject as ICmPossibility; // this will be null for the features 'list', but not to worry, since the menu won't be built for that tool.
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Insert_Subtype, image: AreaResources.AddSubItem.ToBitmap());
			menu.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _propertyTable, AreaServices.PopulateForSubitemInsert(ListsAreaMenuHelper.GetPossibilityList(MyRecordList), currentPossibility, ListResources.Insert_Subtype) };

			// End: <menu id="mnuDataTree-SubVariantEntryType">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_MoveMainReversalPOS(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_MoveMainReversalPOS, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_MoveMainReversalPOS}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-MoveMainReversalPOS">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_MoveMainReversalPOS
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(4);

			/*
		      <item command="CmdDataTree-Move-MoveReversalPOS" /> // Shared locally
				    <command id="CmdDataTree-Move-MoveReversalPOS" label="Move Category..." message="MoveReversalPOS">
				      <!--<parameters field="SubPossibilities" className="PartOfSpeech"/>-->
				    </command>
			*/
			var currentPartOfSpeech = MyRecordList.CurrentObject as IPartOfSpeech; // this will be null for the features 'list', but not to worry, since the menu won't be built for that tool.
			var enabled = CanMergeOrMovePos(currentPartOfSpeech);
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveReversalPOS_Clicked, ListResources.Move_Category);
			menu.Enabled = enabled;
			menu.Tag = currentPartOfSpeech;

			/*
		      <item label="-" translate="do not translate" />
			*/
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			/*
		      <item command="CmdDataTree-Merge-MergeReversalPOS" /> // Shared locally
				<command id="CmdDataTree-Merge-MergeReversalPOS" label="Merge Category into..." message="MergeReversalPOS" />
			*/
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MergeReversalPOS_Clicked, enabled ? ListResources.Merge_Category_into : $"{ListResources.Merge_Category_into} {StringTable.Table.GetString("(cannot merge this)")}");
			menu.Enabled = enabled;
			menu.Tag = currentPartOfSpeech;

			/*
		      <item command="CmdDataTree-Delete-ReversalSubPOS" />
			    <command id="CmdDataTree-Delete-ReversalSubPOS" label="Delete this Category and any Subcategories" message="DataTreeDelete" icon="Delete">
			      <parameters field="SubPossibilities" className="PartOfSpeech" />
			    </command> Delete_this_Category_and_any_Subcategories
			*/
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, ListResources.Delete_this_Category_and_any_Subcategories, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// End: <menu id="mnuDataTree-MoveMainReversalPOS">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_MoveReversalPOS(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_MoveReversalPOS, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_MoveReversalPOS}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-MoveReversalPOS">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_MoveReversalPOS
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

			/*
		      <item command="CmdDataTree-Move-MoveReversalPOS" /> // Shared locally
			*/
			var currentPartOfSpeech = MyRecordList.CurrentObject as IPartOfSpeech; // this will be null for the features 'list', but not to worry, since the menu won't be built for that tool.
			var enabled = CanMergeOrMovePos(currentPartOfSpeech);
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveReversalPOS_Clicked, ListResources.Move_Category);
			menu.Enabled = enabled;
			menu.Tag = currentPartOfSpeech;

			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				/*
					<command id="CmdDataTree-Promote-ProReversalSubPOS" label="Promote" message="PromoteReversalSubPOS" icon="MoveLeft">
					  <parameters field="SubPossibilities" className="PartOfSpeech" />
					</command>
				*/
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Promote_ReversalSubPOS_Clicked, AreaResources.Promote, image: imageHolder.smallCommandImages.Images[AreaServices.MoveLeft]);
				menu.Tag = currentPartOfSpeech;
			}

			// <item label="-" translate="do not translate" />
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

		    // <item command="CmdDataTree-Merge-MergeReversalPOS" />
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MergeReversalPOS_Clicked, enabled ? ListResources.Merge_Category_into : $"{ListResources.Merge_Category_into} {StringTable.Table.GetString("(cannot merge this)")}");
			menu.Enabled = enabled;
			menu.Tag = currentPartOfSpeech;

			// <item command="CmdDataTree-Delete-ReversalSubPOS" />
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, ListResources.Delete_this_Category_and_any_Subcategories, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// End: <menu id="mnuDataTree-MoveReversalPOS">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_SubPossibilities(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == ListsAreaConstants.mnuDataTree_POS_SubPossibilities, $"Expected argument value of '{ListsAreaConstants.mnuDataTree_POS_SubPossibilities}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-POS-SubPossibilities">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = ListsAreaConstants.mnuDataTree_POS_SubPossibilities
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			      <item command="CmdDataTree-Insert-POS-SubPossibilities" />
				    <command id="CmdDataTree-Insert-POS-SubPossibilities" label="Insert Subcategory..." message="DataTreeInsert" icon="AddSubItem">
				      <parameters field="SubPossibilities" className="PartOfSpeech" slice="owner" />
				    </command>
			*/
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.InsertCategory), AreaResources.Insert_Subcategory, image: AreaResources.AddSubItem.ToBitmap());
			menu.Tag = new List<object> { (ICmPossibilityList)MyRecordList.OwningObject, MyRecordList };

			// End: <menu id="mnuDataTree-POS-SubPossibilities">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private static bool CanMergeOrMovePos(IPartOfSpeech partOfSpeech)
		{
			return partOfSpeech.OwningList.ReallyReallyAllPossibilities.Count > 1;
		}

		private static IEnumerable<IPartOfSpeech> MergeOrMoveCandidates(IPartOfSpeech partOfSpeechCandidate)
		{
			var retval = new HashSet<IPartOfSpeech>();
			foreach (var partOfSpeech in partOfSpeechCandidate.OwningList.ReallyReallyAllPossibilities)
			{
				if (ReferenceEquals(partOfSpeechCandidate, partOfSpeech))
				{
					continue;
				}
				retval.Add((IPartOfSpeech)partOfSpeech);
			}
			return retval;
		}

		private void MoveReversalPOS_Clicked(object sender, EventArgs e)
		{
			var slice = MyDataTree.CurrentSlice;
			if (slice == null)
			{
				return;
			}
			var currentPartOfSpeech = (IPartOfSpeech)slice.MyCmObject;
			var cache = MyDataTree.Cache;
			var labels = new List<ObjectLabel>();
			foreach (var pos in MergeOrMoveCandidates(currentPartOfSpeech))
			{
				if (!pos.SubPossibilitiesOS.Contains(currentPartOfSpeech))
				{
					labels.Add(ObjectLabel.CreateObjectLabelOnly(cache, pos, "ShortNameTSS", "best analysis"));
				}
			}
			using (var dlg = new SimpleListChooser(cache, null, _propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), labels, null, AreaResources.Category_to_move_to, null))
			{
				dlg.SetHelpTopic("khtpChoose-CategoryToMoveTo");
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					var currentPOS = currentPartOfSpeech;
					var newOwner = (IPartOfSpeech)dlg.ChosenOne.Object;
					AreaServices.UndoExtension(AreaResources.Move_Reversal_Category, cache.ActionHandlerAccessor, ()=>
					{
						newOwner.MoveIfNeeded(currentPOS); //important when an item is moved into it's own subcategory
						if (!newOwner.SubPossibilitiesOS.Contains(currentPOS)) //this is also prevented in the interface, but I'm paranoid
						{
							newOwner.SubPossibilitiesOS.Add(currentPOS);
						}
					});
					// Note: PropChanged should happen on the old owner and the new in the 'Add" method call.
					// Have to jump to a main PartOfSpeech, as RecordClerk doesn't know anything about subcategories.
					//m_mediator.BroadcastMessageUntilHandled("JumpToRecord", newOwner.MainPossibility.Hvo);
				}
			}
		}

		private void MergeReversalPOS_Clicked(object sender, EventArgs e)
		{
			var slice = MyDataTree.CurrentSlice;
			if (slice == null)
			{
				return;
			}
			var currentPartOfSpeech = (IPartOfSpeech)slice.MyCmObject;
			var cache = MyDataTree.Cache;
			var labels = MergeOrMoveCandidates(currentPartOfSpeech).Select(pos => ObjectLabel.CreateObjectLabelOnly(cache, pos, "ShortNameTSS", "best analysis")).ToList();
			using (var dlg = new SimpleListChooser(cache, null, _propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), labels, null, AreaResources.Category_to_merge_into, null))
			{
				dlg.SetHelpTopic("khtpMergeCategories");
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					var currentPOS = currentPartOfSpeech;
					var survivor = (IPartOfSpeech)dlg.ChosenOne.Object;
					// Pass false to MergeObject, since we really don't want to merge the string info.
					AreaServices.UndoExtension(AreaResources.Merge_Reversal_Category, cache.ActionHandlerAccessor, ()=> survivor.MergeObject(currentPOS, false));
					// Note: PropChanged should happen on the old owner and the new in the 'Add" method call.
					// Have to jump to a main PartOfSpeech, as RecordList doesn't know anything about subcategories.
					//m_mediator.BroadcastMessageUntilHandled("JumpToRecord", survivor.MainPossibility.Hvo);
				}
			}
		}

		private void Promote_ReversalSubPOS_Clicked(object sender, EventArgs e)
		{
			var slice = MyDataTree.CurrentSlice;
			if (slice == null)
			{
				return;
			}
			var cache = MyDataTree.Cache;
			var currentPartOfSpeech = (ICmPossibility)slice.MyCmObject;
			var newOwner = currentPartOfSpeech.Owner.Owner;
			switch (newOwner.ClassID)
			{
				default:
					throw new ArgumentException("Illegal class.");
				case PartOfSpeechTags.kClassId:
					AreaServices.UndoExtension(AreaResources.Promote, cache.ActionHandlerAccessor, () => ((IPartOfSpeech)newOwner).SubPossibilitiesOS.Add(currentPartOfSpeech));
					break;
				case CmPossibilityListTags.kClassId:
					AreaServices.UndoExtension(AreaResources.Promote, cache.ActionHandlerAccessor, () => ((ICmPossibilityList)newOwner).PossibilitiesOS.Add(currentPartOfSpeech));
					break;
			}
		}
	}
}
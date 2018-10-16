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
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookEdit
{
	/// <summary>
	/// This class handles all interaction for the NotebookEditTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class NotebookEditToolMenuHelper : IFlexComponent, IDisposable
	{
		internal const string mnuDataTree_Subrecord_Hotlinks = "mnuDataTree-Subrecord-Hotlinks";
		internal const string mnuDataTree_Participants = "mnuDataTree-Participants";
		internal const string mnuDataTree_SubRecords = "mnuDataTree-SubRecords";
		internal const string mnuDataTree_SubRecords_Hotlinks = "mnuDataTree-SubRecords-Hotlinks";
		internal const string mnuDataTree_SubRecordSummary = "mnuDataTree-SubRecordSummary";

		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ITool _notebookEditTool;
		private NotebookAreaMenuHelper _notebookAreaMenuHelper;
		private IToolUiWidgetManager _rightClickContextMenuManager;
		private DataTree MyDataTree { get; set; }
		private RecordBrowseView RecordBrowseView { get; set; }
		private IRecordList MyRecordList { get; set; }
		private ISharedEventHandlers _sharedEventHandlers;
		private ToolStripButton _insertRecordToolStripButton;
		private ToolStripButton _insertFindRecordToolStripButton;
		private ToolStripSeparator _insertToolStripSeparator;
		private ToolStripButton _insertAddToDictionaryToolStripButton;
		private ToolStripButton _insertFindInDictionaryToolStripButton;
		private ToolStripMenuItem _toolsMenu;
		private ToolStripSeparator _toolsFindInDictionarySeparator;
		private ToolStripMenuItem _toolsFindInDictionaryMenu;

		internal BrowseViewContextMenuFactory MyBrowseViewContextMenuFactory { get; private set; }

		internal NotebookEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool currentNotebookTool, IRecordList recordList, DataTree dataTree, RecordBrowseView recordBrowseView)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(currentNotebookTool, nameof(currentNotebookTool));
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNull(recordList, nameof(recordList));
			Require.That(currentNotebookTool.MachineName == AreaServices.NotebookEditToolMachineName);

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
			_notebookEditTool = currentNotebookTool;
			MyDataTree = dataTree;
			RecordBrowseView = recordBrowseView;
			MyRecordList = recordList;

			var insertIndex = -1;
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(majorFlexComponentParameters, currentNotebookTool, recordList, dataTree);
			MyBrowseViewContextMenuFactory = new BrowseViewContextMenuFactory();
			MyBrowseViewContextMenuFactory.RegisterBrowseViewContextMenuCreatorMethod(AreaServices.mnuBrowseView, BrowseViewContextMenuCreatorMethod);
			_rightClickContextMenuManager = new RightClickContextMenuManager(_notebookEditTool, MyDataTree);
			// <item command="CmdConfigureColumns" defaultVisible="false" />
			_notebookAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsConfigureColumnsMenu(RecordBrowseView.BrowseViewer, ++insertIndex);
		}

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
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			_notebookAreaMenuHelper.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
			_notebookAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsCustomFieldsMenu();
			MyDataTree.DataTreeStackContextMenuFactory.MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(AreaServices.PanelMenuId, CreateMainPanelContextMenuStrip);
			_rightClickContextMenuManager.Initialize(_majorFlexComponentParameters, MyRecordList);

			// Add Insert menus, & Insert toolbar buttons.
			_notebookAreaMenuHelper.AddInsertMenuItems();
			AddInsertToolbarItems();
			SetupSliceMenus();

			_toolsMenu = MenuServices.GetToolsMenu(_majorFlexComponentParameters.MenuStrip);
			var insertIndex = 1;
			_toolsFindInDictionarySeparator = ToolStripMenuItemFactory.CreateToolStripSeparatorForToolStripMenuItem(_toolsMenu, insertIndex++);
			/*
			  <item command="CmdLexiconLookup" defaultVisible="false" />
				<command id="CmdLexiconLookup" label="Find in _Dictionary..." message="LexiconLookup" icon="findInDictionary" />
			*/
			_toolsMenu.DropDownOpening += ToolsMenu_DropDownOpening;
			_toolsFindInDictionaryMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_toolsMenu, _sharedEventHandlers.Get(AreaServices.LexiconLookup), AreaResources.Find_in_Dictionary, image: AreaResources.Find_Dictionary.ToBitmap(), insertIndex: insertIndex);

			Application.Idle += Application_Idle;
			MyDataTree.CurrentSliceChanged += MyDataTreeOnCurrentSliceChanged;
		}

		private void ToolsMenu_DropDownOpening(object sender, EventArgs e)
		{
			_toolsFindInDictionaryMenu.Enabled = AreaWideMenuHelper.DataTreeCurrentSliceAsStTextSlice(MyDataTree) != null;
		}

		#endregion

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
				return; // No need to do it more than once.
			}

			if (disposing)
			{
				_toolsMenu.DropDownItems.Remove(_toolsFindInDictionarySeparator);
				_toolsFindInDictionarySeparator.Dispose();
				_toolsMenu.DropDownOpening -= ToolsMenu_DropDownOpening;
				_toolsMenu.DropDownItems.Remove(_toolsFindInDictionaryMenu);
				_toolsFindInDictionaryMenu.Click -= _sharedEventHandlers.Get(AreaServices.LexiconLookup);
				_toolsFindInDictionaryMenu.Dispose();

				Application.Idle -= Application_Idle;
				MyDataTree.CurrentSliceChanged -= MyDataTreeOnCurrentSliceChanged;
				_rightClickContextMenuManager?.UnwireSharedEventHandlers();
				InsertToolbarManager.ResetInsertToolbar(_majorFlexComponentParameters);
				_rightClickContextMenuManager?.Dispose();
				_insertRecordToolStripButton?.Dispose();
				_insertFindRecordToolStripButton?.Dispose();
				_insertToolStripSeparator?.Dispose();
				_insertAddToDictionaryToolStripButton?.Dispose();
				_insertFindInDictionaryToolStripButton?.Dispose();
				MyBrowseViewContextMenuFactory?.Dispose();
				_notebookAreaMenuHelper?.Dispose();
			}
			_majorFlexComponentParameters = null;
			_notebookEditTool = null;
			_notebookAreaMenuHelper = null;
			_rightClickContextMenuManager = null;
			MyDataTree = null;
			MyRecordList = null;
			_insertRecordToolStripButton = null;
			_insertFindRecordToolStripButton = null;
			_insertToolStripSeparator = null;
			_insertAddToDictionaryToolStripButton = null;
			_insertFindInDictionaryToolStripButton = null;
			MyBrowseViewContextMenuFactory = null;
			_toolsMenu = null;
			_toolsFindInDictionarySeparator = null;
			_toolsFindInDictionaryMenu = null;
			_sharedEventHandlers = null;

			_isDisposed = true;
		}
		#endregion

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
		{
			var contextMenuStrip = new ContextMenuStrip();
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);

			// <item label="Insert _Subrecord" command="CmdDataTree-Insert-Subrecord"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdInsertSubRecord), NotebookResources.Insert_Subrecord);

			// <item label="Insert S_ubrecord of Subrecord" command="CmdDataTree-Insert-Subsubrecord" defaultVisible="false"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdInsertSubSubRecord), NotebookResources.Insert_Subrecord_of_Subrecord);

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
				var root = MyDataTree.Root;
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
			var record = MyDataTree.Root as IRnGenericRec;
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

			AreaServices.UndoExtensionUsingNewOrCurrentUOW(NotebookResources.Demote_SansDots, cache.ActionHandlerAccessor, () =>
			{
				newOwner.SubRecordsOS.Insert(0, record);
			});
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			// Deal with visibility and enabling of toolbar buttons.
			var currentSliceAsStTextSlice = AreaWideMenuHelper.DataTreeCurrentSliceAsStTextSlice(MyDataTree);
			IVwSelection currentSelection = null;
			if (_insertAddToDictionaryToolStripButton != null && currentSliceAsStTextSlice != null)
			{
				currentSelection = currentSliceAsStTextSlice.RootSite.RootBox.Selection;
				AreaWideMenuHelper.Set_CmdAddToLexicon_State(_majorFlexComponentParameters.LcmCache, _insertAddToDictionaryToolStripButton, currentSelection);
			}
			else
			{
				_insertAddToDictionaryToolStripButton.Enabled = false;
			}
			if (_insertFindInDictionaryToolStripButton != null && currentSliceAsStTextSlice != null)
			{
				currentSelection = currentSliceAsStTextSlice.RootSite.RootBox.Selection;
				_insertFindInDictionaryToolStripButton.Enabled = _notebookAreaMenuHelper.MyAreaWideMenuHelper.IsLexiconLookupEnabled(currentSelection);
			}
			else
			{
				_insertFindInDictionaryToolStripButton.Enabled = false;
			}
			_insertFindInDictionaryToolStripButton.Tag = currentSelection; // May be null, which is fine.
		}

		private void MyDataTreeOnCurrentSliceChanged(object sender, CurrentSliceChangedEventArgs e)
		{
			if (_insertFindInDictionaryToolStripButton != null)
			{
				_insertFindInDictionaryToolStripButton.Enabled = AreaWideMenuHelper.DataTreeCurrentSliceAsStTextSlice(MyDataTree) != null;
			}
		}

		private void AddInsertToolbarItems()
		{
			var newToolbarItems = new List<ToolStripItem>(5);
			/*
			  <item command="CmdInsertRecord" defaultVisible="false" /> // Shared locally
				Tooltip: <item id="CmdInsertRecord">Create a new Record in your Notebook.</item>
					<command id="CmdInsertRecord" label="Record" message="InsertItemInVector" icon="nbkRecord" shortcut="Ctrl+I">
					  <params className="RnGenericRec" />
					</command>
			*/
			_insertRecordToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdInsertRecord), "toolStripButtonInsertRecord", NotebookResources.nbkRecord, $"{NotebookResources.Create_a_new_Record_in_your_Notebook} (CTRL+I)");
			newToolbarItems.Add(_insertRecordToolStripButton);

			/*
			  <item command="CmdGoToRecord" defaultVisible="false" /> // Shared from afar
			*/
			_insertFindRecordToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdGoToRecord), "toolStripButtonInsertFindRecord", NotebookResources.goToRecord, $"{NotebookResources.Find_a_Record_in_your_Notebook} (CTRL+F)");
			newToolbarItems.Add(_insertFindRecordToolStripButton);

			/*
			  <item label="-" translate="do not translate" />
			*/
			_insertToolStripSeparator = ToolStripButtonFactory.CreateToolStripSeparator();
			newToolbarItems.Add(_insertToolStripSeparator);

			/*
			  <item command="CmdAddToLexicon" defaultVisible="false" /> // Shared from afar
				Tooltip: <item id="CmdAddToLexicon">Add the current word to the lexicon (if it is a vernacular word).</item>
					<command id="CmdAddToLexicon" label="Add to Dictionary..." message="AddToLexicon" icon="majorEntry" />
			*/
			_insertAddToDictionaryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(AreaServices.CmdAddToLexicon), "toolStripButtonAddToLexicon", AreaResources.Major_Entry.ToBitmap(), AreaResources.Add_the_current_word_to_the_lexicon);
			newToolbarItems.Add(_insertAddToDictionaryToolStripButton);
			_insertAddToDictionaryToolStripButton.Enabled = AreaWideMenuHelper.DataTreeCurrentSliceAsStTextSlice(MyDataTree) != null;
			_insertAddToDictionaryToolStripButton.Tag = MyDataTree;

			/*
			  <item command="CmdLexiconLookup" defaultVisible="false" />
				<command id="CmdLexiconLookup" label="Find in _Dictionary..." message="LexiconLookup" icon="findInDictionary" />
			*/
			_insertFindInDictionaryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(AreaServices.LexiconLookup), "toolStripButtonLexiconLookup", AreaResources.Find_Dictionary.ToBitmap(), AreaResources.Find_in_Dictionary);
			newToolbarItems.Add(_insertFindInDictionaryToolStripButton);
			_insertFindInDictionaryToolStripButton.Enabled = AreaWideMenuHelper.DataTreeCurrentSliceAsStTextSlice(MyDataTree) != null;

			InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, newToolbarItems);
		}

		private void SetupSliceMenus()
		{
			#region Left edge context menus

			// <menu id="mnuDataTree-Participants">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(mnuDataTree_Participants, Create_mnuDataTree_Participants);

			// <menu id="mnuDataTree-SubRecords">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(mnuDataTree_SubRecords, Create_mnuDataTree_SubRecords);

			// <menu id="mnuDataTree-SubRecordSummary">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(mnuDataTree_SubRecordSummary, Create_mnuDataTree_SubRecordSummary);

			#endregion Left edge context menus

			#region Hotlinks menus

			// <menu id="mnuDataTree-Subrecord-Hotlinks">
			MyDataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Subrecord_Hotlinks, Create_mnuDataTree_Subrecord_Hotlinks);

			// <menu id="mnuDataTree-SubRecords-Hotlinks">
			MyDataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_SubRecords_Hotlinks, Create_mnuDataTree_SubRecords_Hotlinks);

			#endregion Hotlinks menus
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Participants(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == mnuDataTree_Participants, $"Expected argument value of '{mnuDataTree_Subrecord_Hotlinks}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-Participants">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_Participants
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <command id="CmdDataTree-Delete-Participants" label="Delete Participants" message="DeleteParticipants" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_Participants_Clicked, NotebookResources.Delete_Participants);

			// End: <menu id="mnuDataTree-Participants">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void Delete_Participants_Clicked(object sender, EventArgs e)
		{
			var slice = MyDataTree.CurrentSlice;
			var parentSlice = slice.ParentSlice;
			var roledPartic = slice.MyCmObject as IRnRoledPartic;
			if (roledPartic == null)
			{
				// This may happen if the user started Flex no participants of any kind, and then added a generic one, and then added a typed one.
				roledPartic = parentSlice.MyCmObject as IRnRoledPartic;
			}
			if (roledPartic == null)
			{
				// Just give up.
				return;
			}
			AreaServices.UndoExtension(NotebookResources.Delete_Participants, roledPartic.Cache.ActionHandlerAccessor, () =>
			{
				roledPartic.Delete();
			});

			parentSlice.Collapse();
			parentSlice.Expand();
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubRecords(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == mnuDataTree_SubRecords, $"Expected argument value of '{mnuDataTree_Subrecord_Hotlinks}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-SubRecords">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_Participants
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <item command="CmdDataTree-Insert-Subrecord" /> // Shared locally
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdInsertSubRecord), NotebookResources.Insert_Subrecord);

			// End: <menu id="mnuDataTree-SubRecords">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubRecordSummary(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == mnuDataTree_SubRecordSummary, $"Expected argument value of '{mnuDataTree_Subrecord_Hotlinks}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-SubRecordSummary">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_Participants
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

			// <item command="CmdDataTree-Insert-Subrecord" /> // Shared locally
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdInsertSubRecord), NotebookResources.Insert_Subrecord);

			// <item command="CmdDataTree-Insert-Subsubrecord" /> // Shared locally
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdInsertSubSubRecord), NotebookResources.Insert_Subrecord_of_Subrecord);

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

			// End: <menu id="mnuDataTree-SubRecordSummary">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		/// <summary>
		/// See if it makes sense to provide the "Move Up" command.
		/// </summary>
		private bool CanMoveRecordUp
		{
			get
			{
				var currentSliceObject = MyDataTree.CurrentSlice?.MyCmObject as IRnGenericRec;
				var recordOwner = currentSliceObject?.Owner as IRnGenericRec;
				return currentSliceObject != null && recordOwner != null && currentSliceObject.OwnOrd > 0;
			}
		}

		private void MoveRecordUp_Clicked(object sender, EventArgs e)
		{
			var record = (IRnGenericRec)MyDataTree.CurrentSlice.MyCmObject;
			var recordOwner = (IRnGenericRec)record.Owner;
			var idxOrig = record.OwnOrd;
			AreaServices.UndoExtensionUsingNewOrCurrentUOW(LanguageExplorerResources.MoveUp, record.Cache.ActionHandlerAccessor, () =>
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
				var currentSliceObject = MyDataTree.CurrentSlice?.MyCmObject as IRnGenericRec;
				var recordOwner = currentSliceObject?.Owner as IRnGenericRec;
				return currentSliceObject!= null && recordOwner != null && currentSliceObject.OwnOrd < recordOwner.SubRecordsOS.Count - 1;
			}
		}

		private void MoveRecordDown_Clicked(object sender, EventArgs e)
		{
			var record = (IRnGenericRec)MyDataTree.CurrentSlice.MyCmObject;
			var recordOwner = (IRnGenericRec)record.Owner;
			var idxOrig = record.OwnOrd;
			AreaServices.UndoExtensionUsingNewOrCurrentUOW(LanguageExplorerResources.MoveDown, record.Cache.ActionHandlerAccessor, () =>
			{
				// idxOrig + 2 looks strange, but it's the correct value to make this work.
				recordOwner.SubRecordsOS.MoveTo(idxOrig, idxOrig, recordOwner.SubRecordsOS, idxOrig + 2);
			});
		}

		private bool CanPromoteSubitemInVector
		{
			get
			{
				var slice = MyDataTree.CurrentSlice;
				var currentSliceObject = slice.MyCmObject as IRnGenericRec;
				return currentSliceObject != null  && currentSliceObject.Owner is IRnGenericRec;
			}
		}

		private void PromoteSubitemInVector_Clicked(object sender, EventArgs e)
		{
			var record = (IRnGenericRec)MyDataTree.CurrentSlice.MyCmObject;
			var recordOwner = record.Owner as IRnGenericRec;
			AreaServices.UndoExtensionUsingNewOrCurrentUOW(AreaResources.Promote, record.Cache.ActionHandlerAccessor, () =>
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
				Publisher.Publish("JumpToRecord", record.Hvo);
			}
		}

		/// <summary>
		/// See if it makes sense to provide the "Demote..." command.
		/// </summary>
		private bool CanDemoteSubitemInVector
		{
			get
			{
				var currentSliceObject = MyDataTree.CurrentSlice?.MyCmObject as IRnGenericRec;
				return currentSliceObject?.Owner is IRnGenericRec && (currentSliceObject.Owner as IRnGenericRec).SubRecordsOS.Count > 1;
			}
		}

		private void DemoteSubrecord_Clicked(object sender, EventArgs e)
		{
			var cache = _majorFlexComponentParameters.LcmCache;
			var record = (IRnGenericRec)MyDataTree.CurrentSlice.MyCmObject;
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

			AreaServices.UndoExtensionUsingNewOrCurrentUOW(NotebookResources.Demote_SansDots, cache.ActionHandlerAccessor, () =>
			{
				newOwner.SubRecordsOS.Insert(0, record);
			});
		}

		private IRnGenericRec ChooseNewOwner(IRnGenericRec[] records, string sTitle)
		{
			var cache = _majorFlexComponentParameters.LcmCache;
			using (var dlg = new ReallySimpleListChooser(PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable), ObjectLabel.CreateObjectLabels(cache, records, "ShortName", cache.WritingSystemFactory.GetStrFromWs(cache.DefaultAnalWs)),
				string.Empty, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
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

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Subrecord_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			Require.That(hotlinksMenuId == mnuDataTree_Subrecord_Hotlinks, $"Expected argument value of '{mnuDataTree_Subrecord_Hotlinks}', but got '{hotlinksMenuId}' instead.");

			/*
			    <command id="CmdDataTree-Insert-Subrecord" label="Insert _Subrecord" message="InsertItemInVector">
			      <parameters className="RnGenericRec" subrecord="true" />
			    </command>
			*/
			return CommonHotlinksCreator();
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_SubRecords_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			Require.That(hotlinksMenuId == mnuDataTree_SubRecords_Hotlinks, $"Expected argument value of '{mnuDataTree_SubRecords_Hotlinks}', but got '{hotlinksMenuId}' instead.");

			/*
			    <command id="CmdDataTree-Insert-Subrecord" label="Insert _Subrecord" message="InsertItemInVector"> // Shared locally
			      <parameters className="RnGenericRec" subrecord="true" />
			    </command>
			*/
			return CommonHotlinksCreator();
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> CommonHotlinksCreator()
		{
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			/*
			    <command id="CmdDataTree-Insert-Subrecord" label="Insert _Subrecord" message="InsertItemInVector"> // Shared locally
			      <parameters className="RnGenericRec" subrecord="true" />
			    </command>
			*/
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdInsertSubRecord), NotebookResources.Insert_Subrecord);

			return hotlinksMenuItemList;
		}

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

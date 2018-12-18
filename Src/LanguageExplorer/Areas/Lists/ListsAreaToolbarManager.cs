// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lists
{
	internal sealed class ListsAreaToolbarManager : IToolUiWidgetManager
	{
		private DataTree MyDataTree { get; set; }
		private IRecordList MyRecordList { get; set; }
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ISharedEventHandlers _sharedEventHandlers;
		private IListArea _listArea;
		private ToolStripButton _insertItemToolStripButton;
		private ToolStripButton _insertSubItemToolStripButton;
		private ToolStripButton _insertEntryToolStripButton;
		private ToolStripButton _duplicateItemToolStripButton;

		internal ListsAreaToolbarManager(DataTree dataTree, IListArea listArea)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNull(listArea, nameof(listArea));

			MyDataTree = dataTree;
			_listArea = listArea;
		}

		#region Implementation of IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			MyRecordList = recordList;

			AddInsertToolbarItems();
		}

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
			if (_insertItemToolStripButton != null)
			{
				_insertItemToolStripButton.Click -= _sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem);
			}
			if (_insertSubItemToolStripButton != null)
			{
				_insertSubItemToolStripButton.Click -= _sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem);
			}
		}
		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~ListsAreaToolbarManager()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
		void IDisposable.Dispose()
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
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
				Application.Idle -= ApplicationOnIdle;
				MyDataTree.CurrentSliceChanged -= MyDataTreeOnCurrentSliceChanged;
				InsertToolbarManager.ResetInsertToolbar(_majorFlexComponentParameters);
				_insertItemToolStripButton?.Dispose();
				_insertSubItemToolStripButton?.Dispose();
				_duplicateItemToolStripButton?.Dispose();
			}
			MyRecordList = null;
			MyDataTree = null;
			_majorFlexComponentParameters = null;
			_sharedEventHandlers = null;
			_listArea = null;
			_insertItemToolStripButton = null;
			_insertSubItemToolStripButton = null;
			_duplicateItemToolStripButton = null;

			_isDisposed = true;
		}

		#endregion

		private void AddInsertToolbarItems()
		{
			const string insertMainItem = "insertMainItem";
			const string insertSubItem = "insertSubItem";
			const string duplicateMainItem = "duplicateMainItem";
			/*
			These all go on the "Insert" toolbar, but they are tool-specific.
			*/
			var activeListTool = _listArea.ActiveTool;
			var currentPossibilityList = MyRecordList.OwningObject as ICmPossibilityList; // Will be null for AreaServices.FeatureTypesAdvancedEditMachineName tool.
			if (currentPossibilityList != null && currentPossibilityList.IsClosed)
			{
				// No sense in bothering with toolbar buttons, since the list is closed to adding new items.
				return;
			}

			var toolsThatSupportItemDuplication = new HashSet<string>
			{
				AreaServices.ChartmarkEditMachineName,
				AreaServices.CharttempEditMachineName,
				AreaServices.TextMarkupTagsEditMachineName
			};
			// May be a subclass of ICmPossibility. Will be null, if nothing is in the list.
			var currentPossibility = MyRecordList.CurrentObject as ICmPossibility;
			// The first one will be the main Insert item button
			// The second is optional for lists that allow inserting subitems.
			// The third is option for lists that allow duplicating top level items.
			// If the third is added, but not the second, then null must be added to hold the place of the third.
			var toolbarButtonCreationData = new Dictionary<string, Tuple<EventHandler, string, Dictionary<string, string>>>(3);
			switch (activeListTool.MachineName)
			{
				case AreaServices.AnthroEditMachineName:
					/*
						<item command="CmdInsertAnthroCategory" defaultVisible="false" />
					*/
					toolbarButtonCreationData.Add(insertMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Anthropology_Category, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Anthropology_Category)));

					/*
						<item command="CmdDataTree-Insert-AnthroCategory" defaultVisible="false" label="Subcategory" />
					*/
					toolbarButtonCreationData.Add(insertSubItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subcategory, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Anthropology_Category)));
					// No duplication.
					break;
				case AreaServices.FeatureTypesAdvancedEditMachineName:
					/*
						  <item command="CmdInsertFeatureType" defaultVisible="false" />
								<command id="CmdInsertFeatureType" label="_Feature Type" message="InsertItemInVector" icon="AddItem">
								  <params className="FsFeatStrucType" />
								</command>
					*/
					_insertItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(ListsAreaMenuHelper.InsertFeatureType), "toolStripButtonInsertItem", AreaResources.AddItem.ToBitmap(), ListResources.Feature_Type);
					InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripItem> { _insertItemToolStripButton });
					// No duplication.
					break;
				case AreaServices.LexRefEditMachineName:
					/*
						<item command="CmdInsertLexRefType" defaultVisible="false" />
					    <command id="CmdInsertLexRefType" label="_Lexical Reference Type" message="InsertItemInVector" icon="AddItem">
					      <params className="LexRefType" />
					    </command>
						<item id="CmdInsertLexRefType">Create a new lexical relation.</item>
					*/
					// No need for something like: CmdDataTree-Insert-LexRefType, since they are not nested.
					toolbarButtonCreationData.Add(insertMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Lexical_Relation, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Lexical_Reference_Type)));
					// No duplication.
					break;
				case AreaServices.LocationsEditMachineName:
					/*
						<item command="CmdInsertLocation" defaultVisible="false" />
					*/
					toolbarButtonCreationData.Add(insertMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Location, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Location)));
					/*
						<item command="CmdDataTree-Insert-Location" defaultVisible="false" label="Subitem" />
					*/
					toolbarButtonCreationData.Add(insertSubItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subitem, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Location)));
					// No duplication.
					break;
				case AreaServices.MorphTypeEditMachineName:
					// Can't get here, since code above would have returned.
					// List is closed at least as of 6AUG2018, when I (RBR) tried to add the menus.
					// No duplication.
					break;
				case AreaServices.PeopleEditMachineName:
					/*
						<item command="CmdInsertPerson" defaultVisible="false" />
					*/
					// No need for something like: CmdDataTree-Insert-Person, since there are no nested people.
					toolbarButtonCreationData.Add(insertMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Person, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Person)));
					// No duplication.
					break;
				case AreaServices.SemanticDomainEditMachineName:
					/*
						<item command="CmdInsertSemDom" defaultVisible="false" />
					*/
					toolbarButtonCreationData.Add(insertMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Semantic_Domain, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Semantic_Domain)));
					/*
						<item command="CmdDataTree-Insert-SemanticDomain" defaultVisible="false" label="Subdomain" />
					*/
					toolbarButtonCreationData.Add(insertSubItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subdomain, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Semantic_Domain)));
					// No duplication.
					break;
				case AreaServices.ComplexEntryTypeEditMachineName:
					// This list can only have LexEntryType instances.
					/*
						<command id="CmdInsertLexEntryType" label="_Type" message="InsertItemInVector" icon="AddItem">
						  <params className="LexEntryType" />
						</command>
					*/
					toolbarButtonCreationData.Add(insertMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Complex_Form_Type, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					/*
						<command id="CmdDataTree-Insert-LexEntryType" label="Insert Subtype" message="DataTreeInsert" icon="AddSubItem">
						  <parameters field="SubPossibilities" className="LexEntryType" />
						</command>
					*/
					toolbarButtonCreationData.Add(insertSubItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subtype, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					// No duplication.
					break;
				case AreaServices.VariantEntryTypeEditMachineName:
					// NB: Inserts one of two class options:
					//		1) It will insert LexEntryInflType instances in an owning LexEntryInflType instance.
					//		2) It will only insert at the top or nested in other LexEntryType instances.
					toolbarButtonCreationData.Add(insertMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Variant_Type, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					toolbarButtonCreationData.Add(insertSubItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subtype, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					break;
				case AreaServices.ReversalToolReversalIndexPOSMachineName:
					// The FW 8 & 9 behavior has this one below the custom list creation menu, but I'm (RBR) going to regularize it and put it above.
					/*
						<item command="CmdInsertPOS" defaultVisible="false" />
					    <command id="CmdInsertPOS" label="Category" message="InsertItemInVector" shortcut="Ctrl+I" icon="AddItem">
					      <params className="PartOfSpeech" />
					    </command>
					*/
					_insertItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(AreaServices.InsertCategory), "toolStripButtonInsertItem", AreaResources.AddItem.ToBitmap(), AreaResources.Add_a_new_category);
					_insertItemToolStripButton.Tag = new List<object> { currentPossibilityList, MyRecordList };
					InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripItem> { _insertItemToolStripButton });
					/*
						<item command="CmdDataTree-Insert-POS-SubPossibilities" defaultVisible="false" label="Subcategory..." />
						<command id="CmdDataTree-Insert-POS-SubPossibilities" label="Insert Subcategory..." message="DataTreeInsert" icon="AddSubItem">
						  <parameters field="SubPossibilities" className="PartOfSpeech" slice="owner" />
						</command>
					*/
					_insertSubItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(AreaServices.InsertCategory), "toolStripButtonInsertSubItem", AreaResources.AddSubItem.ToBitmap(), AreaResources.Subcategory);
					_insertSubItemToolStripButton.Tag = new List<object> { currentPossibilityList, MyRecordList };
					InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripItem> { _insertSubItemToolStripButton });
					// No duplication.
					break;
				case AreaServices.DomainTypeEditMachineName: // Fall through to default.
				case AreaServices.ConfidenceEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.DialectsListEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.EducationEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.ExtNoteTypeEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.GenresEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.LanguagesListEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.RecTypeEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.PositionsEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.PublicationsEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.RestrictionsEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.RoleEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.SenseTypeEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.StatusEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.ChartmarkEditMachineName: // Fall through to default.
				case AreaServices.CharttempEditMachineName: // Fall through to default.
				case AreaServices.TextMarkupTagsEditMachineName: // Fall through to default.
				case AreaServices.TimeOfDayEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.TranslationTypeEditMachineName: // Fall through to default. // No duplication.
				case AreaServices.UsageTypeEditMachineName: // Fall through to default. // No duplication.
				default:
					/*
						NB: use this class: <CmCustomItem num="27" abstract="false" base="CmPossibility" />
						<item command="CmdInsertCustomItem" defaultVisible="false" />

					XOR

						NB: use this class: <CmPossibility num="7" abstract="false" base="CmObject">
						<item command="CmdInsertPossibility" defaultVisible="false" />
					*/
					toolbarButtonCreationData.Add(insertMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Insert_Item, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Item)));
					/*
					*/
					if (currentPossibilityList.Depth > 1)
					{
						/*
							NB: Use this class: <CmCustomItem num="27" abstract="false" base="CmPossibility" />
							<item command="CmdDataTree-Insert-CustomItem" defaultVisible="false" label="Subitem" />

						XOR

							NB: Use this class: <CmPossibility num="7" abstract="false" base="CmObject">
							<item command="CmdDataTree-Insert-Possibility" defaultVisible="false" label="Subitem" />
						*/
						toolbarButtonCreationData.Add(insertSubItem, new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subtype, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Item)));
					}
					if (toolsThatSupportItemDuplication.Contains(activeListTool.MachineName) || activeListTool.MachineName.StartsWith("CustomList"))
					{
						// Add support for the duplicate main item button.
						toolbarButtonCreationData.Add(duplicateMainItem, new Tuple<EventHandler, string, Dictionary<string, string>>(Duplicate_Item_Clicked, ListResources.Duplicate_Item, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Duplicate_Item)));
					}
					break;
			}
			if (activeListTool.MachineName != AreaServices.FeatureTypesAdvancedEditMachineName)
			{
				// Add "Entry" menu to all other tools.
				_insertEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(AreaServices.CmdAddToLexicon), "toolStripButtonInsertEntry", AreaResources.Major_Entry.ToBitmap(), AreaResources.Add_to_Dictionary);
				_insertEntryToolStripButton.Tag = MyDataTree;
				_insertEntryToolStripButton.Visible = false;
			}
			// If there is nothing in "toolbarButtonCreationData", then the tool did everything.
			if (toolbarButtonCreationData.Any())
			{
				// Menu creation baton passed here.
				// The first item in "toolbarButtonCreationData", it is the main "Add" item.
				var currentMenuTuple = toolbarButtonCreationData[insertMainItem];
				_insertItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(currentMenuTuple.Item1, "toolStripButtonInsertItem", AreaResources.AddItem.ToBitmap(), currentMenuTuple.Item2);
				_insertItemToolStripButton.Tag = new List<object> { currentPossibilityList, MyDataTree, MyRecordList, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, currentMenuTuple.Item3 };

				// SubItem information is optionally present in "toolbarButtonCreationData".
				if (toolbarButtonCreationData.TryGetValue(insertSubItem, out currentMenuTuple))
				{
					// NB: Lists that cannot have sub-items won't be adding this toolbar button option at all.
					_insertSubItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(currentMenuTuple.Item1, "toolStripButtonInsertSubItem", AreaResources.AddSubItem.ToBitmap(), currentMenuTuple.Item2);
					_insertSubItemToolStripButton.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, currentMenuTuple.Item3 };
					_insertSubItemToolStripButton.Enabled = currentPossibilityList.PossibilitiesOS.Any(); // Visbile, but only enabled, if there are possible owners for the new sub item.
				}
				// The Duplicate item button is optionally present .
				if (toolbarButtonCreationData.TryGetValue(duplicateMainItem, out currentMenuTuple))
				{
					_duplicateItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(currentMenuTuple.Item1, "toolStripButtonDuplicateItem", ListResources.Copy, currentMenuTuple.Item2);
					_duplicateItemToolStripButton.ImageTransparentColor = Color.Magenta;
					_duplicateItemToolStripButton.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, currentMenuTuple.Item3 };
				}
				var itemsToAdd = new List<ToolStripItem>(4);
				if (_insertEntryToolStripButton != null)
				{
					itemsToAdd.Add(_insertEntryToolStripButton);
				}
				itemsToAdd.Add(_insertItemToolStripButton);
				if (_insertSubItemToolStripButton != null)
				{
					itemsToAdd.Add(_insertSubItemToolStripButton);
				}
				if (_duplicateItemToolStripButton != null)
				{
					itemsToAdd.Add(_duplicateItemToolStripButton);
				}
				InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, itemsToAdd);

				Application.Idle += ApplicationOnIdle;
				MyDataTree.CurrentSliceChanged += MyDataTreeOnCurrentSliceChanged;
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
			AreaServices.UndoExtension(otherOptions[AreaServices.BaseUowMessage], possibilityList.Cache.ActionHandlerAccessor, () =>
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
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"Start: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
			if (_duplicateItemToolStripButton != null)
			{
				_duplicateItemToolStripButton.Enabled = MyRecordList.CurrentObject != null && MyRecordList.CurrentObject.Owner.ClassID == CmPossibilityListTags.kClassId;
			}
			var currentSliceAsStTextSlice = AreaWideMenuHelper.DataTreeCurrentSliceAsStTextSlice(MyDataTree);
			if (_insertEntryToolStripButton != null && _insertEntryToolStripButton.Visible && currentSliceAsStTextSlice != null)
			{
				AreaWideMenuHelper.Set_CmdAddToLexicon_State(_majorFlexComponentParameters.LcmCache, _insertEntryToolStripButton, currentSliceAsStTextSlice.RootSite.RootBox.Selection);
			}
			else
			{
				_insertEntryToolStripButton.Enabled = false;
			}
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"End: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
		}

		private void MyDataTreeOnCurrentSliceChanged(object sender, CurrentSliceChangedEventArgs e)
		{
			if (_insertEntryToolStripButton == null)
			{
				return;
			}
			_insertEntryToolStripButton.Visible = e.CurrentSlice is StTextSlice;
		}
	}
}
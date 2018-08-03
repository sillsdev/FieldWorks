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
using SIL.Code;
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

		private void AddInsertToolbarItems()
		{
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
			var currentPossibility = (ICmPossibility)MyRecordList.CurrentObject; // May be a subclass of ICmPossibility.
			var toolbarButtonCreationData = new List<Tuple<EventHandler, string, Dictionary<string, string>>>(2);
			//string tooltip;
			switch (activeListTool.MachineName)
			{
				case AreaServices.AnthroEditMachineName:
					/*
						<item command="CmdInsertAnthroCategory" defaultVisible="false" />
					*/
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Anthropology_Category, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Anthropology_Category)));

					/*
						<item command="CmdDataTree-Insert-AnthroCategory" defaultVisible="false" label="Subcategory" />
					*/
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subcategory, AreaServices.PopulateForSubitemInsert(currentPossibility, ListResources.Insert_Anthropology_Category)));
					break;
				case AreaServices.FeatureTypesAdvancedEditMachineName:
					/*
						  <item command="CmdInsertFeatureType" defaultVisible="false" />
								<command id="CmdInsertFeatureType" label="_Feature Type" message="InsertItemInVector" icon="AddItem">
								  <params className="FsFeatStrucType" />
								</command>
					*/
					_insertItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(ListsAreaMenuHelper.InsertFeatureType), "toolStripButtonInsertItem", AreaResources.AddItem.ToBitmap(), ListResources.Feature_Type);
					InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripButton> { _insertItemToolStripButton });
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
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Lexical_Relation, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Lexical_Reference_Type)));
					break;
				case AreaServices.LocationsEditMachineName:
					/*
						<item command="CmdInsertLocation" defaultVisible="false" />
					*/
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Location, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Location)));
					/*
						<item command="CmdDataTree-Insert-Location" defaultVisible="false" label="Subitem" />
					*/
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subitem, AreaServices.PopulateForSubitemInsert(currentPossibility, ListResources.Insert_Location)));
					break;
				case AreaServices.MorphTypeEditMachineName:
					// List is closed at least as of 6AUG2018, when I (RBR) tried to add the menus.
					break;
				case AreaServices.PeopleEditMachineName:
					/*
						<item command="CmdInsertPerson" defaultVisible="false" />
					*/
					// No need for something like: CmdDataTree-Insert-Person, since there are no nested people.
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Person, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Person)));
					break;
				case AreaServices.SemanticDomainEditMachineName:
					/*
						<item command="CmdInsertSemDom" defaultVisible="false" />
					*/
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Semantic_Domain, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Semantic_Domain)));
					/*
						<item command="CmdDataTree-Insert-SemanticDomain" defaultVisible="false" label="Subdomain" />
					*/
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subdomain, AreaServices.PopulateForSubitemInsert(currentPossibility, ListResources.Insert_Semantic_Domain)));
					break;
				case AreaServices.ComplexEntryTypeEditMachineName:
					// This list can only have LexEntryType instances.
					/*
						<command id="CmdInsertLexEntryType" label="_Type" message="InsertItemInVector" icon="AddItem">
						  <params className="LexEntryType" />
						</command>
					*/
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Complex_Form_Type, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					/*
						<command id="CmdDataTree-Insert-LexEntryType" label="Insert Subtype" message="DataTreeInsert" icon="AddSubItem">
						  <parameters field="SubPossibilities" className="LexEntryType" />
						</command>
					*/
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subtype, AreaServices.PopulateForSubitemInsert(currentPossibility, ListResources.Insert_Type)));
					break;
				case AreaServices.VariantEntryTypeEditMachineName:
					// NB: Inserts one of two class options:
					//		1) It will insert LexEntryInflType instances in an owning LexEntryInflType instance.
					//		2) It will only insert at the top or nested in other LexEntryType instances.
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.InsertVariantEntryTypeItem), ListResources.Variant_Type, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.InsertVariantEntryTypeSubitem), ListResources.Subtype, AreaServices.PopulateForSubitemInsert(currentPossibility, ListResources.Insert_Type)));
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
					InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripButton> { _insertItemToolStripButton });
					/*
						<item command="CmdDataTree-Insert-POS-SubPossibilities" defaultVisible="false" label="Subcategory..." />
						<command id="CmdDataTree-Insert-POS-SubPossibilities" label="Insert Subcategory..." message="DataTreeInsert" icon="AddSubItem">
						  <parameters field="SubPossibilities" className="PartOfSpeech" slice="owner" />
						</command>
					*/
					_insertSubItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(AreaServices.InsertCategory), "toolStripButtonInsertSubItem", AreaResources.AddSubItem.ToBitmap(), AreaResources.Subcategory);
					_insertSubItemToolStripButton.Tag = new List<object> { currentPossibilityList, MyRecordList };
					InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripButton> { _insertSubItemToolStripButton });
					break;
				case AreaServices.DomainTypeEditMachineName: // Fall through to default.
				case AreaServices.ConfidenceEditMachineName: // Fall through to default.
				case AreaServices.DialectsListEditMachineName: // Fall through to default.
				case AreaServices.EducationEditMachineName: // Fall through to default.
				case AreaServices.ExtNoteTypeEditMachineName: // Fall through to default.
				case AreaServices.GenresEditMachineName: // Fall through to default.
				case AreaServices.LanguagesListEditMachineName: // Fall through to default.
				case AreaServices.RecTypeEditMachineName: // Fall through to default.
				case AreaServices.PositionsEditMachineName: // Fall through to default.
				case AreaServices.PublicationsEditMachineName: // Fall through to default.
				case AreaServices.RestrictionsEditMachineName: // Fall through to default.
				case AreaServices.RoleEditMachineName: // Fall through to default.
				case AreaServices.SenseTypeEditMachineName: // Fall through to default.
				case AreaServices.StatusEditMachineName: // Fall through to default.
				case AreaServices.ChartmarkEditMachineName: // Fall through to default.
				case AreaServices.CharttempEditMachineName: // Fall through to default.
				case AreaServices.TextMarkupTagsEditMachineName: // Fall through to default.
				case AreaServices.TimeOfDayEditMachineName: // Fall through to default.
				case AreaServices.TranslationTypeEditMachineName: // Fall through to default.
				case AreaServices.UsageTypeEditMachineName: // Fall through to default.
				default:
					/*
						NB: use this class: <CmCustomItem num="27" abstract="false" base="CmPossibility" />
						<item command="CmdInsertCustomItem" defaultVisible="false" />

					XOR

						NB: use this class: <CmPossibility num="7" abstract="false" base="CmObject">
						<item command="CmdInsertPossibility" defaultVisible="false" />
					*/
					toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewPossibilityListItem), ListResources.Insert_Item, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Item)));
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
						toolbarButtonCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(_sharedEventHandlers.Get(ListsAreaMenuHelper.AddNewSubPossibilityListItem), ListResources.Subtype, AreaServices.PopulateForSubitemInsert(currentPossibility, ListResources.Insert_Item)));
					}
					break;
			}

			// If there is nothing in "toolbarButtonCreationData", then the tool did everything.
			if (toolbarButtonCreationData.Any())
			{
				// Menu creation baton passed here.
				// The first item in "toolbarButtonCreationData", it is the main "Add" item.
				var currentMenuTuple = toolbarButtonCreationData[0];
				_insertItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(currentMenuTuple.Item1, "toolStripButtonInsertItem", AreaResources.AddItem.ToBitmap(), currentMenuTuple.Item2);
				_insertItemToolStripButton.Tag = new List<object> { currentPossibilityList, MyDataTree, MyRecordList, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, currentMenuTuple.Item3 };

				// SubItem information is optionally present in "menuCreationData" in the second space.
				if (toolbarButtonCreationData.Count == 2)
				{
					// NB: Lists that cannot have subitems won't be adding this toolbar button option at all.
					currentMenuTuple = toolbarButtonCreationData[1];
					_insertSubItemToolStripButton = ToolStripButtonFactory.CreateToolStripButton(currentMenuTuple.Item1, "toolStripButtonInsertSubItem", AreaResources.AddSubItem.ToBitmap(), currentMenuTuple.Item2);
					_insertSubItemToolStripButton.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, currentMenuTuple.Item3 };
					_insertSubItemToolStripButton.Enabled = currentPossibilityList.PossibilitiesOS.Any(); // Visbile, but only enabled, if there are possible owners for the new sub item.
				}

				var itemsToAdd = new List<ToolStripButton> { _insertItemToolStripButton };
				if (_insertSubItemToolStripButton != null)
				{
					itemsToAdd.Add(_insertSubItemToolStripButton);
				}
				InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, itemsToAdd);
			}
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
				InsertToolbarManager.ResetInsertToolbar(_majorFlexComponentParameters);
				_insertItemToolStripButton?.Dispose();
				_insertSubItemToolStripButton?.Dispose();
			}
			MyRecordList = null;
			_majorFlexComponentParameters = null;
			_sharedEventHandlers = null;
			_insertItemToolStripButton = null;
			_insertSubItemToolStripButton = null;

			_isDisposed = true;
		}

		#endregion
	}
}
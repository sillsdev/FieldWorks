// Copyright (c) 2018-2019 SIL International
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
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lists
{
	/// <summary>
	/// Implementation that supports the addition(s) to FLEx's main Insert menu for the Lists Area.
	/// </summary>
	internal sealed class ListsAreaInsertMenuManager : IToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ISharedEventHandlers _sharedEventHandlers;
		private ToolStripMenuItem _insertMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newInsertMenusAndHandlers;
		private ToolStripMenuItem _insertEntryMenu;
		private ToolStripSeparator _toolStripSeparator;
		private DataTree MyDataTree { get; set; }
		private IRecordList MyRecordList { get; set; }
		private IListArea _listArea;
		private IPropertyTable _propertyTable;
		private IPublisher _publisher;

		internal ListsAreaInsertMenuManager(DataTree dataTree, IListArea listArea)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNull(listArea, nameof(listArea));

			MyDataTree = dataTree;
			_listArea = listArea;
			_newInsertMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
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
			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			_publisher = majorFlexComponentParameters.FlexComponentParameters.Publisher;

			_majorFlexComponentParameters.SharedEventHandlers.Add(ListsAreaMenuHelper.AddNewPossibilityListItem, AddNewPossibilityListItem_Clicked);
			_majorFlexComponentParameters.SharedEventHandlers.Add(ListsAreaMenuHelper.AddNewSubPossibilityListItem, AddNewSubPossibilityListItem_Clicked);
			_majorFlexComponentParameters.SharedEventHandlers.Add(ListsAreaMenuHelper.InsertFeatureType, InsertFeatureType_Clicked);

			// Add Lists area Insert menus
			AddInsertMenus();
		}

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
		}

		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~ListsAreaInsertMenuManager()
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
				if (_toolStripSeparator != null)
				{
					_insertMenu.DropDownItems.Remove(_toolStripSeparator);
					_toolStripSeparator.Dispose();
				}
				foreach (var tuple in _newInsertMenusAndHandlers)
				{
					_insertMenu.DropDownItems.Remove(tuple.Item1);
					tuple.Item1.Click -= tuple.Item2;
					tuple.Item1.Dispose();
				}
				_newInsertMenusAndHandlers.Clear();
				_majorFlexComponentParameters.SharedEventHandlers.Remove(ListsAreaMenuHelper.AddNewPossibilityListItem);
				_majorFlexComponentParameters.SharedEventHandlers.Remove(ListsAreaMenuHelper.AddNewSubPossibilityListItem);
				_majorFlexComponentParameters.SharedEventHandlers.Remove(ListsAreaMenuHelper.InsertFeatureType);
			}
			_newInsertMenusAndHandlers = null;
			_insertMenu = null;
			_toolStripSeparator = null;
			_listArea = null;
			MyDataTree = null;
			MyRecordList = null;

			_isDisposed = true;
		}

		#endregion

		private void AddInsertMenus()
		{
			_insertMenu = MenuServices.GetInsertMenu(_majorFlexComponentParameters.MenuStrip);

			/*
			These all go on the "Insert" menu, but they are tool-specific. Start at 0.
			*/
			var activeListTool = _listArea.ActiveTool;
			var currentPossibilityList = MyRecordList.OwningObject as ICmPossibilityList; // Will be null for AreaServices.FeatureTypesAdvancedEditMachineName tool.
			if (currentPossibilityList != null && currentPossibilityList.IsClosed)
			{
				// No sense in bothering with menus, since the list is closed to adding new items.
				return;
			}
			var currentPossibility = MyRecordList.CurrentObject as ICmPossibility; // May be a subclass of ICmPossibility. Will be null, if nothing is in the list.
			var menuCreationData = new List<Tuple<EventHandler, string, Dictionary<string, string>>>(3);
			ToolStripMenuItem menu;
			string tooltip;
			var insertIndex = 0;
			switch (activeListTool.MachineName)
			{
				case AreaServices.AnthroEditMachineName:
					/*
						<command id="CmdInsertAnthroCategory" label="Anthropology _Category" message="InsertItemInVector" icon="AddItem">
						  <params className="CmAnthroItem" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewPossibilityListItem_Clicked, ListResources.Anthropology_Category, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Anthropology_Category)));
					/*
						<command id="CmdDataTree-Insert-AnthroCategory" label="Insert subcategory" message="DataTreeInsert" icon="AddSubItem">
						  <parameters field="SubPossibilities" className="CmAnthroItem" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewSubPossibilityListItem_Clicked, ListResources.Subcategory, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Anthropology_Category)));
					break;
				case AreaServices.FeatureTypesAdvancedEditMachineName:
					/*
						<command id="CmdInsertFeatureType" label="_Feature Type" message="InsertItemInVector" icon="AddItem">
						  <params className="FsFeatStrucType" />
						</command>
					*/
					ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, InsertFeatureType_Clicked, ListResources.Feature_Type, image: AreaResources.AddItem.ToBitmap(), insertIndex: insertIndex++);
					break;
				case AreaServices.LexRefEditMachineName:
					/*
					    <command id="CmdInsertLexRefType" label="_Lexical Reference Type" message="InsertItemInVector" icon="AddItem">
					      <params className="LexRefType" />
					    </command>
						// No need for something like: CmdDataTree-Insert-LexRefType, since they are not nested.
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewPossibilityListItem_Clicked, ListResources.Lexical_Relation, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Lexical_Reference_Type)));
					break;
				case AreaServices.LocationsEditMachineName:
					/*
						<command id="CmdInsertLocation" label="_Location" message="InsertItemInVector" icon="AddItem">
						  <params className="CmLocation" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewPossibilityListItem_Clicked, ListResources.Location, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Location)));
					/*
						<command id="CmdDataTree-Insert-Location" label="Insert subitem" message="DataTreeInsert" icon="AddSubItem">
						  <parameters field="SubPossibilities" className="CmLocation" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewSubPossibilityListItem_Clicked, ListResources.Subitem, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Location)));
					break;
				case AreaServices.MorphTypeEditMachineName:
					// Can't get here, since code above would have returned.
					// List is closed at least as of 6AUG2018, when I (RBR) tried to add the menus.
					break;
				case AreaServices.PeopleEditMachineName:
					/*
						<command id="CmdInsertPerson" label="_Person" message="InsertItemInVector" icon="AddItem">
						  <params className="CmPerson" />
						</command>
						// No need for something like: CmdDataTree-Insert-Person, since there are no nested people.
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewPossibilityListItem_Clicked, ListResources.Person, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Person)));
					break;
				case AreaServices.SemanticDomainEditMachineName:
					/*
						<command id="CmdInsertSemDom" label="_Semantic Domain" message="InsertItemInVector" icon="AddItem">
						  <params className="CmSemanticDomain" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewPossibilityListItem_Clicked, ListResources.Semantic_Domain, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Semantic_Domain)));
					/*
						 <command id="CmdDataTree-Insert-SemanticDomain" label="Insert subdomain" message="DataTreeInsert" icon="AddSubItem">
						   <parameters field="SubPossibilities" className="CmSemanticDomain" />
						 </command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewSubPossibilityListItem_Clicked, ListResources.Subdomain, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Semantic_Domain)));
					break;
				case AreaServices.ComplexEntryTypeEditMachineName:
					// This list can only have LexEntryType instances.
					/*
						<command id="CmdInsertLexEntryType" label="_Type" message="InsertItemInVector" icon="AddItem">
						  <params className="LexEntryType" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewPossibilityListItem_Clicked, ListResources.Complex_Form_Type, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					/*
						<command id="CmdDataTree-Insert-LexEntryType" label="Insert Subtype" message="DataTreeInsert" icon="AddSubItem">
						  <parameters field="SubPossibilities" className="LexEntryType" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewSubPossibilityListItem_Clicked, ListResources.Subtype, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					break;
				case AreaServices.VariantEntryTypeEditMachineName:
					// NB: Inserts one of two class options:
					//		1) It will insert LexEntryInflType instances in an owning LexEntryInflType instance.
					//		2) It will only insert at the top or nested in other LexEntryType instances.
					/*
						<command id="CmdInsertLexEntryType" label="_Type" message="InsertItemInVector" icon="AddItem">
						  <params className="LexEntryType" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewPossibilityListItem_Clicked, ListResources.Variant_Type, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					/*
						<command id="CmdDataTree-Insert-LexEntryType" label="Insert Subtype" message="DataTreeInsert" icon="AddSubItem">
						  <parameters field="SubPossibilities" className="LexEntryType" />
						</command>
						<command id="CmdDataTree-Insert-LexEntryInflType" label="Insert Subtype" message="DataTreeInsert" icon="AddSubItem">
						  <parameters field="SubPossibilities" className="LexEntryInflType" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewSubPossibilityListItem_Clicked, ListResources.Subtype, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Type)));
					break;
				case AreaServices.ReversalToolReversalIndexPOSMachineName:
					// The FW 8 behavior has this one below the custom list creation menu, but I'm (RBR) going to regularize it and put it above.
					/*
					    <command id="CmdInsertPOS" label="Category" message="InsertItemInVector" shortcut="Ctrl+I" icon="AddItem">
					      <params className="PartOfSpeech" />
					    </command>
					*/
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, _sharedEventHandlers.Get(AreaServices.InsertCategory), AreaResources.Category, image: AreaResources.AddItem.ToBitmap(), shortcutKeys: Keys.Control | Keys.I, insertIndex: insertIndex++);
					menu.Tag = new List<object> { currentPossibilityList, MyRecordList };
					/*
					    <command id="CmdDataTree-Insert-POS-SubPossibilities" label="Insert Subcategory..." message="DataTreeInsert" icon="AddSubItem">
					      <parameters field="SubPossibilities" className="PartOfSpeech" slice="owner" />
					    </command>
					*/
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, _sharedEventHandlers.Get(AreaServices.InsertCategory), AreaResources.Subcategory, image: AreaResources.AddSubItem.ToBitmap(), insertIndex: insertIndex++);
					menu.Tag = new List<object> { currentPossibilityList, MyRecordList };
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
						<command id="CmdInsertCustomItem" label="_Item" message="InsertItemInVector" icon="AddItem">
						  <params className="CmCustomItem" />
						</command>

					XOR

						NB: use this class: <CmPossibility num="7" abstract="false" base="CmObject">
						<command id="CmdInsertPossibility" label="_Item" message="InsertItemInVector" icon="AddItem">
						  <params className="CmPossibility" restrictFromClerkID="ProdRestrict DiscChartTemplateList" />
						</command>
					*/
					menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewPossibilityListItem_Clicked, ListResources.Insert_Item, AreaServices.PopulateForMainItemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Item)));

					// The list determines if subitems are allowed.
					// If the list has this property value, then sub-items cannot be added: <Depth val="1" />
					if (currentPossibilityList.Depth > 1)
					{
						/*
							NB: Use this class: <CmCustomItem num="27" abstract="false" base="CmPossibility" />
							<command id="CmdDataTree-Insert-CustomItem" label="Insert subitem" message="DataTreeInsert" icon="AddSubItem">
							  <parameters field="SubPossibilities" className="CmCustomItem" />
							</command>

						XOR

						NB: Use this class: <CmPossibility num="7" abstract="false" base="CmObject">
							<command id="CmdDataTree-Insert-Possibility" label="Insert subitem" message="DataTreeInsert" icon="AddSubItem">
							  <parameters field="SubPossibilities" className="CmPossibility" />
							</command>
						*/
						menuCreationData.Add(new Tuple<EventHandler, string, Dictionary<string, string>>(AddNewSubPossibilityListItem_Clicked, ListResources.Insert_Subitem, AreaServices.PopulateForSubitemInsert(currentPossibilityList, currentPossibility, ListResources.Insert_Subitem)));
					}
					break;
			}
			if (activeListTool.MachineName != AreaServices.FeatureTypesAdvancedEditMachineName)
			{
				// Add "Entry" menu to all other tools.
				_insertEntryMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, _sharedEventHandlers.Get(AreaServices.CmdAddToLexicon), AreaResources.EntryWithDots, image: AreaResources.Major_Entry.ToBitmap(), insertIndex: insertIndex++);
				_insertEntryMenu.Tag = MyDataTree;
				_insertEntryMenu.Visible = false;
			}
			// If there is nothing in "menuCreationData", then the tool did everything.
			if (menuCreationData.Any())
			{
				// Menu creation baton passed here.
				// The first item in "menuCreationData", it is the main "Add" item.
				var currentMenuTuple = menuCreationData[0];
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, currentMenuTuple.Item1, currentMenuTuple.Item2, image: AreaResources.AddItem.ToBitmap(), insertIndex: insertIndex++);
				menu.Name = AreaServices.MainItem;
				menu.Tag = new List<object> { currentPossibilityList, MyDataTree, MyRecordList, _propertyTable, currentMenuTuple.Item3 };

				// SubItem information is optionally present in "menuCreationData" in the second space.
				if (menuCreationData.Count == 2)
				{
					// NB: Lists that cannot have subitems won't be adding this menu option at all.
					currentMenuTuple = menuCreationData[1];
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, currentMenuTuple.Item1, currentMenuTuple.Item2, image: AreaResources.AddSubItem.ToBitmap(), insertIndex: insertIndex++);
					menu.Name = AreaServices.SubItem;
					menu.Tag = new List<object> { currentPossibility, MyDataTree, MyRecordList, _propertyTable, currentMenuTuple.Item3 };
					menu.Enabled = currentPossibilityList.PossibilitiesOS.Any(); // Visbile, but only enabled, if there are possible owners for the new sub item.
				}
			}
			if (insertIndex > 0)
			{
				// <item label="-" translate="do not translate" />
				_toolStripSeparator = ToolStripMenuItemFactory.CreateToolStripSeparatorForToolStripMenuItem(_insertMenu, insertIndex++);
			}
			// <item command="CmdAddCustomList" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, AddCustomList_Click, ListResources.AddCustomList, ListResources.AddCustomListTooltip, insertIndex: insertIndex);

			Application.Idle += ApplicationOnIdle;
			MyDataTree.CurrentSliceChanged += MyDataTreeOnCurrentSliceChanged;
		}

		private void InsertFeatureType_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new MasterInflectionFeatureListDlg("FsFeatDefn"))
			{
				dlg.SetDlginfo(MyRecordList.CurrentObject.Cache.LanguageProject.MsFeatureSystemOA, _propertyTable, true);
				dlg.ShowDialog(_propertyTable.GetValue<Form>(FwUtils.window));
			}
		}

		private static void AddNewPossibilityListItem_Clicked(object sender, EventArgs e)
		{
			var tag = (List<object>)((ToolStripItem)sender).Tag;
			var possibilityList = (ICmPossibilityList)tag[0];
			var otherOptions = (Dictionary<string, string>)tag[4];
			var cache = possibilityList.Cache;
			ICmPossibility newPossibility = null;
			UowHelpers.UndoExtension(otherOptions[AreaServices.BaseUowMessage], cache.ActionHandlerAccessor, () =>
			{
				switch (otherOptions[AreaServices.ClassName])
				{
					case CmPossibilityTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), possibilityList);
						break;
					case CmLocationTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmLocationFactory>().Create(Guid.NewGuid(), possibilityList);
						break;
					case CmPersonTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create(Guid.NewGuid(), possibilityList);
						break;
					case CmAnthroItemTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>().Create(Guid.NewGuid(), possibilityList);
						break;
					case CmCustomItemTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmCustomItemFactory>().Create(Guid.NewGuid(), possibilityList);
						break;
					case CmAnnotationDefnTags.kClassName:
						throw new NotSupportedException("Cannot create CmAnnotationDefn instances in Flex.");
					case CmSemanticDomainTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>().Create(Guid.NewGuid(), possibilityList);
						break;
					case MoMorphTypeTags.kClassName:
						throw new NotSupportedException("Cannot create MoMorphType instances in Flex, since the list is closed.");
					case PartOfSpeechTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create(Guid.NewGuid(), possibilityList);
						break;
					case LexEntryTypeTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>().Create(possibilityList);
						break;
					case LexEntryInflTypeTags.kClassName:
						throw new NotSupportedException("Cannot create LexEntryInflType instances in Flex at the list level.");
					case LexRefTypeTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create(possibilityList);
						break;
					case ChkTermTags.kClassName:
						throw new NotSupportedException("Cannot create ChkTerm instances in Flex.");
					case PhPhonRuleFeatTags.kClassName:
						throw new NotSupportedException("Cannot create PhPhonRuleFeat instances in Flex.");
				}
			});
			if (newPossibility != null)
			{
				((IRecordList)tag[2]).UpdateRecordTreeBar();
			}
		}

		private void AddNewSubPossibilityListItem_Clicked(object sender, EventArgs e)
		{
			var tag = (List<object>)((ToolStripItem)sender).Tag;
			var owningPossibility = (ICmPossibility)tag[0];
			var otherOptions = (Dictionary<string, string>)tag[4];
			var cache = owningPossibility.Cache;
			ICmPossibility newPossibility = null;
			UowHelpers.UndoExtension(otherOptions[AreaServices.BaseUowMessage], cache.ActionHandlerAccessor, () =>
			{
				switch (otherOptions[AreaServices.ClassName])
				{
					case CmPossibilityTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(Guid.NewGuid(), owningPossibility);
						break;
					case CmLocationTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmLocationFactory>().Create(Guid.NewGuid(), (ICmLocation)owningPossibility);
						break;
					case CmPersonTags.kClassName:
						throw new NotSupportedException("Cannot create CmPerson sub-item instances in Flex.");
					case CmAnthroItemTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>().Create(Guid.NewGuid(), (ICmAnthroItem)owningPossibility);
						break;
					case CmCustomItemTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmCustomItemFactory>().Create(Guid.NewGuid(), (ICmCustomItem)owningPossibility);
						break;
					case CmAnnotationDefnTags.kClassName:
						throw new NotSupportedException("Cannot create CmAnnotationDefn instances in Flex.");
					case CmSemanticDomainTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>().Create(Guid.NewGuid(), (ICmSemanticDomain)owningPossibility);
						break;
					case MoMorphTypeTags.kClassName:
						throw new NotSupportedException("Cannot create MoMorphType instances in Flex, since the list is closed.");
					case PartOfSpeechTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create(Guid.NewGuid(), (IPartOfSpeech)owningPossibility);
						break;
					case LexEntryTypeTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>().Create((ILexEntryType)owningPossibility);
						break;
					case LexEntryInflTypeTags.kClassName:
						newPossibility = cache.ServiceLocator.GetInstance<ILexEntryInflTypeFactory>().Create((ILexEntryInflType)owningPossibility);
						break;
					case LexRefTypeTags.kClassName:
						throw new NotSupportedException("Cannot create LexRefType sub-item instances in Flex.");
					case ChkTermTags.kClassName:
						throw new NotSupportedException("Cannot create ChkTerm instances in Flex.");
					case PhPhonRuleFeatTags.kClassName:
						throw new NotSupportedException("Cannot create PhPhonRuleFeat instances in Flex.");
				}
			});
			if (newPossibility != null)
			{
				((IRecordList)tag[2]).UpdateRecordTreeBar();
			}
		}

		private void AddCustomList_Click(object sender, EventArgs e)
		{
			using (var dlg = new AddCustomListDlg(_propertyTable, _publisher, _majorFlexComponentParameters.LcmCache))
			{
				if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK)
				{
					_listArea.AddCustomList(dlg.NewList);
				}
			}
		}

		private void ApplicationOnIdle(object sender, EventArgs e)
		{
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"Start: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
			var currentList = ListsAreaMenuHelper.GetPossibilityList(MyRecordList);
			var currentTuple = _newInsertMenusAndHandlers.FirstOrDefault(tuple => tuple.Item1.Name == AreaServices.SubItem);
			if (currentTuple != null)
			{
				currentTuple.Item1.Enabled = currentList.PossibilitiesOS.Any();
			}
			var currentSliceAsStTextSlice = AreaWideMenuHelper.DataTreeCurrentSliceAsStTextSlice(MyDataTree);
			if (_insertEntryMenu != null && _insertEntryMenu.Visible && currentSliceAsStTextSlice != null)
			{
				AreaWideMenuHelper.Set_CmdAddToLexicon_State(_majorFlexComponentParameters.LcmCache, _insertEntryMenu, currentSliceAsStTextSlice.RootSite.RootBox.Selection);
			}
#if RANDYTODO_TEST_Application_Idle
// TODO: Remove when finished sorting out idle issues.
Debug.WriteLine($"End: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
#endif
		}

		private void MyDataTreeOnCurrentSliceChanged(object sender, CurrentSliceChangedEventArgs e)
		{
			if (_insertEntryMenu == null)
			{
				return;
			}
			_insertEntryMenu.Visible = e.CurrentSlice is StTextSlice;
		}
	}
}
// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lists;
using LanguageExplorer.Areas.Lists.Tools;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Grammar.Tools.PosEdit
{
	/// <summary>
	/// ITool implementation for the "posEdit" tool in the "grammar" area.
	/// </summary>
	[Export(AreaServices.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class PosEditTool : ITool
	{
		private const string Categories_withTreeBarHandler = "categories_withTreeBarHandler";
		private PosEditToolMenuHelper _toolMenuHelper;
		/// <summary>
		/// Main control to the right of the side bar control. This holds a RecordBar on the left and a PaneBarContainer on the right.
		/// The RecordBar has no top PaneBar for information, menus, etc.
		/// </summary>
		private CollapsingSplitContainer _collapsingSplitContainer;
		private IRecordList _recordList;
		[Import(AreaServices.GrammarAreaMachineName)]
		private IArea _area;

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
			CollapsingSplitContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _collapsingSplitContainer);

			_toolMenuHelper.Dispose();
			_toolMenuHelper = null;
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
			majorFlexComponentParameters.FlexComponentParameters.PropertyTable.SetDefault("PartsOfSpeech.posEdit.DataTree-Splitter", 200, true);
			majorFlexComponentParameters.FlexComponentParameters.PropertyTable.SetDefault("PartsOfSpeech.posAdvancedEdit.DataTree-Splitter", 200, true);

			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(Categories_withTreeBarHandler, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers, majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(UiWidgetServices.CreateShowHiddenFieldsPropertyName(MachineName), false));
			_toolMenuHelper = new PosEditToolMenuHelper(majorFlexComponentParameters, this, _recordList, dataTree);
			_collapsingSplitContainer = CollapsingSplitContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, true,
				XDocument.Parse(ListResources.PosEditParameters).Root, XDocument.Parse(AreaResources.HideAdvancedListItemFields), MachineName,
				majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
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
		public string MachineName => AreaServices.PosEditMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.PosEditUiName);

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.EditView.SetBackgroundColor(Color.Magenta);

		#endregion

		private static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == Categories_withTreeBarHandler, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{Categories_withTreeBarHandler}'.");
			/*
            <clerk id="categories">
              <recordList owner="LangProject" property="PartsOfSpeech">
                <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.PossibilityRecordList" />
              </recordList>
              <treeBarHandler assemblyPath="xWorks.dll" expand="true" hierarchical="true" ws="best analorvern" class="SIL.FieldWorks.XWorks.PossibilityTreeBarHandler" />
              <filters />
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
            </clerk>
			*/
			return new TreeBarHandlerAwarePossibilityRecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(),
				cache.LanguageProject.PartsOfSpeechOA, new PossibilityTreeBarHandler(flexComponentParameters.PropertyTable, true, true, false, "best analorvern"));
		}

		private sealed class PosEditToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private SharedListToolsUiWidgetMenuHelper _sharedListToolsUiWidgetMenuHelper;
			private IRecordList _recordList;
			private DataTree _dataTree;
			private ISharedEventHandlers _sharedEventHandlers;

			internal PosEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, IRecordList recordList, DataTree dataTree)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(dataTree, nameof(dataTree));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordList = recordList;
				_dataTree = dataTree;

				SetupUiWidgets(tool);
			}

			private void SetupUiWidgets(ITool tool)
			{
				_sharedListToolsUiWidgetMenuHelper = new SharedListToolsUiWidgetMenuHelper(_majorFlexComponentParameters, tool, _majorFlexComponentParameters.LcmCache.LanguageProject.PartsOfSpeechOA, _recordList, _dataTree);
				_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				// Insert menu & tool bar for CmdInsertPossibility and CmdDataTree_Insert_Possibility.
				_sharedListToolsUiWidgetMenuHelper.MyPartiallySharedForToolsWideMenuHelper.SetupCmdInsertPossibility(toolUiWidgetParameterObject, ()=> CanCmdInsertPOS);
				_sharedListToolsUiWidgetMenuHelper.MyPartiallySharedForToolsWideMenuHelper.SetupCmdDataTree_Insert_Possibility(toolUiWidgetParameterObject, () => CanCmdDataTree_Insert_POS_SubPossibilities);
				// Insert menu commands: CmdDataTree_Insert_POS_AffixTemplate
				var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
				insertMenuDictionary.Add(Command.CmdDataTree_Insert_POS_AffixTemplate, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdDataTree_Insert_POS_AffixTemplate_Click, ()=> CanCmdDataTree_Insert_POS_AffixTemplate));

				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);

				RegisterSliceLeftEdgeMenus();
			}

			private void RegisterSliceLeftEdgeMenus()
			{
				// <menu id="mnuDataTree_POS_AffixTemplates">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_AffixTemplates, Create_mnuDataTree_POS_AffixTemplates);
				// <menu id="mnuDataTree_POS_AffixTemplate">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_AffixTemplate, Create_mnuDataTree_POS_AffixTemplate);
				// < menu id = "mnuDataTree_POS_AffixSlots" >
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_AffixSlots, Create_mnuDataTree_POS_AffixSlots);
				// <menu id="mnuDataTree_POS_AffixSlot">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_AffixSlot, Create_mnuDataTree_POS_AffixSlot);
				// <menu id="mnuDataTree_POS_InflectionClasses">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_InflectionClasses, Create_mnuDataTree_POS_InflectionClasses);
				// <menu id="mnuDataTree_POS_InflectionClass">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_InflectionClass, Create_mnuDataTree_POS_InflectionClass);
				// <menu id="mnuDataTree_POS_InflectionClass_Subclasses">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_InflectionClass_Subclasses, Create_mnuDataTree_POS_InflectionClass_Subclasses);
				// <menu id="mnuDataTree_POS_StemNames">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_StemNames, Create_mnuDataTree_POS_StemNames);
				// <menu id="mnuDataTree_POS_StemName">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_POS_StemName, Create_mnuDataTree_POS_StemName);
				// <menu id="mnuDataTree_MoStemName_Regions">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_MoStemName_Regions, Create_mnuDataTree_MoStemName_Regions);
				// <menu id="mnuDataTree_MoStemName_Region">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_MoStemName_Region, Create_mnuDataTree_MoStemName_Region);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_StemName(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_StemName, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_StemName.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_POS_StemName">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_POS_StemName.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Delete_POS_StemName" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, GrammarResources.Delete_Stem_Name, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_POS_StemName">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_MoStemName_Region(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_MoStemName_Region, $"Expected argument value of '{ContextMenuName.mnuDataTree_MoStemName_Region.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_MoStemName_Region">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_MoStemName_Region.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Delete_MoStemName_Region" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, GrammarResources.Delete_Feature_Set, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_MoStemName_Region">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_MoStemName_Regions(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_MoStemName_Regions, $"Expected argument value of '{ContextMenuName.mnuDataTree_MoStemName_Regions.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_MoStemName_Regions">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_MoStemName_Regions.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_MoStemName_Region" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_MoStemName_Region_Clicked, GrammarResources.Insert_Feature_Set);

				// End: <menu id="mnuDataTree_MoStemName_Regions">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Insert_MoStemName_Region_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("Regions", FsFeatStrucTags.kClassName, FsFeatStrucTags.kClassName);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_StemNames(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_StemNames, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_StemNames.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_POS_StemNames">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_POS_StemNames.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_POS_StemName" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_POS_StemName_Clicked, GrammarResources.Insert_Stem_Name);

				// End: <menu id="mnuDataTree_POS_StemNames">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Insert_POS_StemName_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("StemNames", MoStemNameTags.kClassName, PartOfSpeechTags.kClassName);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_InflectionClass_Subclasses(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_InflectionClass_Subclasses, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_InflectionClass_Subclasses.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_POS_InflectionClass_Subclasses">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_POS_InflectionClass_Subclasses.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="mnuDataTree_POS_InflectionClass_Subclasses" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, POS_InflectionClass_Subclasses_Clicked, GrammarResources.Insert_Inflection_Class);

				// End: <menu id="mnuDataTree_POS_InflectionClasses">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void POS_InflectionClass_Subclasses_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("Subclasses", MoInflClassTags.kClassName, MoInflClassTags.kClassName);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_InflectionClass(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_InflectionClass, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_InflectionClass.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_POS_InflectionClass">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_POS_InflectionClass.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Delete_POS_InflectionClass" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, GrammarResources.Delete_Inflection_Class, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_POS_InflectionClass">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_InflectionClasses(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_InflectionClasses, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_InflectionClasses.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_POS_InflectionClasses">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_POS_InflectionClasses.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_POS_InflectionClass" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_POS_InflectionClass_Clicked, GrammarResources.Insert_Inflection_Class);

				// End: <menu id="mnuDataTree_POS_InflectionClasses">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Insert_POS_InflectionClass_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("InflectionClasses", MoInflClassTags.kClassName, PartOfSpeechTags.kClassName);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_AffixSlot(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_AffixSlot, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_AffixSlot.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_POS_AffixSlot">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_POS_AffixSlot.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Delete_POS_AffixSlot" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, GrammarResources.Delete_Affix_Slot, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_POS_AffixSlot">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_AffixSlots(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_AffixSlots, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_AffixSlots.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_POS_AffixSlots">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_POS_AffixSlots.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_POS_AffixSlot" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_POS_AffixSlot_Clicked, GrammarResources.Insert_Affix_Slot);

				// End: <menu id="mnuDataTree_POS_AffixSlots">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Insert_POS_AffixSlot_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("AffixSlots", MoInflAffixSlotTags.kClassName, PartOfSpeechTags.kClassName);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_AffixTemplates(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_AffixTemplates, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_AffixTemplates.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_POS_AffixTemplates">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_POS_AffixTemplates.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_POS_AffixTemplate" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_POS_AffixTemplate_Clicked, GrammarResources.Insert_Affix_Template);

				// End: <menu id="mnuDataTree_POS_AffixTemplates">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Insert_POS_AffixTemplate_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("AffixTemplates", MoInflAffixTemplateTags.kClassName, PartOfSpeechTags.kClassName);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_POS_AffixTemplate(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_POS_AffixTemplate, $"Expected argument value of '{ContextMenuName.mnuDataTree_POS_AffixTemplate.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_POS_AffixTemplate">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_POS_AffixTemplate.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Delete_POS_AffixTemplate" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, GrammarResources.Delete_Affix_Template, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));
				// <item command="CmdDataTree_Copy_POS_AffixTemplate" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Copy_POS_AffixTemplate_Clicked, GrammarResources.Duplicate_Affix_Template);

				// End: <menu id="mnuDataTree_POS_AffixTemplate">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private static Tuple<bool, bool> CanCmdInsertPOS => new Tuple<bool, bool>(true, true);

			private Tuple<bool, bool> CanCmdDataTree_Insert_POS_SubPossibilities => new Tuple<bool, bool>(true, _recordList.CurrentObject != null);

			private static Tuple<bool, bool> CanCmdDataTree_Insert_POS_AffixTemplate => new Tuple<bool, bool>(true, true);

			private void CmdDataTree_Insert_POS_AffixTemplate_Click(object sender, EventArgs e)
			{
				// Owner: POS:AffixTemplates
				// Class: MoInflAffixTemplate
				UowHelpers.UndoExtension(GrammarResources.Affix_Template, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					((IPartOfSpeech)_recordList.CurrentObject).AffixTemplatesOS.Add(_majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IMoInflAffixTemplateFactory>().Create());
				});
			}

			private void Copy_POS_AffixTemplate_Clicked(object sender, EventArgs e)
			{
				UowHelpers.UndoExtension(GrammarResources.Duplicate_Affix_Template, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					var currentAffixTemplate = (IMoInflAffixTemplate)_dataTree.CurrentSlice.MyCmObject;
					var newAffixTemplate = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IMoInflAffixTemplateFactory>().Create();
					((IPartOfSpeech)_recordList.CurrentObject).AffixTemplatesOS.Add(newAffixTemplate);
					currentAffixTemplate.SetCloneProperties(newAffixTemplate);
				});
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~PosEditToolMenuHelper()
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
					_sharedListToolsUiWidgetMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_sharedListToolsUiWidgetMenuHelper = null;
				_recordList = null;
				_dataTree = null;
				_sharedEventHandlers = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
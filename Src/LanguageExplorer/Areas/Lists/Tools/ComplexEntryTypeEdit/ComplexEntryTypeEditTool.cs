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
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lists.Tools.ComplexEntryTypeEdit
{
	/// <summary>
	/// ITool implementation for the "complexEntryTypeEdit" tool in the "lists" area.
	/// </summary>
	[Export(AreaServices.ListsAreaMachineName, typeof(ITool))]
	internal sealed class ComplexEntryTypeEditTool : ITool
	{
		private const string ComplexEntryTypeList = "ComplexEntryTypeList";
		/// <summary>
		/// Main control to the right of the side bar control. This holds a RecordBar on the left and a PaneBarContainer on the right.
		/// The RecordBar has no top PaneBar for information, menus, etc.
		/// </summary>
		private CollapsingSplitContainer _collapsingSplitContainer;
		private ComplexEntryTypeEditMenuHelper _toolMenuHelper;
		private IRecordList _recordList;
		[Import(AreaServices.ListsAreaMachineName)]
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

			// Dispose after the main UI stuff.
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
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(ComplexEntryTypeList, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}

			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers);
			_toolMenuHelper = new ComplexEntryTypeEditMenuHelper(majorFlexComponentParameters, this, majorFlexComponentParameters.LcmCache.LanguageProject.LexDbOA.ComplexEntryTypesOA, _recordList, dataTree);
			_collapsingSplitContainer = CollapsingSplitContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer,
				true, XDocument.Parse(ListResources.ComplexEntryTypeEditParameters).Root, XDocument.Parse(ListResources.ListToolsSliceFilters), MachineName,
				majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);

			// Too early before now.
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), false, SettingsGroup.LocalSettings))
			{
				majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish(LanguageExplorerConstants.ShowHiddenFields, true);
			}
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
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
		public string MachineName => AreaServices.ComplexEntryTypeEditMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Complex Form Types";
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

		private static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == ComplexEntryTypeList, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{ComplexEntryTypeList}'.");
			/*
            <clerk id="ComplexEntryTypeList">
              <recordList owner="LexDb" property="ComplexEntryTypes">
                <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.PossibilityRecordList" />
              </recordList>
              <treeBarHandler assemblyPath="xWorks.dll" expand="false" hierarchical="true" includeAbbr="false" ws="best analysis" class="SIL.FieldWorks.XWorks.PossibilityTreeBarHandler" />
              <filters />
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
            </clerk>
			*/
			return new TreeBarHandlerAwarePossibilityRecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(),
				cache.LanguageProject.LexDbOA.ComplexEntryTypesOA, new PossibilityTreeBarHandler(flexComponentParameters.PropertyTable, false, true, false, "best analysis"));
		}

		private sealed class ComplexEntryTypeEditMenuHelper : IDisposable
		{
			private readonly MajorFlexComponentParameters _majorFlexComponentParameters;
			private readonly ICmPossibilityList _list;
			private readonly IRecordList _recordList;

			internal ComplexEntryTypeEditMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, ICmPossibilityList list, IRecordList recordList, DataTree dataTree)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(list, nameof(list));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(dataTree, nameof(dataTree));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_list = list;
				_recordList = recordList;
				SetupToolUiWidgets(tool, dataTree);
			}

			private void SetupToolUiWidgets(ITool tool, DataTree dataTree)
			{
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				// <command id="CmdInsertLexEntryType" label="_Type" message="InsertItemInVector" icon="AddItem">
				// <command id="CmdDataTree_Insert_LexEntryType" label="Insert Subtype" message="DataTreeInsert" icon="AddSubItem">
				// Insert menu & tool bar
				var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
				var insertToolbarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];
				insertMenuDictionary.Add(Command.CmdInsertLexEntryType, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertLexEntryType_Click, () => CanCmdInsertLexEntryType));
				insertToolbarDictionary.Add(Command.CmdInsertLexEntryType, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertLexEntryType_Click, () => CanCmdInsertLexEntryType));
				insertMenuDictionary.Add(Command.CmdDataTree_Insert_LexEntryType, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdDataTree_Insert_LexEntryType_Click, () => CanCmdDataTree_Insert_LexEntryType));
				insertToolbarDictionary.Add(Command.CmdDataTree_Insert_LexEntryType, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdDataTree_Insert_LexEntryType_Click, () => CanCmdDataTree_Insert_LexEntryType));

				dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_SubComplexEntryType, Create_mnuDataTree_SubComplexEntryType);

				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_SubComplexEntryType(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_SubComplexEntryType, $"Expected argument value of '{ContextMenuName.mnuDataTree_SubComplexEntryType}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_SubComplexEntryType">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_SubComplexEntryType.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				/*
					<item command="CmdDataTree_Insert_LexEntryType" />
				*/
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Insert_LexEntryType_Click, ListResources.Insert_Subtype, image: AreaResources.AddSubItem.ToBitmap());

				// End: <menu id="mnuDataTree_SubComplexEntryType">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private static Tuple<bool, bool> CanCmdInsertLexEntryType => new Tuple<bool, bool>(true, true);

			private void CmdInsertLexEntryType_Click(object sender, EventArgs e)
			{
				ICmPossibility newPossibility = null;
				UowHelpers.UndoExtension(ListResources.Insert_Type, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					newPossibility = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ILexEntryTypeFactory>().Create(_list);
				});
				if (newPossibility != null)
				{
					_recordList.UpdateRecordTreeBar();
				}
			}

			private Tuple<bool, bool> CanCmdDataTree_Insert_LexEntryType => new Tuple<bool, bool>(true, _recordList.CurrentObject != null);

			private void CmdDataTree_Insert_LexEntryType_Click(object sender, EventArgs e)
			{
				ICmPossibility newSubPossibility = null;
				UowHelpers.UndoExtension(ListResources.Insert_Type, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					newSubPossibility = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ILexEntryTypeFactory>().Create((ILexEntryType)_recordList.CurrentObject);
				});
				if (newSubPossibility != null)
				{
					_recordList.UpdateRecordTreeBar();
				}
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~ComplexEntryTypeEditMenuHelper()
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
				}

				_isDisposed = true;
			}
			#endregion
		}
	}
}
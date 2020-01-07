// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lists.Tools;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Grammar.Tools.FeaturesAdvancedEdit
{
	/// <summary>
	/// ITool implementation for the "featuresAdvancedEdit" tool in the "grammar" area.
	/// </summary>
	[Export(AreaServices.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class FeaturesAdvancedEditTool : ITool
	{
		private FeaturesAdvancedEditToolMenuHelper _toolMenuHelper;
		private const string Features = "features";
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
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
			_toolMenuHelper.Dispose();
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);
			_recordBrowseView = null;
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
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(Features, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}
			var root = XDocument.Parse(GrammarResources.FeaturesAdvancedEditToolParameters).Root;
			_recordBrowseView = new RecordBrowseView(root.Element("browseview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			var showHiddenFieldsPropertyName = UiWidgetServices.CreateShowHiddenFieldsPropertyName(MachineName);
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers, majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false));
			var recordEditView = new RecordEditView(root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "FeatureItemsAndDetailMultiPane",
				ToolMachineName = MachineName
			};
			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });

			// Too early before now.
			_toolMenuHelper = new FeaturesAdvancedEditToolMenuHelper(majorFlexComponentParameters, this, _recordBrowseView, _recordList, dataTree);
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters, _recordBrowseView, "Browse", new PaneBar(), recordEditView, "Details", recordEditViewPaneBar);
			recordEditView.FinishInitialization();
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
		public string MachineName => AreaServices.FeaturesAdvancedEditMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.FeaturesAdvancedEditUiName);

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
			Require.That(recordListId == Features, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{Features}'.");
			/*
            <clerk id="features">
              <recordList owner="MsFeatureSystem" property="Features" />
            </clerk>
			*/
			return new RecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true,
				new VectorPropertyParameterObject(cache.LanguageProject.MsFeatureSystemOA, "Features", FsFeatureSystemTags.kflidFeatures));
		}

		private sealed class FeaturesAdvancedEditToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private SharedListToolsUiWidgetMenuHelper _sharedListToolsUiWidgetMenuHelper;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;
			private DataTree _dataTree;
			private ToolStripMenuItem _menu;
			private ISharedEventHandlers _sharedEventHandlers;

			internal FeaturesAdvancedEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, RecordBrowseView recordBrowseView, IRecordList recordList, DataTree dataTree)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(dataTree, nameof(dataTree));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;
				_dataTree = dataTree;

				SetupUiWidgets(tool);
				CreateBrowseViewContextMenu();
			}

			private void SetupUiWidgets(ITool tool)
			{
				_sharedListToolsUiWidgetMenuHelper = new SharedListToolsUiWidgetMenuHelper(_majorFlexComponentParameters, tool, _majorFlexComponentParameters.LcmCache.LanguageProject.PartsOfSpeechOA, _recordList, _dataTree);
				_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
				var insertToolBarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];
				// There are two always visible menus/buttons, and one menu that shows for two of the three classes that can be in the owning property.
				UiWidgetServices.InsertPair(insertToolBarDictionary, insertMenuDictionary,
					Command.CmdInsertClosedFeature, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertClosedFeature_Clicked, () => UiWidgetServices.CanSeeAndDo));
				UiWidgetServices.InsertPair(insertToolBarDictionary, insertMenuDictionary,
					Command.CmdInsertComplexFeature, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertComplexFeature_Clicked, () => UiWidgetServices.CanSeeAndDo));
				insertMenuDictionary.Add(Command.CmdDataTree_Insert_ClosedFeature_Value, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_ClosedFeature_Value_Clicked, () => CanCmdDataTree_Insert_ClosedFeature_Value));

				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);

				RegisterSliceLeftEdgeMenus();
			}

			private void RegisterSliceLeftEdgeMenus()
			{
				// Nothing for class of FsComplexFeature.
				// The other two classes have "Insert Feature Value" showing in two menus ("Values" property).
				// <menu id="mnuDataTree_ClosedFeature_Values">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_ClosedFeature_Values, Create_mnuDataTree_ClosedFeature_Values);
				// <menu id="mnuDataTree_ClosedFeature_Value">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_ClosedFeature_Value, Create_Delete_ClosedFeature_Value_Values);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ClosedFeature_Values(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_ClosedFeature_Values, $"Expected argument value of '{ContextMenuName.mnuDataTree_ClosedFeature_Values.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_ClosedFeature_Values">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Phoneme_Codes.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_ClosedFeature_Value" label="Insert Feature Value" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_ClosedFeature_Value_Clicked, GrammarResources.Insert_Feature_Value);

				// End: <menu id="mnuDataTree_ClosedFeature_Values">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_Delete_ClosedFeature_Value_Values(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_ClosedFeature_Value, $"Expected argument value of '{ContextMenuName.mnuDataTree_ClosedFeature_Value.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_ClosedFeature_Value">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_ClosedFeature_Value.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <command id="CmdDataTree_Delete_ClosedFeature_Value" label="Delete Feature Value" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, GrammarResources.Delete_Feature_Value, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_ClosedFeature_Value">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void InsertClosedFeature_Clicked(object sender, EventArgs e)
			{
				/*
				<command id="CmdInsertClosedFeature" label="_Feature..." message="InsertItemInVector" icon="addFeature">
					<params className="FsClosedFeature" restrictToClerkID="features" />
				</command>
				*/
				UowHelpers.UndoExtension(GrammarResources.Insert_Feature, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					var currentOwner = (IFsFeatureSystem)_recordList.OwningObject;
					currentOwner.FeaturesOC.Add(_majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create());
				});
			}

			private void InsertComplexFeature_Clicked(object sender, EventArgs e)
			{
				/*
				<command id="CmdInsertComplexFeature" label="_Complex Feature..." message="InsertItemInVector" icon="addComplexFeature">
					<params className="FsComplexFeature" restrictToClerkID="features" />
				</command>
				*/
				UowHelpers.UndoExtension(GrammarResources.Insert_Complex_Feature, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					var currentOwner = (IFsFeatureSystem)_recordList.OwningObject;
					currentOwner.FeaturesOC.Add(_majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IFsComplexFeatureFactory>().Create());
				});
			}

			private Tuple<bool, bool> CanCmdDataTree_Insert_ClosedFeature_Value => new Tuple<bool, bool>(_recordList.CurrentObject is IFsClosedFeature, true);

			private void Insert_ClosedFeature_Value_Clicked(object sender, EventArgs e)
			{
				UowHelpers.UndoExtension(GrammarResources.Insert_Feature_Value, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					var currentFsClosedFeature = (IFsClosedFeature)_recordList.CurrentObject;
					currentFsClosedFeature.ValuesOC.Add( _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create());
				});
			}

			private void CreateBrowseViewContextMenu()
			{
				// The actual menu declaration has a gazillion menu items, but only two of them are seen in this tool (plus the separator).
				// Start: <menu id="mnuBrowseView" (partial) >
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuBrowseView.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				_menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, "FsFeatDefn"));
				contextMenuStrip.Opening += ContextMenuStrip_Opening;

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
			{
				_recordBrowseView.ContextMenuStrip.Visible = !_recordList.HasEmptyList;
				if (!_recordBrowseView.ContextMenuStrip.Visible)
				{
					return;
				}
				// Set to correct class
				_menu.ResetTextIfDifferent(string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString(_recordList.CurrentObject.ClassName, StringTable.ClassNames)));
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				_recordList.DeleteRecord(((ToolStripMenuItem)sender).Text, StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~FeaturesAdvancedEditToolMenuHelper()
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
					_menu.Click -= CmdDeleteSelectedObject_Clicked;
					_menu.Dispose();
					if (_recordBrowseView?.ContextMenuStrip != null)
					{
						_recordBrowseView.ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
						_recordBrowseView.ContextMenuStrip.Dispose();
						_recordBrowseView.ContextMenuStrip = null;
					}
				}
				_majorFlexComponentParameters = null;
				_sharedListToolsUiWidgetMenuHelper = null;
				_recordBrowseView = null;
				_recordList = null;
				_dataTree = null;
				_menu = null;
				_sharedEventHandlers = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
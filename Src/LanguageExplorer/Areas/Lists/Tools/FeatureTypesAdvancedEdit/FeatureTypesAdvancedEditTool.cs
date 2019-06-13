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
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lists.Tools.FeatureTypesAdvancedEdit
{
	/// <summary>
	/// ITool implementation for the "featureTypesAdvancedEdit" tool in the "lists" area.
	/// </summary>
	/// <remarks>
	/// Oddly enough, this tool isn't a real list (e.g., nothing owned by ICmPossibilityList).
	/// </remarks>
	[Export(AreaServices.ListsAreaMachineName, typeof(ITool))]
	internal sealed class FeatureTypesAdvancedEditTool : ITool
	{
		private FeatureTypesAdvancedEditMenuHelper _toolMenuHelper;
		private const string FeatureTypes = "featureTypes";
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

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
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants
						.RecordListRepository).GetRecordList(FeatureTypes, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}
			_recordBrowseView = new RecordBrowseView(XDocument.Parse(ListResources.FeatureTypesAdvancedEditBrowseViewParameters).Root,
				majorFlexComponentParameters.LcmCache, _recordList,
				majorFlexComponentParameters.UiWidgetController);

			var showHiddenFieldsPropertyName = PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName);
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers);
			_toolMenuHelper = new FeatureTypesAdvancedEditMenuHelper(majorFlexComponentParameters, this, _recordBrowseView, _recordList);
			var recordEditView = new RecordEditView(XElement.Parse(ListResources.FeatureTypesAdvancedEditRecordEditViewParameters),
				XDocument.Parse(AreaResources.HideAdvancedListItemFields),
				majorFlexComponentParameters.LcmCache, _recordList, dataTree,
				majorFlexComponentParameters.UiWidgetController);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "FeatureTypesAndDetailMultiPane",
				ToolMachineName = MachineName
			};
			var paneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null,
				showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields,
				LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			paneBar.AddControls(new List<Control> { panelButton });
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
						majorFlexComponentParameters.MainCollapsingSplitContainer,
						mainMultiPaneParameters, _recordBrowseView, "Browse", new PaneBar(),
						recordEditView, "Details", paneBar);

			// Too early before now.
			recordEditView.FinishInitialization();
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false, SettingsGroup.LocalSettings))
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
			_recordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
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
		public string MachineName => AreaServices.FeatureTypesAdvancedEditMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Feature Types";
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

		/// <summary>
		/// Get User-visible localizable name for class of object being deleted.
		/// </summary>
		public string UiDeleteObjectName => StringTable.Table.GetString(_recordList.CurrentObject.ClassName, "ClassNames");

		#endregion

		private static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == FeatureTypes, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{FeatureTypes}'.");
			/*
            <clerk id="featureTypes">
              <recordList owner="MsFeatureSystem" property="FeatureTypes" />
            </clerk>
			*/
			return new RecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true,
				new VectorPropertyParameterObject(cache.LanguageProject.MsFeatureSystemOA, "FeatureTypes", FsFeatureSystemTags.kflidFeatures));
		}

		private sealed class FeatureTypesAdvancedEditMenuHelper : IDisposable
		{
			private readonly MajorFlexComponentParameters _majorFlexComponentParameters;
			private readonly IFsFeatureSystem _featureSystem;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;

			internal FeatureTypesAdvancedEditMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, RecordBrowseView recordBrowseView, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;
				_featureSystem = _majorFlexComponentParameters.LcmCache.LanguageProject.MsFeatureSystemOA;
				SetupToolUiWidgets(tool);
			}

			private void SetupToolUiWidgets(ITool tool)
			{
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				// Add to Insert menu & Insert toolbar
				// <command id="CmdInsertFeatureType" label="_Feature Type" message="InsertItemInVector" icon="AddItem">
				toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert].Add(Command.CmdInsertFeatureType, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertFeatureType_Clicked, ()=> CanCmdInsertFeatureType));
				toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert].Add(Command.CmdInsertFeatureType, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertFeatureType_Clicked, () => CanCmdInsertFeatureType));
				// Change Text.
				_majorFlexComponentParameters.UiWidgetController.InsertMenuDictionary[Command.CmdInsertFeatureType].Text = ListResources.Feature_Type;
				_majorFlexComponentParameters.UiWidgetController.InsertToolBarDictionary[Command.CmdInsertFeatureType].ToolTipText = ListResources.Feature_Type;

				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				CreateBrowseViewContextMenu();
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
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("FsFeatStrucType", "ClassNames")));
				contextMenuStrip.Opening += ContextMenuStrip_Opening;

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
			{
				_recordBrowseView.ContextMenuStrip.Visible = !_recordList.HasEmptyList;
				_recordBrowseView.ContextMenuStrip.Enabled = _recordBrowseView.ContextMenuStrip.Visible;
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				_recordList.DeleteRecord(((ToolStripMenuItem)sender).Text, StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			private static Tuple<bool, bool> CanCmdInsertFeatureType => new Tuple<bool, bool>(true, true);

			private void InsertFeatureType_Clicked(object sender, EventArgs e)
			{
				using (var dlg = new MasterInflectionFeatureListDlg("FsFeatDefn"))
				{
					dlg.SetDlginfo(_featureSystem, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, true);
					dlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<Form>(FwUtils.window));
				}
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~FeatureTypesAdvancedEditMenuHelper()
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
					if (_recordBrowseView?.ContextMenuStrip != null)
					{
						_recordBrowseView.ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
						_recordBrowseView.ContextMenuStrip.Dispose();
						_recordBrowseView.ContextMenuStrip = null;
					}
				}

				_isDisposed = true;
			}
			#endregion
		}
	}
}
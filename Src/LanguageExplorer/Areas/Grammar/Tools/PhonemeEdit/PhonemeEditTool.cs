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
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Grammar.Tools.PhonemeEdit
{
	/// <summary>
	/// ITool implementation for the "phonemeEdit" tool in the "grammar" area.
	/// </summary>
	[Export(AreaServices.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class PhonemeEditTool : ITool
	{
		private PhonemeEditToolMenuHelper _toolMenuHelper;
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
			if (majorFlexComponentParameters.LcmCache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Count == 0)
			{
				// Pathological...this helps the memory-only backend mainly, but makes others self-repairing.
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					majorFlexComponentParameters.LcmCache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				});
			}
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(GrammarAreaServices.Phonemes, majorFlexComponentParameters.StatusBar, GrammarAreaServices.PhonemesFactoryMethod);
			}
			var root = XDocument.Parse(GrammarResources.PhonemeEditToolParameters).Root;
			_recordBrowseView = new RecordBrowseView(root.Element("browseview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			var showHiddenFieldsPropertyName = UiWidgetServices.CreateShowHiddenFieldsPropertyName(MachineName);
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers, majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false));
			var recordEditView = new RecordEditView(root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "PhonemeItemsAndDetailMultiPane",
				ToolMachineName = MachineName
			};

			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });

			// Too early before now. Do not change the order of the following three calls.
			_toolMenuHelper = new PhonemeEditToolMenuHelper(majorFlexComponentParameters, this, _recordBrowseView, _recordList, dataTree);
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer, mainMultiPaneParameters, _recordBrowseView, "Browse", new PaneBar(),
				recordEditView, "Details", recordEditViewPaneBar);
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
		public string MachineName => AreaServices.PhonemeEditMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.PhonemeEditUiName);

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

		private sealed class PhonemeEditToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedListToolsUiWidgetMenuHelper;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;
			private DataTree _dataTree;
			private ISharedEventHandlers _sharedEventHandlers;
			private GrammarAreaServices _grammarAreaServices;

			internal PhonemeEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, RecordBrowseView recordBrowseView, IRecordList recordList, DataTree dataTree)
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
				_partiallySharedListToolsUiWidgetMenuHelper = new PartiallySharedForToolsWideMenuHelper(_majorFlexComponentParameters, _recordList);
				_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
				_grammarAreaServices = new GrammarAreaServices();
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				_grammarAreaServices.Setup_CmdInsertPhoneme(_majorFlexComponentParameters.LcmCache, toolUiWidgetParameterObject);
				// <command id="CmdDataTree_Insert_Phoneme_Code" label="Grapheme" message="DataTreeInsert">
				toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert].Add(Command.CmdDataTree_Insert_Phoneme_Code, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Phoneme_Code_Clicked, () => CanCmdDataTree_Insert_Phoneme_Code));

				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);

				RegisterSliceLeftEdgeMenus();
			}

			private static Tuple<bool, bool> CanCmdDataTree_Insert_Phoneme_Code => new Tuple<bool, bool>(true, true);

			private void Insert_Phoneme_Code_Clicked(object sender, EventArgs e)
			{
				/*
				<command id="CmdDataTree_Insert_Phoneme_Code" label="Grapheme" message="DataTreeInsert">
					<parameters field="Codes" className="PhCode" />
				</command>
				*/
				_dataTree.CurrentSlice.HandleInsertCommand("Codes", PhCodeTags.kClassName);
			}

			private void RegisterSliceLeftEdgeMenus()
			{
				// <menu id="mnuDataTree_Phoneme_Codes">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Phoneme_Codes, Create_mnuDataTree_Phoneme_Codes);
				// <menu id="mnuDataTree_Phoneme_Code">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Phoneme_Code, Create_mnuDataTree_Phoneme_Code);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Phoneme_Codes(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Phoneme_Codes, $"Expected argument value of '{ContextMenuName.mnuDataTree_Phoneme_Codes.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Phoneme_Codes">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Phoneme_Codes.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_Phoneme_Code" label="Insert Grapheme" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Phoneme_Code_Clicked, GrammarResources.Insert_Grapheme);

				// End: <menu id="mnuDataTree_Phoneme_Codes">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Phoneme_Code(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Phoneme_Code, $"Expected argument value of '{ContextMenuName.mnuDataTree_Phoneme_Code.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Phoneme_Code">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Phoneme_Code.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Delete_Phoneme_Code" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, GrammarResources.Delete_Grapheme, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_Phoneme_Code">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
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
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("PhPhoneme", StringTable.ClassNames)));
				contextMenuStrip.Opening += ContextMenuStrip_Opening;

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
			{
				_recordBrowseView.ContextMenuStrip.Visible = !_recordList.HasEmptyList;
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				_recordList.DeleteRecord(((ToolStripMenuItem)sender).Text, StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~PhonemeEditToolMenuHelper()
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
					_partiallySharedListToolsUiWidgetMenuHelper.Dispose();
					if (_recordBrowseView?.ContextMenuStrip != null)
					{
						_recordBrowseView.ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
						_recordBrowseView.ContextMenuStrip.Dispose();
						_recordBrowseView.ContextMenuStrip = null;
					}
				}
				_majorFlexComponentParameters = null;
				_partiallySharedListToolsUiWidgetMenuHelper = null;
				_recordBrowseView = null;
				_recordList = null;
				_dataTree = null;
				_sharedEventHandlers = null;
				_grammarAreaServices = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
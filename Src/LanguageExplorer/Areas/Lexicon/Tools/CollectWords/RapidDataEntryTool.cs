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
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.CollectWords
{
	/// <summary>
	/// ITool implementation for the "rapidDataEntry" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class RapidDataEntryTool : ITool
	{
		internal const string RDEwords = "RDEwords";
		private CollapsingSplitContainer _collapsingSplitContainer;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordListProvidingOwner;
		private IRecordList _subservientRecordList;
		[Import(AreaServices.LexiconAreaMachineName)]
		private IArea _area;
		[Import]
		private IPropertyTable _propertyTable;
		private RapidDataEntryToolMenuHelper _toolMenuHelper;

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
			_propertyTable.SetProperty(LanguageExplorerConstants.RecordListWidthGlobal, _collapsingSplitContainer.SplitterDistance, true, settingsGroup: SettingsGroup.GlobalSettings);

#if RANDYTODO
			// If these removals are more permanent, then move up to the "RemoveObsoleteProperties" method on the main window.
			/*
			Q: Jason: "This won't result in us losing track of current entries when switching between tools will it?
					I can't remember what this property is used for at the moment."
			A: Randy: "One of the changes (not integration stuff like all of these ones) I plan is to get all record
					record list instances to come out of a repository like class, which creates them all and returns them, when requested.
					A tool will then activate them, when the tool is activated, and the tool will deactivate them, when the tool changes.
					That is something like what is done in 'develop' with the PropertyTable (sans creation).
					But, I'd like to see use of PropertyTable reduced to actual properties that are persisted,
					and not as yet another 'God-object' in the code that knows how to get anything (eg. LCMCache, service locator, etc).
					I'm not there yet, but that is where I'd like to go.

					So, I suspect those properties will eventually go away permanently, but I'm not there yet."
			*/
#endif
			_propertyTable.RemoveProperty(RecordList.RecordListSelectedObjectPropertyId(_subservientRecordList.Id));
			_propertyTable.RemoveProperty(RecordList.RecordListSelectedObjectPropertyId(_recordListProvidingOwner.Id));
			_propertyTable.RemoveProperty("ActiveListSelectedObject");
			majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).RemoveRecordList(_recordListProvidingOwner);

			CollapsingSplitContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _collapsingSplitContainer);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

			_recordBrowseView = null;
			_subservientRecordList = null;
			_toolMenuHelper = null;
			_recordListProvidingOwner = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			var mainCollapsingSplitContainerAsControl = (Control)majorFlexComponentParameters.MainCollapsingSplitContainer;
			mainCollapsingSplitContainerAsControl.SuspendLayout();

			var root = XDocument.Parse(LexiconResources.RapidDataEntryToolParameters).Root;
			if (_recordListProvidingOwner == null)
			{
				_recordListProvidingOwner = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LexiconArea.SemanticDomainList_LexiconArea, majorFlexComponentParameters.StatusBar, LexiconArea.SemanticDomainList_LexiconAreaFactoryMethod);
			}
			var recordBar = new RecordBar(_propertyTable)
			{
				IsFlatList = false,
				Dock = DockStyle.Fill
			};
			_collapsingSplitContainer = new CollapsingSplitContainer();
			_collapsingSplitContainer.SuspendLayout();
			_collapsingSplitContainer.SecondCollapseZone = CollapsingSplitContainerFactory.BasicSecondCollapseZoneWidth;
			_collapsingSplitContainer.Dock = DockStyle.Fill;
			_collapsingSplitContainer.Orientation = Orientation.Vertical;
			_collapsingSplitContainer.FirstLabel = AreaResources.ksRecordListLabel;
			_collapsingSplitContainer.FirstControl = recordBar;
			_collapsingSplitContainer.SecondLabel = AreaResources.ksMainContentLabel;
			_collapsingSplitContainer.SplitterDistance = _propertyTable.GetValue<int>(LanguageExplorerConstants.RecordListWidthGlobal, SettingsGroup.GlobalSettings);

			var showHiddenFieldsPropertyName = UiWidgetServices.CreateShowHiddenFieldsPropertyName(MachineName);
			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers, majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(UiWidgetServices.CreateShowHiddenFieldsPropertyName(MachineName), false));
			var recordEditView = new RecordEditView(root.Element("recordeditview").Element("parameters"), XDocument.Parse(LexiconResources.HideAdvancedFeatureFields), majorFlexComponentParameters.LcmCache, _recordListProvidingOwner, dataTree, majorFlexComponentParameters.UiWidgetController);
			if (_subservientRecordList == null)
			{
				_subservientRecordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(RDEwords, majorFlexComponentParameters.StatusBar, SubservientListFactoryMethod);
			}
			_recordBrowseView = new RecordBrowseView(root.Element("recordbrowseview").Element("parameters"), majorFlexComponentParameters.LcmCache, _subservientRecordList, majorFlexComponentParameters.UiWidgetController);
			_toolMenuHelper = new RapidDataEntryToolMenuHelper(majorFlexComponentParameters, this, _recordBrowseView, _recordListProvidingOwner);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				Area = _area,
				Id = "SemanticCategoryAndItems",
				ToolMachineName = MachineName,
				DefaultFocusControl = "RecordBrowseView",
				FirstControlParameters = new SplitterChildControlParameters { Control = recordEditView, Label = "Semantic Domain" },
				SecondControlParameters = new SplitterChildControlParameters { Control = _recordBrowseView, Label = "Details" }
			};
			var nestedMultiPane = MultiPaneFactory.CreateNestedMultiPane(majorFlexComponentParameters.FlexComponentParameters, mainMultiPaneParameters);
			nestedMultiPane.SplitterDistance = _propertyTable.GetValue<int>($"MultiPaneSplitterDistance_{_area.MachineName}_{MachineName}_{mainMultiPaneParameters.Id}");
			_collapsingSplitContainer.SecondControl = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, nestedMultiPane, recordEditViewPaneBar);
			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = _collapsingSplitContainer;
			_collapsingSplitContainer.ResumeLayout();
			mainCollapsingSplitContainerAsControl.ResumeLayout();
			recordEditView.BringToFront();
			recordBar.BringToFront();

			// Too early before now.
			((ISemanticDomainTreeBarHandler)_recordListProvidingOwner.MyTreeBarHandler).FinishInitialization(new PaneBar());
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
			_recordListProvidingOwner.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordListProvidingOwner.VirtualListPublisher).Refresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			_propertyTable.SetProperty(LanguageExplorerConstants.RecordListWidthGlobal, _collapsingSplitContainer.SplitterDistance, true, settingsGroup: SettingsGroup.GlobalSettings);
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.RapidDataEntryMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.RapidDataEntryUiName);

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.BrowseView.SetBackgroundColor(Color.Magenta);

		#endregion

		private IRecordList SubservientListFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == RDEwords, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{RDEwords}'.");
			/*
            <clerk id="RDEwords" clerkProvidingOwner="SemanticDomainList">
              <recordList class="CmSemanticDomain" field="ReferringSenses" />
              <sortMethods />
            </clerk>
			*/
			return new SubservientRecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true,
				new VectorPropertyParameterObject(cache.LanguageProject.SemanticDomainListOA, "ReferringSenses", cache.MetaDataCacheAccessor.GetFieldId2(CmSemanticDomainTags.kClassId, "ReferringSenses", false)), _recordListProvidingOwner);
		}

		/// <summary>
		/// This class handles all interaction for the RapidDataEntryTool for its menus and tool bars.
		/// </summary>
		private sealed class RapidDataEntryToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;
			private ToolStripMenuItem _jumpMenu;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;

			internal RapidDataEntryToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, RecordBrowseView recordBrowseView, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;
				_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(_majorFlexComponentParameters, _recordList);

				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				// Only register tool for now. The tool's RecordBrowseView will register as a UserControl, so a tool must be registered before that happens.
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
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

				// <item command="CmdEntryJumpToDefault" />
				_jumpMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.ksShowEntryInLexicon);
				_jumpMenu.Tag = new List<object> { _majorFlexComponentParameters.FlexComponentParameters.Publisher, AreaServices.LexiconEditMachineName, _recordList };

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~RapidDataEntryToolMenuHelper()
			{
				// The base class finalizer is called automatically.
				Dispose(false);
			}

			/// <inheritdoc />
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
					_jumpMenu.Click -= _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool);
					_jumpMenu.Dispose();
					_recordBrowseView.ContextMenuStrip.Dispose();
					_recordBrowseView.ContextMenuStrip = null;
					_partiallySharedForToolsWideMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_jumpMenu = null;
				_recordBrowseView = null;
				_partiallySharedForToolsWideMenuHelper = null;
				_recordList = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
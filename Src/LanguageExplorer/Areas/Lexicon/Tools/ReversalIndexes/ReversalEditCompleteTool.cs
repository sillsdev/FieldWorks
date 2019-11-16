// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Areas.Lexicon.Reversals;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	/// <summary>
	/// ITool implementation for the "reversalEditComplete" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class ReversalEditCompleteTool : ITool
	{
		private ReversalEditCompleteToolMenuHelper _toolMenuHelper;
		private MultiPane _multiPane;
		private IRecordList _recordList;
		private XhtmlDocView _xhtmlDocView;
		[Import(AreaServices.LexiconAreaMachineName)]
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

			_xhtmlDocView = null;
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
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LexiconArea.AllReversalEntries, majorFlexComponentParameters.StatusBar, ReversalServices.AllReversalEntriesFactoryMethod);
			}
			var root = XDocument.Parse(LexiconResources.ReversalEditCompleteToolParameters).Root;
			_xhtmlDocView = new XhtmlDocView(root.Element("docview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			var showHiddenFieldsPropertyName = UiWidgetServices.CreateShowHiddenFieldsPropertyName(MachineName);
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers, majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false));
			_toolMenuHelper = new ReversalEditCompleteToolMenuHelper(majorFlexComponentParameters, this, dataTree, _recordList, _xhtmlDocView);
			var recordEditView = new RecordEditView(root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.HideAdvancedListItemFields), majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "ReversalIndexItemsAndDetailMultiPane",
				ToolMachineName = MachineName
			};
			var docViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var rightSpacer = new Spacer
			{
				Width = 10,
				Dock = DockStyle.Right
			};
			var rightPanelMenu = new PanelMenu(_toolMenuHelper.MainPanelMenuContextMenuFactory, AreaServices.RightPanelMenuId)
			{
				Dock = DockStyle.Right,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			var leftSpacer = new Spacer
			{
				Width = 10,
				Dock = DockStyle.Left
			};
			var leftPanelMenu = new PanelMenu(_toolMenuHelper.MainPanelMenuContextMenuFactory, AreaServices.LeftPanelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			docViewPaneBar.AddControls(new List<Control> { leftPanelMenu, leftSpacer, rightPanelMenu, rightSpacer });
			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer, mainMultiPaneParameters, _xhtmlDocView, "Doc Reversals", docViewPaneBar, recordEditView, "Browse Entries", recordEditViewPaneBar);

			// Too early before now.
			recordEditView.FinishInitialization();
			_xhtmlDocView.FinishInitialization();
			((IPostLayoutInit)_multiPane).PostLayoutInit();
			_xhtmlDocView.OnPropertyChanged("ReversalIndexPublicationLayout");
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
			_xhtmlDocView.PublicationDecorator.Refresh();
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
		public string MachineName => AreaServices.ReversalEditCompleteMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.ReversalEditCompleteUiName);

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

		private sealed class ReversalEditCompleteToolMenuHelper : IDisposable
		{
			private LcmCache _cache;
			private IReversalIndexRepository _reversalIndexRepository;
			private IReversalIndex _currentReversalIndex;
			private IPropertyTable _propertyTable;
			private IRecordList _recordList;
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private CommonReversalIndexMenuHelper _commonReversalIndexMenuHelper;
			private XhtmlDocView _docView;
			internal PanelMenuContextMenuFactory MainPanelMenuContextMenuFactory { get; private set; }

			internal ReversalEditCompleteToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, DataTree dataTree, IRecordList recordList, XhtmlDocView docView)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(dataTree, nameof(dataTree));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(docView, nameof(docView));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordList = recordList;
				_docView = docView;

				_cache = majorFlexComponentParameters.LcmCache;
				_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
				_reversalIndexRepository = _cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
				_reversalIndexRepository.EnsureReversalIndicesExist(_cache, _propertyTable);
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Edit].Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditFindMenu_Click, () => UiWidgetServices.CanSeeAndDo));
				toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert].Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditFindMenu_Click, () => UiWidgetServices.CanSeeAndDo));
				_commonReversalIndexMenuHelper = new CommonReversalIndexMenuHelper(_majorFlexComponentParameters, recordList);
				_commonReversalIndexMenuHelper.SetupUiWidgets(toolUiWidgetParameterObject);
				majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				MainPanelMenuContextMenuFactory = new PanelMenuContextMenuFactory();
				// Left menu on doc view
				MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(AreaServices.LeftPanelMenuId, CreateLeftMainPanelContextMenuStrip);
				// Right menu on doc view.
				MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(AreaServices.RightPanelMenuId, CreateRightMainPanelContextMenuStrip);
				dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuReorderVector, Create_mnuReorderVector);
#if RANDYTODO
				// TODO: See LexiconEditTool for how to set up all the slice menus in the right side record view.
    <menu id="mnuDataTree_MoveReversalIndexEntry">
      <item command="CmdDataTree_MoveUp_ReversalSubentry" />
      <item command="CmdDataTree_MoveDown_ReversalSubentry" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Move_MoveReversalIndexEntry" />
      <item command="CmdDataTree_Promote_ProReversalIndexEntry" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Merge_Subentry" />
      <item command="CmdDataTree_Delete_Subentry" />
    </menu>
    <command id="CmdDataTree_MoveUp_ReversalSubentry" label="Move Subentry _Up" message="MoveUpObjectInSequence" icon="MoveUp">
      <parameters field="Subentries" className="ReversalindexEntry" />
    </command>
    <command id="CmdDataTree_MoveDown_ReversalSubentry" label="Move Subentry Down" message="MoveDownObjectInSequence" icon="MoveDown">
      <parameters field="Subentries" className="ReversalindexEntry" />
    </command>
    <command id="CmdDataTree_Move_MoveReversalIndexEntry" label="Move Entry..." message="MoveReversalindexEntry">
      <parameters field="Subentries" className="ReversalindexEntry" />
    </command>
    <command id="CmdDataTree_Promote_ProReversalIndexEntry" label="Promote" message="PromoteReversalindexEntry" icon="MoveLeft">
      <parameters field="Subentries" className="ReversalindexEntry" />
    </command>
    <command id="CmdDataTree_Merge_Subentry" label="Merge Entry into..." message="DataTreeMerge">
      <parameters field="Subentries" className="ReversalindexEntry" />
    </command>
    <command id="CmdDataTree_Delete_Subentry" label="Delete this Entry and any Subentries" message="DataTreeDelete" icon="Delete">
      <parameters field="Subentries" className="ReversalindexEntry" />
    </command>

    <menu id="mnuDataTree_InsertReversalSubentry">
      <item command="CmdDataTree_Insert_ReversalSubentry" />
    </menu>
    <command id="CmdDataTree_Insert_ReversalSubentry" label="Insert Reversal Subentry" message="DataTreeInsert">
      <parameters field="Subentries" className="ReversalIndexEntry" />
    </command>

    <menu id="mnuDataTree_InsertReversalSubentry_Hotlinks">
      <item command="CmdDataTree_Insert_ReversalSubentry" />
    </menu>
#endif
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuReorderVector(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(slice is ReferenceVectorSlice, $"Expected Slice class of 'ReferenceVectorSlice', but got on of class '{slice.GetType().Name}'.");

				return ((ReferenceVectorSlice)slice).Create_mnuReorderVector(contextMenuId);
			}

			private void EditFindMenu_Click(object sender, EventArgs e)
			{
				_docView.FindText();
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateLeftMainPanelContextMenuStrip(string panelMenuId)
			{
				Require.That(panelMenuId == AreaServices.LeftPanelMenuId, $"I don't know how to create a context menu with an ID of '{panelMenuId}', as I can only create one with an id of '{AreaServices.LeftPanelMenuId}'.");

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = "ReversalIndexPaneMenu"
				};

				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
				var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);

				// <menu inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" list="ReversalIndexList" property="ReversalIndexPublicationLayout" />
				var currentGuid = ReversalIndexServices.GetObjectGuidIfValid(_propertyTable, "ReversalIndexGuid");
				if (currentGuid != Guid.Empty)
				{
					_currentReversalIndex = (IReversalIndex)_majorFlexComponentParameters.LcmCache.ServiceLocator.GetObject(currentGuid);
				}
				var allInstancesinRepository = _reversalIndexRepository.AllInstances().ToDictionary(rei => rei.Guid);
				foreach (var rei in allInstancesinRepository.Values)
				{
					var newMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ReversalIndex_Menu_Clicked, rei.ChooserNameTS.Text);
					newMenuItem.Tag = rei;
					if (_currentReversalIndex == rei)
					{
						newMenuItem.Checked = true;
					}
				}
				// <item label="-" translate="do not translate" />
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <item command="CmdConfigureDictionary" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ConfigureDictionary_Clicked, LexiconResources.ConfigureReversalIndex);

				return retVal;
			}

			private void ReversalIndex_Menu_Clicked(object sender, EventArgs e)
			{
				var contextMenuItem = (ToolStripMenuItem)sender;
				_currentReversalIndex = (IReversalIndex)contextMenuItem.Tag;
				_propertyTable.SetProperty("ReversalIndexGuid", _currentReversalIndex.Guid.ToString(), true, settingsGroup: SettingsGroup.LocalSettings);
				((ReversalListBase)_recordList).ChangeOwningObjectIfPossible();
				contextMenuItem.Checked = ((IReversalIndex)contextMenuItem.Tag).Guid.ToString() == _propertyTable.GetValue<string>("ReversalIndexGuid");
			}

			private void ConfigureDictionary_Clicked(object sender, EventArgs e)
			{
				var fwMainWnd = _majorFlexComponentParameters.MainWindow;
				if (DictionaryConfigurationDlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters, (Form)fwMainWnd, _recordList?.CurrentObject, "khtpConfigureReversalIndex", LanguageExplorerResources.ReversalIndex))
				{
					fwMainWnd.RefreshAllViews();
				}
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateRightMainPanelContextMenuStrip(string panelMenuId)
			{
				Require.That(panelMenuId == AreaServices.RightPanelMenuId, $"I don't know how to create a context menu with an ID of '{panelMenuId}', as I can only create one with an id of '{AreaServices.RightPanelMenuId}'.");

				var currentPublication = _propertyTable.GetValue<string>(LanguageExplorerConstants.SelectedPublication);
				var contextMenuStrip = new ContextMenuStrip();

				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
				var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
				// <item command="CmdShowAllPublications" />
				var currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ShowAllPublications_Clicked, LexiconResources.AllEntries);
				currentToolStripMenuItem.Tag = "All Entries";
				currentToolStripMenuItem.Checked = (currentPublication == "All Entries");
				var pubName = _docView.GetCurrentPublication();
				currentToolStripMenuItem.Checked = (LanguageExplorerResources.AllEntriesPublication == pubName);

				// <menu list="Publications" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="SelectedPublication" />
				List<string> inConfig;
				List<string> notInConfig;
				_docView.SplitPublicationsByConfiguration(_majorFlexComponentParameters.LcmCache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS, _docView.GetCurrentConfiguration(false), out inConfig, out notInConfig);
				foreach (var pub in inConfig)
				{
					currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Publication_Clicked, pub);
					currentToolStripMenuItem.Tag = pub;
					currentToolStripMenuItem.Checked = (currentPublication == pub);
				}
				if (notInConfig.Any())
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
					foreach (var pub in notInConfig)
					{
						currentToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Publication_Clicked, pub);
						currentToolStripMenuItem.Tag = pub;
						currentToolStripMenuItem.Checked = (currentPublication == pub);
					}
				}

				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <item command="CmdPublicationsJumpToDefault" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, EditPublications_Clicked, LexiconResources.EditPublications);

				return retVal;
			}

			private void ShowAllPublications_Clicked(object sender, EventArgs e)
			{
				_propertyTable.SetProperty(LanguageExplorerConstants.SelectedPublication, LanguageExplorerResources.AllEntriesPublication, true);
				_docView.OnPropertyChanged(LanguageExplorerConstants.SelectedPublication);
			}

			private void Publication_Clicked(object sender, EventArgs e)
			{
				var clickedToolStripMenuItem = (ToolStripMenuItem)sender;
				if (clickedToolStripMenuItem.Checked)
				{
					return; // No change.
				}
				var newValue = (string)clickedToolStripMenuItem.Tag;
				_propertyTable.SetProperty(LanguageExplorerConstants.SelectedPublication, newValue, true, settingsGroup: SettingsGroup.LocalSettings);
				_docView.OnPropertyChanged(LanguageExplorerConstants.SelectedPublication);
			}

			private void EditPublications_Clicked(object sender, EventArgs e)
			{
				/*
			<command id="CmdPublicationsJumpToDefault" label="Edit Publications" message="JumpToTool">
				<parameters tool="publicationsEdit" className="ICmPossibility" ownerClass="LangProject" ownerField="PublicationTypes" />
			</command>
				 */
				MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, @"Stay tuned for jump to: 'publicationsEdit' in list 'LangProject->PublicationTypes'");
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~ReversalEditCompleteToolMenuHelper()
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
					MainPanelMenuContextMenuFactory.Dispose();
					_commonReversalIndexMenuHelper.Dispose();
				}
				MainPanelMenuContextMenuFactory = null;
				_majorFlexComponentParameters = null;
				_commonReversalIndexMenuHelper = null;
				_docView = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
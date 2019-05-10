// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// ITool implementation for the "lexiconEdit" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class LexiconEditTool : ITool
	{
		internal const string Show_DictionaryPubPreview = "Show_DictionaryPubPreview";
		private LexiconEditToolMenuHelper _lexiconEditToolMenuHelper;
		private ISharedEventHandlers _sharedEventHandlers;
		private DataTree MyDataTree { get; set; }
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private MultiPane _innerMultiPane;
		private IRecordList _recordList;
		[Import(AreaServices.LexiconAreaMachineName)]
		private IArea _area;
		[Import]
		private IPropertyTable _propertyTable;
		[Import]
		private IPublisher _publisher;

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
			_lexiconEditToolMenuHelper.Dispose();

			_recordBrowseView = null;
			_innerMultiPane = null;
			_lexiconEditToolMenuHelper = null;
			MyDataTree = null;
			_sharedEventHandlers = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_propertyTable.SetDefault(Show_DictionaryPubPreview, true, true);
			_propertyTable.SetDefault($"{AreaServices.ToolForAreaNamed_}{_area.MachineName}", MachineName, true);
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LexiconArea.Entries, majorFlexComponentParameters.StatusBar, LexiconArea.EntriesFactoryMethod);
			}

			var root = XDocument.Parse(LexiconResources.LexiconBrowseParameters).Root;
			// Modify the basic parameters for this tool.
			root.Attribute("id").Value = "lexentryList";
			root.Add(new XAttribute("defaultCursor", "Arrow"), new XAttribute("hscroll", "true"));

			var overrides = XElement.Parse(LexiconResources.LexiconBrowseOverrides);
			// Add one more element to 'overrides'.
			overrides.Add(new XElement("column", new XAttribute("layout", "DefinitionsForSense"), new XAttribute("visibility", "menu")));
			var columnsElement = XElement.Parse(LexiconResources.LexiconBrowseDialogColumnDefinitions);
			OverrideServices.OverrideVisibiltyAttributes(columnsElement, overrides);
			root.Add(columnsElement);

			_recordBrowseView = new RecordBrowseView(root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);

			var showHiddenFieldsPropertyName = PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName);
			MyDataTree = new DataTree(_sharedEventHandlers);
			_lexiconEditToolMenuHelper = new LexiconEditToolMenuHelper(majorFlexComponentParameters, this, MyDataTree, _recordList, _recordBrowseView, showHiddenFieldsPropertyName);

			var recordEditView = new RecordEditView(XElement.Parse(LexiconResources.LexiconEditRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordList, MyDataTree, majorFlexComponentParameters.UiWidgetController);
			var nestedMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				Area = _area,
				DefaultFixedPaneSizePoints = "60",
				Id = "TestEditMulti",
				ToolMachineName = MachineName,
				FirstControlParameters = new SplitterChildControlParameters
				{
					Control = new XhtmlRecordDocView(XDocument.Parse(LexiconResources.LexiconEditRecordDocViewParameters).Root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController),
					Label = "Dictionary"
				},
				SecondControlParameters = new SplitterChildControlParameters
				{
					Control = recordEditView,
					Label = "Details"
				}
			};
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "LexItemsAndDetailMultiPane",
				ToolMachineName = MachineName,
				DefaultPrintPane = "DictionaryPubPreview"
			};
			var paneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);

			var panelMenu = new PanelMenu(MyDataTree.DataTreeStackContextMenuFactory.MainPanelMenuContextMenuFactory, AreaServices.PanelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			paneBar.AddControls(new List<Control> { panelMenu, panelButton });
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer, mainMultiPaneParameters, _recordBrowseView, "Browse", new PaneBar(),
				_innerMultiPane = MultiPaneFactory.CreateNestedMultiPane(majorFlexComponentParameters.FlexComponentParameters, nestedMultiPaneParameters), "Dictionary & Details", paneBar);
			_innerMultiPane.Panel1Collapsed = !_propertyTable.GetValue<bool>(Show_DictionaryPubPreview);
			_lexiconEditToolMenuHelper.InnerMultiPane = _innerMultiPane;
			panelButton.MyDataTree = recordEditView.MyDataTree;

			// Too early before now.
			recordEditView.FinishInitialization();
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false, SettingsGroup.LocalSettings))
			{
				majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish("ShowHiddenFields", true);
			}
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
		public string MachineName => AreaServices.LexiconEditMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Lexicon Edit";

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

		/// <summary>
		/// This class handles all interaction for the LexiconEditTool for its menus, tool bars, plus all context menus that are used in Slices and PaneBars.
		/// </summary>
		private sealed class LexiconEditToolMenuHelper : IDisposable
		{
			private string _extendedPropertyName;
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private IPropertyTable _propertyTable;
			private ISubscriber _subscriber;
			private IPublisher _publisher;
			private LcmCache _cache;
			private IRecordList _recordList;
			private DataTree _dataTree;
			private ISharedEventHandlers _sharedEventHandlers;
			private IFwMainWnd _mainWnd;
			private ToolStripMenuItem _show_DictionaryPubPreviewContextMenu;
			private RightClickContextMenuManager _rightClickContextMenuManager;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;
			private RecordBrowseView _recordBrowseView;

			internal MultiPane InnerMultiPane { get; set; }

			internal LexiconEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, DataTree dataTree, IRecordList recordList, RecordBrowseView recordBrowseView, string extendedPropertyName)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(dataTree, nameof(dataTree));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNullOrEmptyString(extendedPropertyName, nameof(extendedPropertyName));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_propertyTable = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
				_subscriber = _majorFlexComponentParameters.FlexComponentParameters.Subscriber;
				_publisher = _majorFlexComponentParameters.FlexComponentParameters.Publisher;
				_cache = _majorFlexComponentParameters.LcmCache;
				_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
				_recordList = recordList;
				_recordBrowseView = recordBrowseView;
				_mainWnd = majorFlexComponentParameters.MainWindow;
				_dataTree = dataTree;
				_extendedPropertyName = extendedPropertyName;
				_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(majorFlexComponentParameters, recordList);
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				SetupUiWidgets(toolUiWidgetParameterObject);
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

				// <item command="CmdEntryJumpToConcordance"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdEntryJumpToConcordance_Click, AreaResources.Show_Entry_In_Concordance);

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("LexEntry", "ClassNames")));
				var currentSlice = _dataTree.CurrentSlice;
				if (currentSlice == null)
				{
					_dataTree.GotoFirstSlice();
				}
				menu.Tag = _dataTree.CurrentSlice;

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				var currentSlice = _dataTree.CurrentSlice;
				if (currentSlice == null)
				{
					_dataTree.GotoFirstSlice();
				}
				currentSlice.HandleDeleteCommand();
			}

			private void SetupUiWidgets(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
			{
				// Both used by RightClickContextMenuManager
				_sharedEventHandlers.Add(AreaServices.CmdMoveTargetToPreviousInSequence, MoveReferencedTargetDownInSequence_Clicked);
				_sharedEventHandlers.Add(AreaServices.CmdMoveTargetToNextInSequence, MoveReferencedTargetUpInSequence_Clicked);

				var insertToolBarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];
				// Was: LexiconEditToolEditMenuManager
				// <item command="CmdGoToEntry" />
				InsertPair(insertToolBarDictionary, toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Edit], Command.CmdGoToEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(GoToEntry_Clicked, () => CanCmdGoToEntry));

				// Was: LexiconEditToolViewMenuManager
				// <item label="_Show Hidden Fields" boolProperty="ShowHiddenFields" defaultVisible="false"/>
				_subscriber.Subscribe("ShowHiddenFields", ShowHiddenFields_Handler);
				var viewMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.View];
				viewMenuDictionary.Add(Command.ShowHiddenFields, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Show_Hidden_Fields_Clicked, () => CanShowHiddenFields));
				ShowHiddenFields_Handler(_propertyTable.GetValue(_extendedPropertyName, false));
				// <item label="Show _Dictionary Preview" boolProperty="Show_DictionaryPubPreview" defaultVisible="false"/>
				viewMenuDictionary.Add(Command.Show_DictionaryPubPreview, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Show_Dictionary_Preview_Clicked, () => CanShow_DictionaryPubPreview));
				((ToolStripMenuItem)_majorFlexComponentParameters.UiWidgetController.ViewMenuDictionary[Command.Show_DictionaryPubPreview]).Checked = _propertyTable.GetValue<bool>(Show_DictionaryPubPreview);

				// Was: LexiconEditToolInsertMenuManager
				var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
				// <item command="CmdInsertLexEntry" defaultVisible="false" />
				InsertPair(insertToolBarDictionary, insertMenuDictionary, Command.CmdInsertLexEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Entry_Clicked, () => CanCmdInsertLexEntry));
				// <item command="CmdInsertSense" defaultVisible="false" />;
				insertMenuDictionary.Add(Command.CmdInsertSense, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Sense_Clicked, () => CanCmdInsertSense));
				// <item command="CmdInsertSubsense" defaultVisible="false" />
				insertMenuDictionary.Add(Command.CmdInsertSubsense, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Subsense_Clicked, () => CanCmdInsertSubsense));
				// <item command="CmdDataTree_Insert_AlternateForm" label="A_llomorph" defaultVisible="false" />
				insertMenuDictionary.Add(Command.CmdDataTree_Insert_AlternateForm, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Allomorph_Clicked, () => CanCmdDataTree_Insert_AlternateForm));
				// <item command="CmdDataTree_Insert_Etymology" defaultVisible="false" />
				insertMenuDictionary.Add(Command.CmdDataTree_Insert_Etymology, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Etymology_Clicked, () => CanCmdDataTree_Insert_Etymology));
				// <item command="CmdDataTree_Insert_Pronunciation" defaultVisible="false" />
				insertMenuDictionary.Add(Command.CmdDataTree_Insert_Pronunciation, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Pronunciation_Clicked, () => CanCmdDataTree_Insert_Pronunciation));
				// <item command="CmdInsertExtNote" defaultVisible="false" />
				insertMenuDictionary.Add(Command.CmdInsertExtNote, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_ExtendedNote_Clicked, () => CanCmdInsertExtNote));
				// <item command="CmdInsertPicture" defaultVisible="false" />
				insertMenuDictionary.Add(Command.CmdInsertPicture, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Picture_Clicked, () => CanCmdInsertPicture));
				// <item command="CmdInsertVariant" defaultVisible="false" />
				insertMenuDictionary.Add(Command.CmdInsertVariant, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Variant_Clicked, () => CanCmdInsertVariant));
				// <item command="CmdInsertMediaFile" defaultVisible="false" />
				insertMenuDictionary.Add(Command.CmdInsertMediaFile, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Insert_Sound_Or_Movie_File_Clicked, () => CanCmdInsertMediaFile));

				// Was: LexiconEditToolToolsMenuManager
				var toolsMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Tools];
				// <command id="CmdConfigureDictionary" label="Configure {0}" message="ConfigureDictionary"/>
				toolsMenuDictionary.Add(Command.CmdConfigureDictionary, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Tools_Configure_Dictionary_Clicked, () => CanCmdConfigureDictionary));
				// <item command="CmdMergeEntry" defaultVisible="false"/>
				toolsMenuDictionary.Add(Command.CmdMergeEntry, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Merge_With_Entry_Clicked, () => CanCmdMergeEntry));

				// Was: LexiconEditToolToolbarManager (Blended in, above.)

				// Slice stack from LexEntry.fwlayout (less senses, which are handled in another manager class).
				Register_After_CitationForm_Bundle();
				Register_Pronunciation_Bundle();
				Register_Etymologies_Bundle();
				Register_CurrentLexReferences_Bundle();
				RegisterHotLinkMenus();
				RegisterSliceLeftEdgeMenus();
				// Slice stack for the various MoForm instances here and there in a LexEntry.
				Register_LexemeForm_Bundle();
				// CitationForm has a right-click menu.
				Register_CitationForm_Bundle();
				Register_Forms_Sections_Bundle();
				_dataTree.DataTreeStackContextMenuFactory.MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(AreaServices.PanelMenuId, CreateMainPanelContextMenuStrip);
				// Now, it is fine to finish up the initialization of the managers, since all shared event handlers are in '_sharedEventHandlers'.
				_rightClickContextMenuManager = new RightClickContextMenuManager(_majorFlexComponentParameters, toolUiWidgetParameterObject.Tool, _dataTree, _recordList);
			}

			private static void InsertPair(IDictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> toolBarDictionary, IDictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> menuDictionary, Command key, Tuple<EventHandler, Func<Tuple<bool, bool>>> currentTuple)
			{
				toolBarDictionary.Add(key, currentTuple);
				menuDictionary.Add(key, currentTuple);
			}

			private static readonly Tuple<bool, bool> CanCmdGoToEntry = new Tuple<bool, bool>(true, true);

			private void GoToEntry_Clicked(object sender, EventArgs e)
			{
				using (var dlg = new EntryGoDlg())
				{
					dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
					var windowParameters = new WindowParams
					{
						m_btnText = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go),
						m_label = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go_To),
						m_title = LexiconResources.Go_To_Entry_Dlg_Title
					};
					dlg.SetDlgInfo(_cache, windowParameters);
					dlg.SetHelpTopic("khtpFindLexicalEntry");
					if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK)
					{
						_recordList.JumpToRecord(dlg.SelectedObject.Hvo);
					}
				}
			}

			private static Tuple<bool, bool> CanShowHiddenFields => new Tuple<bool, bool>(true, true);

			private void Show_Hidden_Fields_Clicked(object sender, EventArgs e)
			{
				var menuItem = (ToolStripMenuItem)sender;
				menuItem.Checked = !menuItem.Checked;
				_propertyTable.SetProperty(_extendedPropertyName, menuItem.Checked, true, settingsGroup: SettingsGroup.LocalSettings);
				_publisher.Publish("ShowHiddenFields", menuItem.Checked);
				InnerMultiPane.Panel1Collapsed = !menuItem.Checked;
			}

			private void ShowHiddenFields_Handler(object obj)
			{
				var hiddenFieldsMenu = (ToolStripMenuItem)_majorFlexComponentParameters.UiWidgetController.ViewMenuDictionary[Command.ShowHiddenFields];
				hiddenFieldsMenu.Checked = (bool)obj;
			}

			private static Tuple<bool, bool> CanShow_DictionaryPubPreview => new Tuple<bool, bool>(true, true);

			private void Show_Dictionary_Preview_Clicked(object sender, EventArgs e)
			{
				var menuItem = (ToolStripMenuItem)sender;
				menuItem.Checked = !menuItem.Checked;
				_propertyTable.SetProperty(Show_DictionaryPubPreview, menuItem.Checked, true, settingsGroup: SettingsGroup.LocalSettings);
				InnerMultiPane.Panel1Collapsed = !menuItem.Checked;
			}

			private void DataTreeMerge_Clicked(object sender, EventArgs e)
			{
				var currentSlice = _dataTree.CurrentSlice;
				currentSlice.HandleMergeCommand(true);
			}

			private void DataTreeSplit_Clicked(object sender, EventArgs e)
			{
				var currentSlice = _dataTree.CurrentSlice;
				currentSlice.HandleSplitCommand();
			}

			private static Tuple<bool, bool> CanCmdInsertLexEntry => new Tuple<bool, bool>(true, true);

			private void Insert_Entry_Clicked(object sender, EventArgs e)
			{
				using (var dlg = new InsertEntryDlg())
				{
					dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
					dlg.SetDlgInfo(_cache, PersistenceProviderFactory.CreatePersistenceProvider(_propertyTable));
					if (dlg.ShowDialog((Form)_mainWnd) != DialogResult.OK)
					{
						return;
					}
					ILexEntry entry;
					bool newby;
					dlg.GetDialogInfo(out entry, out newby);
					// No need for a PropChanged here because InsertEntryDlg takes care of that. (LT-3608)
					_mainWnd.RefreshAllViews();
					_recordList.JumpToRecord(entry.Hvo);
				}
			}

			private static Tuple<bool, bool> CanCmdInsertSense => new Tuple<bool, bool>(true, true);

			private void Insert_Sense_Clicked(object sender, EventArgs e)
			{
				LexSenseUi.CreateNewLexSense(_cache, (ILexEntry)_recordList.CurrentObject);
			}

			private bool IsCommonVisible
			{
				get
				{
					var currentSlice = _dataTree.CurrentSlice;
					if (currentSlice == null)
					{
						_dataTree.GotoFirstSlice();
						currentSlice = _dataTree.CurrentSlice;
					}
					if (currentSlice.MyCmObject == null)
					{
						return false;
					}
					var sliceObject = currentSlice.MyCmObject;
					if (sliceObject is ILexSense)
					{
						return true;
					}
					// "owningSense" will be null, if 'sliceObject' is owned by the entry, but not a sense.
					var owningSense = sliceObject.OwnerOfClass<ILexSense>();
					if (owningSense == null)
					{
						return false;
					}
					// We now know that the current slice is a sense or is 'owned' by a sense,
					// so enable the Insert menus that are related to a sense.
					return true;
				}
			}

			private Tuple<bool, bool> CanCmdInsertSubsense => new Tuple<bool, bool>(IsCommonVisible, true);

			private void Insert_Subsense_Clicked(object sender, EventArgs e)
			{
				var owningSense = _dataTree.CurrentSlice.MyCmObject as ILexSense ?? _dataTree.CurrentSlice.MyCmObject.OwnerOfClass<ILexSense>();
				LexSenseUi.CreateNewLexSense(_cache, owningSense);
			}

			private static Tuple<bool, bool> CanCmdDataTree_Insert_AlternateForm => new Tuple<bool, bool>(true, true);

			private void Insert_Allomorph_Clicked(object sender, EventArgs e)
			{
				var lexEntry = (ILexEntry)_recordList.CurrentObject;
				UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoInsert, LcmUiStrings.ksRedoInsert, _cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					_cache.DomainDataByFlid.MakeNewObject(lexEntry.GetDefaultClassForNewAllomorph(), lexEntry.Hvo, LexEntryTags.kflidAlternateForms, lexEntry.AlternateFormsOS.Count);
				});
			}

			private static Tuple<bool, bool> CanCmdDataTree_Insert_Etymology => new Tuple<bool, bool>(true, true);

			private void Insert_Etymology_Clicked(object sender, EventArgs e)
			{
				UndoableUnitOfWorkHelper.Do(LexiconResources.Undo_Insert_Etymology, LexiconResources.Redo_Insert_Etymology, _cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					((ILexEntry)_recordList.CurrentObject).EtymologyOS.Add(_cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create());
				});
			}

			private static Tuple<bool, bool> CanCmdDataTree_Insert_Pronunciation => new Tuple<bool, bool>(true, true);

			private void Insert_Pronunciation_Clicked(object sender, EventArgs e)
			{
				var lexEntry = (ILexEntry)_recordList.CurrentObject;
				UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoInsert, LcmUiStrings.ksRedoInsert, _cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					_cache.DomainDataByFlid.MakeNewObject(LexPronunciationTags.kClassId, lexEntry.Hvo, LexEntryTags.kflidPronunciations, lexEntry.PronunciationsOS.Count);
					// Forces them to be created (lest it try to happen while displaying the new object in PropChanged).
					var dummy = _cache.LangProject.DefaultPronunciationWritingSystem;
				});
			}

			private Tuple<bool, bool> CanCmdInsertExtNote => new Tuple<bool, bool>(IsCommonVisible, true);

			private void Insert_ExtendedNote_Clicked(object sender, EventArgs e)
			{
				var owningSense = _dataTree.CurrentSlice.MyCmObject as ILexSense ?? _dataTree.CurrentSlice.MyCmObject.OwnerOfClass<ILexSense>();
				UndoableUnitOfWorkHelper.Do(LexiconResources.Undo_Create_Extended_Note, LexiconResources.Redo_Create_Extended_Note, owningSense, () =>
				{
					var extendedNote = _cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
					owningSense.ExtendedNoteOS.Add(extendedNote);
				});
			}

			private Tuple<bool, bool> CanCmdInsertPicture => new Tuple<bool, bool>(IsCommonVisible, true);

			private void Insert_Picture_Clicked(object sender, EventArgs e)
			{
				var owningSense = _dataTree.CurrentSlice.MyCmObject as ILexSense ?? _dataTree.CurrentSlice.MyCmObject.OwnerOfClass<ILexSense>();
				var app = _propertyTable.GetValue<IFlexApp>(LanguageExplorerConstants.App);
				using (var dlg = new PicturePropertiesDialog(_cache, null, _propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), app, true))
				{
					if (dlg.Initialize())
					{
						dlg.UseMultiStringCaption(_cache, WritingSystemServices.kwsVernAnals, FwUtils.StyleSheetFromPropertyTable(_propertyTable));
						if (dlg.ShowDialog() == DialogResult.OK)
						{
							UndoableUnitOfWorkHelper.Do(LexiconResources.ksUndoInsertPicture, LexiconResources.ksRedoInsertPicture, owningSense, () =>
							{
								const string defaultPictureFolder = CmFolderTags.DefaultPictureFolder;
								var picture = _cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
								owningSense.PicturesOS.Add(picture);
								dlg.GetMultilingualCaptionValues(picture.Caption);
								picture.UpdatePicture(dlg.CurrentFile, null, defaultPictureFolder, 0);
							});
						}
					}
				}
			}

			private static Tuple<bool, bool> CanCmdInsertVariant => new Tuple<bool, bool>(true, true);

			private void Insert_Variant_Clicked(object sender, EventArgs e)
			{
				using (var dlg = new InsertVariantDlg())
				{
					dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
					var entOld = (ILexEntry)_dataTree.Root;
					dlg.SetHelpTopic("khtpInsertVariantDlg");
					dlg.SetDlgInfo(_cache, entOld);
					dlg.ShowDialog();
				}
			}

			private static Tuple<bool, bool> CanCmdInsertMediaFile => new Tuple<bool, bool>(true, true);

			private void Insert_Sound_Or_Movie_File_Clicked(object sender, EventArgs e)
			{
				const string insertMediaFileLastDirectory = "InsertMediaFile-LastDirectory";
				var lexEntry = (ILexEntry)_recordList.CurrentObject;
				var createdMediaFile = false;
				using (var unitOfWorkHelper = new UndoableUnitOfWorkHelper(_cache.ActionHandlerAccessor, LexiconResources.ksUndoInsertMedia, LexiconResources.ksRedoInsertMedia))
				{
					if (!lexEntry.PronunciationsOS.Any())
					{
						// Ensure that the pronunciation writing systems have been initialized.
						// Otherwise, the crash reported in FWR-2086 can happen!
						lexEntry.PronunciationsOS.Add(_cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create());
					}
					var firstPronunciation = lexEntry.PronunciationsOS[0];
					using (var dlg = new OpenFileDialogAdapter())
					{
						dlg.InitialDirectory = _propertyTable.GetValue(insertMediaFileLastDirectory, _cache.LangProject.LinkedFilesRootDir);
						dlg.Filter = ResourceHelper.BuildFileFilter(FileFilterType.AllAudio, FileFilterType.AllVideo, FileFilterType.AllFiles);
						dlg.FilterIndex = 1;
						if (String.IsNullOrEmpty(dlg.Title) || dlg.Title == "*kstidInsertMediaChooseFileCaption*")
						{
							dlg.Title = LexiconResources.ChooseSoundOrMovieFile;
						}
						dlg.RestoreDirectory = true;
						dlg.CheckFileExists = true;
						dlg.CheckPathExists = true;
						dlg.Multiselect = true;

						var dialogResult = DialogResult.None;
						var helpProvider = _propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider);
						var linkedFilesRootDir = _cache.LangProject.LinkedFilesRootDir;
						var mediaFactory = _cache.ServiceLocator.GetInstance<ICmMediaFactory>();
						while (dialogResult != DialogResult.OK && dialogResult != DialogResult.Cancel)
						{
							dialogResult = dlg.ShowDialog();
							if (dialogResult == DialogResult.OK)
							{
								var fileNames = MoveOrCopyFilesController.MoveCopyOrLeaveMediaFiles(dlg.FileNames, linkedFilesRootDir, helpProvider);
								var mediaFolderName = StringTable.Table.GetString("kstidMediaFolder");
								if (String.IsNullOrEmpty(mediaFolderName) || mediaFolderName == "*kstidMediaFolder*")
								{
									mediaFolderName = CmFolderTags.LocalMedia;
								}
								foreach (var fileName in fileNames.Where(f => !String.IsNullOrEmpty(f)))
								{
									var media = mediaFactory.Create();
									firstPronunciation.MediaFilesOS.Add(media);
									media.MediaFileRA = DomainObjectServices.FindOrCreateFile(DomainObjectServices.FindOrCreateFolder(_cache, LangProjectTags.kflidMedia, mediaFolderName), fileName);
								}
								createdMediaFile = true;
								var selectedFileName = dlg.FileNames.FirstOrDefault(f => !String.IsNullOrEmpty(f));
								if (selectedFileName != null)
								{
									_propertyTable.SetProperty(insertMediaFileLastDirectory, Path.GetDirectoryName(selectedFileName), true);
								}
							}
						}
						// If we didn't create any ICmMedia instances, then roll back the UOW, even if it created a new ILexPronunciation.
						unitOfWorkHelper.RollBack = !createdMediaFile;
					}
				}
			}

			private static Tuple<bool, bool> CanCmdConfigureDictionary => new Tuple<bool, bool>(true, true);

			private void Tools_Configure_Dictionary_Clicked(object sender, EventArgs e)
			{
				if (DictionaryConfigurationDlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters, (Form)_mainWnd, _recordList.CurrentObject, "khtpConfigureDictionary", LanguageExplorerResources.Dictionary))
				{
					_mainWnd.RefreshAllViews();
				}
			}

			private Tuple<bool, bool> CanCmdMergeEntry
			{
				get
				{
					var enabled = true;
					var currentObject = _recordList.CurrentObject;
					if (currentObject == null)
					{
						enabled = false; // should never happen, but nothing we can do if it does!
					}
					var currentEntry = currentObject as ILexEntry ?? currentObject.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
					if (currentEntry == null)
					{
						enabled = false;
					}
					return new Tuple<bool, bool>(true, enabled);
				}
			}

			private void Merge_With_Entry_Clicked(object sender, EventArgs e)
			{
				using (var dlg = new MergeEntryDlg())
				{
					var currentObject = _recordList.CurrentObject;
					var currentEntry = currentObject as ILexEntry ?? currentObject.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
					dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
					// <parameters title="Merge Entry" formlabel="_Find:" okbuttonlabel="_Merge"/>
					dlg.SetDlgInfo(_cache, XElement.Parse(LexiconResources.MatchingEntriesParameters), currentEntry, LexiconResources.ksMergeEntry, FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Find), FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Merge));
					if (dlg.ShowDialog((Form)_mainWnd) != DialogResult.OK)
					{
						return;
					}
					var survivor = (ILexEntry)dlg.SelectedObject;
					Debug.Assert(survivor != currentEntry);
					UndoableUnitOfWorkHelper.Do(LexiconResources.ksUndoMergeEntry, LexiconResources.ksRedoMergeEntry, _cache.ActionHandlerAccessor, () =>
					{
						survivor.MergeObject(currentEntry, true);
						survivor.DateModified = DateTime.Now;
					});
					MessageBox.Show((Form)_mainWnd, LexiconResources.ksEntriesHaveBeenMerged, LexiconResources.ksMergeReport, MessageBoxButtons.OK, MessageBoxIcon.Information);
					LinkHandler.PublishFollowLinkMessage(_publisher, new FwLinkArgs(null, survivor.Guid));
				}
			}

			#region After_CitationForm_Bundle

			/// <summary>
			/// Starts after the Citation Form slice and goes to (but not including) the Pronunciation bundle.
			/// </summary>
			private void Register_After_CitationForm_Bundle()
			{
				#region left edge menus

				// <part ref="ComplexFormEntries" visibility="always"/>
				// and
				// <part ref="ComponentLexemes"/>
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuReorderVector, Create_mnuReorderVector);

				// <part id="LexEntryRef-Detail-VariantEntryTypes" type="Detail">
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_VariantSpec, Create_mnuDataTree_VariantSpec);

				// <part id="LexEntryRef-Detail-ComplexEntryTypes" type="Detail">
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_ComplexFormSpec, Create_mnuDataTree_ComplexFormSpec);

				#endregion left edge menus

				#region hotlinks
				// No hotlinks
				#endregion hotlinks
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuReorderVector(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuReorderVector, $"Expected argument value of '{ContextMenuName.mnuReorderVector.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuReorderVector">
				// This menu and its commands are shared
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuReorderVector.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

				ToolStripMenuItem menu;
				var referenceVectorSlice = (ReferenceVectorSlice)slice;
				bool visible;
				var enabled = referenceVectorSlice.CanDisplayMoveTargetDownInSequence(out visible);
				if (visible)
				{
					// <command id="CmdMoveTargetToPreviousInSequence" label="Move Left" message="MoveTargetDownInSequence"/>
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveReferencedTargetDownInSequence_Clicked, AreaResources.Move_Left);
					menu.Enabled = enabled;
				}
				enabled = referenceVectorSlice.CanDisplayMoveTargetUpInSequence(out visible);
				if (visible)
				{
					// <command id="CmdMoveTargetToNextInSequence" label="Move Right" message="MoveTargetUpInSequence"/>
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveReferencedTargetUpInSequence_Clicked, AreaResources.Move_Right);
					menu.Enabled = enabled;
				}
				if (referenceVectorSlice.CanAlphabetize)
				{
					// <command id="CmdAlphabeticalOrder" label="Alphabetical Order" message="AlphabeticalOrder"/>
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Referenced_AlphabeticalOrder_Clicked, LexiconResources.Alphabetical_Order);
				}
				// End: <menu id="mnuReorderVector">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_VariantSpec(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_VariantSpec, $"Expected argument value of '{ContextMenuName.mnuDataTree_VariantSpec.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_VariantSpec">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_VariantSpec.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

				using (var imageHolder = new ImageHolder())
				{
					ToolStripMenuItem menu;
					bool visible;
					var enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);
					if (visible)
					{
						// <command id="CmdDataTree_MoveUp_VariantSpec" label="Move Variant Info Up" message="MoveUpObjectInSequence" icon="MoveUp"/>
						menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Variant_Info_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
						menu.Enabled = enabled;
					}
					enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
					if (visible)
					{
						// <command id="CmdDataTree_MoveDown_VariantSpec" label="Move Variant Info Down" message="MoveDownObjectInSequence" icon="MoveDown"/>
						menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Variant_Info_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
						menu.Enabled = enabled;
					}
				}

				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDataTree_Insert_VariantSpec" label="Add another Variant Info section" message="DataTreeInsert">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_VariantSpec_Clicked, LexiconResources.Add_another_Variant_Info_section, LexiconResources.Add_another_Variant_Info_section_Tooltip);

				// <command id="CmdDataTree_Delete_VariantSpec" label="Delete Variant Info" message="DataTreeDelete" icon="Delete"/>
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Variant_Info, Delete_this_Foo_Clicked);

				// End: <menu id="mnuDataTree_VariantSpec">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Insert_VariantSpec_Clicked(object sender, EventArgs e)
			{
				/*
				<command id="CmdDataTree_Insert_VariantSpec" label="Add another Variant Info section" message="DataTreeInsert">
					<parameters field="EntryRefs" className="LexEntryRef" ownerClass="LexEntry" />
				</command>
				*/
				_dataTree.CurrentSlice.HandleInsertCommand("EntryRefs", LexEntryRefTags.kClassName, LexEntryTags.kClassName);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ComplexFormSpec(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_ComplexFormSpec, $"Expected argument value of '{ContextMenuName.mnuDataTree_ComplexFormSpec.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_ComplexFormSpec">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_ComplexFormSpec.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <command id="CmdDataTree_Delete_ComplexFormSpec" label="Delete Complex Form Info" message="DataTreeDelete" icon="Delete"/>
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Complex_Form_Info, Delete_this_Foo_Clicked);

				// End: <menu id="mnuDataTree_ComplexFormSpec">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Delete_this_Foo_Clicked(object sender, EventArgs e)
			{
				DeleteSliceObject();
			}

			#endregion After_CitationForm_Bundle

			#region Pronunciation_Bundle

			private void Register_Pronunciation_Bundle()
			{
				// Only one slice has menus, but several have chooser dlgs.
				// <part ref="Pronunciations" param="Normal" visibility="ifdata"/>
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Pronunciation, Create_mnuDataTree_Pronunciation);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Pronunciation(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_AlternateForms, $"Expected argument value of '{ContextMenuName.mnuDataTree_AlternateForms.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Pronunciation">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_AlternateForms.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);
				// <item command="CmdDataTree_Insert_Pronunciation"/>
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Pronunciation_Clicked, LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);
				// <item command="CmdInsertMediaFile" label="Insert _Sound or Movie" defaultVisible="false"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sound_Or_Movie_File_Clicked, LexiconResources.Insert_Sound_or_Movie, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip);
				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				using (var imageHolder = new ImageHolder())
				{
					bool visible;
					var enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);
					if (visible)
					{
						// <command id="CmdDataTree_MoveUp_Pronunciation" label="Move Pronunciation _Up" message="MoveUpObjectInSequence" icon="MoveUp">
						menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Pronunciation_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
						menu.Enabled = enabled;
					}
					enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
					if (visible)
					{
						// <command id="CmdDataTree_MoveDown_Pronunciation" label="Move Pronunciation _Down" message="MoveDownObjectInSequence" icon="MoveDown">
						menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Pronunciation_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
						menu.Enabled = enabled;
					}
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDataTree_Delete_Pronunciation" label="Delete this Pronunciation" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_this_Pronunciation, Delete_this_Foo_Clicked);

				// Not added here. It is added by the slice, along with the generic slice menus.
				// <item label="-" translate="do not translate"/>

				// End: <menu id="mnuDataTree_Pronunciation>

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			#endregion Pronunciation_Bundle

			#region Etymologies_Bundle

			private void Register_Etymologies_Bundle()
			{
				// Register the etymology hotlinks.
				_dataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_Etymology_Hotlinks, Create_mnuDataTree_Etymology_Hotlinks);

				// <part ref="Etymologies" param="Normal" visibility="ifdata" />
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Etymology, Create_mnuDataTree_Etymology);
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Etymology_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_Etymology_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_Etymology_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
				// <item command="CmdDataTree_Insert_Etymology"/>
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Etymology_Clicked, LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

				return hotlinksMenuItemList;
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Etymology(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Etymology, $"Expected argument value of '{ContextMenuName.mnuDataTree_Etymology.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Etymology">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Etymology.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

				// <item command="CmdDataTree_Insert_Etymology" label="Insert _Etymology"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Etymology_Clicked, LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				using (var imageHolder = new ImageHolder())
				{
					// <command id="CmdDataTree_MoveUp_Etymology" label="Move Etymology _Up" message="MoveUpObjectInSequence" icon="MoveUp">
					//	<parameters field="Etymology" className="LexEtymology"/>
					var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Etymology_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
					bool visible;
					menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);

					// <command id="CmdDataTree_MoveDown_Etymology" label="Move Etymology _Down" message="MoveDownObjectInSequence" icon="MoveDown">
					//	<parameters field="Etymology" className="LexEtymology"/>
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Etymology_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
					menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDataTree_Delete_Etymology" label="Delete this Etymology" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_this_Etymology, Delete_this_Foo_Clicked);

				// End: <menu id="mnuDataTree_Etymology">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			#endregion Etymologies_Bundle

			#region CurrentLexReferences_Bundle

			private void Register_CurrentLexReferences_Bundle()
			{
				// The LexReferenceMultiSlice class potentially generates new slice xml information, including a couple left-edge menus.
				// Those two menu factory methods are registered here.

				// "mnuDataTree_DeleteAddLexReference"
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_DeleteAddLexReference, Create_mnuDataTree_DeleteAddLexReference);

				// "mnuDataTree_DeleteReplaceLexReference"
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_DeleteReplaceLexReference, Create_mnuDataTree_DeleteReplaceLexReference);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_DeleteAddLexReference(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_DeleteAddLexReference, $"Expected argument value of '{ContextMenuName.mnuDataTree_DeleteAddLexReference.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_DeleteAddLexReference">
				// This menu and its commands are shared
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_DeleteAddLexReference.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

				// <command id="CmdDataTree_Delete_LexReference" label="Delete Relation" message="DataTreeDelete" icon="Delete" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Relation, DataTreeDelete_LexReference_Clicked);

				// <command id="CmdDataTree_Add_ToLexReference" label="Add Reference" message="DataTreeAddReference" />
				CreateAdd_Replace_LexReferenceMenu(menuItems, contextMenuStrip, slice, LanguageExplorerResources.ksIdentifyRecord);

				// <command id="CmdDataTree_EditDetails_LexReference" label="Edit Reference Set Details" message="DataTreeEdit" />
				Create_Edit_LexReferenceMenu(menuItems, contextMenuStrip, slice);

				// End: <menu id="mnuDataTree_DeleteAddLexReference">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_DeleteReplaceLexReference(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_DeleteReplaceLexReference, $"Expected argument value of '{ContextMenuName.mnuDataTree_DeleteReplaceLexReference.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_DeleteReplaceLexReference">
				// This menu and its commands are shared
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_DeleteReplaceLexReference.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

				// <command id="CmdDataTree_Delete_LexReference" label="Delete Relation" message="DataTreeDelete" icon="Delete" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Relation, DataTreeDelete_LexReference_Clicked);

				// <command id="CmdDataTree_Replace_LexReference" label="Replace Reference" message="DataTreeAddReference" />
				CreateAdd_Replace_LexReferenceMenu(menuItems, contextMenuStrip, slice, LexiconResources.ksReplaceXEntry);

				// <command id="CmdDataTree_EditDetails_LexReference" label="Edit Reference Set Details" message="DataTreeEdit" />
				Create_Edit_LexReferenceMenu(menuItems, contextMenuStrip, slice);

				// End: <menu id="mnuDataTree_DeleteReplaceLexReference">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void DataTreeDelete_LexReference_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleDeleteCommand();
			}

			private void DataTreeAddReference_LexReference_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleLaunchChooser();
			}

			private void DataTree_Edit_LexReference_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleEditCommand();
			}

			private void CreateAdd_Replace_LexReferenceMenu(List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ContextMenuStrip contextMenuStrip, Slice slice, string menuText)
			{
				// Always visible and enabled.
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, DataTreeAddReference_LexReference_Clicked, menuText);
			}

			private void Create_Edit_LexReferenceMenu(List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ContextMenuStrip contextMenuStrip, Slice slice)
			{
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, DataTree_Edit_LexReference_Clicked, LexiconResources.ksRedoEditRefSetDetails);
				menu.Enabled = slice.CanEditNow;
			}

			#endregion CurrentLexReferences_Bundle

			private void DeleteSliceObject()
			{
				var currentSlice = _dataTree.CurrentSlice;
				if (currentSlice.MyCmObject.IsValidObject)
				{
					currentSlice.HandleDeleteCommand();
				}
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
			{
				// <menu id="PaneBar_LexicalDetail" label="">
				// <menu id="LexEntryPaneMenu" icon="MenuWidget">
				// Handled elsewhere: <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_lexiconEdit" defaultVisible="true" settingsGroup="local"/>
				var contextMenuStrip = new ContextMenuStrip();

				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
				var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);

				// Show_Dictionary_Preview menu item.
				_show_DictionaryPubPreviewContextMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Show_Dictionary_Preview_Clicked, LexiconResources.Show_DictionaryPubPreview, LexiconResources.Show_DictionaryPubPreview_ToolTip);
				_show_DictionaryPubPreviewContextMenu.Checked = _propertyTable.GetValue<bool>(Show_DictionaryPubPreview);

				// Separator
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// Insert_Sense menu item. (CmdInsertSense->msg: DataTreeInsert, also on Insert menu)
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sense_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

				// Insert Subsense (in sense) menu item. (CmdInsertSubsense->msg: DataTreeInsert, also on Insert menu)
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Subsense_Clicked, LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

				// Insert _Variant menu item. (CmdInsertVariant->msg: InsertItemViaBackrefVector, also on Insert menu)
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Variant_Clicked, LexiconResources.Insert_Variant, LexiconResources.Insert_Variant_Tooltip);

				// Insert A_llomorph menu item. (CmdDataTree_Insert_AlternateForm->msg: DataTreeInsert, also on Insert menu)
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Allomorph_Clicked, LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip);

				// Insert _Pronunciation menu item. (CmdDataTree_Insert_Pronunciation->msg: DataTreeInsert, also on Insert menu)
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Pronunciation_Clicked, LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);

				// Insert Sound or Movie _File menu item. (CmdInsertMediaFile->msg: InsertMediaFile, also on Insert menu)
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sound_Or_Movie_File_Clicked, LexiconResources.Insert_Sound_Or_Movie_File, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip);

				// Insert _Etymology menu item. (CmdDataTree_Insert_Etymology->msg: DataTreeInsert, also on Insert menu and a hotlionks and another context menu.)
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Etymology_Clicked, LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

				// Separator
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// Lexeme Form has components. (CmdChangeToComplexForm->msg: ConvertEntryIntoComplexForm)
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdChangeToComplexForm_Clicked, LexiconResources.Lexeme_Form_Has_Components, LexiconResources.Lexeme_Form_Has_Components_Tooltip);

				// Lexeme Form is a variant menu item. (CmdChangeToVariant->msg: ConvertEntryIntoVariant)
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdChangeToVariant_Clicked, LexiconResources.Lexeme_Form_Is_A_Variant, LexiconResources.Lexeme_Form_Is_A_Variant_Tooltip);

				// _Merge with entry... menu item. (CmdMergeEntry->msg: MergeEntry, also on Tool menu)
				var contextMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Merge_With_Entry_Clicked, LexiconResources.Merge_With_Entry, LexiconResources.Merge_With_Entry_Tooltip);
				// Original code that controlled: display.Enabled = display.Visible = InFriendlyArea;
				// It is now only in a friendly area, so should always be visible and enabled, per the old code.
				// Trouble is it makes no sense to enable it if the lexicon only has one entry in it, so I'll alter the behavior to be more sensible. ;-)
				contextMenuItem.Enabled = _cache.LanguageProject.LexDbOA.Entries.Any();

				// Separator
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// Show Entry in Concordance menu item. (CmdRootEntryJumpToConcordance->msg: JumpToTool)
				contextMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Entry_In_Concordance);
				contextMenuItem.Tag = new List<object> { _publisher, AreaServices.ConcordanceMachineName, _recordList.CurrentObject.Guid };

				return retVal;
			}

			private void CmdChangeToVariant_Clicked(object sender, EventArgs e)
			{
				// Lexeme Form is a variant menu item. (CmdChangeToVariant->msg: ConvertEntryIntoVariant)
				AddNewLexEntryRef(LexEntryRefTags.kflidVariantEntryTypes, LexiconResources.Lexeme_Form_Is_A_Variant);
			}

			private void CmdChangeToComplexForm_Clicked(object sender, EventArgs e)
			{
				// Lexeme Form has components. (CmdChangeToComplexForm->msg: ConvertEntryIntoComplexForm)
				AddNewLexEntryRef(LexEntryRefTags.kflidComplexEntryTypes, LexiconResources.Lexeme_Form_Has_Components);
			}

			private void AddNewLexEntryRef(int flidTypes, string uowBase)
			{
				UowHelpers.UndoExtension(uowBase, _cache.ActionHandlerAccessor, () =>
				{
					var entry = (ILexEntry)_recordList.CurrentObject;
					var ler = _cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
					if (flidTypes == LexEntryRefTags.kflidVariantEntryTypes)
					{
						entry.EntryRefsOS.Add(ler);
						const string unspecVariantEntryTypeGuid = "3942addb-99fd-43e9-ab7d-99025ceb0d4e";
						var type = entry.Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS.First(lrt => lrt.Guid.ToString() == unspecVariantEntryTypeGuid) as ILexEntryType;
						ler.VariantEntryTypesRS.Add(type);
						ler.RefType = LexEntryRefTags.krtVariant;
						ler.HideMinorEntry = 0;
					}
					else
					{
						entry.EntryRefsOS.Insert(0, ler);
						const string unspecComplexFormEntryTypeGuid = "fec038ed-6a8c-4fa5-bc96-a4f515a98c50";
						var type = entry.Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.First(lrt => lrt.Guid.ToString() == unspecComplexFormEntryTypeGuid) as ILexEntryType;
						ler.ComplexEntryTypesRS.Add(type);
						ler.RefType = LexEntryRefTags.krtComplexForm;
						ler.HideMinorEntry = 0; // LT-10928
						entry.ChangeRootToStem();
					}
				});
			}

			private void MoveReferencedTargetDownInSequence_Clicked(object sender, EventArgs e)
			{
				((ReferenceVectorSlice)_dataTree.CurrentSlice).MoveTargetDownInSequence();
			}

			private void MoveReferencedTargetUpInSequence_Clicked(object sender, EventArgs e)
			{
				((ReferenceVectorSlice)_dataTree.CurrentSlice).MoveTargetUpInSequence();
			}

			private void Referenced_AlphabeticalOrder_Clicked(object sender, EventArgs e)
			{
				((ReferenceVectorSlice)_dataTree.CurrentSlice).Alphabetize();
			}

			private void MoveUpObjectInOwningSequence_Clicked(object sender, EventArgs e)
			{
				var slice = _dataTree.CurrentSlice;
				var owningObject = slice.MyCmObject.Owner;
				var owningFlid = slice.MyCmObject.OwningFlid;
				var indexInOwningProperty = _cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, slice.MyCmObject.Hvo);
				if (indexInOwningProperty > 0)
				{
					// The slice might be invalidated by the MoveOwningSequence, so we get its
					// values first.  See LT-6670.
					// We found it in the sequence, and it isn't already the first.
					UndoableUnitOfWorkHelper.Do(AreaResources.UndoMoveItem, AreaResources.RedoMoveItem, _cache.ActionHandlerAccessor,
						() => _cache.DomainDataByFlid.MoveOwnSeq(owningObject.Hvo, (int)owningFlid, indexInOwningProperty, indexInOwningProperty, owningObject.Hvo, owningFlid, indexInOwningProperty - 1));
				}
			}

			private void MoveDownObjectInOwningSequence_Clicked(object sender, EventArgs e)
			{
				var slice = _dataTree.CurrentSlice;
				var owningObject = slice.MyCmObject.Owner;
				var owningFlid = slice.MyCmObject.OwningFlid;
				var count = _cache.DomainDataByFlid.get_VecSize(owningObject.Hvo, owningFlid);
				var indexInOwningProperty = _cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, slice.MyCmObject.Hvo);
				if (indexInOwningProperty >= 0 && indexInOwningProperty + 1 < count)
				{
					// The slice might be invalidated by the MoveOwningSequence, so we get its
					// values first.  See LT-6670.
					// We found it in the sequence, and it isn't already the last.
					// Quoting from VwOleDbDa.cpp, "Insert the selected records before the
					// DstStart object".  This means we need + 2 instead of + 1 for the
					// new location.
					UndoableUnitOfWorkHelper.Do(AreaResources.UndoMoveItem, AreaResources.RedoMoveItem, _cache.ActionHandlerAccessor,
						() => _cache.DomainDataByFlid.MoveOwnSeq(owningObject.Hvo, owningFlid, indexInOwningProperty, indexInOwningProperty, owningObject.Hvo, owningFlid, indexInOwningProperty + 2));
				}
			}

			#region hotlinks

			private void RegisterHotLinkMenus()
			{
				// mnuDataTree_ExtendedNote_Hotlinks
				_dataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_ExtendedNote_Hotlinks, Create_mnuDataTree_ExtendedNote_Hotlinks);

				// mnuDataTree_Sense_Hotlinks
				_dataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_Sense_Hotlinks, Create_mnuDataTree_Sense_Hotlinks);
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_ExtendedNote_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_ExtendedNote_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_ExtendedNote_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <command id="CmdDataTree_Insert_ExtNote" label="Insert Extended Note" message="DataTreeInsert">
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_ExtendedNote_Clicked, LexiconResources.Insert_Extended_Note);

				return hotlinksMenuItemList;
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Sense_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_Sense_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_Sense_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

				//<command id="CmdDataTree_Insert_Example" label="Insert _Example" message="DataTreeInsert">
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Example_Clicked, LexiconResources.Insert_Example);

				// <item command="CmdDataTree_Insert_SenseBelow"/>
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_SenseBelow_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

				return hotlinksMenuItemList;
			}

			private void Insert_Example_Clicked(object sender, EventArgs e)
			{
				UowHelpers.UndoExtension(LexiconResources.Insert_Example, _cache.ActionHandlerAccessor, () =>
				{
					var sense = (ILexSense)_dataTree.CurrentSlice.MyCmObject;
					sense.ExamplesOS.Add(_cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create());
				});
			}

			private void Insert_Translation_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("Translations", CmTranslationTags.kClassName, LexExampleSentenceTags.kClassName);
			}

			private void Insert_SenseBelow_Clicked(object sender, EventArgs e)
			{
				// Get slice and see what sense is currently selected, so we can add the new sense after (read: 'below") it.
				var currentSlice = _dataTree.CurrentSlice;
				ILexSense currentSense;
				while (true)
				{
					var currentObject = currentSlice.MyCmObject;
					if (currentObject is ILexSense)
					{
						currentSense = (ILexSense)currentObject;
						break;
					}
					currentSlice = currentSlice.ParentSlice;
				}
				if (currentSense.Owner is ILexSense)
				{
					var owningSense = (ILexSense)currentSense.Owner;
					LexSenseUi.CreateNewLexSense(_cache, owningSense, owningSense.SensesOS.IndexOf(currentSense) + 1);
				}
				else
				{
					var owningEntry = (ILexEntry)_recordList.CurrentObject;
					LexSenseUi.CreateNewLexSense(_cache, owningEntry, owningEntry.SensesOS.IndexOf(currentSense) + 1);
				}
			}

			#endregion hotlinks

			#region slice context menus

			private void RegisterSliceLeftEdgeMenus()
			{
				// mnuDataTree_Sense
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Sense, Create_mnuDataTree_Sense);

				// mnuDataTree_Example
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Example, Create_mnuDataTree_Example);

				// <menu id="mnuDataTree_ExtendedNotes">
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_ExtendedNotes, Create_mnuDataTree_ExtendedNotes);

				// <menu id="mnuDataTree_ExtendedNote">
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_ExtendedNote, Create_mnuDataTree_ExtendedNote);

				// <menu id="mnuDataTree_ExtendedNote_Examples">
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_ExtendedNote_Examples, Create_mnuDataTree_ExtendedNote_Examples);

				// <menu id="mnuDataTree_Picture">
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Picture, Create_mnuDataTree_Picture);

				// NB: I don't see "SubSenses" in shipping code.
				// <menu id="mnuDataTree_Subsenses">
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Subsenses, Create_mnuDataTree_Subsenses);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ExtendedNotes(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_ExtendedNotes, $"Expected argument value of '{ContextMenuName.mnuDataTree_ExtendedNotes.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_ExtendedNotes">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_ExtendedNotes.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <command id="CmdDataTree_Insert_ExtNote" label="Insert Extended Note" message="DataTreeInsert">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_ExtNote_Clicked, LexiconResources.Insert_Extended_Note);

				// End: <menu id="mnuDataTree_ExtendedNotes">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Insert_ExtNote_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("ExtendedNote", LexExtendedNoteTags.kClassName);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ExtendedNote(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_ExtendedNote, $"Expected argument value of '{ContextMenuName.mnuDataTree_ExtendedNote.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_ExtendedNote">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_ExtendedNote.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

				// <command id="CmdDataTree_Delete_ExtNote" label="Delete Extended Note" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Extended_Note, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				using (var imageHolder = new ImageHolder())
				{
					// <command id="CmdDataTree_MoveUp_ExtNote" label="Move Extended Note _Up" message="MoveUpObjectInSequence" icon="MoveUp">
					var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Extended_Note_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
					bool visible;
					menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);

					// <command id="CmdDataTree_MoveDown_ExtNote" label="Move Extended Note _Down" message="MoveDownObjectInSequence" icon="MoveDown">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Extended_Note_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
					menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDataTree_Insert_ExampleInNote" label="Insert Example in Note" message="DataTreeInsert">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_ExampleInNote_Clicked, LexiconResources.Insert_Example_in_Note);

				// End: <menu id="mnuDataTree_ExtendedNote">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Insert_ExampleInNote_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("Examples", LexExampleSentenceTags.kClassName, LexExtendedNoteTags.kClassName);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ExtendedNote_Examples(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_ExtendedNote_Examples, $"Expected argument value of '{ContextMenuName.mnuDataTree_ExtendedNote_Examples.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_ExtendedNote_Examples">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_ExtendedNote_Examples.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

				// <command id="CmdDataTree_Insert_ExampleInNote" label="Insert Example in Note" message="DataTreeInsert">
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_ExampleInNote_Clicked, LexiconResources.Insert_Example_in_Note);

				// <command id="CmdDataTree_Delete_ExampleInNote" label="Delete Example from Note" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Example_from_Note, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				using (var imageHolder = new ImageHolder())
				{
					// <command id="CmdDataTree_MoveUp_ExampleInNote" label="Move Example _Up" message="MoveUpObjectInSequence" icon="MoveUp">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Example_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
					bool visible;
					menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);

					// <command id="CmdDataTree_MoveDown_ExampleInNote" label="Move Example _Down" message="MoveDownObjectInSequence" icon="MoveDown">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Example_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
					menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
				}

				// End: <menu id="mnuDataTree_ExtendedNote_Examples">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Picture(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Picture, $"Expected argument value of '{ContextMenuName.mnuDataTree_Picture.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Picture">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Picture.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

				// <command id="CmdDataTree_Properties_Picture" label="Picture Properties" message="PictureProperties">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Properties_Picture_Clicked, LexiconResources.Picture_Properties);

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				using (var imageHolder = new ImageHolder())
				{
					// <command id="CmdDataTree_MoveUp_Picture" label="Move Picture _Up" message="MoveUpObjectInSequence" icon="MoveUp">
					var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Picture_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
					bool visible;
					menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);

					// <command id="CmdDataTree_MoveDown_Picture" label="Move Picture _Down" message="MoveDownObjectInSequence" icon="MoveDown">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Picture_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
					menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDataTree_Delete_Picture" label="Delete Picture" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Picture, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_Picture">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Properties_Picture_Clicked(object sender, EventArgs e)
			{
				var slice = _dataTree.CurrentSlice;
				var pictureSlices = new List<PictureSlice>();

				// Create an array of potential slices to call the showProperties method on.  If we're being called from a PictureSlice,
				// there's no need to go through the whole list, so we can be a little more intelligent
				if (slice is PictureSlice)
				{
					pictureSlices.Add(slice as PictureSlice);
				}
				else
				{
					foreach (var otherSlice in _dataTree.Slices)
					{
						if (otherSlice is PictureSlice && !ReferenceEquals(slice, otherSlice))
						{
							pictureSlices.Add(otherSlice as PictureSlice);
						}
					}
				}

				foreach (var pictureSlice in pictureSlices)
				{
					// Make sure the target slice refers to the same object that we do
					if (ReferenceEquals(pictureSlice.MyCmObject, slice.MyCmObject))
					{
						pictureSlice.showProperties();
						break;
					}
				}
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Subsenses(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Subsenses, $"Expected argument value of '{ContextMenuName.mnuDataTree_Subsenses.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Subsenses">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Subsenses.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_SubSense"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Subsense_Clicked, LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

				// End: <menu id="mnuDataTree_Subsenses">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Example(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Example, $"Expected argument value of '{ContextMenuName.mnuDataTree_Example.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Example">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Example.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(7);

				// <command id="CmdDataTree_Insert_Translation" label="Insert Translation" message="DataTreeInsert">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Translation_Clicked, LexiconResources.Insert_Translation);

				// <command id="CmdDataTree_Delete_Example" label="Delete Example" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Example, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				using (var imageHolder = new ImageHolder())
				{
					// <command id="CmdDataTree_MoveUp_Example" label="Move Example _Up" message="MoveUpObjectInSequence" icon="MoveUp">
					var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Example_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
					bool visible;
					menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);

					// <command id="CmdDataTree_MoveDown_Example" label="Move Example _Down" message="MoveDownObjectInSequence" icon="MoveDown">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Example_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
					menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdFindExampleSentence" label="Find example sentence..." message="LaunchGuiControl">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, FindExampleSentence_Clicked, LexiconResources.Find_example_sentence);

				// End: <menu id="mnuDataTree_Example">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Sense(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Sense, $"Expected argument value of '{ContextMenuName.mnuDataTree_Sense.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Sense">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Sense.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(21);

				//<command id="CmdDataTree_Insert_Example" label="Insert _Example" message="DataTreeInsert">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Example_Clicked, LexiconResources.Insert_Example);

				// <command id="CmdFindExampleSentence" label="Find example sentence..." message="LaunchGuiControl">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, FindExampleSentence_Clicked, LexiconResources.Find_example_sentence);

				// <item command="CmdDataTree_Insert_ExtNote"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_ExtendedNote_Clicked, LexiconResources.Insert_Extended_Note);

				// <item command="CmdDataTree_Insert_SenseBelow"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sense_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

				// <item command="CmdDataTree_Insert_SubSense"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Subsense_Clicked, LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

				// <item command="CmdInsertPicture" label="Insert _Picture" defaultVisible="false"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Picture_Clicked, LexiconResources.Insert_Picture, LexiconResources.Insert_Picture_Tooltip);

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdSenseJumpToConcordance" label="Show Sense in Concordance" message="JumpToTool">
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Sense_in_Concordance);
				menu.Tag = new List<object> { _publisher, AreaServices.ConcordanceMachineName, _recordList.CurrentObject.Guid };

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				using (var imageHolder = new ImageHolder())
				{
					// <command id="CmdDataTree_MoveUp_Sense" label="Move Sense Up" message="MoveUpObjectInSequence" icon="MoveUp">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Sense_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
					bool visible;
					menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);

					// <command id="CmdDataTree_MoveDown_Sense" label="Move Sense Down" message="MoveDownObjectInSequence" icon="MoveDown">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Sense_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
					menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);

					// <command id="CmdDataTree_MakeSub_Sense" label="Demote" message="DemoteSense" icon="MoveRight">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Demote_Sense_Clicked, AreaResources.Demote, image: imageHolder.smallCommandImages.Images[AreaServices.MoveRight]);
					menu.Enabled = CanDemoteSense(slice);

					// <command id="CmdDataTree_Promote_Sense" label="Promote" message="PromoteSense" icon="MoveLeft">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Promote_Sense_Clicked, AreaResources.Promote, image: imageHolder.smallCommandImages.Images[AreaServices.MoveLeft]);
					menu.Enabled = CanPromoteSense(slice);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDataTree_Merge_Sense" label="Merge Sense into..." message="DataTreeMerge">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, DataTreeMerge_Clicked, LexiconResources.Merge_Sense_into);
				menu.Enabled = slice.CanMergeNow;

				// <command id="CmdDataTree_Split_Sense" label="Move Sense to a New Entry" message="DataTreeSplit">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, DataTreeSplit_Clicked, LexiconResources.Move_Sense_to_a_New_Entry);
				menu.Enabled = slice.CanSplitNow;

				// <command id="CmdDataTree_Delete_Sense" label="Delete this Sense and any Subsenses" message="DataTreeDeleteSense" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.DeleteSenseAndSubsenses, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_Sense">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private bool CanDemoteSense(Slice currentSlice)
			{
				return currentSlice.MyCmObject is ILexSense && _cache.DomainDataByFlid.get_VecSize(currentSlice.MyCmObject.Owner.Hvo, currentSlice.MyCmObject.OwningFlid) > 1;
			}

			private void Demote_Sense_Clicked(object sender, EventArgs e)
			{
				UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoDemote, LanguageExplorerResources.ksRedoDemote, _cache.ActionHandlerAccessor, () =>
				{
					var sense = _dataTree.CurrentSlice.MyCmObject;
					var hvoOwner = sense.Owner.Hvo;
					var owningFlid = sense.OwningFlid;
					var ihvo = _cache.DomainDataByFlid.GetObjIndex(hvoOwner, owningFlid, sense.Hvo);
					var hvoNewOwner = _cache.DomainDataByFlid.get_VecItem(hvoOwner, owningFlid, (ihvo == 0) ? 1 : ihvo - 1);
					_cache.DomainDataByFlid.MoveOwnSeq(hvoOwner, owningFlid, ihvo, ihvo, hvoNewOwner, LexSenseTags.kflidSenses, _cache.DomainDataByFlid.get_VecSize(hvoNewOwner, LexSenseTags.kflidSenses));
				});
			}

			private static bool CanPromoteSense(Slice currentSlice)
			{
				var moveeSense = currentSlice.MyCmObject as ILexSense;
				var oldOwningSense = currentSlice.MyCmObject.Owner as ILexSense;
				// Can't promote top-level sense or something that isn't a sense.
				return moveeSense != null && oldOwningSense != null;
			}

			private void Promote_Sense_Clicked(object sender, EventArgs e)
			{
				UowHelpers.UndoExtension(AreaResources.Promote, _cache.ActionHandlerAccessor, () =>
				{
					var slice = _dataTree.CurrentSlice;
					var oldOwner = slice.MyCmObject.Owner;
					var index = oldOwner.IndexInOwner;
					var newOwner = oldOwner.Owner;
					if (newOwner is ILexEntry)
					{
						var newOwningEntry = (ILexEntry)newOwner;
						newOwningEntry.SensesOS.Insert(index + 1, slice.MyCmObject as ILexSense);
					}
					else
					{
						var newOwningSense = (ILexSense)newOwner;
						newOwningSense.SensesOS.Insert(index + 1, slice.MyCmObject as ILexSense);
					}
				});
			}

			private void FindExampleSentence_Clicked(object sender, EventArgs e)
			{
				using (var findExampleSentencesDlg = new FindExampleSentenceDlg(_majorFlexComponentParameters.StatusBar, _dataTree.CurrentSlice.MyCmObject, _recordList))
				{
					findExampleSentencesDlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
					findExampleSentencesDlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
				}
			}

			#endregion slice context menus

			#region LexemeForm_Bundle

			/// <summary>
			/// Register the various alternatives for the "Lexeme Form" bundle of slices.
			/// </summary>
			/// <remarks>
			/// This covers the first "Lexeme Form" slice up to, but not including, the "Citation Form" slice.
			/// </remarks>
			private void Register_LexemeForm_Bundle()
			{
				#region left edge menus

				// 1. <part id="MoForm-Detail-AsLexemeForm" type="Detail">
				//		Needs: menu="mnuDataTree_LexemeForm".
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_LexemeForm, Create_mnuDataTree_LexemeForm);
				// 2. <part ref="PhoneEnvBasic" visibility="ifdata"/>
				//		Needs: menu="mnuDataTree_Environments_Insert".
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Environments_Insert, Create_mnuDataTree_Environments_Insert);

				#endregion left edge menus

				#region hotlinks
				// No hotlinks in this bundle of slices.
				#endregion hotlinks

				#region right click popups

				// "mnuDataTree_LexemeFormContext" (right click menu)
				_dataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(ContextMenuName.mnuDataTree_LexemeFormContext, Create_mnuDataTree_LexemeFormContext_RightClick);

				#endregion right click popups
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_LexemeForm(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_LexemeForm, $"Expected argument value of '{ContextMenuName.mnuDataTree_LexemeForm.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_LexemeForm">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_LexemeForm.ToString()
				};
				var entry = (ILexEntry)_recordList.CurrentObject;
				var hasAllomorphs = entry.AlternateFormsOS.Any();
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(4);

				// <item command="CmdMorphJumpToConcordance" label="Show Lexeme Form in Concordance"/> // NB: Overrides command's label here.
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Lexeme_Form_in_Concordance);
				menu.Tag = new List<object> { _publisher, AreaServices.ConcordanceMachineName, _dataTree.CurrentSlice.MyCmObject.Guid };

				if (hasAllomorphs)
				{
					// <command id="CmdDataTree_Swap_LexemeForm" label="Swap Lexeme Form with Allomorph..." message="SwapLexemeWithAllomorph">
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Swap_LexemeForm_Clicked, LexiconResources.Swap_Lexeme_Form_with_Allomorph);
				}

				var mmt = entry.PrimaryMorphType;
				if (hasAllomorphs && mmt != null && mmt.IsAffixType)
				{
					// <command id="CmdDataTree_Convert_LexemeForm_AffixProcess" label="Convert to Affix Process" message="ConvertLexemeForm"><parameters fromClassName="MoAffixAllomorph" toClassName="MoAffixProcess"/>
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Convert_LexemeForm_AffixProcess_Clicked, LexiconResources.Convert_to_Affix_Process);
				}

				if (hasAllomorphs && entry.AlternateFormsOS[0] is IMoAffixAllomorph)
				{
					// <command id="CmdDataTree_Convert_LexemeForm_AffixAllomorph" label="Convert to Affix Form" message="ConvertLexemeForm"><parameters fromClassName="MoAffixProcess" toClassName="MoAffixAllomorph"/>
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Convert_LexemeForm_AffixAllomorph_Clicked, LexiconResources.Convert_to_Affix_Form);
				}

				// End: <menu id="mnuDataTree_LexemeForm">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void CmdDataTree_Convert_LexemeForm_AffixProcess_Clicked(object sender, EventArgs e)
			{
				Convert_LexemeForm(MoAffixProcessTags.kClassId);
			}

			private void CmdDataTree_Convert_LexemeForm_AffixAllomorph_Clicked(object sender, EventArgs e)
			{
				Convert_LexemeForm(MoAffixAllomorphTags.kClassId);
			}

			private void Convert_LexemeForm(int toClsid)
			{
				var entry = (ILexEntry)_recordList.CurrentObject;
				if (CheckForFormDataLoss(entry.LexemeFormOA))
				{
					IMoForm newForm = null;
					using (new WaitCursor((Form)_majorFlexComponentParameters.MainWindow))
					{
						UowHelpers.UndoExtension(LexiconResources.Convert_to_Affix_Process, _cache.ActionHandlerAccessor, () =>
						{
							switch (toClsid)
							{
								case MoAffixProcessTags.kClassId:
									newForm = _cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create();
									break;
								case MoAffixAllomorphTags.kClassId:
									newForm = _cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
									break;
								case MoStemAllomorphTags.kClassId:
									newForm = _cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
									break;
							}
							entry.ReplaceMoForm(entry.LexemeFormOA, newForm);
						});
						_dataTree.RefreshList(false);
					}

					SelectNewFormSlice(newForm);
				}
			}

			private static bool CheckForFormDataLoss(IMoForm origForm)
			{
				string msg = null;
				switch (origForm.ClassID)
				{
					case MoAffixAllomorphTags.kClassId:
						var affAllo = (IMoAffixAllomorph)origForm;
						var loseEnv = affAllo.PhoneEnvRC.Count > 0;
						var losePos = affAllo.PositionRS.Count > 0;
						var loseGram = affAllo.MsEnvFeaturesOA != null || affAllo.MsEnvPartOfSpeechRA != null;
						if (loseEnv && losePos && loseGram)
						{
							msg = LanguageExplorerResources.ksConvertFormLoseEnvInfixLocGramInfo;
						}
						else if (loseEnv && losePos)
						{
							msg = LanguageExplorerResources.ksConvertFormLoseEnvInfixLoc;
						}
						else if (loseEnv && loseGram)
						{
							msg = LanguageExplorerResources.ksConvertFormLoseEnvGramInfo;
						}
						else if (losePos && loseGram)
						{
							msg = LanguageExplorerResources.ksConvertFormLoseInfixLocGramInfo;
						}
						else if (loseEnv)
						{
							msg = LanguageExplorerResources.ksConvertFormLoseEnv;
						}
						else if (losePos)
						{
							msg = LanguageExplorerResources.ksConvertFormLoseInfixLoc;
						}
						else if (loseGram)
						{
							msg = LanguageExplorerResources.ksConvertFormLoseGramInfo;
						}
						break;
					case MoAffixProcessTags.kClassId:
						msg = LanguageExplorerResources.ksConvertFormLoseRule;
						break;
					case MoStemAllomorphTags.kClassId:
						// not implemented
						break;
				}

				if (msg != null)
				{
					return MessageBox.Show(msg, LanguageExplorerResources.ksConvertFormLoseCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
				}
				return true;
			}

			private void SelectNewFormSlice(IMoForm newForm)
			{
				foreach (var slice in _dataTree.Slices)
				{
					if (slice.MyCmObject.Hvo == newForm.Hvo)
					{
						_dataTree.ActiveControl = slice;
						break;
					}
				}
			}

			private static Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Environments_Insert(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Environments_Insert, $"Expected argument value of '{ContextMenuName.mnuDataTree_Environments_Insert.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Environments_Insert">
				// This "mnuDataTree_Environments_Insert" menu is used in four places.
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_Environments_Insert.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

				PartiallySharedForToolsWideMenuHelper.CreateCommonEnvironmentContextMenuStripMenus(slice, menuItems, contextMenuStrip);

				// End: <menu id="mnuDataTree_Environments_Insert">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_LexemeFormContext_RightClick(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_LexemeFormContext, $"Expected argument value of '{ContextMenuName.mnuDataTree_LexemeFormContext}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_LexemeFormContext">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_LexemeFormContext.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

				// <item command="CmdEntryJumpToConcordance"/>
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Entry_In_Concordance);
				menu.Tag = new List<object> { _publisher, AreaServices.ConcordanceMachineName, _recordList.CurrentObject.Guid };
				// <item command="CmdLexemeFormJumpToConcordance"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Lexeme_Form_in_Concordance);
				menu.Tag = new List<object> { _publisher, AreaServices.ConcordanceMachineName, _dataTree.CurrentSlice.MyCmObject.Guid };
				// <item command="CmdDataTree_Swap_LexemeForm"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Swap_LexemeForm_Clicked, LexiconResources.Swap_Lexeme_Form_with_Allomorph);

				// End: <menu id="mnuDataTree_LexemeFormContext">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void CmdDataTree_Swap_LexemeForm_Clicked(object sender, EventArgs e)
			{
				var entry = (ILexEntry)_recordList.CurrentObject;
				var form = (Form)_majorFlexComponentParameters.MainWindow;
				using (new WaitCursor(form))
				using (var dlg = new SwapLexemeWithAllomorphDlg())
				{
					dlg.SetDlgInfo(_cache, _propertyTable, entry);
					if (DialogResult.OK == dlg.ShowDialog(form))
					{
						SwapAllomorphWithLexeme(entry, dlg.SelectedAllomorph, LexiconResources.Swap_Lexeme_Form_with_Allomorph);
					}
				}
			}

			private void SwapAllomorphWithLexeme(ILexEntry entry, IMoForm allomorph, string uowBase)
			{
				UowHelpers.UndoExtension(uowBase, _cache.ActionHandlerAccessor, () =>
				{
					entry.AlternateFormsOS.Insert(allomorph.IndexInOwner, entry.LexemeFormOA);
					entry.LexemeFormOA = allomorph;
				});
			}

			#endregion LexemeForm_Bundle

			#region CitationForm

			private void Register_CitationForm_Bundle()
			{
				#region right click popups

				// <part label="Citation Form" ref="CitationFormAllV"/>
				_dataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(ContextMenuName.mnuDataTree_CitationFormContext, Create_mnuDataTree_CitationFormContext);

				#endregion right click popups
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_CitationFormContext(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_CitationFormContext, $"Expected argument value of '{ContextMenuName.mnuDataTree_CitationFormContext.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_CitationFormContext">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_CitationFormContext.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdEntryJumpToConcordance"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdEntryJumpToConcordance_Click, AreaResources.Show_Entry_In_Concordance);

				// End: <menu id="mnuDataTree_CitationFormContext">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void CmdEntryJumpToConcordance_Click(object sender, EventArgs e)
			{
				LinkHandler.PublishFollowLinkMessage(_publisher, new FwLinkArgs(AreaServices.ConcordanceMachineName, _recordList.CurrentObject.Guid));
			}
			#endregion CitationForm

			#region Forms_Sections_Bundle

			private void Register_Forms_Sections_Bundle()
			{
				// mnuDataTree_Allomorph (shared: MoStemAllomorph & MoAffixAllomorph)
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_Allomorph, Create_mnuDataTree_Allomorph);

				// mnuDataTree_AffixProcess
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_AffixProcess, Create_mnuDataTree_AffixProcess);

				// mnuDataTree_VariantForm
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_VariantForm, Create_mnuDataTree_VariantForm);

				// mnuDataTree_AlternateForm
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_AlternateForm, Create_mnuDataTree_AlternateForm);

				// mnuDataTree_VariantForms
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_VariantForms, Create_mnuDataTree_VariantForms);
				// mnuDataTree_VariantForms_Hotlinks
				_dataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_VariantForms_Hotlinks, Create_mnuDataTree_VariantForms_Hotlinks);

				// mnuDataTree_AlternateForms
				_dataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_AlternateForms, Create_mnuDataTree_AlternateForms);
				// mnuDataTree_AlternateForms_Hotlinks
				_dataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_AlternateForms_Hotlinks, Create_mnuDataTree_AlternateForms_Hotlinks);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_AffixProcess(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_AffixProcess, $"Expected argument value of '{ContextMenuName.mnuDataTree_AffixProcess.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_AffixProcess">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_AffixProcess.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(8);

				ToolStripMenuItem menu;
				bool visible;
				using (var imageHolder = new ImageHolder())
				{
					// <item command="CmdDataTree_MoveUp_Allomorph"/>
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Form_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
					menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);

					// <item command="CmdDataTree_MoveDown_Allomorph"/>
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Form_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
					menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDataTree_Delete_Allomorph" label="Delete Allomorph" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Allomorph, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// <command id="CmdDataTree_Swap_Allomorph" label="Swap Allomorph with Lexeme Form" message="SwapAllomorphWithLexeme">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, SwapAllomorphWithLexeme_Clicked, LexiconResources.Swap_Allomorph_with_Lexeme_Form);

				visible = slice.MyCmObject.ClassID == MoAffixProcessTags.kClassId;
				if (visible)
				{
					// <command id="CmdDataTree_Convert_Allomorph_AffixAllomorph" label="Convert to Affix Allomorph" message="ConvertAllomorph">
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, AffixAllomorph_Clicked, LexiconResources.Convert_to_Affix_Allomorph);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <item command="CmdMorphJumpToConcordance" label="Show Allomorph in Concordance" />
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), LexiconResources.Show_Allomorph_in_Concordance);
				menu.Tag = new List<object> { _publisher, AreaServices.ConcordanceMachineName, _dataTree.CurrentSlice.MyCmObject.Guid };

				// End: <menu id="mnuDataTree_AffixProcess">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void AffixAllomorph_Clicked(object sender, EventArgs e)
			{
				// <command id="CmdDataTree_Convert_Allomorph_AffixAllomorph" label="Convert to Affix Allomorph" message="ConvertAllomorph">
				// <parameters fromClassName="MoAffixProcess" toClassName="MoAffixAllomorph"/>
				var entry = (ILexEntry)_dataTree.Root;
				var slice = _dataTree.CurrentSlice;
				var allomorph = (IMoForm)slice.MyCmObject;
				if (CheckForFormDataLoss(allomorph))
				{
					var mainWindow = _propertyTable.GetValue<Form>(FwUtils.window);
					IMoForm newForm = null;
					using (new WaitCursor(mainWindow))
					{
						UowHelpers.UndoExtension(LexiconResources.Convert_to_Affix_Allomorph, _cache.ActionHandlerAccessor, () =>
						{
							newForm = entry.Services.GetInstance<IMoAffixAllomorphFactory>().Create();
							entry.ReplaceMoForm(allomorph, newForm);
						});
						_dataTree.RefreshList(false);
					}
					SelectNewFormSlice(newForm);
				}
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_VariantForm(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_VariantForm, $"Expected argument value of '{ContextMenuName.mnuDataTree_VariantForm.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_VariantForm">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_VariantForm.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(4);

				// <command id="CmdEntryJumpToDefault" label="Show Entry in Lexicon" message="JumpToTool">
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.ksShowEntryInLexicon);
				menu.Tag = new List<object> { _publisher, AreaServices.LexiconEditMachineName, _recordList.CurrentObject.Guid };

				// <item command="CmdEntryJumpToConcordance"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Entry_In_Concordance);
				menu.Tag = new List<object> { _publisher, AreaServices.ConcordanceMachineName, _recordList.CurrentObject.Guid };

				if (!slice.IsGhostSlice)
				{
					// <command id="CmdDataTree_Delete_VariantReference" label="Delete Reference" message="DataTreeDeleteReference" icon="Delete">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Delete_VariantReference_Clicked, LexiconResources.Delete_Reference, image: LanguageExplorerResources.Delete);
					menu.Enabled = slice.NextSlice.MyCmObject is ILexEntryRef && (slice.MyCmObject.ClassID == LexEntryTags.kClassId || slice.MyCmObject.Owner.ClassID == LexEntryTags.kClassId);
					menu.ImageTransparentColor = Color.Magenta;
				}

				// <command id="CmdDataTree_Delete_Variant" label="Delete Variant" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Variant, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_VariantForm">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void CmdDataTree_Delete_VariantReference_Clicked(object sender, EventArgs e)
			{
				UndoableUnitOfWorkHelper.Do(AreaResources.ksUndoDeleteRef, AreaResources.ksRedoDeleteRef, _cache.ActionHandlerAccessor, () =>
				{
					var slice = _dataTree.CurrentSlice;
					var ler = (ILexEntryRef)slice.NextSlice.MyCmObject;
					ler.ComponentLexemesRS.Remove(_dataTree.Root);
					// probably not needed, but safe...
					if (ler.PrimaryLexemesRS.Contains(_dataTree.Root))
					{
						ler.PrimaryLexemesRS.Remove(_dataTree.Root);
					}
					var entry = ler.OwningEntry;
					if (entry.EntryRefsOS.Contains(ler))
					{
						entry.EntryRefsOS.Remove(ler);
					}
				});
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_AlternateForm(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_AlternateForm, $"Expected argument value of '{ContextMenuName.mnuDataTree_AlternateForm.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_AlternateForm">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_AlternateForm.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

				ToolStripMenuItem menu;
				using (var imageHolder = new ImageHolder())
				{
					// <command id="CmdDataTree_MoveUp_AlternateForm" label="Move Form _Up" message="MoveUpObjectInSequence" icon="MoveUp">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Form_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
					bool visible;
					menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);
					// <command id="CmdDataTree_MoveDown_AlternateForm" label="Move Form _Down" message="MoveDownObjectInSequence" icon="MoveDown">
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Form_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
					menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDataTree_Merge_AlternateForm" label="Merge AlternateForm into..." message="DataTreeMerge">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, DataTreeMerge_Clicked, LexiconResources.Merge_AlternateForm_into);
				menu.Enabled = slice.CanMergeNow;

				// <command id="CmdDataTree_Delete_AlternateForm" label="Delete AlternateForm" message="DataTreeDelete" icon="Delete"> LexiconResources.Delete_Allomorph
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_AlternateForm, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_AlternateForm">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Allomorph(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_Allomorph, $"Expected argument value of '{ContextMenuName.mnuDataTree_Allomorph.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_Allomorph">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_VariantForms.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(10);

				ToolStripMenuItem menu;
				using (var imageHolder = new ImageHolder())
				{
					// <item command="CmdDataTree_MoveUp_Allomorph"/>
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Form_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
					bool visible;
					menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(_dataTree, _cache, out visible);

					// <item command="CmdDataTree_MoveDown_Allomorph"/>
					menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Form_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
					menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(_dataTree, _cache, out visible);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <command id="CmdDataTree_Merge_Allomorph" label="Merge Allomorph into..." message="DataTreeMerge">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, DataTreeMerge_Clicked, LexiconResources.Merge_Allomorph_into);
				menu.Enabled = slice.CanMergeNow;

				// <command id="CmdDataTree_Delete_Allomorph" label="Delete Allomorph" message="DataTreeDelete" icon="Delete">
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Allomorph, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// <command id="CmdDataTree_Swap_Allomorph" label="Swap Allomorph with Lexeme Form" message="SwapAllomorphWithLexeme">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, SwapAllomorphWithLexeme_Clicked, LexiconResources.Swap_Allomorph_with_Lexeme_Form);

				if (slice.MyCmObject.ClassID == MoAffixAllomorphTags.kClassId)
				{
					// <command id="CmdDataTree_Convert_Allomorph_AffixProcess" label="Convert to Affix Process" message="ConvertAllomorph">
					// <parameters fromClassName="MoAffixAllomorph" toClassName="MoAffixProcess"/>
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Convert_MoAffixAllomorph_To_MoAffixProcess_Clicked, LexiconResources.Convert_to_Affix_Process);
				}

				// <item label="-" translate="do not translate"/>
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				// <item command="CmdMorphJumpToConcordance" label="Show Allomorph in Concordance" />
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), LexiconResources.Show_Allomorph_in_Concordance);
				menu.Tag = new List<object> { _publisher, AreaServices.ConcordanceMachineName, _dataTree.CurrentSlice.MyCmObject.Guid };

				// End: <menu id="mnuDataTree_Allomorph">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Convert_MoAffixAllomorph_To_MoAffixProcess_Clicked(object sender, EventArgs e)
			{
				var allomorph = (IMoForm)_dataTree.CurrentSlice.MyCmObject;
				if (CheckForFormDataLoss(allomorph))
				{
					var mainWindow = _propertyTable.GetValue<Form>(FwUtils.window);
					IMoForm newForm = null;
					using (new WaitCursor(mainWindow))
					{
						UowHelpers.UndoExtension(LexiconResources.Convert_to_Affix_Process, _cache.ActionHandlerAccessor, () =>
						{
							var entry = (ILexEntry)_recordList.CurrentObject;
							newForm = entry.Services.GetInstance<IMoAffixProcessFactory>().Create();
							entry.ReplaceMoForm(allomorph, newForm);
						});
						_dataTree.RefreshList(false);
					}
					SelectNewFormSlice(newForm);
				}
			}

			private void SwapAllomorphWithLexeme_Clicked(object sender, EventArgs e)
			{
				var entry = (ILexEntry)_dataTree.Root;
				UowHelpers.UndoExtension(LexiconResources.Swap_Allomorph_with_Lexeme_Form, _cache.ActionHandlerAccessor, () =>
				{
					var allomorph = (IMoForm)_dataTree.CurrentSlice.MyCmObject;
					entry.AlternateFormsOS.Insert(allomorph.IndexInOwner, entry.LexemeFormOA);
					entry.LexemeFormOA = allomorph;
				});
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_VariantForms(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_VariantForms, $"Expected argument value of '{ContextMenuName.mnuDataTree_VariantForms.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_VariantForms">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_VariantForms.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_VariantForm"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Variant_Clicked, LexiconResources.Insert_Variant);

				// End: <menu id="mnuDataTree_VariantForms">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_VariantForms_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_VariantForms_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_VariantForms_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
				// NB: "CmdDataTree_Insert_VariantForm" is also used in two ordinary slice menus, which are defined in this class, so no need to add to shares.
				// Real work is the same as the Insert Variant Insert menu item.
				// <item command="CmdDataTree_Insert_VariantForm"/>
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Variant_Clicked, LexiconResources.Insert_Variant);

				return hotlinksMenuItemList;
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_AlternateForms(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_AlternateForms, $"Expected argument value of '{ContextMenuName.mnuDataTree_AlternateForms.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_AlternateForms">
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_AlternateForms.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

				// <item command="CmdDataTree_Insert_AlternateForm"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Allomorph_Clicked, LexiconResources.Insert_Allomorph);

				if (((ILexEntry)_recordList.CurrentObject).MorphTypes.FirstOrDefault(mt => mt.IsAffixType) != null)
				{
					// It is only visible/enabled for affixes.
					// <item command="CmdDataTree_Insert_AffixProcess"/>
					ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Affix_Process_Clicked, LexiconResources.Insert_Affix_Process);
				}

				// End: <menu id="mnuDataTree_AlternateForms">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Insert_Affix_Process_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleInsertCommand("AlternateForms", MoAffixProcessTags.kClassName, LexEntryTags.kClassName);
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_AlternateForms_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_AlternateForms_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_AlternateForms_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdDataTree_Insert_AlternateForm"/>
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Allomorph_Clicked, LexiconResources.Insert_Allomorph);

				return hotlinksMenuItemList;
			}

			#endregion Forms_Sections_Bundle

			#region IDisposable
			private bool _isDisposed;

			~LexiconEditToolMenuHelper()
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
					_partiallySharedForToolsWideMenuHelper.Dispose();
					_subscriber.Unsubscribe("ShowHiddenFields", ShowHiddenFields_Handler);
					_rightClickContextMenuManager.Dispose();
					_sharedEventHandlers.Remove(AreaServices.CmdMoveTargetToPreviousInSequence);
					_sharedEventHandlers.Remove(AreaServices.CmdMoveTargetToNextInSequence);
					_show_DictionaryPubPreviewContextMenu?.Dispose();
					_recordBrowseView.ContextMenuStrip?.Dispose();
					_recordBrowseView.ContextMenuStrip = null;
				}
				_extendedPropertyName = null;
				_majorFlexComponentParameters = null;
				_propertyTable = null;
				_subscriber = null;
				_publisher = null;
				_rightClickContextMenuManager = null;
				_partiallySharedForToolsWideMenuHelper = null;
				_sharedEventHandlers = null;
				_dataTree = null;
				_recordList = null;
				_cache = null;
				InnerMultiPane = null;
				_mainWnd = null;
				_show_DictionaryPubPreviewContextMenu = null;
				_recordBrowseView = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
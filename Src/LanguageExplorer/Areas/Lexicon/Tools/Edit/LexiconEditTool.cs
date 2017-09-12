// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// ITool implementation for the "lexiconEdit" tool in the "lexicon" area.
	/// </summary>
	internal sealed class LexiconEditTool : ITool
	{
		private const string Show_DictionaryPubPreview = "Show_DictionaryPubPreview";
		private IFwMainWnd _mainWindow; // Temporary(?) use to show MessageBox
		private LcmCache _cache;
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private MultiPane _innerMultiPane;
		private RecordClerk _recordClerk;
		private ToolStripMenuItem _insertMenu;
		private ToolStripButton _insertEntryToolStripButton;
		private readonly HashSet<Tuple<ToolStripMenuItem, EventHandler>> _newInsertMenusAndHandlers = new HashSet<Tuple<ToolStripMenuItem, EventHandler>>();
		private SliceContextMenuFactory _sliceContextMenuFactory;
		private DataTree _dataTree;

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			PropertyTable.SetDefault($"ToolForAreaNamed_{AreaMachineName}", MachineName, SettingsGroup.LocalSettings, true, false);
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_sliceContextMenuFactory.Dispose();
			foreach (var menuTuple in _newInsertMenusAndHandlers)
			{
				menuTuple.Item1.Click -= menuTuple.Item2;
				_insertMenu.DropDownItems.Remove(menuTuple.Item1);
				menuTuple.Item1.Dispose();
			}
			_newInsertMenusAndHandlers.Clear();

			_insertEntryToolStripButton.Click -= Insert_Entry_Clicked;
			InsertToolbarManager.DeactivateInsertToolbar(majorFlexComponentParameters);
			_insertEntryToolStripButton.Dispose();

			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);
			_recordBrowseView = null;
			_innerMultiPane = null;
			_insertEntryToolStripButton = null;
			_cache = null;
			_mainWindow = null;
			_sliceContextMenuFactory = null;
			_dataTree = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_sliceContextMenuFactory = new SliceContextMenuFactory();
			_cache = majorFlexComponentParameters.LcmCache;
			_mainWindow = majorFlexComponentParameters.MainWindow;

			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(LexiconArea.Entries, majorFlexComponentParameters.Statusbar, LexiconArea.EntriesFactoryMethod);
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

			_recordBrowseView = new RecordBrowseView(root, majorFlexComponentParameters.LcmCache, _recordClerk);

			Image majorEntryImage;
			using (var images = new LexEntryImages())
			{
				majorEntryImage = images.buttonImages.Images["majorEntry"];
			}
			AddInsertMenuItems(majorFlexComponentParameters, majorEntryImage);
			AddToolbarItems(majorFlexComponentParameters, majorEntryImage);
			_dataTree = new DataTree(_sliceContextMenuFactory);
			RegisterMenuMethods();

			var recordEditView = new RecordEditView(XElement.Parse(LexiconResources.LexiconEditRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordClerk, _dataTree);
			var nestedMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				AreaMachineName = AreaMachineName,
				DefaultFixedPaneSizePoints = "60",
				Id = "TestEditMulti",
				ToolMachineName = MachineName,
				FirstControlParameters = new SplitterChildControlParameters
				{
					Control = new RecordDocXmlView(XDocument.Parse(LexiconResources.LexiconEditRecordDocViewParameters).Root, majorFlexComponentParameters.LcmCache, _recordClerk, (StatusBarProgressPanel)majorFlexComponentParameters.Statusbar.Panels[LanguageExplorerConstants.StatusBarPanelProgressBar]), Label = "Dictionary"
				},
				SecondControlParameters = new SplitterChildControlParameters
				{
					Control = recordEditView, Label = "Details"
				}
			};
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
				Id = "LexItemsAndDetailMultiPane",
				ToolMachineName = MachineName,
				DefaultPrintPane = "DictionaryPubPreview"
			};
			var paneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);

			var panelMenu = new PanelMenu(_sliceContextMenuFactory, panelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			var panelButton = new PanelButton(PropertyTable, null, PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), LanguageExplorerResources.ksHideFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			paneBar.AddControls(new List<Control> { panelMenu, panelButton });
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters,
				_recordBrowseView, "Browse", new PaneBar(),
				_innerMultiPane = MultiPaneFactory.CreateNestedMultiPane(majorFlexComponentParameters.FlexComponentParameters, nestedMultiPaneParameters), "Dictionary & Details", paneBar);
			_innerMultiPane.Panel1Collapsed = !PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);
			panelButton.DatTree = recordEditView.DatTree;

			// Too early before now.
			recordEditView.FinishInitialization();
			((RecordDocXmlView)nestedMultiPaneParameters.FirstControlParameters.Control).ReallyShowRecordNow();
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
		}

		private void RegisterMenuMethods()
		{
			_sliceContextMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Sense_Hotlinks, Create_mnuDataTree_Sense_Hotlinks);
			_sliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(mnuDataTree_Sense, Create_mnuDataTree_Sense);
			_sliceContextMenuFactory.RegisterPanelMenuCreatorMethod(panelMenuId, CreateMainPanelContextMenuStrip);
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
			_recordClerk.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordClerk.VirtualListPublisher).Refresh();
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
		public string MachineName => "lexiconEdit";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Lexicon Edit";
#endregion

#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "lexicon";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion

		private const string mnuDataTree_Sense_Hotlinks = "mnuDataTree-Sense-Hotlinks";
		private const string mnuDataTree_Sense = "mnuDataTree-Sense";
		private const string panelMenuId = "left";

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Sense_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_Sense_Hotlinks)
			{
				throw new ArgumentException($"Expected argmuent value of '{mnuDataTree_Sense_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);
#if RANDYTODO
// TODO: Add "CmdDataTree-Insert-Example" to "mnuDataTree-Sense-Hotlinks"
// <item command="CmdDataTree-Insert-Example"/>
#endif
			// <item command="CmdDataTree-Insert-SenseBelow"/>
			var toolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(LexiconResources.Insert_Sense.Replace("_", string.Empty), LexiconResources.InsertSenseToolTip, null, Insert_SenseBelow_Clicked);
			hotlinksMenuItemList.Add(new Tuple<ToolStripMenuItem, EventHandler>(toolStripMenuItem, Insert_SenseBelow_Clicked));
			return hotlinksMenuItemList;
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Sense(Slice slice, string hotlinksMenuId)
		{
			// Start: <menu id="mnuDataTree-Sense">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_Sense
			};
			contextMenuStrip.Opening += MenuDataTree_SenseContextMenuStrip_Opening;
			List<Tuple<ToolStripMenuItem, EventHandler>> menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(21);
			/*
			<command id="CmdDataTree-Insert-Example" label="Insert _Example" message="DataTreeInsert">
				<parameters field="Examples" className="LexExampleSentence" />
			</command>
			<item command="CmdDataTree-Insert-Example"/>

			<command id="CmdFindExampleSentence" label="Find example sentence..." message="LaunchGuiControl">
				<parameters field="Example" ownerClass="LexExampleSentence" guicontrol="findExampleSentences" />
			</command>
			<item command="CmdFindExampleSentence"/>

			<command id="CmdDataTree-Insert-ExtNote" label="Insert Extended Note" message="DataTreeInsert">
				<parameters field="ExtendedNote" className="LexExtendedNote" />
			</command>
			<item command="CmdDataTree-Insert-ExtNote"/>
			*/
			// TODO: Add above menus.
			/*
			<command id="CmdDataTree-Insert-SenseBelow" label="Insert _Sense" message="DataTreeInsert">
				<parameters field="Senses" className="LexSense" slice="owner" recomputeVirtual="LexSense.LexSenseOutline" />
			</command>
			<item command="CmdDataTree-Insert-SenseBelow"/>
			*/
			CreateToolStripMenuItem(menuItems, contextMenuStrip, mnuDataTree_Sense, "CmdDataTree-Insert-SenseBelow", LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip, Insert_Sense_Clicked);

			/*
			<command id="CmdDataTree-Insert-SubSense" label="Insert Su_bsense" message="DataTreeInsert">
				<parameters field="Senses" className="LexSense" ownerClass="LexSense" recomputeVirtual="LexSense.LexSenseOutline" />
			</command>
			<item command="CmdDataTree-Insert-SubSense"/>
			*/
			CreateToolStripMenuItem(menuItems, contextMenuStrip, mnuDataTree_Sense, "CmdDataTree-Insert-SubSense", LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip, Insert_Subsense_Clicked);

			// TODO: Add below menus.
			/*
			<item command="CmdInsertPicture" label="Insert _Picture" defaultVisible="false"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdSenseJumpToConcordance"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-MoveUp-Sense"/>
			<item command="CmdDataTree-MoveDown-Sense"/>
			<item command="CmdDataTree-MakeSub-Sense"/>
			<item command="CmdDataTree-Promote-Sense"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-Merge-Sense"/>
			<item command="CmdDataTree-Split-Sense"/>
			*/
			/*
			<command id="CmdDataTree-Delete-Sense" label="Delete this Sense and any Subsenses" message="DataTreeDeleteSense" icon="Delete">
				<parameters field="Senses" className="LexSense" />
			</command>
			<item command="CmdDataTree-Delete-Sense"/>
			*/
			var toolStripMenuItem = CreateToolStripMenuItem(menuItems, contextMenuStrip, mnuDataTree_Sense, "CmdDataTree-Delete-Sense", LexiconResources.DeleteSenseAndSubsenses, string.Empty, Delete_Sense_Clicked);
			toolStripMenuItem.Image = LanguageExplorerResources.Delete;
			toolStripMenuItem.ImageTransparentColor = Color.Magenta;
			// TODO: Add below menus.
			/*
				// Plus it gets these more general menus added to it, which shoudl be commmon to all (almost all?) slices:
				<item label="-" translate="do not translate"/>
				Field Visibility
				Help
			*/
			// End: <menu id="mnuDataTree-Sense">

			return new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MenuDataTree_SenseContextMenuStrip_Opening, menuItems);
		}

		private void MenuDataTree_SenseContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
		{
			var contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Opening += MainPanelContextMenuStrip_Opening;

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MainPanelContextMenuStrip_Opening, menuItems);

			// Show_Dictionary_Preview menu item.
			var contextMenuItem = CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Show_DictionaryPubPreview, LexiconResources.Show_DictionaryPubPreview_ToolTip, Show_Dictionary_Preview_Clicked);
			contextMenuItem.Checked = PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Insert_Sense menu item. (CmdInsertSense->msg: DataTreeInsert, also on Insert menu)
			CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip, Insert_Sense_Clicked);

			// Insert Subsense (in sense) menu item. (CmdInsertSubsense->msg: DataTreeInsert, also on Insert menu)
			CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip, Insert_Subsense_Clicked);

			// Insert _Variant menu item. (CmdInsertVariant->msg: InsertItemViaBackrefVector, also on Insert menu)
			CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Insert_Variant, LexiconResources.Insert_Variant_Tooltip, Insert_Variant_Clicked);

			// Insert A_llomorph menu item. (CmdDataTree-Insert-AlternateForm->msg: DataTreeInsert, also on Insert menu)
			CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip, Insert_Allomorph_Clicked);

			// Insert _Pronunciation menu item. (CmdDataTree-Insert-Pronunciation->msg: DataTreeInsert, also on Insert menu)
			CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip, Insert_Pronunciation_Clicked);

			// Insert Sound or Movie _File menu item. (CmdInsertMediaFile->msg: InsertMediaFile, also on Insert menu)
			CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Insert_Sound_Or_Movie_File, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip, Insert_Sound_Or_Movie_File_Clicked);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Lexeme Form has components menu item. (CmdChangeToVariant->msg: ConvertEntryIntoComplexForm)
			CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Lexeme_Form_Has_Components, LexiconResources.Lexeme_Form_Has_Components_Tooltip, Lexeme_Form_Has_Components_Clicked);

			// Lexeme Form is a variant menu item. (CmdChangeToComplexForm->msg: ConvertEntryIntoVariant)
			CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Lexeme_Form_Is_A_Variant, LexiconResources.Lexeme_Form_Is_A_Variant_Tooltip, Lexeme_Form_Is_A_Variant_Clicked);

			// _Merge with entry... menu item. (CmdMergeEntry->msg: MergeEntry, also on Tool menu)
			contextMenuItem = CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Merge_With_Entry, LexiconResources.Merge_With_Entry_Tooltip, Merge_With_Entry_Clicked);
			// NB: defaultVisible="false"
			// Original code that controlled: display.Enabled = display.Visible = InFriendlyArea;
			// It is now only in a friendly area, so should always be visible and enabled, per the old code.
			// Trouble is it makes no sense to enable it if the lexicon only has one entry in it, so I'll alter the behavior to be more sensible. ;-)
			contextMenuItem.Enabled = PropertyTable.GetValue<LcmCache>("cache").LanguageProject.LexDbOA.Entries.Any();

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Show Entry in Concordance menu item. (CmdRootEntryJumpToConcordance->msg: JumpToTool, also on Insert menu)
			CreateToolStripMenuItem(menuItems, contextMenuStrip, LexiconResources.Show_Entry_In_Concordance, null, Show_Entry_In_Concordance_Clicked);

			return retVal;
		}

		private void MainPanelContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
#if RANDYTODO
			// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private ToolStripMenuItem CreateToolStripMenuItem(List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ContextMenuStrip contextMenuStrip, string menuText, string menuTooltip, EventHandler eventHandler)
		{
			var toolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(contextMenuStrip, menuText, null, eventHandler, menuTooltip);
			menuItems.Add(new Tuple<ToolStripMenuItem, EventHandler>(toolStripMenuItem, eventHandler));
			return toolStripMenuItem;
		}

		private ToolStripMenuItem CreateToolStripMenuItem(List<Tuple<ToolStripMenuItem, EventHandler>> menuItemsContextMenuStrip, ContextMenuStrip contextMenuStrip, string menuId, string commandId, string menuText, string menuTooltip, EventHandler eventHandler)
		{
			var toolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(contextMenuStrip, menuText.Replace("_", string.Empty), null, eventHandler, menuTooltip);
			menuItemsContextMenuStrip.Add(new Tuple<ToolStripMenuItem, EventHandler>(toolStripMenuItem, eventHandler));
			return toolStripMenuItem;
		}

		private void Show_Entry_In_Concordance_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_mainWindow, "Show Entry In Concordance...");
		}

		private void Delete_Sense_Clicked(object sender, EventArgs e)
		{
			_dataTree.CurrentSlice.HandleDeleteCommand();
		}

		private void Merge_With_Entry_Clicked(object sender, EventArgs e)
		{
			var currentObject = _recordClerk.CurrentObject;
			if (currentObject == null)
				return;	// should never happen, but nothing we can do if it does!

			var currentEntry = currentObject as ILexEntry ?? currentObject.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
			if (currentEntry == null)
				return;

			using (var dlg = new MergeEntryDlg())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				var cache = PropertyTable.GetValue<LcmCache>("cache");
				// <parameters title="Merge Entry" formlabel="_Find:" okbuttonlabel="_Merge"/>
				dlg.SetDlgInfo(cache, XElement.Parse(LexiconResources.MatchingEntriesParameters), currentEntry, LexiconResources.ksMergeEntry, FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Find), FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Merge));
				if (dlg.ShowDialog() != DialogResult.OK)
					return;

				var survivor = (ILexEntry)dlg.SelectedObject;
				Debug.Assert(survivor != currentEntry);
				UndoableUnitOfWorkHelper.Do(LexiconResources.ksUndoMergeEntry, LexiconResources.ksRedoMergeEntry, cache.ActionHandlerAccessor, () =>
					{
						survivor.MergeObject(currentEntry, true);
						survivor.DateModified = DateTime.Now;
					});
				MessageBox.Show(null,
					LexiconResources.ksEntriesHaveBeenMerged,
					LexiconResources.ksMergeReport,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				var commands = new List<string>
				{
					"AboutToFollowLink",
					"FollowLink"
				};
				var parms = new List<object>
				{
					null,
					survivor.Hvo
				};
				Publisher.Publish(commands, parms);
			}
		}

		private void Lexeme_Form_Is_A_Variant_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_mainWindow, "Lexeme Form Is A Variant...");
		}

		private void Lexeme_Form_Has_Components_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_mainWindow, "Lexeme Form Has Components...");
		}

		private void Insert_Sound_Or_Movie_File_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_mainWindow, "Inserting Sound or Movie File...");
		}

		private void Insert_Pronunciation_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_mainWindow, "Inserting Pronunciation...");
		}

		private void Insert_Allomorph_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_mainWindow, "Inserting Allomorph...");
		}

		private void Insert_Variant_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_mainWindow, "Inserting Variant...");
		}

		private void Insert_Subsense_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_mainWindow, "Inserting Subsense...");
		}

		private void Insert_Sense_Clicked(object sender, EventArgs e)
		{
			LexSenseUi.CreateNewLexSense(_cache, (ILexEntry)_recordClerk.CurrentObject);
		}

		private void Insert_SenseBelow_Clicked(object sender, EventArgs e)
		{
			// Get slice and see what sense is currently selected, so we can add the new sense after (read: 'below") it.
			var currentSlice = _dataTree.CurrentSlice;
			ILexSense currentSense;
			while (true)
			{
				var currentObject = currentSlice.Object;
				if (currentObject is ILexSense)
				{
					currentSense = (ILexSense)currentObject;
					break;
				}
				currentSlice = currentSlice.ParentSlice;
			}
			var indexInOwningSensesProperty = 0;
			if (currentSense.Owner is ILexSense)
			{
				var owningSense = (ILexSense)currentSense.Owner;
				indexInOwningSensesProperty = owningSense.SensesOS.IndexOf(currentSense);
			}
			else
			{
				var owningEntry = (ILexEntry)currentSense.Owner;
				indexInOwningSensesProperty = owningEntry.SensesOS.IndexOf(currentSense);
			}
			LexSenseUi.CreateNewLexSense(_cache, (ILexEntry)_recordClerk.CurrentObject, indexInOwningSensesProperty + 1);
		}

		private void Insert_Entry_Clicked(object sender, EventArgs e)
		{
			using (InsertEntryDlg dlg = new InsertEntryDlg())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.SetDlgInfo(_cache, PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable));
				if (dlg.ShowDialog(Form.ActiveForm) == DialogResult.OK)
				{
					ILexEntry entry;
					bool newby;
					dlg.GetDialogInfo(out entry, out newby);
					// No need for a PropChanged here because InsertEntryDlg takes care of that. (LT-3608)
					_recordClerk.JumpToRecord(entry.Hvo);
				}
			}
		}

		private void Show_Dictionary_Preview_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem)sender;
			menuItem.Checked = !menuItem.Checked;
			PropertyTable.SetProperty(Show_DictionaryPubPreview, menuItem.Checked, SettingsGroup.LocalSettings, true, false);
			_innerMultiPane.Panel1Collapsed = !PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);
		}

		private void AddToolbarItems(MajorFlexComponentParameters majorFlexComponentParameters, Image majorEntryImage)
		{
			/*
<command id="CmdInsertLexEntry" label="_Entry..." message="InsertItemInVector" shortcut="Ctrl+E" icon="majorEntry">
	<parameters className="LexEntry" />
</command>
<item command="CmdInsertLexEntry" defaultVisible="false" />
			 */
			_insertEntryToolStripButton = new ToolStripButton("toolStripButtonInsertEntry", majorEntryImage,
				Insert_Entry_Clicked)
			{
				DisplayStyle = ToolStripItemDisplayStyle.Image,
				ToolTipText = LexiconResources.Entry_Tooltip
			};
			InsertToolbarManager.AddInsertToolbarItems(majorFlexComponentParameters, new List<ToolStripButton> { _insertEntryToolStripButton });
#if RANDYTODO
			// TODO: Add new "Insert" toolbar item.
/*
<command id="CmdGoToEntry" label="_Find lexical entry..." message="GotoLexEntry" icon="goToEntry" shortcut="Ctrl+F" a10status="Derfined here, but used in Main.xml.">
<parameters title="Go To Entry" formlabel="Go _To..." okbuttonlabel="_Go" />
</command>
<item command="CmdGoToEntry" defaultVisible="false" />
*/
#endif
		}

		private void AddInsertMenuItems(MajorFlexComponentParameters majorFlexComponentParameters, Image majorEntryImage)
		{
			_insertMenu = (ToolStripMenuItem)majorFlexComponentParameters.MenuStrip.Items["_insertToolStripMenuItem"];
			/*
<command id="CmdInsertLexEntry" label="_Entry..." message="InsertItemInVector" shortcut="Ctrl+E" icon="majorEntry">
	<parameters className="LexEntry" />
</command>
<item command="CmdInsertLexEntry" defaultVisible="false" />
			 */
			PaneBarContextMenuFactory.CreateToolStripMenuItem(_insertMenu, 0, LexiconResources.Entry, majorEntryImage, Keys.Control | Keys.E, Insert_Entry_Clicked, LexiconResources.Entry_Tooltip);
			/*
<command id="CmdInsertSense" label="_Sense" message="DataTreeInsert">
	<parameters field="Senses" className="LexSense" ownerClass="LexEntry" />
</command>
<item command="CmdInsertSense" defaultVisible="false" />
			 */
			PaneBarContextMenuFactory.CreateToolStripMenuItem(_insertMenu, 1, LexiconResources.Insert_Sense, null, Keys.None, Insert_Sense_Clicked, LexiconResources.InsertSenseToolTip);
#if RANDYTODO
			// TODO: Add these to the main Insert menu.
/*
<command id="CmdInsertVariant" label="_Variant" message="InsertItemViaBackrefVector">
	<parameters className="LexEntry" fieldName="VariantFormEntryBackRefs" restrictToTool="lexiconEdit" />
</command>
			<item command="CmdInsertVariant" defaultVisible="false" />
<command id="CmdDataTree-Insert-AlternateForm" label="Insert Allomorph" message="DataTreeInsert">
	<parameters field="AlternateForms" className="MoForm" />
</command>
			<item command="CmdDataTree-Insert-AlternateForm" label="A_llomorph" defaultVisible="false" />
<command id="CmdInsertReversalEntry" label="Reversal Entry" message="InsertItemInVector" icon="reversalEntry">
	<parameters className="ReversalIndexEntry" />
	</command>
			<item command="CmdInsertReversalEntry" defaultVisible="false" />
<command id="CmdDataTree-Insert-Pronunciation" label="_Pronunciation" message="DataTreeInsert">
	<parameters field="Pronunciations" className="LexPronunciation" ownerClass="LexEntry" />
	</command>
			<item command="CmdDataTree-Insert-Pronunciation" defaultVisible="false" />
<command id="CmdInsertMediaFile" label="_Sound or Movie" message="InsertMediaFile">
	<parameters field="MediaFiles" className="LexPronunciation" />
</command>
			<item command="CmdInsertMediaFile" defaultVisible="false" />
<command id="CmdDataTree-Insert-Etymology" label="_Etymology" message="DataTreeInsert">
	<parameters field="Etymology" className="LexEtymology" ownerClass="LexEntry" />
</command>
			<item command="CmdDataTree-Insert-Etymology" defaultVisible="false" />
			<item label="-" translate="do not translate" />
<command id="CmdInsertSubsense" label="Subsense (in sense)" message="DataTreeInsert">
	<parameters field="Senses" className="LexSense" ownerClass="LexSense" />
</command>
			<item command="CmdInsertSubsense" defaultVisible="false" />
<command id="CmdInsertPicture" label="_Picture" message="InsertPicture">
	<parameters field="Pictures" className="LexSense" />
</command>
			<item command="CmdInsertPicture" defaultVisible="false" />
<command id="CmdInsertExtNote" label="_Extended Note" message="DataTreeInsert">
	<parameters field="ExtendedNote" className="LexExtendedNote" ownerClass="LexSense" />
</command>
			<item command="CmdInsertExtNote" defaultVisible="false" />
			*/
#endif
		}
	}
}
// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
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
		private IFwMainWnd _mainWindow;
		private IFlexApp _flexApp;
		private LcmCache _cache;
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private MultiPane _innerMultiPane;
		private RecordClerk _recordClerk;
		private ToolStripMenuItem _insertMenu;
		private ToolStripButton _insertEntryToolStripButton;
		private readonly HashSet<Tuple<ToolStripMenuItem, EventHandler>> _newInsertMenusAndHandlers = new HashSet<Tuple<ToolStripMenuItem, EventHandler>>();
		private readonly HashSet<Tuple<ToolStripMenuItem, EventHandler>> _newContextMenusAndHandlers = new HashSet<Tuple<ToolStripMenuItem, EventHandler>>();

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
			foreach (var menuTuple in _newInsertMenusAndHandlers)
			{
				menuTuple.Item1.Click -= menuTuple.Item2;
				_insertMenu.DropDownItems.Remove(menuTuple.Item1);
			}
			_newInsertMenusAndHandlers.Clear();

			foreach (var menuTuple in _newContextMenusAndHandlers)
			{
				menuTuple.Item1.Click -= menuTuple.Item2;
			}
			_newContextMenusAndHandlers.Clear();

			_insertEntryToolStripButton.Click -= Insert_Entry_Clicked;
			InsertToolbarManager.DeactivateInsertToolbar(majorFlexComponentParameters);
			_insertEntryToolStripButton.Dispose();

			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);
			_recordBrowseView = null;
			_innerMultiPane = null;
			_insertEntryToolStripButton = null;
			_cache = null;
			_mainWindow = null;
			_flexApp = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_cache = majorFlexComponentParameters.LcmCache;
			_mainWindow = majorFlexComponentParameters.MainWindow;
			_flexApp = majorFlexComponentParameters.FlexApp;

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
			AddToolbarItems(majorFlexComponentParameters, majorEntryImage);

			AddInsertMenuItems(majorFlexComponentParameters, majorEntryImage);

			var dataTreeMenuHandler = new DataTreeMenuHandler(_recordClerk, new DataTree());
#if RANDYTODO
			// TODO: Set up 'dataTreeMenuHandler' to handle menu events.
			// TODO: Install menus and connect them to event handlers. (See "CreateMainPanelContextMenuStrip" method for where the menus are.)
			// NB: "CreateMainPanelContextMenuStrip" adds the context menu for the top left main panel up top in the pane bar.
			// Other menus are needed for individual slices. The theory (for now) is that a slice can fetch context menus via a dictionary
			// on DataTreeMenuHandler, the key of which (if any) is known to a given slice from those xml layout/part elements.
			// This spike is to see if the theory works, or not.
#endif
			var recordEditView = new RecordEditView(XElement.Parse(LexiconResources.LexiconEditRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordClerk, dataTreeMenuHandler);
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

			var panelMenu = new PanelMenu
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center,
				ContextMenuStrip = CreateMainPanelContextMenuStrip()
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

		private ContextMenuStrip CreateMainPanelContextMenuStrip()
		{
			var contextMenuStrip = new ContextMenuStrip();

			// Show_Dictionary_Preview menu item.
			var contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Show_DictionaryPubPreview, LexiconResources.Show_DictionaryPubPreview_ToolTip, Show_Dictionary_Preview_Clicked);
			contextMenuItem.Checked = PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Insert_Sense menu item. (CmdInsertSense->msg: DataTreeInsert, also on Insert menu)
			CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip, Insert_Sense_Clicked);

			// Insert Subsense (in sense) menu item. (CmdInsertSubsense->msg: DataTreeInsert, also on Insert menu)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip, Insert_Subsense_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			// Insert _Variant menu item. (CmdInsertVariant->msg: InsertItemViaBackrefVector, also on Insert menu)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Insert_Variant, LexiconResources.Insert_Variant_Tooltip, Insert_Variant_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			// Insert A_llomorph menu item. (CmdDataTree-Insert-AlternateForm->msg: DataTreeInsert, also on Insert menu)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip, Insert_Allomorph_Clicked);

#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			// Insert _Pronunciation menu item. (CmdDataTree-Insert-Pronunciation->msg: DataTreeInsert, also on Insert menu)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip, Insert_Pronunciation_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			// Insert Sound or Movie _File menu item. (CmdInsertMediaFile->msg: InsertMediaFile, also on Insert menu)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Insert_Sound_Or_Movie_File, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip, Insert_Sound_Or_Movie_File_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Lexeme Form has components menu item. (CmdChangeToVariant->msg: ConvertEntryIntoComplexForm)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Lexeme_Form_Has_Components, LexiconResources.Lexeme_Form_Has_Components_Tooltip, Lexeme_Form_Has_Components_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			// Lexeme Form is a variant menu item. (CmdChangeToComplexForm->msg: ConvertEntryIntoVariant)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Lexeme_Form_Is_A_Variant, LexiconResources.Lexeme_Form_Is_A_Variant_Tooltip, Lexeme_Form_Is_A_Variant_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			// _Merge with entry... menu item. (CmdMergeEntry->msg: MergeEntry, also on Tool menu)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Merge_With_Entry, LexiconResources.Merge_With_Entry_Tooltip, Merge_With_Entry_Clicked);
			// NB: defaultVisible="false"
			// Original code that controlled: display.Enabled = display.Visible = InFriendlyArea;
			// It is now only in a friendly area, so should always be visible and enabled, per the old code.
			// Trouble is it makes no sense to enable it if the lexicon only has one entry in it, so I'll alter the behavior to be more sensible. ;-)
			contextMenuItem.Enabled = PropertyTable.GetValue<LcmCache>("cache").LanguageProject.LexDbOA.Entries.Any();

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Show Entry in Concordance menu item. (CmdRootEntryJumpToConcordance->msg: JumpToTool, also on Insert menu)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Show_Entry_In_Concordance, null, Show_Entry_In_Concordance_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

			return contextMenuStrip;
		}

		private ToolStripMenuItem GetItemForItemText(string menuText)
		{
			return _newContextMenusAndHandlers.First(t => t.Item1.Text == FwUtils.ReplaceUnderlineWithAmpersand(menuText)).Item1;
		}

		private ToolStripMenuItem CreateToolStripMenuItem(ContextMenuStrip contextMenuStrip, string menuText, string menuTooltip, EventHandler eventHandler)
		{
			var toolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(contextMenuStrip, menuText, null, eventHandler, menuTooltip);
			_newContextMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(toolStripMenuItem, eventHandler));
			return toolStripMenuItem;
		}

		private void Show_Entry_In_Concordance_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Move to LexEntryMenuHandler?
#endif
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
#if RANDYTODO
			// TODO: Move to LexEntryMenuHandler?
#endif
		}

		private void Lexeme_Form_Has_Components_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Move to LexEntryMenuHandler?
#endif
		}

		private void Insert_Sound_Or_Movie_File_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Move to LexEntryMenuHandler?
#endif
		}

		private void Insert_Pronunciation_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Move to LexEntryMenuHandler?
#endif
		}

		private void Insert_Allomorph_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Move to LexEntryMenuHandler?
#endif
		}

		private void Insert_Variant_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Move to LexEntryMenuHandler?
#endif
		}

		private void Insert_Subsense_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Needs the sense slice to know which sense gets the subsense.
#endif
		}

		private void Insert_Sense_Clicked(object sender, EventArgs e)
		{
			LexSenseUi.CreateNewLexSense(_cache, (ILexEntry)_recordClerk.CurrentObject);
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
			var menuItem = GetItemForItemText(LexiconResources.Show_DictionaryPubPreview);
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
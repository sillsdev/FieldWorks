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
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// ITool implementation for the "lexiconEdit" tool in the "lexicon" area.
	/// </summary>
	internal sealed class LexiconEditTool : ITool
	{
		private const string Show_DictionaryPubPreview = "Show_DictionaryPubPreview";
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private MultiPane _innerMultiPane;
		private RecordClerk _recordClerk;
		private readonly HashSet<Tuple<ToolStripMenuItem, EventHandler>> _newMenusAndHandlers = new HashSet<Tuple<ToolStripMenuItem, EventHandler>>();

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
			foreach (var menuTuple in _newMenusAndHandlers)
			{
				menuTuple.Item1.Click -= menuTuple.Item2;
			}
			_newMenusAndHandlers.Clear();

			MultiPaneFactory.RemoveFromParentAndDispose(
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				ref _multiPane,
				ref _recordClerk);
			_recordBrowseView = null;
			_innerMultiPane = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
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

			_recordClerk = LexiconArea.CreateBasicClerkForLexiconArea(PropertyTable.GetValue<FdoCache>("cache"));
			_recordClerk.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);

			_recordBrowseView = new RecordBrowseView(root, _recordClerk);

			var dataTreeMenuHandler = new LexEntryMenuHandler();
			dataTreeMenuHandler.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
#if RANDYTODO
			// TODO: Set up 'dataTreeMenuHandler' to handle menu events.
			// TODO: Install menus and connect them to event handlers. (See "CreateContextMenuStrip" method for where the menus are.)
#endif
			var recordEditView = new RecordEditView(XElement.Parse(LexiconResources.LexiconEditRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), _recordClerk, dataTreeMenuHandler);
			var nestedMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Horizontal,
				AreaMachineName = AreaMachineName,
				DefaultFixedPaneSizePoints = "60",
				Id = "TestEditMulti",
				ToolMachineName = MachineName,
				FirstControlParameters = new SplitterChildControlParameters
				{
					Control = new RecordDocXmlView(XDocument.Parse(LexiconResources.LexiconEditRecordDocViewParameters).Root, _recordClerk), Label = "Dictionary"
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
				ContextMenuStrip = CreateContextMenuStrip()
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
			majorFlexComponentParameters.DataNavigationManager.Clerk = _recordClerk;
		}

		private ContextMenuStrip CreateContextMenuStrip()
		{
			var contextMenuStrip = new ContextMenuStrip();

			// Show_Dictionary_Preview menu item.
			var contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Show_DictionaryPubPreview, LexiconResources.Show_DictionaryPubPreview_ToolTip, Show_Dictionary_Preview_Clicked);
			contextMenuItem.Checked = PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Insert_Sense menu item. (CmdInsertSense->msg: DataTreeInsert, also on Insert menu)
			contextMenuItem = CreateToolStripMenuItem(contextMenuStrip, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip, Insert_Sense_Clicked);
#if !RANDYTODO
			// TODO: Enable it and have better event handler deal with it.
			contextMenuItem.Enabled = false;
#endif

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
			contextMenuItem.Enabled = PropertyTable.GetValue<FdoCache>("cache").LanguageProject.LexDbOA.Entries.Any();

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
			return _newMenusAndHandlers.First(t => t.Item1.Text == FwUtils.ReplaceUnderlineWithAmpersand(menuText)).Item1;
		}

		private ToolStripMenuItem CreateToolStripMenuItem(ContextMenuStrip contextMenuStrip, string menuText, string menuTooltip, EventHandler eventHandler)
		{
			var toolStripMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(contextMenuStrip, menuText, null, eventHandler, menuTooltip);
			_newMenusAndHandlers.Add(new Tuple<ToolStripMenuItem, EventHandler>(toolStripMenuItem, eventHandler));
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
				return;		// should never happen, but nothing we can do if it does!

			var currentEntry = currentObject as ILexEntry ?? currentObject.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
			if (currentEntry == null)
				return;

			using (var dlg = new MergeEntryDlg())
			{
				var cache = PropertyTable.GetValue<FdoCache>("cache");
				// <parameters title="Merge Entry" formlabel="_Find:" okbuttonlabel="_Merge"/>
				dlg.SetDlgInfo(cache, PropertyTable, Publisher, Subscriber, XElement.Parse(LexiconResources.MatchingEntriesParameters), currentEntry, LexiconResources.ksMergeEntry, FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Find), FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Merge));
				if (dlg.ShowDialog() != DialogResult.OK)
					return;

				var survivor = (ILexEntry)dlg.SelectedObject;
				Debug.Assert(survivor != currentEntry);
				UndoableUnitOfWorkHelper.Do(LexiconResources.ksUndoMergeEntry,
					LexiconResources.ksRedoMergeEntry, cache.ActionHandlerAccessor,
					() =>
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
			// TODO: Move to LexEntryMenuHandler?
#endif
		}

		private void Insert_Sense_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Move to LexEntryMenuHandler?
#endif
		}

		private void Show_Dictionary_Preview_Clicked(object sender, EventArgs e)
		{
			var menuItem = GetItemForItemText(LexiconResources.Show_DictionaryPubPreview);
			menuItem.Checked = !menuItem.Checked;
			PropertyTable.SetProperty(Show_DictionaryPubPreview, menuItem.Checked, SettingsGroup.LocalSettings, true, false);
			_innerMultiPane.Panel1Collapsed = !PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);
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
	}
}
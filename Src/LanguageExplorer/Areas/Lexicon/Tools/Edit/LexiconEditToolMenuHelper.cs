// Copyright (c) 2017-2018 SIL International
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
using LanguageExplorer.LcmUi;
using LanguageExplorer.Works;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This class handles all interaction for the LexiconEditTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class LexiconEditToolMenuHelper : IFlexComponent, IDisposable
	{
		private LexiconAreaMenuHelper _lexiconAreaMenuHelper;
		internal const string Show_DictionaryPubPreview = "Show_DictionaryPubPreview";
		internal const string panelMenuId = "left";
		private const string mnuDataTree_Sense_Hotlinks = "mnuDataTree-Sense-Hotlinks";
		private const string mnuDataTree_Sense = "mnuDataTree-Sense";
		private const string mnuDataTree_Etymology = "mnuDataTree-Etymology";
		private const string mnuDataTree_Etymology_Hotlinks = "mnuDataTree-Etymology-Hotlinks";
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ToolStripMenuItem _editMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newEditMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripMenuItem _insertMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newInsertMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripButton _insertEntryToolStripButton;
		private ToolStripButton _insertGoToEntryToolStripButton;
		private DataTree DataTree { get; set; }
		private RecordClerk RecordClerk { get; set; }
		internal MultiPane InnerMultiPane { get; set; }
		internal SliceContextMenuFactory SliceContextMenuFactory { get; set; }

		internal LexiconEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, DataTree dataTree, RecordClerk recordClerk)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNull(recordClerk, nameof(recordClerk));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			DataTree = dataTree;
			RecordClerk = recordClerk;
			SliceContextMenuFactory = DataTree.SliceContextMenuFactory;
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper(_majorFlexComponentParameters, RecordClerk);

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		internal void Initialize()
		{
			_lexiconAreaMenuHelper.Initialize();

			AddEditMenuItems();
			AddInsertMenuItems();
			AddToolbarItems();

			RegisterHotLinkMenus();
			RegisterOrdinaryContextMenus();
			SliceContextMenuFactory.RegisterPanelMenuCreatorMethod(panelMenuId, CreateMainPanelContextMenuStrip);
		}

		private void RegisterHotLinkMenus()
		{
			SliceContextMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Sense_Hotlinks, Create_mnuDataTree_Sense_Hotlinks);
			SliceContextMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Etymology_Hotlinks, Create_mnuDataTree_Etymology_Hotlinks);
		}

		private void RegisterOrdinaryContextMenus()
		{
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(mnuDataTree_Sense, Create_mnuDataTree_Sense);
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(mnuDataTree_Etymology, Create_mnuDataTree_Etymology);
		}

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
		}

		#endregion

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
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
				return; // No need to do it more than once.

			if (disposing)
			{
				_lexiconAreaMenuHelper.Dispose();
				foreach (var menuTuple in _newEditMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_editMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newEditMenusAndHandlers.Clear();

				foreach (var menuTuple in _newInsertMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_insertMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newInsertMenusAndHandlers.Clear();

				_insertEntryToolStripButton.Click -= Insert_Entry_Clicked;
				InsertToolbarManager.DeactivateInsertToolbar(_majorFlexComponentParameters);
				_insertEntryToolStripButton.Dispose();
			}
			_lexiconAreaMenuHelper = null;
			_majorFlexComponentParameters = null;
			_insertMenu = null;
			_insertEntryToolStripButton = null;
			_newInsertMenusAndHandlers = null;
			SliceContextMenuFactory = null;
			DataTree = null;
			RecordClerk = null;
			InnerMultiPane = null;

			_isDisposed = true;
		}
		#endregion

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Sense_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_Sense_Hotlinks)
			{
				throw new ArgumentException($"Expected argmuent value of '{mnuDataTree_Sense_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

			// <item command="CmdDataTree-Insert-Example"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Example_Clicked, LexiconResources.Insert_Example);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_SenseBelow_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			return hotlinksMenuItemList;
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Etymology_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_Etymology_Hotlinks)
			{
				throw new ArgumentException($"Expected argmuent value of '{mnuDataTree_Etymology_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
			// <item command="CmdDataTree-Insert-Etymology"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Etymology_Clicked, LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			return hotlinksMenuItemList;
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Etymology(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Etymology">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_Etymology
			};
			contextMenuStrip.Opening += MenuDataTree_EtymologyContextMenuStrip_Opening;
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

			// <item command="CmdDataTree-Insert-Etymology" label="Insert _Etymology"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Etymology_Clicked, LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);
			/*
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-MoveUp-Etymology"/>
			<item command="CmdDataTree-MoveDown-Etymology"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-Delete-Etymology"/>
			 */

			// End: <menu id="mnuDataTree-Etymology">

			return new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MenuDataTree_EtymologyContextMenuStrip_Opening, menuItems);
		}

		private void MenuDataTree_EtymologyContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
			// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private void Insert_Etymology_Clicked(object sender, EventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(LexiconResources.Undo_Insert_Etymology, LexiconResources.Redo_Insert_Etymology, _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				((ILexEntry)RecordClerk.CurrentObject).EtymologyOS.Add(_majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create());
			});
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Sense(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Sense">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_Sense
			};
			contextMenuStrip.Opening += MenuDataTree_SenseContextMenuStrip_Opening;
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(21);

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

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sense_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// <item command="CmdDataTree-Insert-SubSense"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Subsense_Clicked, LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

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

			//<item command="CmdDataTree-Delete-Sense"/>
			var toolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_Sense_Clicked, LexiconResources.DeleteSenseAndSubsenses);
			toolStripMenuItem.Image = LanguageExplorerResources.Delete;
			toolStripMenuItem.ImageTransparentColor = Color.Magenta;

			// TODO: Add below menus.
			/*
				// Plus it gets these more general menus added to it, which should be commmon to all (almost all?) slices:
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
			// <menu id="PaneBar-LexicalDetail" label="">
			// <menu id="LexEntryPaneMenu" icon="MenuWidget">
			// Handled elsewhere: <item label="Show Hidden Fields" boolProperty="ShowHiddenFields-lexiconEdit" defaultVisible="true" settingsGroup="local"/>
			var contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Opening += MainPanelContextMenuStrip_Opening;

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MainPanelContextMenuStrip_Opening, menuItems);

			// Show_Dictionary_Preview menu item.
			var contextMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Show_Dictionary_Preview_Clicked, LexiconResources.Show_DictionaryPubPreview, LexiconResources.Show_DictionaryPubPreview_ToolTip);
			contextMenuItem.Checked = PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Insert_Sense menu item. (CmdInsertSense->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sense_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// Insert Subsense (in sense) menu item. (CmdInsertSubsense->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Subsense_Clicked, LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// Insert _Variant menu item. (CmdInsertVariant->msg: InsertItemViaBackrefVector, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Variant_Clicked, LexiconResources.Insert_Variant, LexiconResources.Insert_Variant_Tooltip);

			// Insert A_llomorph menu item. (CmdDataTree-Insert-AlternateForm->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Allomorph_Clicked, LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip);

			// Insert _Pronunciation menu item. (CmdDataTree-Insert-Pronunciation->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Pronunciation_Clicked, LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);

			// Insert Sound or Movie _File menu item. (CmdInsertMediaFile->msg: InsertMediaFile, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sound_Or_Movie_File_Clicked, LexiconResources.Insert_Sound_Or_Movie_File, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip);

			// Insert _Etymology menu item. (CmdDataTree-Insert-Etymology->msg: DataTreeInsert, also on Insert menu and a hotlionks and another context menu.)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Etymology_Clicked, LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Lexeme Form has components. (CmdChangeToComplexForm->msg: ConvertEntryIntoVariant)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Lexeme_Form_Has_Components_Clicked, LexiconResources.Lexeme_Form_Has_Components, LexiconResources.Lexeme_Form_Has_Components_Tooltip);

			// Lexeme Form is a variant menu item. (CmdChangeToVariant->msg: ConvertEntryIntoComplexForm)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Lexeme_Form_Is_A_Variant_Clicked, LexiconResources.Lexeme_Form_Is_A_Variant, LexiconResources.Lexeme_Form_Is_A_Variant_Tooltip);

			// _Merge with entry... menu item. (CmdMergeEntry->msg: MergeEntry, also on Tool menu)
			contextMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Merge_With_Entry_Clicked, LexiconResources.Merge_With_Entry, LexiconResources.Merge_With_Entry_Tooltip);
			// NB: defaultVisible="false"
			// Original code that controlled: display.Enabled = display.Visible = InFriendlyArea;
			// It is now only in a friendly area, so should always be visible and enabled, per the old code.
			// Trouble is it makes no sense to enable it if the lexicon only has one entry in it, so I'll alter the behavior to be more sensible. ;-)
			contextMenuItem.Enabled = PropertyTable.GetValue<LcmCache>("cache").LanguageProject.LexDbOA.Entries.Any();

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Show Entry in Concordance menu item. (CmdRootEntryJumpToConcordance->msg: JumpToTool, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Show_Entry_In_Concordance_Clicked, LexiconResources.Show_Entry_In_Concordance);

			return retVal;
		}


		private void Insert_Example_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: "CmdDataTree-Insert-Example"
#endif
		}

		private void Insert_SenseBelow_Clicked(object sender, EventArgs e)
		{
			// Get slice and see what sense is currently selected, so we can add the new sense after (read: 'below") it.
			var currentSlice = DataTree.CurrentSlice;
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
			if (currentSense.Owner is ILexSense)
			{
				var owningSense = (ILexSense)currentSense.Owner;
				LexSenseUi.CreateNewLexSense(_majorFlexComponentParameters.LcmCache, owningSense, owningSense.SensesOS.IndexOf(currentSense) + 1);
			}
			else
			{
				var owningEntry = (ILexEntry)RecordClerk.CurrentObject;
				LexSenseUi.CreateNewLexSense(_majorFlexComponentParameters.LcmCache, owningEntry, owningEntry.SensesOS.IndexOf(currentSense) + 1);
			}
		}

		private void MainPanelContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
			// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private void Show_Entry_In_Concordance_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Show Entry In Concordance...");
		}

		private void Delete_Sense_Clicked(object sender, EventArgs e)
		{
			DataTree.CurrentSlice.HandleDeleteCommand();
		}

		private void Merge_With_Entry_Clicked(object sender, EventArgs e)
		{
			var currentObject = RecordClerk.CurrentObject;
			if (currentObject == null)
				return; // should never happen, but nothing we can do if it does!

			var currentEntry = currentObject as ILexEntry ?? currentObject.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
			if (currentEntry == null)
				return;

			using (var dlg = new MergeEntryDlg())
			{
				var window = PropertyTable.GetValue<Form>("window");
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				// <parameters title="Merge Entry" formlabel="_Find:" okbuttonlabel="_Merge"/>
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, XElement.Parse(LexiconResources.MatchingEntriesParameters), currentEntry, LexiconResources.ksMergeEntry, FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Find), FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Merge));
				if (dlg.ShowDialog(window) != DialogResult.OK)
				{
					return;
				}

				var survivor = (ILexEntry)dlg.SelectedObject;
				Debug.Assert(survivor != currentEntry);
				UndoableUnitOfWorkHelper.Do(LexiconResources.ksUndoMergeEntry, LexiconResources.ksRedoMergeEntry, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					survivor.MergeObject(currentEntry, true);
					survivor.DateModified = DateTime.Now;
				});
				MessageBox.Show(window, LexiconResources.ksEntriesHaveBeenMerged, LexiconResources.ksMergeReport, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Lexeme Form Is A Variant...");
		}

		private void Lexeme_Form_Has_Components_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Lexeme Form Has Components...");
		}

		private void Insert_Sound_Or_Movie_File_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Inserting Sound or Movie File...");
		}

		private void Insert_Pronunciation_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Inserting Pronunciation...");
		}

		private void Insert_Allomorph_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Inserting Allomorph...");
		}

		private void Insert_Variant_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Inserting Variant...");
		}

		private void Insert_Subsense_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Inserting Subsense...");
		}

		private void Insert_Sense_Clicked(object sender, EventArgs e)
		{
			LexSenseUi.CreateNewLexSense(_majorFlexComponentParameters.LcmCache, (ILexEntry)RecordClerk.CurrentObject);
		}

		private void Insert_Entry_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new InsertEntryDlg())
			{
				var mainWindow = PropertyTable.GetValue<IFwMainWnd>("window");
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable));
				if (dlg.ShowDialog((Form)mainWindow) == DialogResult.OK)
				{
					ILexEntry entry;
					bool newby;
					dlg.GetDialogInfo(out entry, out newby);
					// No need for a PropChanged here because InsertEntryDlg takes care of that. (LT-3608)
					mainWindow.RefreshAllViews();
					RecordClerk.JumpToRecord(entry.Hvo);
				}
			}
		}

		private void GoToEntry_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new EntryGoDlg())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				var windowParameters = new WindowParams
				{
					m_btnText = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go),
					m_label = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go_To),
					m_title = LexiconResources.Go_To_Entry_Dlg_Title
				};
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, windowParameters);
				dlg.SetHelpTopic("khtpFindLexicalEntry");
				if (dlg.ShowDialog(PropertyTable.GetValue<Form>("window")) == DialogResult.OK)
				{
					RecordClerk.JumpToRecord(dlg.SelectedObject.Hvo);
				}
			}
		}

		private void Show_Dictionary_Preview_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem)sender;
			menuItem.Checked = !menuItem.Checked;
			PropertyTable.SetProperty(Show_DictionaryPubPreview, menuItem.Checked, SettingsGroup.LocalSettings, true, false);
			InnerMultiPane.Panel1Collapsed = !PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);
		}

		private void AddToolbarItems()
		{
			// <item command="CmdInsertLexEntry" defaultVisible="false" />
			_insertEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(Insert_Entry_Clicked, "toolStripButtonInsertEntry", LexiconResources.Major_Entry.ToBitmap(), LexiconResources.Entry_Tooltip);
			// <item command="CmdGoToEntry" defaultVisible="false" />
			_insertGoToEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(GoToEntry_Clicked, "toolStripButtonGoToEntry", LexiconResources.Find_Lexical_Entry.ToBitmap(), LexiconResources.GoToEntryToolTip);

			InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripButton> { _insertEntryToolStripButton, _insertGoToEntryToolStripButton });
		}

		private void AddEditMenuItems()
		{
			_editMenu = MenuServices.GetEditMenu(_majorFlexComponentParameters.MenuStrip);
			// Insert before third separator menu
			// <item command="CmdGoToEntry" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newEditMenusAndHandlers, _editMenu, GoToEntry_Clicked, LexiconResources.Find_Entry, LexiconResources.GoToEntryToolTip, Keys.Control | Keys.F, LexiconResources.Find_Lexical_Entry.ToBitmap(), 10);
		}

		private void AddInsertMenuItems()
		{
			_insertMenu = MenuServices.GetInsertMenu(_majorFlexComponentParameters.MenuStrip);

			var insertIndex = 0;
			// <item command="CmdInsertLexEntry" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Entry_Clicked, LexiconResources.Entry, LexiconResources.Entry_Tooltip, Keys.Control | Keys.E, LexiconResources.Major_Entry.ToBitmap(), insertIndex);
			// <item command="CmdInsertSense" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Sense_Clicked, LexiconResources.Sense, LexiconResources.InsertSenseToolTip, Keys.None, null, ++insertIndex);
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
*/
#endif
			//<item command="CmdDataTree-Insert-Etymology" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Etymology_Clicked, LexiconResources.Etymology, LexiconResources.Insert_Etymology_Tooltip, Keys.None, null, ++insertIndex);
#if RANDYTODO
/*
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
// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
#if RANDYTODO
	// TODO: Consider breaking this one up into three more manager impls: 1) slice hotlinks, 2) slice context menus, and 3) main pane bar context menus.
	// TODO: Maybe it would be only two: 1) slice hotlinks/context menus, and 2) main pane bar context menus
#endif
	/// <summary>
	/// Implementation that supports the addition(s) to FLEx's main View menu for the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolDataTreeStackManager : IToolUiWidgetManager
	{
		private IRecordList MyRecordList { get; set; }
		private Dictionary<string, EventHandler> _sharedEventHandlers;
		private Dictionary<string, EventHandler> _sharedWithMeEventHandlers;
		private SliceContextMenuFactory SliceContextMenuFactory { get; set; }
		private DataTree MyDataTree { get; set; }
		private IPropertyTable _propertyTable;
		private LcmCache _cache;
		private IFwMainWnd _mainWnd;
		private ToolStripMenuItem _show_DictionaryPubPreviewContextMenu;

#region IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList, IReadOnlyDictionary<string, EventHandler> sharedEventHandlers, IReadOnlyList<object> randomParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));
			Guard.AgainstNull(randomParameters, nameof(randomParameters));
			Guard.AssertThat(randomParameters.Count == 2, "Wrong number of random parameters.");

			MyRecordList = recordList;
			_sharedWithMeEventHandlers = new Dictionary<string, EventHandler>(2)
			{
				{ LexiconEditToolConstants.CmdInsertSense, sharedEventHandlers[LexiconEditToolConstants.CmdInsertSense] },
				{ LexiconEditToolConstants.Show_Dictionary_Preview_Clicked, sharedEventHandlers[LexiconEditToolConstants.Show_Dictionary_Preview_Clicked] },
				{ LexiconEditToolConstants.CmdInsertSubsense, sharedEventHandlers[LexiconEditToolConstants.CmdInsertSubsense] },
				{ LexiconEditToolConstants.CmdInsertVariant, sharedEventHandlers[LexiconEditToolConstants.CmdInsertVariant] },
				{ LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm, sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm] },
				{ LexiconEditToolConstants.CmdDataTree_Insert_Pronunciation, sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Pronunciation] },
				{ LexiconEditToolConstants.CmdInsertMediaFile, sharedEventHandlers[LexiconEditToolConstants.CmdInsertMediaFile] },
				{ LexiconEditToolConstants.CmdDataTree_Insert_Etymology, sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Etymology] },
				{ LexiconEditToolConstants.CmdMergeEntry, sharedEventHandlers[LexiconEditToolConstants.CmdMergeEntry] },
				{ LexiconEditToolConstants.CmdInsertExtNote, sharedEventHandlers[LexiconEditToolConstants.CmdInsertExtNote] },
				{ LexiconEditToolConstants.CmdInsertPicture, sharedEventHandlers[LexiconEditToolConstants.CmdInsertPicture] }
			};

			SliceContextMenuFactory = (SliceContextMenuFactory)randomParameters[0];
			MyDataTree = (DataTree)randomParameters[1];

			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			_cache = majorFlexComponentParameters.LcmCache;
			_mainWnd = majorFlexComponentParameters.MainWindow;

			RegisterHotLinkMenus();
			RegisterOrdinaryContextMenus();
			SliceContextMenuFactory.RegisterPanelMenuCreatorMethod(LexiconEditToolConstants.PanelMenuId, CreateMainPanelContextMenuStrip);
		}

		/// <inheritdoc />
		IReadOnlyDictionary<string, EventHandler> IToolUiWidgetManager.SharedEventHandlers => _sharedEventHandlers ?? (_sharedEventHandlers = new Dictionary<string, EventHandler>());

#endregion

#region IDisposable

		private bool _isDisposed;

		~LexiconEditToolDataTreeStackManager()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
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
			{
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
				if (_show_DictionaryPubPreviewContextMenu != null)
				{
					_show_DictionaryPubPreviewContextMenu.Click -= _sharedWithMeEventHandlers[LexiconEditToolConstants.Show_Dictionary_Preview_Clicked];
					_show_DictionaryPubPreviewContextMenu.Dispose();
				}
				_sharedEventHandlers.Clear();
				_sharedWithMeEventHandlers.Clear();
			}
			MyRecordList = null;
			_sharedEventHandlers = null;
			_sharedWithMeEventHandlers = null;
			SliceContextMenuFactory = null;
			MyDataTree = null;
			_propertyTable = null;
			_cache = null;
			_mainWnd = null;
			_show_DictionaryPubPreviewContextMenu = null;

			_isDisposed = true;
		}

#endregion

#region main panel handling

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
		{
			// <menu id="PaneBar-LexicalDetail" label="">
			// <menu id="LexEntryPaneMenu" icon="MenuWidget">
			// Handled elsewhere: <item label="Show Hidden Fields" boolProperty="ShowHiddenFields-lexiconEdit" defaultVisible="true" settingsGroup="local"/>
			var contextMenuStrip = new ContextMenuStrip();

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, null, menuItems);

			// Show_Dictionary_Preview menu item.
			_show_DictionaryPubPreviewContextMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.Show_Dictionary_Preview_Clicked], LexiconResources.Show_DictionaryPubPreview, LexiconResources.Show_DictionaryPubPreview_ToolTip);
			_show_DictionaryPubPreviewContextMenu.Checked = _propertyTable.GetValue<bool>(LexiconEditToolConstants.Show_DictionaryPubPreview);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Insert_Sense menu item. (CmdInsertSense->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertSense], LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// Insert Subsense (in sense) menu item. (CmdInsertSubsense->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertSubsense], LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// Insert _Variant menu item. (CmdInsertVariant->msg: InsertItemViaBackrefVector, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertVariant], LexiconResources.Insert_Variant, LexiconResources.Insert_Variant_Tooltip);

			// Insert A_llomorph menu item. (CmdDataTree-Insert-AlternateForm->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm], LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip);

			// Insert _Pronunciation menu item. (CmdDataTree-Insert-Pronunciation->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Pronunciation], LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);

			// Insert Sound or Movie _File menu item. (CmdInsertMediaFile->msg: InsertMediaFile, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertMediaFile], LexiconResources.Insert_Sound_Or_Movie_File, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip);

			// Insert _Etymology menu item. (CmdDataTree-Insert-Etymology->msg: DataTreeInsert, also on Insert menu and a hotlionks and another context menu.)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Etymology], LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Lexeme Form has components. (CmdChangeToComplexForm->msg: ConvertEntryIntoVariant)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Lexeme_Form_Has_Components_Clicked, LexiconResources.Lexeme_Form_Has_Components, LexiconResources.Lexeme_Form_Has_Components_Tooltip);

			// Lexeme Form is a variant menu item. (CmdChangeToVariant->msg: ConvertEntryIntoComplexForm)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Lexeme_Form_Is_A_Variant_Clicked, LexiconResources.Lexeme_Form_Is_A_Variant, LexiconResources.Lexeme_Form_Is_A_Variant_Tooltip);

			// _Merge with entry... menu item. (CmdMergeEntry->msg: MergeEntry, also on Tool menu)
			var contextMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdMergeEntry], LexiconResources.Merge_With_Entry, LexiconResources.Merge_With_Entry_Tooltip);
			// Original code that controlled: display.Enabled = display.Visible = InFriendlyArea;
			// It is now only in a friendly area, so should always be visible and enabled, per the old code.
			// Trouble is it makes no sense to enable it if the lexicon only has one entry in it, so I'll alter the behavior to be more sensible. ;-)
			contextMenuItem.Enabled = _cache.LanguageProject.LexDbOA.Entries.Any();

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Show Entry in Concordance menu item. (CmdRootEntryJumpToConcordance->msg: JumpToTool)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Show_Entry_In_Concordance_Clicked, LexiconResources.Show_Entry_In_Concordance);

			return retVal;
		}

		private void Show_Entry_In_Concordance_Clicked(object sender, EventArgs e)
		{
			// Show Entry in Concordance menu item. (CmdRootEntryJumpToConcordance->msg: JumpToTool)
			MessageBox.Show((Form)_mainWnd, "Show Entry In Concordance...");
		}

		private void Lexeme_Form_Is_A_Variant_Clicked(object sender, EventArgs e)
		{
			// Lexeme Form is a variant menu item. (CmdChangeToVariant->msg: ConvertEntryIntoComplexForm)
			MessageBox.Show((Form)_mainWnd, "Lexeme Form Is A Variant...");
		}

		private void Lexeme_Form_Has_Components_Clicked(object sender, EventArgs e)
		{
			// Lexeme Form has components. (CmdChangeToComplexForm->msg: ConvertEntryIntoVariant)
			MessageBox.Show((Form)_mainWnd, "Lexeme Form Has Components...");
		}

#endregion

#region hotlinks

		private void RegisterHotLinkMenus()
		{
			SliceContextMenuFactory.RegisterHotlinksMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Sense_Hotlinks, Create_mnuDataTree_Sense_Hotlinks);
			SliceContextMenuFactory.RegisterHotlinksMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Etymology_Hotlinks, Create_mnuDataTree_Etymology_Hotlinks);
			SliceContextMenuFactory.RegisterHotlinksMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_AlternateForms_Hotlinks, Create_mnuDataTree_AlternateForms_Hotlinks);
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Sense_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != LexiconEditToolConstants.mnuDataTree_Sense_Hotlinks)
			{
				throw new ArgumentException($"Expected argmuent value of '{LexiconEditToolConstants.mnuDataTree_Sense_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

			// <item command="CmdDataTree-Insert-Example"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Example_Clicked, LexiconResources.Insert_Example);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_SenseBelow_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			return hotlinksMenuItemList;
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
			var currentSlice = MyDataTree.CurrentSlice;
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
				LexSenseUi.CreateNewLexSense(_cache, owningSense, owningSense.SensesOS.IndexOf(currentSense) + 1);
			}
			else
			{
				var owningEntry = (ILexEntry)MyRecordList.CurrentObject;
				LexSenseUi.CreateNewLexSense(_cache, owningEntry, owningEntry.SensesOS.IndexOf(currentSense) + 1);
			}
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Etymology_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != LexiconEditToolConstants.mnuDataTree_Etymology_Hotlinks)
			{
				throw new ArgumentException($"Expected argmuent value of '{LexiconEditToolConstants.mnuDataTree_Etymology_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
			// <item command="CmdDataTree-Insert-Etymology"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Etymology], LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			return hotlinksMenuItemList;
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_AlternateForms_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != LexiconEditToolConstants.mnuDataTree_AlternateForms_Hotlinks)
			{
				throw new ArgumentException($"Expected argmuent value of '{LexiconEditToolConstants.mnuDataTree_AlternateForms_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <item command="CmdDataTree-Insert-AlternateForm"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm], LexiconResources.Insert_Allomorph);

			return hotlinksMenuItemList;
		}

#endregion

#region slice context menus

		private void RegisterOrdinaryContextMenus()
		{
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Sense, Create_mnuDataTree_Sense);
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Etymology, Create_mnuDataTree_Etymology);
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_AlternateForms, Create_mnuDataTree_AlternateForms);
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Pronunciation, Create_mnuDataTree_Pronunciation);
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Sense(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Sense">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Sense
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
			*/
			// TODO: Add above menus.

			// <item command="CmdDataTree-Insert-ExtNote"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertExtNote], LexiconResources.Insert_Extended_Note);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertSense], LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// <item command="CmdDataTree-Insert-SubSense"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertSubsense], LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// <item command="CmdInsertPicture" label="Insert _Picture" defaultVisible="false"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertPicture], LexiconResources.Insert_Picture, LexiconResources.Insert_Picture_Tooltip);

			// TODO: Add below menus.
			/*
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
			// End: <menu id="mnuDataTree-Sense">

			return new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MenuDataTree_SenseContextMenuStrip_Opening, menuItems);
		}

		private void MenuDataTree_SenseContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
			// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private void Delete_Sense_Clicked(object sender, EventArgs e)
		{
			MyDataTree.CurrentSlice.HandleDeleteCommand();
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Etymology(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Etymology">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Etymology
			};
			contextMenuStrip.Opening += MenuDataTree_EtymologyContextMenuStrip_Opening;
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

			// <item command="CmdDataTree-Insert-Etymology" label="Insert _Etymology"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Etymology], LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);
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

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_AlternateForms(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-AlternateForms">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_AlternateForms
			};
			contextMenuStrip.Opening += MenuDataTree_AlternateFormsContextMenuStrip_Opening;
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);
			// <item command="CmdDataTree-Insert-AlternateForm"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm], LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip);
			/*
			<item command="CmdDataTree-Insert-AffixProcess"/>
			<command id="CmdDataTree-Insert-AffixProcess" label="Insert Affix Process" message="DataTreeInsert">
				<parameters field="AlternateForms" className="MoAffixProcess"/>
			</command>
			*/
			// End: <menu id="mnuDataTree-AlternateForms">

			return new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MenuDataTree_AlternateFormsContextMenuStrip_Opening, menuItems);
		}

		private void MenuDataTree_AlternateFormsContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
			// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Pronunciation(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Pronunciation">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_AlternateForms
			};
			contextMenuStrip.Opening += MenuDataTree_PronunciationContextMenuStrip_Opening;
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);
			// <item command="CmdDataTree-Insert-Pronunciation"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Pronunciation], LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);
			/*
			<item command="CmdInsertMediaFile" label="Insert _Sound or Movie" defaultVisible="false"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-MoveUp-Pronunciation"/>
			<item command="CmdDataTree-MoveDown-Pronunciation"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-Delete-Pronunciation"/>
			<item label="-" translate="do not translate"/>
			*/
			// End: <menu id="mnuDataTree-Pronunciation>

			return new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MenuDataTree_PronunciationContextMenuStrip_Opening, menuItems);
		}

		private void MenuDataTree_PronunciationContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
			// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		#endregion
	}
}
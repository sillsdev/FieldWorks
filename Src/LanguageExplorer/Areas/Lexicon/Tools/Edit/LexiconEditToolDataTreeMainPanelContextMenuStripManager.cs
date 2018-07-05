// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to the DataTree's top panel's context menus for the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolDataTreeMainPanelContextMenuStripManager : IToolUiWidgetManager
	{
		private IRecordList MyRecordList { get; set; }
		private ISharedEventHandlers _sharedEventHandlers;
		private DataTreeStackContextMenuFactory MyDataTreeStackContextMenuFactory { get; set; }
		private IPropertyTable _propertyTable;
		private IPublisher _publisher;
		private LcmCache _cache;
		private ToolStripMenuItem _show_DictionaryPubPreviewContextMenu;

		internal LexiconEditToolDataTreeMainPanelContextMenuStripManager(DataTreeStackContextMenuFactory dataTreeStackContextMenuFactory)
		{
			Guard.AgainstNull(dataTreeStackContextMenuFactory, nameof(dataTreeStackContextMenuFactory));

			MyDataTreeStackContextMenuFactory = dataTreeStackContextMenuFactory;
		}

		#region IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			_publisher = majorFlexComponentParameters.FlexComponentParameters.Publisher;
			_cache = majorFlexComponentParameters.LcmCache;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			MyRecordList = recordList;

			MyDataTreeStackContextMenuFactory.MainPanelMenuContextMenuFactory.RegisterPanelMenuCreatorMethod(LexiconEditToolConstants.PanelMenuId, CreateMainPanelContextMenuStrip);
		}

		#endregion

		#region IDisposable

		private bool _isDisposed;

		~LexiconEditToolDataTreeMainPanelContextMenuStripManager()
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
					_show_DictionaryPubPreviewContextMenu.Click -= _sharedEventHandlers.Get(LexiconEditToolConstants.Show_Dictionary_Preview_Clicked);
					_show_DictionaryPubPreviewContextMenu.Dispose();
				}
			}
			MyRecordList = null;
			_sharedEventHandlers = null;
			MyDataTreeStackContextMenuFactory = null;
			_propertyTable = null;
			_cache = null;
			_show_DictionaryPubPreviewContextMenu = null;

			_isDisposed = true;
		}

		#endregion

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
		{
			// <menu id="PaneBar-LexicalDetail" label="">
			// <menu id="LexEntryPaneMenu" icon="MenuWidget">
			// Handled elsewhere: <item label="Show Hidden Fields" boolProperty="ShowHiddenFields-lexiconEdit" defaultVisible="true" settingsGroup="local"/>
			var contextMenuStrip = new ContextMenuStrip();

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);

			// Show_Dictionary_Preview menu item.
			_show_DictionaryPubPreviewContextMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.Show_Dictionary_Preview_Clicked), LexiconResources.Show_DictionaryPubPreview, LexiconResources.Show_DictionaryPubPreview_ToolTip);
			_show_DictionaryPubPreviewContextMenu.Checked = _propertyTable.GetValue<bool>(LexiconEditToolConstants.Show_DictionaryPubPreview);

			// Separator
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// Insert_Sense menu item. (CmdInsertSense->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertSense), LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// Insert Subsense (in sense) menu item. (CmdInsertSubsense->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertSubsense), LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// Insert _Variant menu item. (CmdInsertVariant->msg: InsertItemViaBackrefVector, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertVariant), LexiconResources.Insert_Variant, LexiconResources.Insert_Variant_Tooltip);

			// Insert A_llomorph menu item. (CmdDataTree-Insert-AlternateForm->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm), LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip);

			// Insert _Pronunciation menu item. (CmdDataTree-Insert-Pronunciation->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdDataTree_Insert_Pronunciation), LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);

			// Insert Sound or Movie _File menu item. (CmdInsertMediaFile->msg: InsertMediaFile, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertMediaFile), LexiconResources.Insert_Sound_Or_Movie_File, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip);

			// Insert _Etymology menu item. (CmdDataTree-Insert-Etymology->msg: DataTreeInsert, also on Insert menu and a hotlionks and another context menu.)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdDataTree_Insert_Etymology), LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			// Separator
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// Lexeme Form has components. (CmdChangeToComplexForm->msg: ConvertEntryIntoComplexForm)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdChangeToComplexForm_Clicked, LexiconResources.Lexeme_Form_Has_Components, LexiconResources.Lexeme_Form_Has_Components_Tooltip);

			// Lexeme Form is a variant menu item. (CmdChangeToVariant->msg: ConvertEntryIntoVariant)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdChangeToVariant_Clicked, LexiconResources.Lexeme_Form_Is_A_Variant, LexiconResources.Lexeme_Form_Is_A_Variant_Tooltip);

			// _Merge with entry... menu item. (CmdMergeEntry->msg: MergeEntry, also on Tool menu)
			var contextMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdMergeEntry), LexiconResources.Merge_With_Entry, LexiconResources.Merge_With_Entry_Tooltip);
			// Original code that controlled: display.Enabled = display.Visible = InFriendlyArea;
			// It is now only in a friendly area, so should always be visible and enabled, per the old code.
			// Trouble is it makes no sense to enable it if the lexicon only has one entry in it, so I'll alter the behavior to be more sensible. ;-)
			contextMenuItem.Enabled = _cache.LanguageProject.LexDbOA.Entries.Any();

			// Separator
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// Show Entry in Concordance menu item. (CmdRootEntryJumpToConcordance->msg: JumpToTool)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdRootEntryJumpToConcordance_Clicked, LexiconResources.Show_Entry_In_Concordance);

			return retVal;
		}

		private void CmdRootEntryJumpToConcordance_Clicked(object sender, EventArgs e)
		{
			// CreateMainPanelContextMenuStrip
			// Show Entry in Concordance menu item. (CmdRootEntryJumpToConcordance->msg: JumpToTool)
			LinkHandler.PublishFollowLinkMessage(_publisher, new FwLinkArgs(AreaServices.ConcordanceMachineName, MyRecordList.CurrentObject.Guid));
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
			AreaServices.UndoExtension(uowBase, _cache.ActionHandlerAccessor, () =>
			{
				var entry = (ILexEntry)MyRecordList.CurrentObject;
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
	}
}

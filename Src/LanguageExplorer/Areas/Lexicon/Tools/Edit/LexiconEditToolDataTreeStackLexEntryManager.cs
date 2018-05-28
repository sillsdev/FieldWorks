// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to the DataTree's context menus and hotlinks for a LexEntry, and objects it owns, in the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolDataTreeStackLexEntryManager : IToolUiWidgetManager
	{
		private const string LexSenseManager = "LexSenseManager";
		internal const string mnuDataTree_Etymology_Hotlinks = "mnuDataTree-Etymology-Hotlinks";
		private const string mnuDataTree_AlternateForms_Hotlinks = "mnuDataTree-AlternateForms-Hotlinks";
		private const string mnuDataTree_VariantForms_Hotlinks = "mnuDataTree-VariantForms-Hotlinks";
		private const string mnuDataTree_LexemeFormContext = "mnuDataTree-LexemeFormContext";
		private Dictionary<string, EventHandler> _sharedEventHandlers;
		private IRecordList MyRecordList { get; set; }
		private DataTree MyDataTree { get; set; }
		private IPropertyTable _propertyTable;
		private IPublisher _publisher;
		private IFwMainWnd _mainWindow;
		private LcmCache _cache;
		private Dictionary<string, IToolUiWidgetManager> _dataTreeWidgetManagers;

		internal LexiconEditToolDataTreeStackLexEntryManager(DataTree dataTree)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));

			MyDataTree = dataTree;

			_dataTreeWidgetManagers = new Dictionary<string, IToolUiWidgetManager>
			{
				{ LexSenseManager, new LexiconEditToolDataTreeStackLexSenseManager(dataTree) }
			};
		}

		#region Implementation of IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, Dictionary<string, EventHandler> sharedEventHandlers, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));
			Guard.AgainstNull(recordList, nameof(recordList));

			_mainWindow = majorFlexComponentParameters.MainWindow;
			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			_publisher = majorFlexComponentParameters.FlexComponentParameters.Publisher;
			_cache = majorFlexComponentParameters.LcmCache;
			_sharedEventHandlers = sharedEventHandlers;
			MyRecordList = recordList;

			RegisterSliceMenus();

			foreach (var manager in _dataTreeWidgetManagers.Values)
			{
				manager.Initialize(majorFlexComponentParameters, sharedEventHandlers, recordList);
			}
		}

		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~LexiconEditToolDataTreeStackLexEntryManager()
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
				foreach (var manager in _dataTreeWidgetManagers.Values)
				{
					manager.Dispose();
				}
				_dataTreeWidgetManagers.Clear();
			}
			_sharedEventHandlers = null;
			MyRecordList = null;
			MyDataTree = null;
			_cache = null;
			_dataTreeWidgetManagers = null;

			_isDisposed = true;
		}
		#endregion

		private void RegisterSliceMenus()
		{
			// Slice stack from LexEntry.fwlayout (less senses, which are handled in another manager class).
			Register_LexemeForm_Bundle();

			// TODO: These two methods will get revised, as I get to the remaining bundle(s) of slices.
			RegisterHotLinkMenus();
			RegisterPopupSliceMenus();
			/*
			<part label="Citation Form" ref="CitationFormAllV"/>
			<part ref="DialectLabelsEntry" visibility="ifdata"/>
			<part ref="ComplexFormEntries" visibility="always"/>
			<part ref="EntryRefs" param="Normal" visibility="ifdata"/>
			<part ref="EntryRefsGhostComponents" visibility="always"/>
			<part ref="EntryRefsGhostVariantOf" visibility="never"/>
			<part ref="Pronunciations" param="Normal" visibility="ifdata"/>
			*/
				MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Pronunciation, Create_mnuDataTree_Pronunciation);
			/*
			<part ref="Etymologies" param="Normal" visibility="ifdata" />
			*/
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Etymology, Create_mnuDataTree_Etymology);
			/*
		   <part ref="CommentAllA"/>
		   <part ref="LiteralMeaningAllA"  visibility="ifdata"/>
		   <!-- Only for Subentries. -->
		   <part ref="BibliographyAllA"   visibility="ifdata" />
		   <part ref="RestrictionsAllA"   visibility="ifdata" />
		   <part ref="SummaryDefinitionAllA" visibility="ifdata"/>

		   <part ref="CurrentLexReferences"   visibility="ifdata" />

		   <!-- Special part to indicate where custom fields should be inserted at.  Handled in Common.Framework.DetailControls.DataTree -->
		   <part ref="_CustomFieldPlaceholder" customFields="here" />

		   <part ref="ImportResidue" label="Import Residue" visibility="ifdata"/>
		   <part ref="DateCreatedAllA"  visibility="never"/>
		   <part ref="DateModifiedAllA"  visibility="never"/>
		   <part ref="Messages" visibility="always"/>

		   // NB: Senses go here. But, another manager worries about them.

		   <part ref="VariantFormsSection" expansion="expanded" label="Variants" menu="mnuDataTree-VariantForms" hotlinks="mnuDataTree-VariantForms-Hotlinks">
			   <indent>
				   <part ref="VariantForms"/>
			   </indent>
		   </part>
		   <part ref="AlternateFormsSection" expansion="expanded" label="Allomorphs" menu="mnuDataTree-AlternateForms" hotlinks="mnuDataTree-AlternateForms-Hotlinks">
			   <indent>
				   <part ref="AlternateForms" param="Normal"/>
			   </indent>
		   </part>
		   */
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_AlternateForms, Create_mnuDataTree_AlternateForms);
			/*
			<part ref="GrammaticalFunctionsSection" label="Grammatical Info. Details" menu="mnuDataTree-Help" hotlinks="mnuDataTree-Help">
				<indent>
					<part ref="MorphoSyntaxAnalyses" param="Normal"/>
				</indent>
			</part>
			<part ref="PublicationSection" label="Publication Settings" menu="mnuDataTree-Help" hotlinks="mnuDataTree-Help">
				<indent>
					<part ref="PublishIn"   visibility="always" />
					<part ref="ShowMainEntryIn" label="Show As Headword In" visibility="always" />
					<part ref="EntryRefs" param="Publication" visibility="ifdata"/>
					<part ref="ShowMinorEntry"/>
					<part ref="Subentries" visibility="ifdata"/>
					<part ref="VisibleComplexFormEntries" visibility="ifdata"/>
				</indent>
			</part>
				*/
		}

		#region hotlinks

		private void RegisterHotLinkMenus()
		{
			// mnuDataTree-VariantForms-Hotlinks (mnuDataTree_VariantForms_Hotlinks)
			MyDataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_VariantForms_Hotlinks, Create_mnuDataTree_VariantForms_Hotlinks);
			// mnuDataTree-Help (x2) // Note: I don't see any help hotlinks on those two slices.
			MyDataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Etymology_Hotlinks, Create_mnuDataTree_Etymology_Hotlinks);
			MyDataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_AlternateForms_Hotlinks, Create_mnuDataTree_AlternateForms_Hotlinks);
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_VariantForms_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_VariantForms_Hotlinks)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_VariantForms_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}

			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
			// NB: "CmdDataTree-Insert-VariantForm" is also used in two ordinary slice menus, which are defined in this class, so no need to add to shares.
			// Real work is the same as the Insert Variant Insert menu item.
			// <item command="CmdDataTree-Insert-VariantForm"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertVariant], LexiconResources.Insert_Variant);

			return hotlinksMenuItemList;
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Etymology_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_Etymology_Hotlinks)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_Etymology_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
			// <item command="CmdDataTree-Insert-Etymology"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Etymology], LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			return hotlinksMenuItemList;
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_AlternateForms_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_AlternateForms_Hotlinks)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_AlternateForms_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <item command="CmdDataTree-Insert-AlternateForm"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm], LexiconResources.Insert_Allomorph);

			return hotlinksMenuItemList;
		}

		#endregion

		#region ordinary slice menus

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
			//		Needs: menu="mnuDataTree-LexemeForm".
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_LexemeForm, Create_mnuDataTree_LexemeForm);
			// 2. <part ref="PhoneEnvBasic" visibility="ifdata"/>
			//		Needs: menu="mnuDataTree-Environments-Insert".
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Environments_Insert, Create_mnuDataTree_Environments_Insert);

			#endregion left edge menus

			#region hotlinks
			// No hotlinks in tjhis bundle of slices.
			#endregion hotlinks

			#region right click popups

			// "mnuDataTree-LexemeFormContext" (right click menu)
			MyDataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(mnuDataTree_LexemeFormContext, Create_mnuDataTree_LexemeFormContext_RightClick);

			#endregion right click popups
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_LexemeForm(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_LexemeForm)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_LexemeForm}', but got '{nameof(contextMenuId)}' instead.");
			}

			// Start: <menu id="mnuDataTree-LexemeForm">
			var mnuDataTree_LexemeForm = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_LexemeForm
			};
			var entry = (ILexEntry)MyRecordList.CurrentObject;
			var hasAllomorphs = entry.AlternateFormsOS.Any();
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(4);

			// <item command="CmdMorphJumpToConcordance" label="Show Lexeme Form in Concordance"/> // NB: Overrides command's label here.
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, mnuDataTree_LexemeForm, CmdMorphJumpToConcordance_Clicked, LexiconResources.Show_Lexeme_Form_in_Concordance);
			menu.Visible = true;
			menu.Enabled = true;

			// <command id="CmdDataTree-Swap-LexemeForm" label="Swap Lexeme Form with Allomorph..." message="SwapLexemeWithAllomorph">
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, mnuDataTree_LexemeForm, CmdDataTree_Swap_LexemeForm_Clicked, LexiconResources.Swap_Lexeme_Form_with_Allomorph);
			menu.Visible = hasAllomorphs;
			menu.Enabled = hasAllomorphs;

			// <command id="CmdDataTree-Convert-LexemeForm-AffixProcess" label="Convert to Affix Process" message="ConvertLexemeForm"><parameters fromClassName="MoAffixAllomorph" toClassName="MoAffixProcess"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, mnuDataTree_LexemeForm, CmdDataTree_Convert_LexemeForm_AffixProcess_Clicked, LexiconResources.Convert_to_Affix_Process);
			var mmt = entry.PrimaryMorphType;
			var enabled = hasAllomorphs && mmt != null && mmt.IsAffixType;
			menu.Visible = enabled;
			menu.Enabled = enabled;

			// <command id="CmdDataTree-Convert-LexemeForm-AffixAllomorph" label="Convert to Affix Form" message="ConvertLexemeForm"><parameters fromClassName="MoAffixProcess" toClassName="MoAffixAllomorph"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, mnuDataTree_LexemeForm, CmdDataTree_Convert_LexemeForm_AffixAllomorph_Clicked, LexiconResources.Convert_to_Affix_Form);
			enabled = hasAllomorphs && entry.AlternateFormsOS[0] is IMoAffixAllomorph;
			menu.Visible = enabled;
			menu.Enabled = enabled;

			// End: <menu id="mnuDataTree-LexemeForm">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(mnuDataTree_LexemeForm, menuItems);
		}

		private void CmdMorphJumpToConcordance_Clicked(object sender, EventArgs e)
		{
			var commands = new List<string>
			{
				"AboutToFollowLink",
				"FollowLink"
			};
			var parms = new List<object>
			{
				null,
				new FwLinkArgs("concordance", MyDataTree.CurrentSlice.MyCmObject.Guid)
			};
			_publisher.Publish(commands, parms);
		}

		private void CmdDataTree_Swap_LexemeForm_Clicked(object sender, EventArgs e)
		{
			var entry = (ILexEntry)MyRecordList.CurrentObject;
			using (new WaitCursor((Form)_mainWindow))
			{
				using (var dlg = new SwapLexemeWithAllomorphDlg())
				{
					dlg.SetDlgInfo(_cache, _propertyTable, entry);
					if (DialogResult.OK == dlg.ShowDialog((Form)_mainWindow))
					{
						SwapAllomorphWithLexeme(entry, dlg.SelectedAllomorph, LexiconResources.Swap_Lexeme_Form_with_Allomorph);
					}
				}
			}
		}

		private void SwapAllomorphWithLexeme(ILexEntry entry, IMoForm allomorph, string uowBase)
		{
			UndoableUnitOfWorkHelper.Do(string.Format(LanguageExplorerResources.Undo_0, uowBase), string.Format(LanguageExplorerResources.Redo_0, uowBase), entry, () =>
			{
				entry.AlternateFormsOS.Insert(allomorph.IndexInOwner, entry.LexemeFormOA);
				entry.LexemeFormOA = allomorph;
			});
		}

		private void CmdDataTree_Convert_LexemeForm_AffixProcess_Clicked(object sender, EventArgs e)
		{
			Convert_LexemeForm(MoAffixProcessTags.kClassId);
		}

		private void Convert_LexemeForm(int toClsid)
		{
			var entry = (ILexEntry)MyRecordList.CurrentObject;
			if (CheckForFormDataLoss(entry.LexemeFormOA))
			{
				IMoForm newForm = null;
				using (new WaitCursor((Form)_mainWindow))
				{
					UndoableUnitOfWorkHelper.Do(string.Format(LanguageExplorerResources.Undo_0, LexiconResources.Convert_to_Affix_Process), string.Format(LanguageExplorerResources.Redo_0, LexiconResources.Convert_to_Affix_Process), entry, () =>
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
					MyDataTree.RefreshList(false);
				}

				SelectNewFormSlice(newForm);
			}
		}

		private void SelectNewFormSlice(IMoForm newForm)
		{
			foreach (var slice in MyDataTree.Slices)
			{
				if (slice.MyCmObject.Hvo == newForm.Hvo)
				{
					MyDataTree.ActiveControl = slice;
					break;
				}
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

		private void CmdDataTree_Convert_LexemeForm_AffixAllomorph_Clicked(object sender, EventArgs e)
		{
			Convert_LexemeForm(MoAffixAllomorphTags.kClassId);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Environments_Insert(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_Environments_Insert)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_Environments_Insert}', but got '{nameof(contextMenuId)}' instead.");
			}

			// Start: <menu id="mnuDataTree-Environments-Insert">
			// This "mnuDataTree-Environments-Insert" menu is used in four places.
			var mnuDataTree_Environments_Insert = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Environments_Insert
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

			// <command id="CmdDataTree-Insert-Slash" label="Insert Environment slash" message="InsertSlash"/>
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, mnuDataTree_Environments_Insert, CmdDataTree_Insert_Slash_Clicked, LexiconResources.Insert_Environment_slash);
			menu.Enabled = slice is PhoneEnvReferenceSlice ? ((PhoneEnvReferenceSlice)slice).CanInsertSlash : ((PhEnvStrRepresentationSlice)slice).CanInsertSlash;

			// <command id="CmdDataTree-Insert-Underscore" label="Insert Environment bar" message="InsertEnvironmentBar"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, mnuDataTree_Environments_Insert, CmdDataTree_Insert_Underscore_Clicked, LexiconResources.Insert_Environment_bar);
			menu.Enabled = slice is PhoneEnvReferenceSlice ? ((PhoneEnvReferenceSlice)slice).CanInsertEnvironmentBar : ((PhEnvStrRepresentationSlice)slice).CanInsertEnvironmentBar;

			// <command id="CmdDataTree-Insert-NaturalClass" label="Insert Natural Class" message="InsertNaturalClass"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, mnuDataTree_Environments_Insert, CmdDataTree_Insert_NaturalClass_Clicked, LexiconResources.Insert_Natural_Class);
			menu.Enabled = slice is PhoneEnvReferenceSlice ? ((PhoneEnvReferenceSlice)slice).CanInsertNaturalClass : ((PhEnvStrRepresentationSlice)slice).CanInsertNaturalClass;

			// <command id="CmdDataTree-Insert-OptionalItem" label="Insert Optional Item" message="InsertOptionalItem"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, mnuDataTree_Environments_Insert, CmdDataTree_Insert_OptionalItem_Clicked, LexiconResources.Insert_Optional_Item);
			menu.Enabled = slice is PhoneEnvReferenceSlice ? ((PhoneEnvReferenceSlice)slice).CanInsertOptionalItem : ((PhEnvStrRepresentationSlice)slice).CanInsertOptionalItem;

			// <command id="CmdDataTree-Insert-HashMark" label="Insert Word Boundary" message="InsertHashMark"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, mnuDataTree_Environments_Insert, CmdDataTree_Insert_HashMark_Clicked, LexiconResources.Insert_Word_Boundary);
			menu.Enabled = slice is PhoneEnvReferenceSlice ? ((PhoneEnvReferenceSlice)slice).CanInsertHashMark : ((PhEnvStrRepresentationSlice)slice).CanInsertHashMark;

			// End: <menu id="mnuDataTree-Environments-Insert">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(mnuDataTree_Environments_Insert, menuItems);
		}

		private void CmdDataTree_Insert_HashMark_Clicked(object sender, EventArgs e)
		{
			var tag = (Slice)((ToolStripMenuItem)sender).Tag;
			if (tag is PhoneEnvReferenceSlice)
			{
				((PhoneEnvReferenceSlice)tag).InsertHashMark();
			}
			else
			{
				((PhEnvStrRepresentationSlice)tag).InsertHashMark();
			}
		}

		private void CmdDataTree_Insert_OptionalItem_Clicked(object sender, EventArgs e)
		{
			var tag = (Slice)((ToolStripMenuItem)sender).Tag;
			if (tag is PhoneEnvReferenceSlice)
			{
				((PhoneEnvReferenceSlice)tag).InsertOptionalItem();
			}
			else
			{
				((PhEnvStrRepresentationSlice)tag).InsertOptionalItem();
			}
		}

		private void CmdDataTree_Insert_NaturalClass_Clicked(object sender, EventArgs e)
		{
			var tag = (Slice)((ToolStripMenuItem)sender).Tag;
			if (tag is PhoneEnvReferenceSlice)
			{
				((PhoneEnvReferenceSlice)tag).InsertNaturalClass();
			}
			else
			{
				((PhEnvStrRepresentationSlice)tag).InsertNaturalClass();
			}
		}

		private void CmdDataTree_Insert_Underscore_Clicked(object sender, EventArgs e)
		{
			var tag = (Slice)((ToolStripMenuItem)sender).Tag;
			if (tag is PhoneEnvReferenceSlice)
			{
				((PhoneEnvReferenceSlice)tag).InsertEnvironmentBar();
			}
			else
			{
				((PhEnvStrRepresentationSlice)tag).InsertEnvironmentBar();
			}
		}

		private void CmdDataTree_Insert_Slash_Clicked(object sender, EventArgs e)
		{
			var tag = (Slice)((ToolStripMenuItem)sender).Tag;
			if (tag is PhoneEnvReferenceSlice)
			{
				((PhoneEnvReferenceSlice)tag).InsertSlash();
			}
			else
			{
				((PhEnvStrRepresentationSlice)tag).InsertSlash();
			}
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Etymology(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_Etymology)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_Etymology}', but got '{nameof(contextMenuId)}' instead.");
			}

			// Start: <menu id="mnuDataTree-Etymology">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Etymology
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

			// <item command="CmdDataTree-Insert-Etymology" label="Insert _Etymology"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Etymology], LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);
			/*
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-MoveUp-Etymology"/>
			<item command="CmdDataTree-MoveDown-Etymology"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-Delete-Etymology"/>
			 */

			// End: <menu id="mnuDataTree-Etymology">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_AlternateForms(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_AlternateForms)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_AlternateForms}', but got '{nameof(contextMenuId)}' instead.");
			}

			// Start: <menu id="mnuDataTree-AlternateForms">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_AlternateForms
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);
			// <item command="CmdDataTree-Insert-AlternateForm"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm], LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip);
			/*
			<item command="CmdDataTree-Insert-AffixProcess"/>
			<command id="CmdDataTree-Insert-AffixProcess" label="Insert Affix Process" message="DataTreeInsert">
				<parameters field="AlternateForms" className="MoAffixProcess"/>
			</command>
			*/
			// End: <menu id="mnuDataTree-AlternateForms">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Pronunciation(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Pronunciation">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_AlternateForms
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);
			// <item command="CmdDataTree-Insert-Pronunciation"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Pronunciation], LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);
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

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		#endregion ordinary slice menus

		#region popup slice menus

		private void RegisterPopupSliceMenus()
		{
#if RANDYTODO
			// TODO: Do these in some following pass, when I get to those slices.
#endif
			// 1.C: "mnuDataTree-VariantFormContext" (right click menu)
			/*
		<part id="MoForm-Detail-AsVariantForm" type="Detail">
			<slice field="Form" label="Form" editor="multistring" ws="all vernacular" weight="field" spell="no"
				   menu="mnuDataTree-VariantForm" contextMenu="mnuDataTree-VariantFormContext"
				   helpTopicID="khtpField-lexiconEdit-LexEntryRef-VariantLevel-MoStemAllomorph-Form">
				<properties>
					<bold value="on"/>
					<fontsize value="120%"/>
				</properties>
			</slice>
		</part>
		<menu id="mnuDataTree-VariantFormContext">
			<item command="CmdEntryJumpToDefault"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdEntryJumpToConcordance"/>
		</menu>
			*/
			// 1.D: "mnuDataTree-CitationFormContext" (right click menu)
			/*
		<part id="LexEntry-Detail-CitationFormAllV" type="detail">
			<slice field="CitationForm" label="Lexeme" editor="multistring" ws="all vernacular" weight="light"
				   contextMenu="mnuDataTree-CitationFormContext"
				   tooltip="The Citation Form is used to override the Lexeme Form as the headword in the printed dictionary.">
				<!-- removed per LT-4192 properties>
					 <bold value="on" />
					 <fontsize value="150%" />
					 </properties-->
			</slice>
		</part>
		<menu id="mnuDataTree-CitationFormContext">
			<item command="CmdEntryJumpToConcordance"/>
		</menu>
			*/
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_LexemeFormContext_RightClick(Slice slice, string contextMenuId)
		{
			if (contextMenuId != mnuDataTree_LexemeFormContext)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_LexemeFormContext}', but got '{nameof(contextMenuId)}' instead.");
			}

			// Start: <menu id="mnuDataTree-LexemeFormContext">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_LexemeFormContext
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

			/* <item command="CmdEntryJumpToConcordance"/>		<!-- Show Entry in Concordance -->
				<command id="CmdEntryJumpToConcordance" label="Show Entry in Concordance" message="JumpToTool">
					<parameters tool="concordance" className="LexEntry"/>
				</command>
			*/
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdEntryJumpToConcordance_Clicked, LexiconResources.Show_Entry_In_Concordance);
			/* <item command="CmdLexemeFormJumpToConcordance"/>
				<command id="CmdLexemeFormJumpToConcordance" label="Show Lexeme Form in Concordance" message="JumpToTool"> // NB: Also used in: <menu id="mnuReferenceChoices">
					<parameters tool="concordance" className="MoForm"/>
				</command>
			*/
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdLexemeFormJumpToConcordance_Clicked, LexiconResources.Show_Lexeme_Form_in_Concordance);
			/* <item command="CmdDataTree-Swap-LexemeForm"/>
				<command id="CmdDataTree-Swap-LexemeForm" label="Swap Lexeme Form with Allomorph..." message="SwapLexemeWithAllomorph">
					<parameters field="LexemeForm" className="MoForm"/>
				</command>
			*/
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Swap_LexemeForm_Clicked, LexiconResources.Swap_Lexeme_Form_with_Allomorph);

			// End: <menu id="mnuDataTree-LexemeFormContext">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void CmdEntryJumpToConcordance_Clicked(object sender, EventArgs e)
		{
			// Should be a LexEntry
			var commands = new List<string>
			{
				"AboutToFollowLink",
				"FollowLink"
			};
			var parms = new List<object>
			{
				null,
				new FwLinkArgs("concordance", MyRecordList.CurrentObject.Guid)
			};
			_publisher.Publish(commands, parms);
		}

		private void CmdLexemeFormJumpToConcordance_Clicked(object sender, EventArgs e)
		{
			// Should be a MoForm
			var commands = new List<string>
			{
				"AboutToFollowLink",
				"FollowLink"
			};
			var parms = new List<object>
			{
				null,
				new FwLinkArgs("concordance", MyDataTree.CurrentSlice.MyCmObject.Guid)
			};
			_publisher.Publish(commands, parms);
		}
		#endregion popup slice menus
	}
}
// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to the DataTree's context menus and hotlinks for the various MoForms owned by a LexEntry,
	/// and objects they own, in the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolDataTreeStackLexEntryFormsManager : IToolUiWidgetManager
	{
		private const string mnuDataTree_LexemeFormContext = "mnuDataTree-LexemeFormContext";
		private const string mnuDataTree_AlternateForms_Hotlinks = "mnuDataTree-AlternateForms-Hotlinks";
		private const string mnuDataTree_VariantForms_Hotlinks = "mnuDataTree-VariantForms-Hotlinks";
		private const string mnuDataTree_CitationFormContext = "mnuDataTree-CitationFormContext";

		private DataTree MyDataTree { get; set; }
		private ISharedEventHandlers _sharedEventHandlers;
		private IRecordList MyRecordList { get; set; }
		private IPropertyTable _propertyTable;
		private IPublisher _publisher;
		private IFwMainWnd _mainWindow;
		private LcmCache _cache;

		internal LexiconEditToolDataTreeStackLexEntryFormsManager(DataTree dataTree)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));

			MyDataTree = dataTree;
		}

		#region Implementation of IToolUiWidgetManager

		/// <inheritdoc />
		public void Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_mainWindow = majorFlexComponentParameters.MainWindow;
			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			_publisher = majorFlexComponentParameters.FlexComponentParameters.Publisher;
			_cache = majorFlexComponentParameters.LcmCache; ;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			MyRecordList = recordList;

			// Slice stack for the various MoForm instances here and there in a LexEntry.
			Register_LexemeForm_Bundle();
			// CitationForm has a right-click menu.
			Register_CitationForm_Bundle();
			Register_Forms_Sections_Bundle();
		}

		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~LexiconEditToolDataTreeStackLexEntryFormsManager()
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
			}
			MyDataTree = null;
			_sharedEventHandlers = null;
			MyRecordList = null;
			_propertyTable = null;
			_publisher = null;
			_mainWindow = null;
			_cache = null;

			_isDisposed = true;
		}

		#endregion

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
			//		Needs: menu="mnuDataTree-LexemeForm".
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_LexemeForm, Create_mnuDataTree_LexemeForm);
			// 2. <part ref="PhoneEnvBasic" visibility="ifdata"/>
			//		Needs: menu="mnuDataTree-Environments-Insert".
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Environments_Insert, Create_mnuDataTree_Environments_Insert);

			#endregion left edge menus

			#region hotlinks
			// No hotlinks in this bundle of slices.
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
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_LexemeForm}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-LexemeForm">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_LexemeForm
			};
			var entry = (ILexEntry)MyRecordList.CurrentObject;
			var hasAllomorphs = entry.AlternateFormsOS.Any();
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(4);

			// <item command="CmdMorphJumpToConcordance" label="Show Lexeme Form in Concordance"/> // NB: Overrides command's label here.
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdMorphJumpToConcordance_Clicked, LexiconResources.Show_Lexeme_Form_in_Concordance);

			if (hasAllomorphs)
			{
				// <command id="CmdDataTree-Swap-LexemeForm" label="Swap Lexeme Form with Allomorph..." message="SwapLexemeWithAllomorph">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Swap_LexemeForm_Clicked, LexiconResources.Swap_Lexeme_Form_with_Allomorph);
			}

			var mmt = entry.PrimaryMorphType;
			if (hasAllomorphs && mmt != null && mmt.IsAffixType)
			{
				// <command id="CmdDataTree-Convert-LexemeForm-AffixProcess" label="Convert to Affix Process" message="ConvertLexemeForm"><parameters fromClassName="MoAffixAllomorph" toClassName="MoAffixProcess"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Convert_LexemeForm_AffixProcess_Clicked, LexiconResources.Convert_to_Affix_Process);
			}

			if (hasAllomorphs && entry.AlternateFormsOS[0] is IMoAffixAllomorph)
			{
				// <command id="CmdDataTree-Convert-LexemeForm-AffixAllomorph" label="Convert to Affix Form" message="ConvertLexemeForm"><parameters fromClassName="MoAffixProcess" toClassName="MoAffixAllomorph"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Convert_LexemeForm_AffixAllomorph_Clicked, LexiconResources.Convert_to_Affix_Form);
			}

			// End: <menu id="mnuDataTree-LexemeForm">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void CmdMorphJumpToConcordance_Clicked(object sender, EventArgs e)
		{
			LinkHandler.PublishFollowLinkMessage(_publisher, new FwLinkArgs(AreaServices.ConcordanceMachineName, MyDataTree.CurrentSlice.MyCmObject.Guid));
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
			var entry = (ILexEntry)MyRecordList.CurrentObject;
			if (CheckForFormDataLoss(entry.LexemeFormOA))
			{
				IMoForm newForm = null;
				using (new WaitCursor((Form)_mainWindow))
				{
					AreaServices.UndoExtension(LexiconResources.Convert_to_Affix_Process, _cache.ActionHandlerAccessor, () =>
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
			foreach (var slice in MyDataTree.Slices)
			{
				if (slice.MyCmObject.Hvo == newForm.Hvo)
				{
					MyDataTree.ActiveControl = slice;
					break;
				}
			}
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Environments_Insert(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_Environments_Insert)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_Environments_Insert}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-Environments-Insert">
			// This "mnuDataTree-Environments-Insert" menu is used in four places.
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Environments_Insert
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

			// <command id="CmdDataTree-Insert-Slash" label="Insert Environment slash" message="InsertSlash"/>
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.InsertSlash), AreaResources.Insert_Environment_slash);
			menu.Enabled = AreaWideMenuHelper.SliceAsIPhEnvSliceCommon(slice).CanInsertSlash;
			menu.Tag = slice;

			// <command id="CmdDataTree-Insert-Underscore" label="Insert Environment bar" message="InsertEnvironmentBar"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.InsertEnvironmentBar), AreaResources.Insert_Environment_bar);
			menu.Enabled = AreaWideMenuHelper.SliceAsIPhEnvSliceCommon(slice).CanInsertEnvironmentBar;
			menu.Tag = slice;

			// <command id="CmdDataTree-Insert-NaturalClass" label="Insert Natural Class" message="InsertNaturalClass"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.InsertNaturalClass), AreaResources.Insert_Natural_Class);
			menu.Enabled = AreaWideMenuHelper.SliceAsIPhEnvSliceCommon(slice).CanInsertNaturalClass;
			menu.Tag = slice;

			// <command id="CmdDataTree-Insert-OptionalItem" label="Insert Optional Item" message="InsertOptionalItem"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.InsertOptionalItem), AreaResources.Insert_Optional_Item);
			menu.Enabled = AreaWideMenuHelper.SliceAsIPhEnvSliceCommon(slice).CanInsertOptionalItem;
			menu.Tag = slice;

			// <command id="CmdDataTree-Insert-HashMark" label="Insert Word Boundary" message="InsertHashMark"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.InsertHashMark), AreaResources.Insert_Word_Boundary);
			menu.Enabled = AreaWideMenuHelper.SliceAsIPhEnvSliceCommon(slice).CanInsertHashMark;
			menu.Tag = slice;

			// End: <menu id="mnuDataTree-Environments-Insert">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_LexemeFormContext_RightClick(Slice slice, string contextMenuId)
		{
			if (contextMenuId != mnuDataTree_LexemeFormContext)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_LexemeFormContext}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-LexemeFormContext">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_LexemeFormContext
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

			// <item command="CmdEntryJumpToConcordance"/>		<!-- Show Entry in Concordance -->
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.CmdEntryJumpToConcordance), LexiconResources.Show_Entry_In_Concordance);
			// <item command="CmdLexemeFormJumpToConcordance"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdLexemeFormJumpToConcordance_Clicked, LexiconResources.Show_Lexeme_Form_in_Concordance);
			// <item command="CmdDataTree-Swap-LexemeForm"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Swap_LexemeForm_Clicked, LexiconResources.Swap_Lexeme_Form_with_Allomorph);

			// End: <menu id="mnuDataTree-LexemeFormContext">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void CmdLexemeFormJumpToConcordance_Clicked(object sender, EventArgs e)
		{
			// Should be a MoForm
			LinkHandler.PublishFollowLinkMessage(_publisher, new FwLinkArgs(AreaServices.ConcordanceMachineName, MyDataTree.CurrentSlice.MyCmObject.Guid));
		}

		private void CmdDataTree_Swap_LexemeForm_Clicked(object sender, EventArgs e)
		{
			var entry = (ILexEntry)MyRecordList.CurrentObject;
			using (new WaitCursor((Form)_mainWindow))
			using (var dlg = new SwapLexemeWithAllomorphDlg())
			{
				dlg.SetDlgInfo(_cache, _propertyTable, entry);
				if (DialogResult.OK == dlg.ShowDialog((Form)_mainWindow))
				{
					SwapAllomorphWithLexeme(entry, dlg.SelectedAllomorph, LexiconResources.Swap_Lexeme_Form_with_Allomorph);
				}
			}
		}

		private void SwapAllomorphWithLexeme(ILexEntry entry, IMoForm allomorph, string uowBase)
		{
			AreaServices.UndoExtension(uowBase, _cache.ActionHandlerAccessor, () =>
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
			MyDataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory.RegisterPopupContextCreatorMethod(mnuDataTree_CitationFormContext, Create_mnuDataTree_CitationFormContext);

			#endregion right click popups
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_CitationFormContext(Slice slice, string contextMenuId)
		{
			if (contextMenuId != mnuDataTree_CitationFormContext)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_CitationFormContext}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-CitationFormContext">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_CitationFormContext
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <item command="CmdEntryJumpToConcordance"/>		<!-- Show Entry in Concordance -->
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.CmdEntryJumpToConcordance), LexiconResources.Show_Entry_In_Concordance);

			// End: <menu id="mnuDataTree-CitationFormContext">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		#endregion CitationForm

		#region Forms_Sections_Bundle

		private void Register_Forms_Sections_Bundle()
		{
			// mnuDataTree-Allomorph (shared: MoStemAllomorph & MoAffixAllomorph)
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Allomorph, Create_mnuDataTree_Allomorph);

			// mnuDataTree-AffixProcess
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_AffixProcess, Create_mnuDataTree_AffixProcess);

			// mnuDataTree-VariantForm
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_VariantForm, Create_mnuDataTree_VariantForm);

			// mnuDataTree-AlternateForm
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_AlternateForm, Create_mnuDataTree_AlternateForm);

			// mnuDataTree-VariantForms
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_VariantForms, Create_mnuDataTree_VariantForms);
			// mnuDataTree-VariantForms-Hotlinks
			MyDataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_VariantForms_Hotlinks, Create_mnuDataTree_VariantForms_Hotlinks);

			// mnuDataTree-AlternateForms
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_AlternateForms, Create_mnuDataTree_AlternateForms);
			// mnuDataTree-AlternateForms-Hotlinks
			MyDataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_AlternateForms_Hotlinks, Create_mnuDataTree_AlternateForms_Hotlinks);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_AffixProcess(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_AffixProcess)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_AffixProcess}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-AffixProcess">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_AffixProcess
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(8);

			bool visible;
			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <item command="CmdDataTree-MoveUp-Allomorph"/>
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveUpObjectInOwningSequence), LexiconResources.Move_Form_Up, image: imageHolder.smallCommandImages.Images[12]);
				menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);

				// <item command="CmdDataTree-MoveDown-Allomorph"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveDownObjectInOwningSequence), LexiconResources.Move_Form_Down, image: imageHolder.smallCommandImages.Images[14]);
				menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdDataTree-Delete-Allomorph" label="Delete Allomorph" message="DataTreeDelete" icon="Delete">
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Allomorph, _sharedEventHandlers.Get(LexiconAreaConstants.DataTreeDelete));

			// <command id="CmdDataTree-Swap-Allomorph" label="Swap Allomorph with Lexeme Form" message="SwapAllomorphWithLexeme">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, SwapAllomorphWithLexeme_Clicked, LexiconResources.Swap_Allomorph_with_Lexeme_Form);

			visible = slice.MyCmObject.ClassID == MoAffixProcessTags.kClassId;
			if (visible)
			{
				// <command id="CmdDataTree-Convert-Allomorph-AffixAllomorph" label="Convert to Affix Allomorph" message="ConvertAllomorph">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, AffixAllomorph_Clicked, LexiconResources.Convert_to_Affix_Allomorph);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <item command="CmdMorphJumpToConcordance" label="Show Allomorph in Concordance" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdMorphJumpToConcordance_Clicked, LexiconResources.Show_Allomorph_in_Concordance);

			// End: <menu id="mnuDataTree-AffixProcess">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void AffixAllomorph_Clicked(object sender, EventArgs e)
		{
			// <command id="CmdDataTree-Convert-Allomorph-AffixAllomorph" label="Convert to Affix Allomorph" message="ConvertAllomorph">
			// <parameters fromClassName="MoAffixProcess" toClassName="MoAffixAllomorph"/>
			var entry = (ILexEntry)MyDataTree.Root;
			var slice = MyDataTree.CurrentSlice;
			var allomorph = (IMoForm)slice.MyCmObject;
			if (CheckForFormDataLoss(allomorph))
			{
				var mainWindow = _propertyTable.GetValue<Form>("window");
				IMoForm newForm = null;
				using (new WaitCursor(mainWindow))
				{
					AreaServices.UndoExtension(LexiconResources.Convert_to_Affix_Allomorph, _cache.ActionHandlerAccessor, () =>
					{
						newForm = entry.Services.GetInstance<IMoAffixAllomorphFactory>().Create();
						entry.ReplaceMoForm(allomorph, newForm);
					});
					MyDataTree.RefreshList(false);
				}
				SelectNewFormSlice(newForm);
			}
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_VariantForm(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_VariantForm)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_VariantForm}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-VariantForm">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_VariantForm
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(4);

			// <command id="CmdEntryJumpToDefault" label="Show Entry in Lexicon" message="JumpToTool">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdEntryJumpToDefault_Clicked, XMLViewsStrings.ksShowEntryInLexicon);

			// <item command="CmdEntryJumpToConcordance"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.CmdEntryJumpToConcordance), LexiconResources.Show_Entry_In_Concordance);

			if (!slice.IsGhostSlice)
			{
				// <command id="CmdDataTree-Delete-VariantReference" label="Delete Reference" message="DataTreeDeleteReference" icon="Delete">
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDataTree_Delete_VariantReference_Clicked, LexiconResources.Delete_Reference, image: LanguageExplorerResources.Delete);
				menu.Enabled = slice.NextSlice.MyCmObject is ILexEntryRef && (slice.MyCmObject.ClassID == LexEntryTags.kClassId || slice.MyCmObject.Owner.ClassID == LexEntryTags.kClassId);
				menu.ImageTransparentColor = Color.Magenta;
			}

			// <command id="CmdDataTree-Delete-Variant" label="Delete Variant" message="DataTreeDelete" icon="Delete">
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Variant, _sharedEventHandlers.Get(LexiconAreaConstants.DataTreeDelete));

			// End: <menu id="mnuDataTree-VariantForm">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void CmdDataTree_Delete_VariantReference_Clicked(object sender, EventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(AreaResources.ksUndoDeleteRef, AreaResources.ksRedoDeleteRef, _cache.ActionHandlerAccessor, () =>
			{
				var slice = MyDataTree.CurrentSlice;
				var ler = (ILexEntryRef)slice.NextSlice.MyCmObject;
				ler.ComponentLexemesRS.Remove(MyDataTree.Root);
				// probably not needed, but safe...
				if (ler.PrimaryLexemesRS.Contains(MyDataTree.Root))
				{
					ler.PrimaryLexemesRS.Remove(MyDataTree.Root);
				}
				var entry = ler.OwningEntry;
				if (entry.EntryRefsOS.Contains(ler))
				{
					entry.EntryRefsOS.Remove(ler);
				}
			});
		}

		private void CmdEntryJumpToDefault_Clicked(object sender, EventArgs e)
		{
			// <command id="CmdEntryJumpToDefault" label="Show Entry in Lexicon" message="JumpToTool">
			// <parameters tool="lexiconEdit" className="LexEntry"/>
			LinkHandler.PublishFollowLinkMessage(_publisher, new FwLinkArgs(AreaServices.LexiconEditMachineName, MyRecordList.CurrentObject.Guid));
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_AlternateForm(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_AlternateForm)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_AlternateForm}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-AlternateForm">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_AlternateForm
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

			ToolStripMenuItem menu;
			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <command id="CmdDataTree-MoveUp-AlternateForm" label="Move Form _Up" message="MoveUpObjectInSequence" icon="MoveUp">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveUpObjectInOwningSequence), LexiconResources.Move_Form_Up, image: imageHolder.smallCommandImages.Images[12]);
				bool visible;
				menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);
				// <command id="CmdDataTree-MoveDown-AlternateForm" label="Move Form _Down" message="MoveDownObjectInSequence" icon="MoveDown">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveDownObjectInOwningSequence), LexiconResources.Move_Form_Down, image: imageHolder.smallCommandImages.Images[14]);
				menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdDataTree-Merge-AlternateForm" label="Merge AlternateForm into..." message="DataTreeMerge">
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.DataTreeMerge), LexiconResources.Merge_AlternateForm_into);
			menu.Enabled = slice.CanMergeNow;

			// <command id="CmdDataTree-Delete-AlternateForm" label="Delete AlternateForm" message="DataTreeDelete" icon="Delete"> LexiconResources.Delete_Allomorph
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_AlternateForm, _sharedEventHandlers.Get(LexiconAreaConstants.DataTreeDelete));

			// End: <menu id="mnuDataTree-AlternateForm">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Allomorph(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_Allomorph)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_Allomorph}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-Allomorph">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_VariantForms
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(10);

			ToolStripMenuItem menu;
			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <item command="CmdDataTree-MoveUp-Allomorph"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveUpObjectInOwningSequence), LexiconResources.Move_Form_Up, image: imageHolder.smallCommandImages.Images[12]);
				bool visible;
				menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);

				// <item command="CmdDataTree-MoveDown-Allomorph"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveDownObjectInOwningSequence), LexiconResources.Move_Form_Down, image: imageHolder.smallCommandImages.Images[14]);
				menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdDataTree-Merge-Allomorph" label="Merge Allomorph into..." message="DataTreeMerge">
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.DataTreeMerge), LexiconResources.Merge_Allomorph_into);
			menu.Enabled = slice.CanMergeNow;

			// <command id="CmdDataTree-Delete-Allomorph" label="Delete Allomorph" message="DataTreeDelete" icon="Delete">
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Allomorph, _sharedEventHandlers.Get(LexiconAreaConstants.DataTreeDelete));

			// <command id="CmdDataTree-Swap-Allomorph" label="Swap Allomorph with Lexeme Form" message="SwapAllomorphWithLexeme">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, SwapAllomorphWithLexeme_Clicked, LexiconResources.Swap_Allomorph_with_Lexeme_Form);

			if (slice.MyCmObject.ClassID == MoAffixAllomorphTags.kClassId)
			{
				// <command id="CmdDataTree-Convert-Allomorph-AffixProcess" label="Convert to Affix Process" message="ConvertAllomorph">
				// <parameters fromClassName="MoAffixAllomorph" toClassName="MoAffixProcess"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Convert_MoAffixAllomorph_To_MoAffixProcess_Clicked, LexiconResources.Convert_to_Affix_Process);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <item command="CmdMorphJumpToConcordance" label="Show Allomorph in Concordance" a10status="See LexiconEditToolDataTreeStackLexEntryManager: This overrides base label." />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdMorphJumpToConcordance_Clicked, LexiconResources.Show_Allomorph_in_Concordance);

			// End: <menu id="mnuDataTree-Allomorph">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void Convert_MoAffixAllomorph_To_MoAffixProcess_Clicked(object sender, EventArgs e)
		{
			var allomorph = (IMoForm)MyDataTree.CurrentSlice.MyCmObject;
			if (CheckForFormDataLoss(allomorph))
			{
				var mainWindow = _propertyTable.GetValue<Form>("window");
				IMoForm newForm = null;
				using (new WaitCursor(mainWindow))
				{
					AreaServices.UndoExtension(LexiconResources.Convert_to_Affix_Process, _cache.ActionHandlerAccessor, () =>
					{
						var entry = (ILexEntry)MyRecordList.CurrentObject;
						newForm = entry.Services.GetInstance<IMoAffixProcessFactory>().Create();
						entry.ReplaceMoForm(allomorph, newForm);
					});
					MyDataTree.RefreshList(false);
				}
				SelectNewFormSlice(newForm);
			}
		}

		private void SwapAllomorphWithLexeme_Clicked(object sender, EventArgs e)
		{
			var entry = (ILexEntry)MyDataTree.Root;
			AreaServices.UndoExtension(LexiconResources.Swap_Allomorph_with_Lexeme_Form, _cache.ActionHandlerAccessor, () =>
			{
				var allomorph = (IMoForm)MyDataTree.CurrentSlice.MyCmObject;
				entry.AlternateFormsOS.Insert(allomorph.IndexInOwner, entry.LexemeFormOA);
				entry.LexemeFormOA = allomorph;
			});
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_VariantForms(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_VariantForms)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_VariantForms}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-VariantForms">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_VariantForms
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <item command="CmdDataTree-Insert-VariantForm"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertVariant), LexiconResources.Insert_Variant);

			// End: <menu id="mnuDataTree-VariantForms">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_VariantForms_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_VariantForms_Hotlinks)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_VariantForms_Hotlinks}', but got '{hotlinksMenuId}' instead.");
			}

			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
			// NB: "CmdDataTree-Insert-VariantForm" is also used in two ordinary slice menus, which are defined in this class, so no need to add to shares.
			// Real work is the same as the Insert Variant Insert menu item.
			// <item command="CmdDataTree-Insert-VariantForm"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertVariant), LexiconResources.Insert_Variant);

			return hotlinksMenuItemList;
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_AlternateForms(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_AlternateForms)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_AlternateForms}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-AlternateForms">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_AlternateForms
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

			// <item command="CmdDataTree-Insert-AlternateForm"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm), LexiconResources.Insert_Allomorph);

			if (((ILexEntry)MyRecordList.CurrentObject).MorphTypes.FirstOrDefault(mt => mt.IsAffixType) != null)
			{
				// It is only visible/enabled for affixes.
				// <item command="CmdDataTree-Insert-AffixProcess"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Affix_Process_Clicked, LexiconResources.Insert_Affix_Process);
			}

			// End: <menu id="mnuDataTree-AlternateForms">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void Insert_Affix_Process_Clicked(object sender, EventArgs e)
		{
			MyDataTree.CurrentSlice.HandleInsertCommand("AlternateForms", "MoAffixProcess", "LexEntry");
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_AlternateForms_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_AlternateForms_Hotlinks)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_AlternateForms_Hotlinks}', but got '{hotlinksMenuId}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <item command="CmdDataTree-Insert-AlternateForm"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm), LexiconResources.Insert_Allomorph);

			return hotlinksMenuItemList;
		}

		#endregion Forms_Sections_Bundle
	}
}
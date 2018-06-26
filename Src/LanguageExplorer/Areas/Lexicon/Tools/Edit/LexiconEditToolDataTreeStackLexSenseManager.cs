// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to the DataTree's context menus and hotlinks for a LexSense, and objects it owns, in the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolDataTreeStackLexSenseManager : IToolUiWidgetManager
	{
		private const string mnuDataTree_Sense_Hotlinks = "mnuDataTree-Sense-Hotlinks";
		private Dictionary<string, EventHandler> _sharedEventHandlers;
		private IRecordList MyRecordList { get; set; }
		private DataTreeStackContextMenuFactory MyDataTreeStackContextMenuFactory { get; }
		private DataTree MyDataTree { get; }
		private LcmCache _cache;
		private StatusBar _statusBar;
		private IFwMainWnd _mainWnd;
		private FlexComponentParameters _flexComponentParameters;

		internal LexiconEditToolDataTreeStackLexSenseManager(DataTree dataTree)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));

			MyDataTree = dataTree;
			MyDataTreeStackContextMenuFactory = MyDataTree.DataTreeStackContextMenuFactory;
		}

		#region Implementation of IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, Dictionary<string, EventHandler> sharedEventHandlers, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));
			Guard.AgainstNull(recordList, nameof(recordList));

			_cache = majorFlexComponentParameters.LcmCache;
			_statusBar = majorFlexComponentParameters.Statusbar;
			_sharedEventHandlers = sharedEventHandlers;
			MyRecordList = recordList;
			_mainWnd = majorFlexComponentParameters.MainWindow;
			_flexComponentParameters = majorFlexComponentParameters.FlexComponentParameters;

			RegisterHotLinkMenus();
			RegisterSliceLeftEdgeMenus();
			/*
		<indent>
			<part ref="GlossAllA"/>
			<part ref="ReversalEntries"   visibility="ifdata" />
			<part ref="DefinitionAllA"/>
			<part ref="RestrictionsAllA"   visibility="ifdata" />
			<part ref="MsaCombo"/>
			<part ref="DialectLabelsSense" visibility="ifdata"/>
			<part ref="ComplexFormEntries" visibility="ifdata"/>
			<part ref="VisibleComplexFormEntries" visibility="ifdata"/>
			<part ref="Subentries" visibility="ifdata"/>
			<part ref="VariantForms" visibility="ifdata"/>
			<part ref="Examples" param="Normal"/>
			<part ref="ScientificName" label="Scientific Name"   visibility="ifdata" />
			<part ref="AnthroNoteAllA" label="Anthropology Note"   visibility="ifdata" />
			<part ref="BibliographyAllA" label="Bibliography"   visibility="ifdata" />
			<part ref="DiscourseNoteAllA" label="Discourse Note"   visibility="ifdata" />
			<part ref="EncyclopedicInfoAllA" label="Encyclopedic Info"   visibility="ifdata" />
			<part ref="GeneralNoteAllA" label="General Note"   visibility="ifdata" />
			<part ref="GrammarNoteAllA" label="Grammar Note"   visibility="ifdata" />
			<part ref="PhonologyNoteAllA" label="Phonology Note"   visibility="ifdata" />
			<part ref="SemanticsNoteAllA" label="Semantics Note"   visibility="ifdata" />
			<part ref="SocioLinguisticsNoteAllA" label="Sociolinguistics Note"   visibility="ifdata" />
			<part ref="ExtendedNotes" param="Normal" visibility="ifdata" />
			<part ref="Source"   visibility="ifdata" />
			<part ref="UsageTypes"   visibility="ifdata" />
			<part ref="SenseType"   visibility="ifdata" />
			<part ref="DomainTypes"   visibility="ifdata" />
			<part ref="SemanticDomains"   visibility="always" />
			<part ref="AnthroCodes"   visibility="ifdata" />
			<part ref="Status"   visibility="ifdata" />
			<part ref="CurrentLexReferences"/>
			<!-- Special part to indicate where custom fields should be inserted at.  Handled in Common.Framework.DetailControls.DataTree -->
			<part ref="_CustomFieldPlaceholder" customFields="here" />
			<part ref="ImportResidue" label="Import Residue" visibility="ifdata"/>
			<part ref="PublishIn"   visibility="ifdata" />
			<part ref="Pictures" param="Normal"/>
			<part ref="Senses" label="SubSenses" param="Normal" menu="mnuDataTree-Subsenses"/>
			    <menu id="mnuDataTree-Subsenses">
			      <item command="CmdDataTree-Insert-SubSense" />
			    </menu>
	</indent>
			*/
		}
		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~LexiconEditToolDataTreeStackLexSenseManager()
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

			_isDisposed = true;
		}
		#endregion Implementation of IDisposable

		#region hotlinks

		private void RegisterHotLinkMenus()
		{
			// mnuDataTree-ExtendedNote-Hotlinks (LexSense: mnuDataTree_ExtendedNote_Hotlinks)
			MyDataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Sense_Hotlinks, Create_mnuDataTree_Sense_Hotlinks); // Only in LexSense.
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Sense_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_Sense_Hotlinks)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_Sense_Hotlinks}', but got '{hotlinksMenuId}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

			//<command id="CmdDataTree-Insert-Example" label="Insert _Example" message="DataTreeInsert">
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Example_Clicked, LexiconResources.Insert_Example);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_SenseBelow_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			return hotlinksMenuItemList;
		}


		private void Insert_Example_Clicked(object sender, EventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(string.Format(LanguageExplorerResources.Undo_0, LexiconResources.Insert_Example), string.Format(LanguageExplorerResources.Redo_0, LexiconResources.Insert_Example), _cache.ActionHandlerAccessor, () =>
			{
				var sense = (ILexSense)MyDataTree.CurrentSlice.MyCmObject;
				sense.ExamplesOS.Add(_cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create());
			});
		}

		private void Insert_SenseBelow_Clicked(object sender, EventArgs e)
		{
			// Get slice and see what sense is currently selected, so we can add the new sense after (read: 'below") it.
			var currentSlice = MyDataTree.CurrentSlice;
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
				var owningEntry = (ILexEntry)MyRecordList.CurrentObject;
				LexSenseUi.CreateNewLexSense(_cache, owningEntry, owningEntry.SensesOS.IndexOf(currentSense) + 1);
			}
		}

		#endregion hotlinks

		#region slice context menus

		private void RegisterSliceLeftEdgeMenus()
		{
			// mnuDataTree-Sense
			MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Sense, Create_mnuDataTree_Sense);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Sense(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Sense">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Sense
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(21);

			//<command id="CmdDataTree-Insert-Example" label="Insert _Example" message="DataTreeInsert">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Example_Clicked, LexiconResources.Insert_Example);

			// <command id="CmdFindExampleSentence" label="Find example sentence..." message="LaunchGuiControl">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, FindExampleSentence_Clicked, LexiconResources.Find_example_sentence);

			// <item command="CmdDataTree-Insert-ExtNote"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertExtNote], LexiconResources.Insert_Extended_Note);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertSense], LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// <item command="CmdDataTree-Insert-SubSense"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertSubsense], LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// <item command="CmdInsertPicture" label="Insert _Picture" defaultVisible="false"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertPicture], LexiconResources.Insert_Picture, LexiconResources.Insert_Picture_Tooltip);

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdSenseJumpToConcordance" label="Show Sense in Concordance" message="JumpToTool">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Show_Sense_in_Concordance_Clicked, LexiconResources.Show_Sense_in_Concordance);

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			ToolStripMenuItem menu;
			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <command id="CmdDataTree-MoveUp-Sense" label="Move Sense Up" message="MoveUpObjectInSequence" icon="MoveUp">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconAreaConstants.MoveUpObjectInOwningSequence], LexiconResources.Move_Sense_Up, image: imageHolder.smallCommandImages.Images[12]);
				bool visible;
				var enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);
				menu.Visible = true;
				menu.Enabled = enabled;

				// <command id="CmdDataTree-MoveDown-Sense" label="Move Sense Down" message="MoveDownObjectInSequence" icon="MoveDown">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconAreaConstants.MoveDownObjectInOwningSequence], LexiconResources.Move_Sense_Down, image: imageHolder.smallCommandImages.Images[14]);
				enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
				menu.Visible = true;
				menu.Enabled = enabled;

				// <command id="CmdDataTree-MakeSub-Sense" label="Demote" message="DemoteSense" icon="MoveRight"> AreaResources.Demote
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Demote_Sense_Clicked, AreaResources.Demote, image: imageHolder.smallCommandImages.Images[13]);
				menu.Visible = true;
				menu.Enabled = CanDemoteSense(slice);

				// <command id="CmdDataTree-Promote-Sense" label="Promote" message="PromoteSense" icon="MoveLeft"> AreaResources.Promote
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Promote_Sense_Clicked, AreaResources.Promote, image: imageHolder.smallCommandImages.Images[15]);
				menu.Visible = true;
				menu.Enabled = CanPromoteSense(slice);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdDataTree-Merge-Sense" label="Merge Sense into..." message="DataTreeMerge">
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconAreaConstants.DataTreeMerge], LexiconResources.Merge_Sense_into);
			menu.Visible = true;
			menu.Enabled = slice.CanMergeNow;

			// <command id="CmdDataTree-Split-Sense" label="Move Sense to a New Entry" message="DataTreeSplit">
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconAreaConstants.DataTreeSplit], LexiconResources.Move_Sense_to_a_New_Entry);
			menu.Visible = true;
			menu.Enabled = slice.CanSplitNow;

			// <command id="CmdDataTree-Delete-Sense" label="Delete this Sense and any Subsenses" message="DataTreeDeleteSense" icon="Delete">
			var toolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconAreaConstants.DataTreeDelete], LexiconResources.DeleteSenseAndSubsenses, image: LanguageExplorerResources.Delete);
			toolStripMenuItem.ImageTransparentColor = Color.Magenta;

			// End: <menu id="mnuDataTree-Sense">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private bool CanDemoteSense(Slice currentSlice)
		{
			return currentSlice.MyCmObject is ILexSense && _cache.DomainDataByFlid.get_VecSize(currentSlice.MyCmObject.Hvo, currentSlice.MyCmObject.OwningFlid) > 1;
		}

		private void Demote_Sense_Clicked(object sender, EventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoDemote, LanguageExplorerResources.ksRedoDemote, _cache.ActionHandlerAccessor, () =>
			{
				var sense = MyDataTree.CurrentSlice.MyCmObject;
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
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoPromote, LanguageExplorerResources.ksRedoPromote, _cache.ActionHandlerAccessor, () =>
			{
				var slice = MyDataTree.CurrentSlice;
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

		private void Show_Sense_in_Concordance_Clicked(object sender, EventArgs e)
		{
			// Show Sense in Concordance menu item. (CmdSenseJumpToConcordance->msg: JumpToTool)
			// <command id="CmdSenseJumpToConcordance" label="Show Sense in Concordance" message="JumpToTool">
			LinkHandler.JumpToTool(_flexComponentParameters.Publisher, new FwLinkArgs(AreaServices.ConcordanceMachineName, MyRecordList.CurrentObject.Guid));
		}

		private void FindExampleSentence_Clicked(object sender, EventArgs e)
		{
			using (var findExampleSentencesDlg = new FindExampleSentenceDlg(_statusBar, MyDataTree.CurrentSlice.MyCmObject, MyRecordList))
			{
				findExampleSentencesDlg.InitializeFlexComponent(_flexComponentParameters);
				findExampleSentencesDlg.ShowDialog((Form)_mainWnd);
			}
		}

		#endregion slice context menus
	}
}
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
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to the DataTree's context menus and hotlinks for a LexEntry, and objects it owns, in the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolDataTreeStackLexEntryManager : IToolUiWidgetManager
	{
		private const string LexSenseManager = "LexSenseManager";
		private const string LexEntryFormsManager = "LexEntryFormsManager";
		internal const string mnuDataTree_Etymology_Hotlinks = "mnuDataTree-Etymology-Hotlinks";
		private const string mnuDataTree_VariantSpec = "mnuDataTree-VariantSpec";
		private const string mnuDataTree_ComplexFormSpec = "mnuDataTree-ComplexFormSpec";
		private Dictionary<string, EventHandler> _sharedEventHandlers;
		private IRecordList MyRecordList { get; set; }
		private DataTree MyDataTree { get; set; }
		private IPublisher _publisher;
		private LcmCache _cache;
		private Dictionary<string, IToolUiWidgetManager> _dataTreeWidgetManagers;

		internal LexiconEditToolDataTreeStackLexEntryManager(DataTree dataTree)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));

			MyDataTree = dataTree;

			_dataTreeWidgetManagers = new Dictionary<string, IToolUiWidgetManager>
			{
				{ LexSenseManager, new LexiconEditToolDataTreeStackLexSenseManager(dataTree) },
				{ LexEntryFormsManager, new LexiconEditToolDataTreeStackLexEntryFormsManager(dataTree) }
			};
		}

		#region Implementation of IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, Dictionary<string, EventHandler> sharedEventHandlers, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));
			Guard.AgainstNull(recordList, nameof(recordList));

			_publisher = majorFlexComponentParameters.FlexComponentParameters.Publisher;
			_cache = majorFlexComponentParameters.LcmCache;
			_sharedEventHandlers = sharedEventHandlers;
			MyRecordList = recordList;

			_sharedEventHandlers.Add(LexiconAreaConstants.CmdMoveTargetToPreviousInSequence, MoveTargetDownInSequence_Clicked);
			_sharedEventHandlers.Add(LexiconAreaConstants.CmdMoveTargetToNextInSequence, MoveTargetUpInSequence_Clicked);
			_sharedEventHandlers.Add(LexiconAreaConstants.CmdAlphabeticalOrder, AlphabeticalOrder_Clicked);
			_sharedEventHandlers.Add(AreaServices.CmdEntryJumpToConcordance, CmdEntryJumpToConcordance_Clicked);

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
			Register_After_CitationForm_Bundle();
			Register_Pronunciation_Bundle();
			Register_Etymologies_Bundle();
			Register_Comment_To_Messages_Bundle();

			// NB: Senses go here. But, another manager worries about them.
			// <part ref="Senses" param="Normal" expansion="expanded"/>

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
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconAreaConstants.mnuReorderVector, Create_mnuReorderVector);

			// <part id="LexEntryRef-Detail-VariantEntryTypes" type="Detail">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(mnuDataTree_VariantSpec, Create_mnuDataTree_VariantSpec);

			// <part id="LexEntryRef-Detail-ComplexEntryTypes" type="Detail">
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(mnuDataTree_ComplexFormSpec, Create_mnuDataTree_ComplexFormSpec);

			#endregion left edge menus

			#region hotlinks
			// No hotlinks
			#endregion hotlinks
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuReorderVector(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconAreaConstants.mnuReorderVector)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconAreaConstants.mnuReorderVector}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuReorderVector">
			// This menu and its commands are shared
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconAreaConstants.mnuReorderVector
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);

			var referenceVectorSlice = (ReferenceVectorSlice)slice;
			// <command id="CmdMoveTargetToPreviousInSequence" label="Move Left" message="MoveTargetDownInSequence"/>
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveTargetDownInSequence_Clicked, LexiconResources.Move_Left);
			bool visible;
			menu.Enabled = referenceVectorSlice.CanDisplayMoveTargetDownInSequence(out visible);
			menu.Visible = visible;

			// <command id="CmdMoveTargetToNextInSequence" label="Move Right" message="MoveTargetUpInSequence"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveTargetUpInSequence_Clicked, LexiconResources.Move_Right);
			menu.Enabled = referenceVectorSlice.CanDisplayMoveTargetUpInSequence(out visible);
			menu.Visible = visible;

			// <command id="CmdAlphabeticalOrder" label="Alphabetical Order" message="AlphabeticalOrder"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, AlphabeticalOrder_Clicked, LexiconResources.Alphabetical_Order);
			menu.Visible = menu.Enabled = referenceVectorSlice.CanAlphabeticalOrder;

			// End: <menu id="mnuReorderVector">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_VariantSpec(Slice slice, string contextMenuId)
		{
			if (contextMenuId != mnuDataTree_VariantSpec)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_VariantSpec}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-VariantSpec">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_VariantSpec
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <command id="CmdDataTree-MoveUp-VariantSpec" label="Move Variant Info Up" message="MoveUpObjectInSequence" icon="MoveUp"/>
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInSequence_Clicked, LexiconResources.Move_Variant_Info_Up, image: imageHolder.smallCommandImages.Images[12]);
				bool visible;
				var enabled = CanMoveUpObjectInSequence(out visible);
				menu.Visible = visible;
				menu.Enabled = enabled;

				// <command id="CmdDataTree-MoveDown-VariantSpec" label="Move Variant Info Down" message="MoveDownObjectInSequence" icon="MoveDown"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInSequence_Clicked, LexiconResources.Move_Variant_Info_Down, image: imageHolder.smallCommandImages.Images[14]);
				enabled = CanMoveDownObjectInSequence(out visible);
				menu.Visible = visible;
				menu.Enabled = enabled;

				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

				/*
				 Add_another_Variant_Info_section
				 Add_another_Variant_Info_section_Tooltip
					<menu id="mnuDataTree-VariantSpec">
						<command id="CmdDataTree-Insert-VariantSpec" label="Add another Variant Info section" message="DataTreeInsert">
							<parameters field="EntryRefs" className="LexEntryRef" ownerClass="LexEntry" />
						</command>
						<command id="CmdDataTree-Delete-VariantSpec" label="Delete Variant Info" message="DataTreeDelete" icon="Delete"/>
					</menu>
				*/
			}

			// End: <menu id="mnuDataTree-VariantSpec">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ComplexFormSpec(Slice slice, string contextMenuId)
		{
			if (contextMenuId != mnuDataTree_ComplexFormSpec)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_ComplexFormSpec}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-ComplexFormSpec">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_ComplexFormSpec
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <command id="CmdDataTree-Delete-ComplexFormSpec" label="Delete Complex Form Info" message="DataTreeDelete" icon="Delete"/>
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_this_Foo_Clicked, LexiconResources.Delete_Complex_Form_Info, image: LanguageExplorerResources.Delete);
			menu.Enabled = slice.GetCanDeleteNow();
			if (!menu.Enabled)
			{
				menu.Text = $"{LexiconResources.Delete_Complex_Form_Info} {StringTable.Table.GetString("(cannot delete this)")}";
			}

			// End: <menu id="mnuDataTree-ComplexFormSpec">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		#endregion After_CitationForm_Bundle

		#region Pronunciation_Bundle

		private void Register_Pronunciation_Bundle()
		{
			// Only one slice has menus, but several have chooser dlgs.
			// <part ref="Pronunciations" param="Normal" visibility="ifdata"/>
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Pronunciation, Create_mnuDataTree_Pronunciation);
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
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Pronunciation], LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);
			// <item command="CmdInsertMediaFile" label="Insert _Sound or Movie" defaultVisible="false"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertMediaFile], LexiconResources.Sound_or_Movie, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip);
			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				/*
					<command id="CmdDataTree-MoveUp-Pronunciation" label="Move Pronunciation _Up" message="MoveUpObjectInSequence" icon="MoveUp">
						<parameters field="Pronunciations" className="LexPronunciation"/>
					</command>
				*/
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInSequence_Clicked, LexiconResources.Move_Pronunciation_Up, image: imageHolder.smallCommandImages.Images[12]);
				bool visible;
				var enabled = CanMoveUpObjectInSequence(out visible);
				menu.Visible = true;
				menu.Enabled = enabled;

				/*
					<command id="CmdDataTree-MoveDown-Pronunciation" label="Move Pronunciation _Down" message="MoveDownObjectInSequence" icon="MoveDown">
						<parameters field="Pronunciations" className="LexPronunciation"/>
					</command>
				*/
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInSequence_Clicked, LexiconResources.Move_Pronunciation_Down, image: imageHolder.smallCommandImages.Images[14]);
				enabled = CanMoveDownObjectInSequence(out visible);
				menu.Visible = true;
				menu.Enabled = enabled;
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			/*
				<command id="CmdDataTree-Delete-Pronunciation" label="Delete this Pronunciation" message="DataTreeDelete" icon="Delete">
					<parameters field="Pronunciations" className="LexPronunciation"/>
				</command>
				Delete_this_Pronunciation
			*/
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_this_Foo_Clicked, LexiconResources.Delete_this_Pronunciation);
			menu.Enabled = !slice.IsGhostSlice;

			// Not added here. It is added by the slice, along with the generic slice menus.
			// <item label="-" translate="do not translate"/>

			// End: <menu id="mnuDataTree-Pronunciation>

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void Delete_this_Foo_Clicked(object sender, EventArgs e)
		{
			DeleteSliceObject();
		}

		#endregion Pronunciation_Bundle

		#region Etymologies_Bundle

		private void Register_Etymologies_Bundle()
		{
			// Register the etymology hotlinks.
			MyDataTree.DataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Etymology_Hotlinks, Create_mnuDataTree_Etymology_Hotlinks);

			// <part ref="Etymologies" param="Normal" visibility="ifdata" />
			MyDataTree.DataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Etymology, Create_mnuDataTree_Etymology);
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Etymology_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_Etymology_Hotlinks)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_Etymology_Hotlinks}', but got '{hotlinksMenuId}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
			// <item command="CmdDataTree-Insert-Etymology"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Etymology], LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			return hotlinksMenuItemList;
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Etymology(Slice slice, string contextMenuId)
		{
			if (contextMenuId != LexiconEditToolConstants.mnuDataTree_Etymology)
			{
				throw new ArgumentException($"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_Etymology}', but got '{contextMenuId}' instead.");
			}

			// Start: <menu id="mnuDataTree-Etymology">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Etymology
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

			// <item command="CmdDataTree-Insert-Etymology" label="Insert _Etymology"/>
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdDataTree_Insert_Etymology], LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				/*
					<command id="CmdDataTree-MoveUp-Etymology" label="Move Etymology _Up" message="MoveUpObjectInSequence" icon="MoveUp">
						<parameters field="Etymology" className="LexEtymology"/>
					</command>
				*/
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInSequence_Clicked, LexiconResources.Move_Etymology_Up, image: imageHolder.smallCommandImages.Images[12]);
				bool visible;
				var enabled = CanMoveUpObjectInSequence(out visible);
				menu.Visible = true;
				menu.Enabled = enabled;
				/*
					<command id="CmdDataTree-MoveDown-Etymology" label="Move Etymology _Down" message="MoveDownObjectInSequence" icon="MoveDown">
						<parameters field="Etymology" className="LexEtymology"/>
					</command>
				*/
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInSequence_Clicked, LexiconResources.Move_Etymology_Down, image: imageHolder.smallCommandImages.Images[14]);
				enabled = CanMoveDownObjectInSequence(out visible);
				menu.Visible = true;
				menu.Enabled = enabled;
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			/*
				<command id="CmdDataTree-Delete-Etymology" label="Delete this Etymology" message="DataTreeDelete" icon="Delete">
					<parameters field="Etymology" className="LexEtymology"/>
				</command>
			 */
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_this_Foo_Clicked, LexiconResources.Delete_this_Etymology);
			menu.Enabled = !slice.IsGhostSlice;

			// End: <menu id="mnuDataTree-Etymology">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		#endregion Etymologies_Bundle

		#region Comment_To_Messages_Bundle

		private void Register_Comment_To_Messages_Bundle()
		{
			/*
		   <part ref="CommentAllA"/>
				<part id="LexEntry-Detail-CommentAllA" type="Detail">
					<slice field="Comment" label="Note" editor="multistring" ws="all analysis" />
				</part>
		   <part ref="LiteralMeaningAllA"  visibility="ifdata"/>
				<part id="LexEntry-Detail-LiteralMeaningAllA" type="detail">
					<slice field="LiteralMeaning" label="Literal Meaning" editor="multistring" ws="all analysis" />
				</part>
		   <!-- Only for Subentries. -->
		   <part ref="BibliographyAllA"   visibility="ifdata" />
				<part id="LexEntry-Detail-BibliographyAllA" type="Detail">
					<slice field="Bibliography" label="Bibliography" editor="multistring" ws="all analysis" />
				</part>
		   <part ref="RestrictionsAllA"   visibility="ifdata" />
				<part id="LexEntry-Detail-RestrictionsAllA" type="Detail">
					<slice field="Restrictions" label="Restrictions" editor="multistring" ws="all analysis" />
				</part>
		   <part ref="SummaryDefinitionAllA" visibility="ifdata"/>
				<part id="LexEntry-Detail-SummaryDefinitionAllA" type="Detail">
					<slice field="SummaryDefinition" label="Summary Definition" editor="multistring" ws="all analysis" />
				</part>
		   <part ref="CurrentLexReferences"   visibility="ifdata" />
				<part id="LexEntry-Detail-CurrentLexReferences" type="detail">
					<slice label="Cross References" field="LexEntryReferences" editor="custom" assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Areas.Lexicon.Tools.Edit.LexReferenceMultiSlice" />
				</part>
		   <!-- Special part to indicate where custom fields should be inserted at.  Handled in Common.Framework.DetailControls.DataTree -->
		   <part ref="_CustomFieldPlaceholder" customFields="here" /> // Nothing special for custom fields and menus.
		   <part ref="ImportResidue" label="Import Residue" visibility="ifdata"/>
				<part id="LexEntry-Detail-ImportResidue" type="Detail">
					<slice field="ImportResidue" label="ImportResidue" editor="String" />
				</part>
		   <part ref="DateCreatedAllA"  visibility="never"/>
				<part id="LexEntry-Detail-DateCreatedAllA" type="Detail">
					<slice field="DateCreated" label="Date Created" editor="Time" />
				</part>
		   <part ref="DateModifiedAllA"  visibility="never"/>
				<part id="LexEntry-Detail-DateModifiedAllA" type="Detail">
					<slice field="DateModified" label="Date Modified" editor="Time" />
				</part>
		   <part ref="Messages" visibility="always"/>
				<part id="LexEntry-Detail-Messages" type="detail">
					<slice field="Self" label="Messages" editor="Custom" assemblyPath="LanguageExplorer.dll" class="LanguageExplorer.Areas.Lexicon.Tools.Edit.ChorusMessageSlice" helpTopicID="khtpField-LexEntry-Messages"  />
				</part>
			*/
		}

		#endregion Comment_To_Messages_Bundle

		private void MoveTargetDownInSequence_Clicked(object sender, EventArgs e)
		{
			((ReferenceVectorSlice)MyDataTree.CurrentSlice).MoveTargetDownInSequence();
		}

		private void MoveTargetUpInSequence_Clicked(object sender, EventArgs e)
		{
			((ReferenceVectorSlice)MyDataTree.CurrentSlice).MoveTargetUpInSequence();
		}

		private void AlphabeticalOrder_Clicked(object sender, EventArgs e)
		{
			((ReferenceVectorSlice)MyDataTree.CurrentSlice).AlphabeticalOrder();
		}

		private void MoveUpObjectInSequence_Clicked(object sender, EventArgs e)
		{
			var slice = MyDataTree.CurrentSlice;
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

		private bool CanMoveUpObjectInSequence(out bool visible)
		{
			visible = false;
			bool enabled;
			var type = CellarPropertyType.ReferenceAtomic;
			var sliceObject = MyDataTree.CurrentSlice.MyCmObject;
			var owningFlid = sliceObject.OwningFlid;
			if (owningFlid > 0)
			{
				type = (CellarPropertyType)_cache.DomainDataByFlid.MetaDataCache.GetFieldType(owningFlid);
			}
			if (type != CellarPropertyType.OwningSequence && type != CellarPropertyType.ReferenceSequence)
			{
				return false;
			}
			var owningObject = sliceObject.Owner;
			var chvo = _cache.DomainDataByFlid.get_VecSize(owningObject.Hvo, owningFlid);
			if (chvo < 2)
			{
				enabled = false;
			}
			else
			{
				var hvo = _cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 0);
				enabled = sliceObject.Hvo != hvo;
				if (enabled && owningFlid == LexEntryTags.kflidEntryRefs && _cache.DomainDataByFlid.get_VecSize(hvo, LexEntryRefTags.kflidComplexEntryTypes) > 0)
				{
					// if the first LexEntryRef in LexEntry.EntryRefs is a complex form, and the
					// slice displays the second LexEntryRef in the sequence, then we can't move it
					// up, since the first slot is reserved for the complex form.
					enabled = sliceObject.Hvo != _cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 1);
				}
				else
				{
					var sliceObjIdx = _cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, sliceObject.Hvo);
					enabled = sliceObjIdx > 0;
				}
			}
			visible = true;

			return enabled;
		}

		private void MoveDownObjectInSequence_Clicked(object sender, EventArgs e)
		{
			var slice = MyDataTree.CurrentSlice;
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

		private bool CanMoveDownObjectInSequence(out bool visible)
		{
			visible = false;
			bool enabled;
			var type = CellarPropertyType.ReferenceAtomic;
			var sliceObject = MyDataTree.CurrentSlice.MyCmObject;
			var owningFlid = sliceObject.OwningFlid;
			if (owningFlid > 0)
			{
				type = (CellarPropertyType)_cache.DomainDataByFlid.MetaDataCache.GetFieldType(owningFlid);
			}
			if (type != CellarPropertyType.OwningSequence && type != CellarPropertyType.ReferenceSequence)
			{
				visible = false;
				return false;
			}
			var owningObject = sliceObject.Owner;
			var chvo = _cache.DomainDataByFlid.get_VecSize(owningObject.Hvo, owningFlid);
			if (chvo < 2)
			{
				enabled = false;
			}
			else
			{
				var hvo = _cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 0);
				enabled = sliceObject.Hvo != hvo;
				// if the first LexEntryRef in LexEntry.EntryRefs is a complex form, and the
				// slice displays the second LexEntryRef in the sequence, then we can't move it
				// up, since the first slot is reserved for the complex form.
				if (enabled && owningFlid == LexEntryTags.kflidEntryRefs && _cache.DomainDataByFlid.get_VecSize(hvo, LexEntryRefTags.kflidComplexEntryTypes) > 0)
				{
					enabled = sliceObject.Hvo != _cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 1);
				}
				else
				{
					var sliceObjIdx = _cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, sliceObject.Hvo);
					enabled = sliceObjIdx < chvo - 1;
				}
			}
			visible = true;
			return enabled;
		}

		#region ordinary slice menus

		#endregion ordinary slice menus

		#region popup slice menus

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
		#endregion popup slice menus

		private void DeleteSliceObject()
		{
			var currentSlice = MyDataTree.CurrentSlice;
			if (currentSlice.MyCmObject.IsValidObject)
			{
				currentSlice.HandleDeleteCommand();
			}
		}
	}
}
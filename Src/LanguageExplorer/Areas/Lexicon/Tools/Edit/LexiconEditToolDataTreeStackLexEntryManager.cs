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

			_sharedEventHandlers.Add(LexiconAreaConstants.CmdMoveTargetToPreviousInSequence, MoveReferencedTargetDownInSequence_Clicked);
			_sharedEventHandlers.Add(LexiconAreaConstants.CmdMoveTargetToNextInSequence, MoveReferencedTargetUpInSequence_Clicked);
			_sharedEventHandlers.Add(LexiconAreaConstants.CmdAlphabeticalOrder, Referenced_AlphabeticalOrder_Clicked);
			_sharedEventHandlers.Add(AreaServices.CmdEntryJumpToConcordance, CmdEntryJumpToConcordance_Clicked);
			_sharedEventHandlers.Add(LexiconAreaConstants.MoveUpObjectInOwningSequence, MoveUpObjectInOwningSequence_Clicked);
			_sharedEventHandlers.Add(LexiconAreaConstants.MoveDownObjectInOwningSequence, MoveDownObjectInOwningSequence_Clicked);

			// Slice stack from LexEntry.fwlayout (less senses, which are handled in another manager class).
			Register_After_CitationForm_Bundle();
			Register_Pronunciation_Bundle();
			Register_Etymologies_Bundle();

			// NB: Senses go here. But, another manager worries about them.
			// <part ref="Senses" param="Normal" expansion="expanded"/>

			// The "Grammatical Info. Details" had no special menus, as most slices are references, and choosers sort it all out.
			// The publication section shares the "LexiconAreaConstants.mnuReorderVector" menu factory method for two slices.
			// So nothing additional needs to be done.

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
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveReferencedTargetDownInSequence_Clicked, LexiconResources.Move_Left);
			bool visible;
			menu.Enabled = referenceVectorSlice.CanDisplayMoveTargetDownInSequence(out visible);
			menu.Visible = visible;

			// <command id="CmdMoveTargetToNextInSequence" label="Move Right" message="MoveTargetUpInSequence"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveReferencedTargetUpInSequence_Clicked, LexiconResources.Move_Right);
			menu.Enabled = referenceVectorSlice.CanDisplayMoveTargetUpInSequence(out visible);
			menu.Visible = visible;

			// <command id="CmdAlphabeticalOrder" label="Alphabetical Order" message="AlphabeticalOrder"/>
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Referenced_AlphabeticalOrder_Clicked, LexiconResources.Alphabetical_Order);
			menu.Visible = menu.Enabled = referenceVectorSlice.CanAlphabetize;

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
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Variant_Info_Up, image: imageHolder.smallCommandImages.Images[12]);
				bool visible;
				var enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);
				menu.Visible = visible;
				menu.Enabled = enabled;

				// <command id="CmdDataTree-MoveDown-VariantSpec" label="Move Variant Info Down" message="MoveDownObjectInSequence" icon="MoveDown"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Variant_Info_Down, image: imageHolder.smallCommandImages.Images[14]);
				enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
				menu.Visible = visible;
				menu.Enabled = enabled;

				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

#if RANDYTODO
				// TODO: Add these two menus.
#endif
				/*
				 Add_another_Variant_Info_section
				 Add_another_Variant_Info_section_Tooltip
						<command id="CmdDataTree-Insert-VariantSpec" label="Add another Variant Info section" message="DataTreeInsert">
							<parameters field="EntryRefs" className="LexEntryRef" ownerClass="LexEntry" />
						</command>
						<command id="CmdDataTree-Delete-VariantSpec" label="Delete Variant Info" message="DataTreeDelete" icon="Delete"/> , image: LanguageExplorerResources.Delete
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
			menu.Enabled = slice.CanDeleteNow;
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
				// <command id="CmdDataTree-MoveUp-Pronunciation" label="Move Pronunciation _Up" message="MoveUpObjectInSequence" icon="MoveUp">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Pronunciation_Up, image: imageHolder.smallCommandImages.Images[12]);
				bool visible;
				var enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);
				menu.Visible = true;
				menu.Enabled = enabled;

				// <command id="CmdDataTree-MoveDown-Pronunciation" label="Move Pronunciation _Down" message="MoveDownObjectInSequence" icon="MoveDown">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Pronunciation_Down, image: imageHolder.smallCommandImages.Images[14]);
				enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
				menu.Visible = true;
				menu.Enabled = enabled;
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdDataTree-Delete-Pronunciation" label="Delete this Pronunciation" message="DataTreeDelete" icon="Delete">
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_this_Foo_Clicked, LexiconResources.Delete_this_Pronunciation, image: LanguageExplorerResources.Delete);
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
				// <command id="CmdDataTree-MoveUp-Etymology" label="Move Etymology _Up" message="MoveUpObjectInSequence" icon="MoveUp">
				//	<parameters field="Etymology" className="LexEtymology"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveUpObjectInOwningSequence_Clicked, LexiconResources.Move_Etymology_Up, image: imageHolder.smallCommandImages.Images[12]);
				bool visible;
				var enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);
				menu.Visible = true;
				menu.Enabled = enabled;
				// <command id="CmdDataTree-MoveDown-Etymology" label="Move Etymology _Down" message="MoveDownObjectInSequence" icon="MoveDown">
				//	<parameters field="Etymology" className="LexEtymology"/>
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, MoveDownObjectInOwningSequence_Clicked, LexiconResources.Move_Etymology_Down, image: imageHolder.smallCommandImages.Images[14]);
				enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
				menu.Visible = true;
				menu.Enabled = enabled;
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdDataTree-Delete-Etymology" label="Delete this Etymology" message="DataTreeDelete" icon="Delete">
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_this_Foo_Clicked, LexiconResources.Delete_this_Etymology, image: LanguageExplorerResources.Delete);
			menu.Enabled = !slice.IsGhostSlice;

			// End: <menu id="mnuDataTree-Etymology">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		#endregion Etymologies_Bundle

		private void MoveReferencedTargetDownInSequence_Clicked(object sender, EventArgs e)
		{
			((ReferenceVectorSlice)MyDataTree.CurrentSlice).MoveTargetDownInSequence();
		}

		private void MoveReferencedTargetUpInSequence_Clicked(object sender, EventArgs e)
		{
			((ReferenceVectorSlice)MyDataTree.CurrentSlice).MoveTargetUpInSequence();
		}

		private void Referenced_AlphabeticalOrder_Clicked(object sender, EventArgs e)
		{
			((ReferenceVectorSlice)MyDataTree.CurrentSlice).Alphabetize();
		}

		private void MoveUpObjectInOwningSequence_Clicked(object sender, EventArgs e)
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

		private void MoveDownObjectInOwningSequence_Clicked(object sender, EventArgs e)
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
// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Palaso.Linq;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.LexText.Controls;
using System.Diagnostics.CodeAnalysis;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	/// <summary>
	/// LexEntryMenuHandler inherits from DTMenuHandler and adds some special smarts.
	/// this class would normally be constructed by the factory method on DTMenuHandler,
	/// when the XML configuration of the RecordEditView specifies this class.
	///
	/// This is an IxCoreColleague, so it gets a chance to modify
	/// the display characteristics of the menu just before the menu is displayed.
	/// </summary>
	internal class ReversalIndexEntryMenuHandler : DTMenuHandler
	{
		/// <summary>
		/// Need a default constructor for dynamic loading
		/// </summary>
		public ReversalIndexEntryMenuHandler()
		{
		}

#if RANDYTODO
		/// <summary>
		/// decide whether to enable this tree delete Menu Item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public override bool OnDisplayDataTreeMerge(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanMergeOrMove;
			if(!display.Enabled)
				display.Text += StringTable.Table.GetString("(cannot merge this)");

			return true;//we handled this, no need to ask anyone else.
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "slice and cache are references")]
		public bool OnPromoteReversalindexEntry(object cmd)
		{
			//Command command = (Command) cmd;
			Slice slice = m_dataEntryForm.CurrentSlice;
			Debug.Assert(slice != null, "No slice was current");
			if (slice != null)
			{
				FdoCache cache = m_dataEntryForm.Cache;
				IReversalIndexEntry entry = slice.Object as IReversalIndexEntry;
				ICmObject newOwner = entry.Owner.Owner;
				UndoableUnitOfWorkHelper.Do(((Command)cmd).UndoText, ((Command)cmd).RedoText, newOwner,
					() =>
						{
							switch (newOwner.ClassID)
							{
								default:
									throw new ArgumentException("Illegal class.");
								case ReversalIndexTags.kClassId:
									{
										IReversalIndex ri = (IReversalIndex) newOwner;
										ri.EntriesOC.Add(entry);
										break;
									}
								case ReversalIndexEntryTags.kClassId:
									{
										IReversalIndexEntry rie = (IReversalIndexEntry) newOwner;
										rie.SubentriesOS.Add(entry);
										break;
									}
							}
						});
				// We may need to notify everyone that a virtual property changed.
				//NotifyVirtualChanged(cache, slice);
			}
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "slice is a reference")]
		public virtual bool OnDisplayPromoteReversalindexEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null
				|| slice.Object == null
				|| slice.Object.OwningFlid == ReversalIndexTags.kflidEntries) // Can't promote a top-level entry.
			{
				display.Enabled = false;
			}
			else
			{
				display.Enabled = true;
			}
			return true; //we've handled this
		}
#endif

		/// <summary />
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "slice is a reference")]
		public bool OnMoveReversalindexEntry(object cmd)
		{
			using (var dlg = new ReversalEntryGoDlg())
			{
				Slice slice = m_dataEntryForm.CurrentSlice;
				var currentEntry = (IReversalIndexEntry) slice.Object;
				dlg.ReversalIndex = currentEntry.ReversalIndex;
				AddEntryAndChildrenRecursively(dlg.FilteredReversalEntryHvos, currentEntry);
				IReversalIndexEntry owningEntry = currentEntry.OwningEntry;
				if (owningEntry != null)
					dlg.FilteredReversalEntryHvos.Add(owningEntry.Hvo);
				dlg.SetHelpTopic("khtpMoveReversalEntry");
				var wp = new WindowParams { m_btnText = LanguageExplorerResources.ks_MoveEntry, m_title = LanguageExplorerResources.ksMoveRevEntry };
				var cache = PropertyTable.GetValue<FdoCache>("cache");
				dlg.SetDlgInfo(cache, wp, PropertyTable, Publisher);
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					var newOwner = (IReversalIndexEntry) dlg.SelectedObject;
					UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMoveRevEntry,
						LanguageExplorerResources.ksRedoMoveRevEntry, Cache.ActionHandlerAccessor,
						() =>
						{
							newOwner.MoveIfNeeded(currentEntry);
							newOwner.SubentriesOS.Add(currentEntry);
						});
					RecordClerk clerk = PropertyTable.GetValue<RecordClerk>("ActiveClerk");
					if (clerk != null)
						clerk.RemoveItemsFor(currentEntry.Hvo);
					// Note: PropChanged should happen on the old owner and the new while completing the unit of work.
					// Have to jump to a main entry, as RecordClerk doesn't know anything about subentries.
					Publisher.Publish("JumpToRecord", newOwner.MainEntry.Hvo);
				}
			}
			return true;
		}

		private static void AddEntryAndChildrenRecursively(ICollection<int> hvos, IReversalIndexEntry entry)
		{
			hvos.Add(entry.Hvo);
			entry.AllOwnedObjects.Where(obj => obj is IReversalIndexEntry).ForEach(subentry => hvos.Add(subentry.Hvo));
		}

#if RANDYTODO
		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "slice is a reference")]
		public virtual bool OnDisplayMoveReversalindexEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			Slice slice = m_dataEntryForm.CurrentSlice;
			if (slice == null || slice.Object == null)
			{
				display.Enabled = false;
			}
			else
			{
				display.Enabled = CanMergeOrMove;
				display.Visible = InFriendlyArea;
			}
			return true; //we've handled this
		}
#endif

		private bool CanMergeOrMove
		{
			get
			{
				Debug.Assert(m_dataEntryForm != null && m_dataEntryForm.CurrentSlice != null, "Current slice information is null, how can this menu be drawing?");
				// ReSharper disable HeuristicUnreachableCode
				// ReSharper disable ConditionIsAlwaysTrueOrFalse
				//ReSharper lies.
				if (m_dataEntryForm == null || m_dataEntryForm.CurrentSlice == null)
				{
					return false; //merge is not possible if all is not right with the world
				}
				// ReSharper restore ConditionIsAlwaysTrueOrFalse
				// ReSharper restore HeuristicUnreachableCode
				IReversalIndexEntry rie = m_dataEntryForm.CurrentSlice.Object as IReversalIndexEntry;
				if(rie == null)
					return false;
				// Merge and move are possible if we have more than one entry.
				if (rie.ReversalIndex.EntriesOC.Count > 1)
					return true;
				if (rie.ReversalIndex.EntriesOC.Count == 0)
					return false;
				// exactly 1 top-level entry.
				return rie.ReversalIndex.EntriesOC.ToArray()[0].SubentriesOS.Count > 0;
			}
		}

		/// <summary />
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		protected  bool InFriendlyArea
		{
			get
			{
				return (PropertyTable.GetValue<string>("areaChoice") == "lexicon"
					&& PropertyTable.GetValue<string>("ToolForAreaNamed_lexicon") == "reversalEditComplete");
			}
		}
	}
}

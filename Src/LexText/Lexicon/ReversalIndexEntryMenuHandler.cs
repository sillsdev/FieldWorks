using System;
using System.Diagnostics;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// LexEntryMenuHandler inherits from DTMenuHandler and adds some special smarts.
	/// this class would normally be constructed by the factory method on DTMenuHandler,
	/// when the XML configuration of the RecordEditView specifies this class.
	///
	/// This is an IxCoreColleague, so it gets a chance to modify
	/// the display characteristics of the menu just before the menu is displayed.
	/// </summary>
	public class ReversalIndexEntryMenuHandler : DTMenuHandler
	{
		//need a default constructor for dynamic loading
		public ReversalIndexEntryMenuHandler()
		{
		}

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
				display.Text += StringTbl.GetString("(cannot merge this)");

			return true;//we handled this, no need to ask anyone else.
		}

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
				UndoableUnitOfWorkHelper.Do((Command) cmd, newOwner,
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
										rie.SubentriesOC.Add(entry);
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

		public bool OnMoveReversalindexEntry(object cmd)
		{
			using (var dlg = new ReversalEntryGoDlg())
			{
				Slice slice = m_dataEntryForm.CurrentSlice;
				Debug.Assert(slice != null, "No slice was current");
				var currentEntry = (IReversalIndexEntry) slice.Object;
				dlg.ReversalIndex = currentEntry.ReversalIndex;
				dlg.FilteredReversalEntries.Add(currentEntry);
				IReversalIndexEntry owningEntry = currentEntry.OwningEntry;
				if (owningEntry != null)
					dlg.FilteredReversalEntries.Add(owningEntry);
				dlg.SetHelpTopic("khtpMoveReversalEntry");
				var wp = new WindowParams {m_btnText = LexEdStrings.ks_MoveEntry, m_title = LexEdStrings.ksMoveRevEntry};
				var cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				dlg.SetDlgInfo(cache, wp, m_mediator);
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					var newOwner = (IReversalIndexEntry) dlg.SelectedObject;
					UndoableUnitOfWorkHelper.Do(LexEdStrings.ksUndoMoveRevEntry,
						LexEdStrings.ksRedoMoveRevEntry, Cache.ActionHandlerAccessor,
						() =>
						{
							newOwner.MoveIfNeeded(currentEntry);
							newOwner.SubentriesOC.Add(currentEntry);
						});
					RecordClerk clerk = m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
					if (clerk != null)
						clerk.RemoveItemsFor(currentEntry.Hvo);
					// Note: PropChanged should happen on the old owner and the new while completing the unit of work.
					// Have to jump to a main entry, as RecordClerk doesn't know anything about subentries.
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", newOwner.MainEntry.Hvo);
				}
			}

			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
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

		private bool CanMergeOrMove
		{
			get
			{
				IReversalIndexEntry rie = m_dataEntryForm.CurrentSlice.Object as IReversalIndexEntry;
				// Merge and move are possible if we have more than one entry.
				if (rie.ReversalIndex.EntriesOC.Count > 1)
					return true;
				if (rie.ReversalIndex.EntriesOC.Count == 0)
					return false;
				// exactly 1 top-level entry.
				return rie.ReversalIndex.EntriesOC.ToArray()[0].SubentriesOC.Count > 0;
			}
		}


		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		protected  bool InFriendlyArea
		{
			get
			{
				return (m_mediator.PropertyTable.GetStringProperty("areaChoice", null) == "lexicon"
					&& m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null) == "reversalEditComplete");
			}
		}
	}
}

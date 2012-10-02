using System;
using System.Diagnostics;
using System.Xml;
using System.Windows.Forms;
using System.Collections.Generic;

using XCore;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.XWorks;
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
				int hvoNewOwner = cache.GetOwnerOfObject(entry.OwnerHVO);
				switch (cache.GetClassOfObject(hvoNewOwner))
				{
					default:
						throw new ArgumentException("Illegal class.");
					case ReversalIndex.kclsidReversalIndex:
					{
						IReversalIndex ri = ReversalIndex.CreateFromDBObject(cache, hvoNewOwner);
						ri.EntriesOC.Add(entry);
						break;
					}
					case ReversalIndexEntry.kclsidReversalIndexEntry:
					{
						IReversalIndexEntry rie = ReversalIndexEntry.CreateFromDBObject(cache, hvoNewOwner);
						rie.SubentriesOC.Add(entry);
						break;
					}
				}
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
				|| slice.Object.OwningFlid == (int)ReversalIndex.ReversalIndexTags.kflidEntries) // Can't promote a top-level entry.
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
			using (ReversalEntryGoDlg dlg = new ReversalEntryGoDlg())
			{
				Slice slice = m_dataEntryForm.CurrentSlice;
				Debug.Assert(slice != null, "No slice was current");
				IReversalIndexEntry currentEntry = (IReversalIndexEntry)slice.Object;
				List<IReversalIndexEntry> filteredEntries = new List<IReversalIndexEntry>();
				filteredEntries.Add(currentEntry);
				IReversalIndexEntry owningEntry = currentEntry.OwningEntry;
				if (owningEntry != null)
					filteredEntries.Add(owningEntry);

				WindowParams wp = new WindowParams();
				wp.m_btnText = LexEdStrings.ks_MoveEntry;
				wp.m_label = LexEdStrings.ks_Find_;
				wp.m_title = LexEdStrings.ksMoveRevEntry;
				dlg.SetDlgInfo(m_mediator, wp, filteredEntries); // , true
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
					IReversalIndexEntry newOwner = ReversalIndexEntry.CreateFromDBObject(cache, dlg.SelectedID);
					cache.BeginUndoTask(LexEdStrings.ksUndoMoveRevEntry,
						LexEdStrings.ksRedoMoveRevEntry);
					ICmObject newOwningObj = newOwner.MoveIfNeeded(currentEntry);
					newOwner.SubentriesOC.Add(currentEntry);
					cache.EndUndoTask();
					RecordClerk clerk = m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
					if (clerk != null)
						clerk.RemoveItemsFor(currentEntry.Hvo);
					// Note: PropChanged should happen on the old owner and the new in the 'Add" method call.
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
				string sQuery = string.Format("SELECT COUNT(*) FROM dbo.fnGetOwnedIds({0},{1},{2})",
					rie.ReversalIndex.Hvo,
					(int)ReversalIndex.ReversalIndexTags.kflidEntries,
					(int)ReversalIndexEntry.ReversalIndexEntryTags.kflidSubentries);
				int crie = 0;
				DbOps.ReadOneIntFromCommand(Cache, sQuery, null, out crie);
				return crie > 1;
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

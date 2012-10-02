using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Generate an additional PropChanged as part of an Undo or Redo action. A typical usage is pretending
	/// that the row that is the target of a dependent clause annotation has changed in its parent.
	/// Note that we create TWO of these, one that is the first action in the group, and one that is the last.
	/// The first is for Undo, and updates the ribbon to the appropriate state for when the action is undone.
	/// (It needs to be first so it will be the last thing undone.)
	/// The last is for Redo, and updates the ribbon after the task is redone (needs to be last so it is the
	/// last thing redone).
	///
	/// A typical usage is
	/// using (new ExtraPropChangedInserter(sda, hvo, tag, ihvo, chvoIns, chvoDel)
	/// {
	///		// Do the changes which require the extra propchanged before and after.
	/// }
	/// </summary>
	public class ExtraPropChangedAction : IUndoAction
	{
		bool m_fForRedo;
		internal ISilDataAccess m_sda;
		internal int m_hvo;
		internal int m_tag;
		internal int m_ihvo;
		internal int m_chvoIns; // On Do, Redo; # deleted on Undo
		internal int m_chvoDel; // On Do, Redo; #inserted on Undo.

		/// <summary>
		/// Make an instance and add it to the undo stack. Also, if it's the 'redo' action added after the
		/// actual changes (fForRedo is true), issue the propchanged at once to complete the original action.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="chvoIns"></param>
		/// <param name="chvoDel"></param>
		/// <param name="fForRedo"></param>
		static public ExtraPropChangedAction AddAndInvokeIfRedo(ISilDataAccess sda, int hvo, int tag, int
			ihvo, int chvoIns, int chvoDel, bool fForRedo)
		{

			ExtraPropChangedAction action = new ExtraPropChangedAction(sda, hvo, tag, ihvo, chvoIns, chvoDel, fForRedo);
			sda.GetActionHandler().AddAction(action);
			if (fForRedo)
				action.Redo(false);
			return action;
		}

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="chvoIns"></param>
		/// <param name="chvoDel"></param>
		/// <param name="fForRedo"></param>
		public ExtraPropChangedAction(ISilDataAccess sda, int hvo, int tag, int
			ihvo, int chvoIns, int chvoDel, bool fForRedo)
		{
			m_sda = sda;
			m_hvo = hvo;
			m_tag = tag;
			m_ihvo = ihvo;
			m_chvoIns = chvoIns;
			m_chvoDel = chvoDel;
			m_fForRedo = fForRedo;
		}

		/// <summary>
		/// Mainly for testing...check whether this one has the same info as the arguments.
		/// May use -1 as 'don't care' argument for ihvo, chvoIns, chvoDel.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="chvoIns"></param>
		/// <param name="chvoDel"></param>
		/// <param name="fForRedo"></param>
		/// <returns></returns>
		public bool SameInfo(int hvo, int tag, int ihvo, int chvoIns, int chvoDel, bool fForRedo)
		{
			return m_hvo == hvo && m_tag == tag && Matches(ihvo, m_ihvo) && Matches(chvoDel, m_chvoDel)
				&& Matches(chvoDel, m_chvoDel) && m_fForRedo == fForRedo;
		}

		bool Matches(int pattern, int val)
		{
			if (pattern == -1)
				return true;
			return pattern == val;
		}
		#region IUndoAction Members

		/// <summary>
		/// Called when action can no longer be undone or redone
		/// </summary>
		public void Commit()
		{
		}

		/// <summary>
		/// no real data changes as a result of this.
		/// </summary>
		/// <returns></returns>
		public bool IsDataChange()
		{
			return false;
		}

		/// <summary>
		/// Yes, we can.
		/// </summary>
		/// <returns></returns>
		public bool IsRedoable()
		{
			return true;
		}

		/// <summary>
		/// Redo it. If a Refresh is going to happen, no need to do anything.
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public bool Redo(bool fRefreshPending)
		{
			if (m_fForRedo && !fRefreshPending)
				m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvo, m_tag, m_ihvo, m_chvoIns, m_chvoDel);
			return true;
		}

		/// <summary>
		/// whole purpose of this is typically to avoid need for refresh
		/// </summary>
		/// <returns></returns>
		public bool RequiresRefresh()
		{
			return false;
		}

		/// <summary>
		/// ??
		/// </summary>
		public bool SuppressNotification
		{
			set { }
		}

		/// <summary>
		/// Undo it. If a Refresh is going to happen, no need to do anything.
		/// </summary>
		/// <param name="fRefreshPending"></param>
		/// <returns></returns>
		public bool Undo(bool fRefreshPending)
		{
			if (!m_fForRedo && !fRefreshPending)
				m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvo, m_tag, m_ihvo, m_chvoDel, m_chvoIns);
			return true;
		}

		#endregion
	}

	/// <summary>
	/// Class which uses Dispose to create ExtraPropChangedActions before and after another sequence
	/// of actions, as described in the ExtraPropChangedAction class comment.
	/// Also ensures that a PropChanged is issued when disposed.
	/// </summary>
	public class ExtraPropChangedInserter : IDisposable
	{
		ExtraPropChangedAction m_action;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ihvo"></param>
		/// <param name="chvoIns"></param>
		/// <param name="chvoDel"></param>
		public ExtraPropChangedInserter(ISilDataAccess sda, int hvo, int tag, int
			ihvo, int chvoIns, int chvoDel)
		{
			m_action = ExtraPropChangedAction.AddAndInvokeIfRedo(sda, hvo, tag, ihvo, chvoIns, chvoDel, false);
		}

		#region IDisposable Members

		/// <summary>
		/// Dispose creates the Redo action.
		/// </summary>
		public void Dispose()
		{
			ExtraPropChangedAction.AddAndInvokeIfRedo(m_action.m_sda, m_action.m_hvo, m_action.m_tag,
				m_action.m_ihvo, m_action.m_chvoIns, m_action.m_chvoDel, true);
		}

		#endregion
	}
}

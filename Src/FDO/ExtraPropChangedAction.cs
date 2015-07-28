// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
	/// using (new ExtraPropChangedInserter(actionHandler, sda, hvo, tag, ihvo, chvoIns, chvoDel)
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
		public static ExtraPropChangedAction AddAndInvokeIfRedo(IActionHandler actionHandler, ISilDataAccess sda, int hvo, int tag, int
			ihvo, int chvoIns, int chvoDel, bool fForRedo)
		{

			ExtraPropChangedAction action = new ExtraPropChangedAction(sda, hvo, tag, ihvo, chvoIns, chvoDel, fForRedo);
			actionHandler.AddAction(action);
			if (fForRedo)
				action.Redo();
			return action;
		}

		/// <summary>
		/// Make one.
		/// </summary>

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
		public bool IsDataChange
		{
			get { return false; }
		}

		/// <summary>
		/// Yes, we can.
		/// </summary>
		/// <returns></returns>
		public bool IsRedoable
		{
			get { return true; }
		}

		/// <summary>
		/// Redo it.
		/// </summary>
		/// <returns></returns>
		public bool Redo()
		{
			if (m_fForRedo)
				m_sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_hvo, m_tag, m_ihvo, m_chvoIns, m_chvoDel);
			return true;
		}

		/// <summary>
		/// ??
		/// </summary>
		public bool SuppressNotification
		{
			set { }
		}

		/// <summary>
		/// Undo it.
		/// </summary>
		/// <returns></returns>
		public bool Undo()
		{
			if (!m_fForRedo)
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
		private ExtraPropChangedAction m_action;
		private IActionHandler m_actionHandler;

		/// <summary>
		/// Make one.
		/// </summary>
		public ExtraPropChangedInserter(IActionHandler actionHandler, ISilDataAccess sda, int hvo, int tag, int
			ihvo, int chvoIns, int chvoDel)
		{
			m_actionHandler = actionHandler;
			m_action = ExtraPropChangedAction.AddAndInvokeIfRedo(m_actionHandler, sda, hvo, tag, ihvo, chvoIns, chvoDel, false);
		}

		#region IDisposable Members

		#if DEBUG
		/// <summary/>
		~ExtraPropChangedInserter()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Dispose creates the Redo action.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
			ExtraPropChangedAction.AddAndInvokeIfRedo(m_actionHandler, m_action.m_sda, m_action.m_hvo, m_action.m_tag,
				m_action.m_ihvo, m_action.m_chvoIns, m_action.m_chvoDel, true);
			}
			IsDisposed = true;
		}
		#endregion
	}
}

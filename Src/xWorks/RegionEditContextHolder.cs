// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Owns the host's current <see cref="IRegionEditContext"/> and enforces the lifecycle rules
	/// that keep the fenced edit session (an open LCModel undo task) safe against the rest of the
	/// app:
	/// (1) a context with an open session is NEVER orphaned — re-showing the region swaps in a
	/// fresh context and the displaced one is cancelled first (an orphaned open undo task makes
	/// every later <c>IUndoStackManager.Save()</c> throw "Commit at wrong place.", which is fatal
	/// at shutdown);
	/// (2) <see cref="Settle"/> is the single auto-save policy (14.4) every host path shares:
	/// commit when validation is clean, roll back otherwise — navigation, go-away, undo and
	/// dispose all settle the same way;
	/// (3) the undo guard intercepts global Undo/Redo while a session is open: LCModel's
	/// <c>UndoStack.Undo()</c> re-enters the non-recursive UOW write lock the open task's thread
	/// already holds (LockRecursionException), so the guard settles the pending edit and cancels
	/// that gesture — the next Ctrl+Z undoes the just-settled step normally.
	/// </summary>
	public sealed class RegionEditContextHolder
	{
		private IActionHandlerExtensions m_undoHook;

		/// <summary>The context currently bound to the shown region, or null.</summary>
		public IRegionEditContext Current { get; private set; }

		/// <summary>
		/// Makes <paramref name="next"/> the current context, cancelling the previous context's
		/// open session (if any). Re-assigning the same instance is a no-op so a live edit is
		/// never killed by redundant wiring. Hosts normally <see cref="Settle"/> first; this
		/// cancel is the safety net, not the auto-save path.
		/// </summary>
		public void Replace(IRegionEditContext next)
		{
			var previous = Current;
			if (!ReferenceEquals(previous, next) && previous != null && previous.IsOpen)
				previous.Cancel();
			Current = next;
		}

		/// <summary>Drops the current context, cancelling its open session (if any).</summary>
		public void Clear()
		{
			Replace(null);
		}

		/// <summary>
		/// Auto-save (14.4): closes any open session — committing when validation is clean,
		/// rolling back otherwise (an invalid state is never silently persisted). No-op when
		/// nothing is open.
		/// </summary>
		public void Settle()
		{
			var current = Current;
			if (current == null || !current.IsOpen)
				return;
			try
			{
				if (current.Validate().Count == 0)
					current.Commit();
				else
					current.Cancel();
			}
			catch (System.Exception e)
			{
				// Settling runs on navigation and teardown paths that must not die (e.g. the
				// entry was deleted under the open session); rolling back is always a safe close.
				SIL.Reporting.Logger.WriteError(e);
				if (current.IsOpen)
					current.Cancel();
			}
		}

		/// <summary>
		/// Intercepts global Undo/Redo for the given action handler while a session is open (see
		/// class remarks). Detaches any previously attached handler first.
		/// </summary>
		public void AttachUndoGuard(IActionHandler actionHandler)
		{
			DetachUndoGuard();
			m_undoHook = actionHandler as IActionHandlerExtensions;
			if (m_undoHook != null)
				m_undoHook.DoingUndoOrRedo += OnDoingUndoOrRedo;
		}

		/// <summary>Stops intercepting Undo/Redo. Safe to call when not attached.</summary>
		public void DetachUndoGuard()
		{
			if (m_undoHook == null)
				return;
			m_undoHook.DoingUndoOrRedo -= OnDoingUndoOrRedo;
			m_undoHook = null;
		}

		private void OnDoingUndoOrRedo(CancelEventArgs e)
		{
			if (Current?.IsOpen != true)
				return;
			// Settling closes the task and releases the write lock; cancelling the gesture keeps
			// its meaning predictable — this press closed the pending edit, the next one undoes it.
			Settle();
			e.Cancel = true;
		}
	}
}

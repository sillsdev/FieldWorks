// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
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
		private Form m_deactivateForm;
		private EventHandler m_settleOnDeactivate;

		/// <summary>The context currently bound to the shown region, or null.</summary>
		public IRegionEditContext Current { get; private set; }

		/// <summary>
		/// ITEM 2 (invalid-edit-on-navigate UX): invoked when <see cref="Settle"/> rolls back an open
		/// session because validation FAILED, carrying the user-facing validation reasons. The pending
		/// edit is still rolled back (the safe close that keeps the UOW write lock from stranding), but
		/// this hook lets the host TELL the user why their edit was discarded rather than losing it
		/// silently (the old behavior surfaced only a Logger line). Null = no host wired (tests/headless),
		/// in which case the rollback stays silent exactly as before. Never fired for a clean commit, a
		/// no-op settle (nothing open), or a settle that threw (those still only log).
		/// </summary>
		public Action<IReadOnlyList<string>> InvalidEditRolledBack { get; set; }

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
		/// ITEM 2: when the close is a rollback FORCED BY a validation failure, the validation
		/// reasons are returned (and <see cref="InvalidEditRolledBack"/> is fired) so the host can tell
		/// the user why their edit was discarded — the data is rolled back safely, but never silently.
		/// </summary>
		/// <returns>
		/// The validation reasons that forced a rollback, or an empty list when the session committed
		/// cleanly, nothing was open, or the settle threw (those paths still only log).
		/// </returns>
		public IReadOnlyList<string> Settle()
		{
			var current = Current;
			if (current == null || !current.IsOpen)
				return System.Array.Empty<string>();
			try
			{
				var errors = current.Validate();
				if (errors.Count == 0)
				{
					current.Commit();
					return System.Array.Empty<string>();
				}

				// Invalid: roll back (the safe close), but surface WHY so the edit is not lost silently.
				current.Cancel();
				var reasons = errors as IReadOnlyList<string> ?? new List<string>(errors);
				SIL.Reporting.Logger.WriteEvent(
					"RegionEditContextHolder.Settle: pending edit rolled back because it failed validation: "
					+ string.Join("; ", reasons));
				InvalidEditRolledBack?.Invoke(reasons);
				return reasons;
			}
			catch (System.Exception e)
			{
				// Settling runs on navigation and teardown paths that must not die (e.g. the
				// entry was deleted under the open session); rolling back is always a safe close.
				SIL.Reporting.Logger.WriteError(e);
				if (current.IsOpen)
					current.Cancel();
				return System.Array.Empty<string>();
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

		/// <summary>
		/// Settles whenever the given top-level window deactivates. The undo guard is per-stack
		/// and cannot reach other windows' undo stacks, so an open session must close before the
		/// user can focus another window and undo there (re-entering the UOW write lock).
		/// Detaches any previously attached form first; a null form is a no-op.
		/// </summary>
		/// <summary>
		/// Whether the deactivate-settle hook is currently attached to a window. The host re-attempts
		/// <see cref="AttachDeactivateHook"/> on each show until this is true, so a first show before
		/// the top-level form handle exists does not permanently lose the cross-window-undo mitigation.
		/// </summary>
		public bool IsDeactivateHookAttached => m_deactivateForm != null;

		public void AttachDeactivateHook(Form form)
		{
			DetachDeactivateHook();
			if (form == null)
				return;
			m_settleOnDeactivate = (sender, e) => Settle();
			form.Deactivate += m_settleOnDeactivate;
			m_deactivateForm = form;
		}

		/// <summary>Stops settling on window deactivation. Safe to call when not attached.</summary>
		public void DetachDeactivateHook()
		{
			if (m_deactivateForm == null)
				return;
			m_deactivateForm.Deactivate -= m_settleOnDeactivate;
			m_deactivateForm = null;
			m_settleOnDeactivate = null;
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

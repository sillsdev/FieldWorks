// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The browse-table edit context (Stage 3, 6.x): unlike the single-entry edit context the detail
	/// view uses, a browse table edits a different row object per cell, so this context resolves the
	/// target object from the edited field's <see cref="LexicalEditRegionField.ObjectHvo"/> and
	/// DELEGATES the actual write to a per-object <see cref="LexicalEditRegionEditContext"/> — reusing
	/// the proven, validated fenced-LCModel write path rather than a second write mechanism. Switching
	/// to a different row commits any open edit on the previous row first, so each row's edit is one
	/// undoable step. Only objects/fields the delegate supports are writable; everything else is a
	/// no-op (read-only), which keeps the conservative, data-safe default.
	///
	/// BULK-EDIT BATCH FENCE (Phase 1 List Choice): the per-row "commit-and-retarget" lifecycle above
	/// produces ONE undo step PER OBJECT, which is correct for inline single-cell editing but wrong for a
	/// bulk edit over N rows (it would litter the undo stack with N steps). <see cref="BeginBatch"/>/
	/// <see cref="EndBatch"/> wrap a span of per-row writes in a SINGLE outer LCModel undo task
	/// (<see cref="LcmRegionEditSession"/>): while a batch is open, the per-object delegates' own
	/// <see cref="LcmRegionEditSession"/> fences nest inside this one (BeginUndoTask is re-entrant on the
	/// action handler), and the per-row Commit calls only close those inner tasks — the single outer task
	/// is closed exactly once by <see cref="EndBatch"/>. Net result: N rows, ONE undo step (the
	/// CommitCount==1 parity gate the bulk-edit tests assert). The batch is balanced/idempotent; a throwing
	/// row write rolls the whole batch back.
	/// </summary>
	internal sealed class ClerkBrowseEditContext : IRegionEditContext
	{
		private readonly LcmCache _cache;
		private LexicalEditRegionEditContext _current;
		private int _currentHvo;
		// The single outer undo task spanning a bulk-edit batch (null when no batch is open). While set,
		// per-row Commit() does NOT retarget-commit the way single-cell editing does; the batch owns the
		// one-UOW boundary and closes it once in EndBatch.
		private LcmRegionEditSession _batch;

		public ClerkBrowseEditContext(LcmCache cache)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		/// <summary>
		/// Opens the single outer undo task that turns a span of per-row bulk writes into ONE undoable
		/// step. Re-entrant on the action handler, so each per-object delegate's own fence nests inside.
		/// Idempotent: a second BeginBatch while one is open is a no-op (keeps the one true outer task).
		/// </summary>
		public void BeginBatch()
		{
			if (_batch != null)
				return;
			_batch = new LcmRegionEditSession(_cache, FwAvaloniaStrings.UndoEditEntry, FwAvaloniaStrings.RedoEditEntry);
		}

		/// <summary>
		/// Closes the outer batch task as ONE undo step. Flushes the last per-row delegate's inner fence
		/// first, then ends the outer task. A no-op when no batch is open. On a caller-signalled failure
		/// (<paramref name="commit"/> false) the whole batch rolls back.
		/// </summary>
		public void EndBatch(bool commit = true)
		{
			// Flush any still-bound per-row delegate so its inner fence is closed before the outer task ends.
			var previous = _current;
			_current = null;
			_currentHvo = 0;
			if (commit)
				previous?.Commit();
			else
				previous?.Cancel();

			var batch = _batch;
			_batch = null;
			if (batch == null)
				return;
			if (commit)
				batch.Commit();
			else
				batch.Cancel();
		}

		// Resolves (and caches) the per-object delegate for the field being edited. Committing the
		// previous row's open edit before retargeting keeps row edits as independent undoable steps —
		// EXCEPT inside a bulk-edit batch, where the per-row inner fences nest in the single outer task
		// and the per-row Commit only closes the inner fence (the outer task is closed once by EndBatch).
		private LexicalEditRegionEditContext For(LexicalEditRegionField field)
		{
			if (field == null || field.ObjectHvo == 0)
				return null;
			if (_current != null && _currentHvo == field.ObjectHvo)
				return _current;

			// Detach first, then commit: a throwing commit must not leave the previous row's delegate bound.
				var previous = _current;
				_current = null;
				_currentHvo = 0;
				previous?.Commit();

			if (!_cache.ServiceLocator.IsValidObjectId(field.ObjectHvo))
				return null;
			var obj = _cache.ServiceLocator.GetObject(field.ObjectHvo);
			if (!(obj is ILexEntry entry))
				return null;

			_current = new LexicalEditRegionEditContext(entry, _cache);
			_currentHvo = field.ObjectHvo;
			return _current;
		}

		public bool IsOpen => _current?.IsOpen ?? false;

		public bool TrySetText(LexicalEditRegionField field, string ws, string value)
			=> For(field)?.TrySetText(field, ws, value) ?? false;

		public bool TrySetRichText(LexicalEditRegionField field, string ws, RegionRichTextValue value)
			=> For(field)?.TrySetRichText(field, ws, value) ?? false;

		public bool TrySetOption(LexicalEditRegionField field, string optionKey)
			=> For(field)?.TrySetOption(field, optionKey) ?? false;

		public bool TryAddReferenceItem(LexicalEditRegionField field, string optionKey)
			=> For(field)?.TryAddReferenceItem(field, optionKey) ?? false;

		public bool TryRemoveReferenceItem(LexicalEditRegionField field, string optionKey)
			=> For(field)?.TryRemoveReferenceItem(field, optionKey) ?? false;

		public IReadOnlyList<string> Validate() => _current?.Validate() ?? new List<string>();

		// Commit/Cancel end the current row's session AND drop the cached delegate, so the next edit
		// re-resolves a fresh one from a clean state. Without the reset, a commit/cancel followed by a
		// re-edit of the SAME row would reuse a delegate whose session was already closed (or force-ended
		// by the clerk's save-on-record-change), instead of starting a clean new undoable step. This keeps
		// the data-side lifecycle aligned with the view's "one active session, torn down on commit/cancel".
		public void Commit()
		{
			_current?.Commit();
			_current = null;
			_currentHvo = 0;
		}

		public void Cancel()
		{
			_current?.Cancel();
			_current = null;
			_currentHvo = 0;
		}

		/// <summary>
		/// Deletes the object identified by <paramref name="hvo"/> as part of the currently-open bulk-edit batch
		/// (the destructive Delete-Rows mode). Must be called between <see cref="BeginBatch"/> and
		/// <see cref="EndBatch"/> so the whole multi-row delete is ONE undoable LCModel task; the per-object
		/// <see cref="ICmObject.Delete"/> cascades owned objects via the LCModel ownership model (the parity of the
		/// legacy SpecialCache.DeleteObj path). Any still-bound per-row text-edit delegate is flushed first so a
		/// pending inline edit on a soon-deleted object cannot fight the delete. Returns true when an object was
		/// deleted, false when the hvo no longer resolves (already gone — a harmless no-op). A throwing delete
		/// propagates so the caller can roll the whole batch back.
		/// </summary>
		public bool TryDeleteObject(int hvo)
		{
			if (hvo == 0)
				return false;
			// Flush any bound per-row text delegate (without committing a half-staged edit on the victim object):
			// the delete owns the change from here, and a left-bound delegate could re-resolve a deleted object.
			var previous = _current;
			_current = null;
			_currentHvo = 0;
			previous?.Commit();

			if (!_cache.ServiceLocator.IsValidObjectId(hvo))
				return false;
			var obj = _cache.ServiceLocator.GetObject(hvo);
			if (obj == null)
				return false;
			obj.Delete();
			return true;
		}
	}
}

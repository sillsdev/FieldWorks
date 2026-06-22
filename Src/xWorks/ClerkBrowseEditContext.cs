// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
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
	/// </summary>
	internal sealed class ClerkBrowseEditContext : IRegionEditContext
	{
		private readonly LcmCache _cache;
		private LexicalEditRegionEditContext _current;
		private int _currentHvo;

		public ClerkBrowseEditContext(LcmCache cache)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		// Resolves (and caches) the per-object delegate for the field being edited. Committing the
		// previous row's open edit before retargeting keeps row edits as independent undoable steps.
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
	}
}

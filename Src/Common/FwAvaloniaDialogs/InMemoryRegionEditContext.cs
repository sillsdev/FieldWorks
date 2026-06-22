// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// A tiny in-memory <see cref="IRegionEditContext"/> for CREATE flows (the Insert Entry dialog): the owned
	/// <see cref="FwMultiWsTextField"/> stages each per-writing-system edit here into an in-memory bag instead of
	/// against a live LCModel cache, so the dialog view-model stays LCModel-free and can read the staged text
	/// back on OK. There is no real undo fence: <see cref="Commit"/>/<see cref="Cancel"/> only flip
	/// <see cref="IsOpen"/> (the launcher does the one real undoable create on OK). Rich-text staging is
	/// flattened to plain text — the Insert Entry fields are plain lexeme-form / gloss strings, not styled runs.
	/// Chooser/reference staging is rejected: the morph-type picker is driven by the view-model directly, not
	/// through this context.
	/// </summary>
	public sealed class InMemoryRegionEditContext : IRegionEditContext
	{
		// Per-field staged text, keyed by field identity then writing-system key.
		private readonly Dictionary<string, Dictionary<string, string>> _staged =
			new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

		/// <summary>Raised after a successful text stage so the view-model can re-derive / re-gate OK.</summary>
		public event Action<LexicalEditRegionField, string, string> TextStaged;

		/// <inheritdoc />
		public bool IsOpen { get; private set; }

		/// <summary>
		/// The staged per-writing-system text for a field (the field's <see cref="FieldKey"/>), or an empty map
		/// when nothing was staged. Keyed by the writing-system key the editor used (the WS tag).
		/// </summary>
		public IReadOnlyDictionary<string, string> GetStaged(LexicalEditRegionField field)
		{
			return _staged.TryGetValue(FieldKey(field), out var byWs)
				? byWs
				: new Dictionary<string, string>();
		}

		public bool TrySetText(LexicalEditRegionField field, string ws, string value)
		{
			if (field == null || string.IsNullOrEmpty(ws))
				return false;
			IsOpen = true;
			var key = FieldKey(field);
			if (!_staged.TryGetValue(key, out var byWs))
			{
				byWs = new Dictionary<string, string>(StringComparer.Ordinal);
				_staged[key] = byWs;
			}
			byWs[ws] = value ?? string.Empty;
			TextStaged?.Invoke(field, ws, value ?? string.Empty);
			return true;
		}

		// Rich text is flattened to its plain text: the Insert Entry fields are plain strings.
		public bool TrySetRichText(LexicalEditRegionField field, string ws, RegionRichTextValue value)
			=> TrySetText(field, ws, value?.PlainText ?? string.Empty);

		// The morph-type picker is driven by the view-model, not through this context; reject option staging.
		public bool TrySetOption(LexicalEditRegionField field, string optionKey) => false;

		public bool TryAddReferenceItem(LexicalEditRegionField field, string optionKey) => false;

		public bool TryRemoveReferenceItem(LexicalEditRegionField field, string optionKey) => false;

		public IReadOnlyList<string> Validate() => Array.Empty<string>();

		public void Commit() => IsOpen = false;

		public void Cancel() => IsOpen = false;

		// A composed field keys on StableId; a fixed field keys on Field; fall back to the automation id.
		private static string FieldKey(LexicalEditRegionField field)
			=> field.StableId ?? field.Field ?? field.AutomationId ?? string.Empty;
	}
}

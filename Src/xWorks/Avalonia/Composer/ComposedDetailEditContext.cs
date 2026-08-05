// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The edit operations of ONE composed field, keyed by StableId. Each delegate is null when the
	/// field's kind does not support that gesture (a text row carries no paragraph delegates; an StText
	/// row carries no option delegate), so <see cref="ComposedDetailEditContext"/> rejects an unsupported
	/// gesture by finding a null slot. Gathers a field's write behavior into one object in place of the
	/// nine parallel setter dictionaries that previously had to agree on the same stable id.
	/// </summary>
	public sealed class FieldEditHandler
	{
		public Func<string, string, bool> Text;
		public Func<string, DetailRichTextValue, bool> RichText;
		public Func<string, bool> Option;
		public Func<string, bool> ReferenceAdd;
		public Func<string, bool> ReferenceRemove;
		public Func<int, DetailRichTextValue, bool> ParagraphText;
		public Func<int, string, bool> ParagraphStyle;
		public Func<int, bool> ParagraphInsert;
		public Func<int, bool> ParagraphDelete;
	}

	/// <summary>
	/// The composed detail view's edit context: staging keyed by composed stable id (unique per object
	/// occurrence, so each sense's Gloss binds its own sense), writes applied through the registered
	/// LCModel setters inside the fenced session owned by <see cref="DetailEditContextBase"/>
	/// (one shared session lifecycle + required-lexeme validation).
	/// </summary>
	public sealed class ComposedDetailEditContext : DetailEditContextBase, IStructuredTextEditing
	{
		// One handler per composed field, keyed by StableId; a null delegate slot means the field's kind
		// does not support that gesture (rejected like an unknown field). Replaces the former nine parallel
		// setter dictionaries — a field's edit behavior now lives in one object rather than being spread
		// across nine maps kept in sync by matching stable id.
		private readonly IReadOnlyDictionary<string, FieldEditHandler> _handlers;

		public ComposedDetailEditContext(
			LcmCache cache,
			ICmObject root, // any record root (LexEntry today)
			IReadOnlyDictionary<string, FieldEditHandler> handlers)
			: base(cache, root)
		{
			_handlers = handlers ?? new Dictionary<string, FieldEditHandler>();
		}

		// The field's handler, or null when the field is unknown or carries no delegate for this gesture.
		private FieldEditHandler Handler(DetailField field)
			=> field != null && _handlers.TryGetValue(field.StableId, out var handler) ? handler : null;

		public override bool TrySetText(DetailField field, string ws, string value)
		{
			if (field != null && field.Values.Any(v => v.RequiresRichEditor))
				return false;

			var setter = Handler(field)?.Text;
			if (setter == null)
				return false;
			// A single field's edit names the undo label (e.g. "Undo change to Gloss").
			return Stage(() => setter(ws, value), FieldLabelFor(field));
		}

		public override bool TrySetRichText(DetailField field, string ws, DetailRichTextValue value)
		{
			if (field != null && field.Values.Any(v => !v.CanEditRichText))
				return false;

			var setter = Handler(field)?.RichText;
			if (setter == null)
				return false;
			return Stage(() => setter(ws, value), FieldLabelFor(field));
		}

		public override bool TrySetOption(DetailField field, string optionKey)
		{
			var setter = Handler(field)?.Option;
			if (setter == null)
				return false;
			return Stage(() => setter(optionKey), FieldLabelFor(field));
		}

		public override bool TryAddReferenceItem(DetailField field, string optionKey)
		{
			var setter = Handler(field)?.ReferenceAdd;
			if (setter == null)
				return false;
			return Stage(() => setter(optionKey), FieldLabelFor(field));
		}

		public override bool TryRemoveReferenceItem(DetailField field, string optionKey)
		{
			var setter = Handler(field)?.ReferenceRemove;
			if (setter == null)
				return false;
			return Stage(() => setter(optionKey), FieldLabelFor(field));
		}

		public bool TrySetParagraphText(DetailField field, int paragraphIndex,
			DetailRichTextValue value)
		{
			var setter = Handler(field)?.ParagraphText;
			if (setter == null)
				return false;
			return Stage(() => setter(paragraphIndex, value), FieldLabelFor(field));
		}

		public bool TrySetParagraphStyle(DetailField field, int paragraphIndex,
			string styleName)
		{
			var setter = Handler(field)?.ParagraphStyle;
			if (setter == null)
				return false;
			return Stage(() => setter(paragraphIndex, styleName), FieldLabelFor(field));
		}

		public bool TryInsertParagraph(DetailField field, int afterParagraphIndex)
		{
			var setter = Handler(field)?.ParagraphInsert;
			if (setter == null)
				return false;
			return Stage(() => setter(afterParagraphIndex), FieldLabelFor(field));
		}

		public bool TryDeleteParagraph(DetailField field, int paragraphIndex)
		{
			var setter = Handler(field)?.ParagraphDelete;
			if (setter == null)
				return false;
			return Stage(() => setter(paragraphIndex), FieldLabelFor(field));
		}

		// The human-readable field label that names the undo step, falling back to the
		// field name (never empty so the generic label is reserved for the batch/bulk path).
		private static string FieldLabelFor(DetailField field)
			=> string.IsNullOrEmpty(field?.Label) ? field?.Field : field.Label;

		// The fenced-session staging helper (open-on-first-edit, close-empty-fence-on-reject) lives
		// on DetailEditContextBase.Stage so a plugin editor's own writes (the Reversal Entries plugin)
		// can ride the SAME undoable step through the shared context.
	}
}

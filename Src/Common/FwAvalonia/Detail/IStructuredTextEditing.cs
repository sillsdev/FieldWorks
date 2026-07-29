// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// The optional paragraph-CRUD capability of a <see cref="DetailFieldKind.StructuredText"/> (StText)
	/// field, kept off the core <see cref="IDetailEditContext"/> so only a context that actually edits
	/// structured text carries these methods. A caller acquires it with <c>ctx as IStructuredTextEditing</c>
	/// and treats a null result as "not supported" (the gesture is rejected), exactly as the former
	/// core-interface methods returned false on a context with no StText rows. A context that supports
	/// structured text implements this alongside <see cref="IDetailEditContext"/>.
	/// </summary>
	public interface IStructuredTextEditing
	{
		/// <summary>
		/// Stages a run-aware text edit to ONE paragraph of a
		/// <see cref="DetailFieldKind.StructuredText"/> (StText) field, opening the session on the first
		/// edit. Returns false — WITHOUT opening the session — for a non-StText row, an out-of-range
		/// paragraph index, or an ORC/lossy (read-only) paragraph. Like the run-aware single-WS path,
		/// the rich payload preserves run metadata so the product <c>ITsString</c> rebuilds without
		/// flattening.
		/// </summary>
		bool TrySetParagraphText(DetailField field, int paragraphIndex, DetailRichTextValue value);

		/// <summary>
		/// Stages setting (or clearing, when <paramref name="styleName"/> is null/empty) the named
		/// paragraph style of ONE paragraph of a <see cref="DetailFieldKind.StructuredText"/> field.
		/// Returns false — without opening the session — for a non-StText row or an out-of-range index.
		/// </summary>
		bool TrySetParagraphStyle(DetailField field, int paragraphIndex, string styleName);

		/// <summary>
		/// Stages inserting a new empty paragraph AFTER <paramref name="afterParagraphIndex"/> in a
		/// <see cref="DetailFieldKind.StructuredText"/> field (a negative index inserts at the start).
		/// Returns false — without opening the session — for a non-StText row. The structural gesture
		/// commits immediately and the host re-shows (the model's paragraph list is a compose snapshot).
		/// </summary>
		bool TryInsertParagraph(DetailField field, int afterParagraphIndex);

		/// <summary>
		/// Stages deleting paragraph <paramref name="paragraphIndex"/> of a
		/// <see cref="DetailFieldKind.StructuredText"/> field. Returns false — without opening the
		/// session — for a non-StText row, an out-of-range index, or when it would delete the only
		/// paragraph (the StText always keeps at least one, like the legacy editor).
		/// </summary>
		bool TryDeleteParagraph(DetailField field, int paragraphIndex);
	}
}

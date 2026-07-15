// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// The editing seam for a region surface (tasks 6.8/6.10): staging writes, validation, and the
	/// fenced commit/cancel boundary. The product implementation (xWorks) opens one fenced LCModel
	/// undo task lazily on the first staged edit, applies writes directly to the domain inside it,
	/// and ends it on <see cref="Commit"/> (one step on the single global undo stack shared with
	/// legacy surfaces) or rolls it back on <see cref="Cancel"/> — the model the
	/// `avalonia-edit-sessions` and `avalonia-undo-redo` seam specs require. This layer stays
	/// LCModel-free so the Avalonia view can drive editing without a domain dependency; tests use a
	/// fake context.
	/// Extends <see cref="IEditSession"/> (the staging-neutral IsOpen/Commit/Cancel fence) so the two
	/// previously-parallel edit-session contracts are one: any region context IS an edit session.
	/// </summary>
	public interface IRegionEditContext : IEditSession
	{

		/// <summary>
		/// Stages a text value for a field (opening the session on the first edit). Returns false when
		/// the field/writing system cannot accept the value (the view leaves the editor unchanged).
		/// <paramref name="ws"/> is the writing system's unique IETF tag
		/// (<see cref="RegionWsValue.WsTag"/>); implementations also accept an unambiguous
		/// abbreviation or legacy alias (e.g. "vern"/"anal") as a fallback for tag-less rows. The
		/// user-editable abbreviation alone is never an identity: it can collide across writing
		/// systems. Implementations key on <see cref="LexicalEditRegionField.Field"/> for fixed
		/// slices or <see cref="LexicalEditRegionField.StableId"/> for composed full-layout regions,
		/// where the same field name (e.g. Gloss) occurs once per sense.
		/// </summary>
		bool TrySetText(LexicalEditRegionField field, string ws, string value);

		/// <summary>
		/// Stages a run-aware text value for a field. The supplied rich-text payload is LCModel-free and
		/// preserves the run metadata needed to rebuild the product <c>ITsString</c> without flattening.
		/// </summary>
		bool TrySetRichText(LexicalEditRegionField field, string ws, RegionRichTextValue value);

		/// <summary>Stages a chooser selection by option key (opening the session on the first edit).</summary>
		bool TrySetOption(LexicalEditRegionField field, string optionKey);

		/// <summary>
		/// Stages adding an item (by option key) to a <see cref="RegionFieldKind.ReferenceVector"/>
		/// row (6.3). Returns false — WITHOUT opening the session — for keys outside the field's
		/// possibility list, duplicates, or non-vector rows, like the legacy chooser.
		/// </summary>
		bool TryAddReferenceItem(LexicalEditRegionField field, string optionKey);

		/// <summary>
		/// Stages removing an item (by option key) from a <see cref="RegionFieldKind.ReferenceVector"/>
		/// row. Returns false — without opening the session — when the item is not in the vector.
		/// </summary>
		bool TryRemoveReferenceItem(LexicalEditRegionField field, string optionKey);

		/// <summary>
		/// §19a: stages a run-aware text edit to ONE paragraph of a
		/// <see cref="RegionFieldKind.StructuredText"/> (StText) field, opening the session on the first
		/// edit. Returns false — WITHOUT opening the session — for a non-StText row, an out-of-range
		/// paragraph index, or an ORC/lossy (read-only) paragraph. Like the run-aware single-WS path,
		/// the rich payload preserves run metadata so the product <c>ITsString</c> rebuilds without
		/// flattening.
		/// </summary>
		bool TrySetParagraphText(LexicalEditRegionField field, int paragraphIndex, RegionRichTextValue value);

		/// <summary>
		/// §19a: stages setting (or clearing, when <paramref name="styleName"/> is null/empty) the named
		/// paragraph style of ONE paragraph of a <see cref="RegionFieldKind.StructuredText"/> field.
		/// Returns false — without opening the session — for a non-StText row or an out-of-range index.
		/// </summary>
		bool TrySetParagraphStyle(LexicalEditRegionField field, int paragraphIndex, string styleName);

		/// <summary>
		/// §19a: stages inserting a new empty paragraph AFTER <paramref name="afterParagraphIndex"/> in a
		/// <see cref="RegionFieldKind.StructuredText"/> field (a negative index inserts at the start).
		/// Returns false — without opening the session — for a non-StText row. The structural gesture
		/// commits immediately and the host re-shows (the model's paragraph list is a compose snapshot).
		/// </summary>
		bool TryInsertParagraph(LexicalEditRegionField field, int afterParagraphIndex);

		/// <summary>
		/// §19a: stages deleting paragraph <paramref name="paragraphIndex"/> of a
		/// <see cref="RegionFieldKind.StructuredText"/> field. Returns false — without opening the
		/// session — for a non-StText row, an out-of-range index, or when it would delete the only
		/// paragraph (the StText always keeps at least one, like the legacy editor).
		/// </summary>
		bool TryDeleteParagraph(LexicalEditRegionField field, int paragraphIndex);

		/// <summary>
		/// §19d: inserts a NEW picture (an <c>ICmPicture</c>) into the picture-collection field
		/// <paramref name="field"/> from the source image file <paramref name="sourceFile"/>, with the
		/// supplied <paramref name="metadata"/> (caption/description/license/creator). The structural
		/// gesture commits immediately and the host re-shows (the picture rows are a compose-time
		/// snapshot). Returns false — WITHOUT opening the session — for a non-picture field, a missing
		/// source file, or a field that does not accept pictures.
		/// </summary>
		bool TryInsertPicture(LexicalEditRegionField field, string sourceFile, RegionPictureMetadata metadata);

		/// <summary>
		/// §19d: replaces the image FILE of the existing picture <paramref name="field"/> represents
		/// (an Image row carries one <c>ICmPicture</c> via <see cref="LexicalEditRegionField.PictureHvo"/>)
		/// with <paramref name="sourceFile"/>, leaving its caption/metadata intact. Returns false —
		/// without opening the session — for a non-picture row, an unresolvable picture, or a missing file.
		/// </summary>
		bool TryReplacePictureFile(LexicalEditRegionField field, string sourceFile);

		/// <summary>
		/// §19d: deletes the picture <paramref name="field"/> represents from its owning collection.
		/// Returns false — without opening the session — for a non-picture row or an unresolvable picture.
		/// </summary>
		bool TryDeletePicture(LexicalEditRegionField field);

		/// <summary>
		/// §19d: updates the metadata (caption/description, and license/creator when the file metadata is
		/// writable) of the picture <paramref name="field"/> represents. The caption/description are real
		/// <c>ICmPicture</c> multistring properties (always written); license/creator are applied to the
		/// image file's Palaso metadata only when the file is present/writable. Returns false — without
		/// opening the session — for a non-picture row or an unresolvable picture.
		/// </summary>
		bool TrySetPictureMetadata(LexicalEditRegionField field, RegionPictureMetadata metadata);

		/// <summary>
		/// §19d (closes §19c's picture-ORC deferral): inserts a picture ORC into the rich-text VALUE of a
		/// text field at <paramref name="caretPosition"/> — creates the <c>ICmPicture</c> from
		/// <paramref name="sourceFile"/> (with <paramref name="metadata"/>) and inserts the
		/// <c>kodtGuidMoveableObjDisp</c> run referencing it, like legacy
		/// <c>FwEditingHelper.InsertPicture</c>. The gesture commits immediately and the host re-shows.
		/// Returns false — without opening the session — for a field whose writing system cannot be
		/// resolved, a missing source file, or a field that does not carry editable rich text.
		/// </summary>
		bool TryInsertPictureOrc(LexicalEditRegionField field, string ws, int caretPosition,
			string sourceFile, RegionPictureMetadata metadata);

		/// <summary>
		/// Validates the staged state. Empty result means commit may proceed; messages are
		/// user-facing (validation seam, deterministic order).
		/// </summary>
		IReadOnlyList<string> Validate();

		// IsOpen, Commit(), and Cancel() are inherited from IEditSession: Commit ends the open
		// session as a single undoable step and Cancel rolls back every staged edit; both no-op when
		// no session is open.
	}
}

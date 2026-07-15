// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A typed value object for one browse column's edit attributes (rendering-cutover F1), replacing the
	/// untyped <c>(out string field, out int ws, out bool transduce)</c> trio at the
	/// <see cref="ClerkBrowseRowSource"/> seam boundary. It carries the result of classifying the legacy
	/// XML column vocabulary — the resolved entry-anchored write token (<see cref="EditField"/>) and the
	/// normalized writing-system tag (<see cref="WritingSystemTag"/>) — into typed form, so the adapter no
	/// longer string-matches XML attributes inline and the classification lives in ONE clearly-named
	/// mapping function (<see cref="FromColumnAttributes"/>). This is the natural extension point for
	/// future date/int/reference editor kinds: add a typed <c>EditorKind</c> here and classify it once,
	/// rather than re-baking "$ws=" / "LexEntry.LexemeForm.Form" string vocabulary across the adapter.
	///
	/// <see cref="IsEditable"/> is the single source of truth the adapter exposes through
	/// <c>IBrowseEditSource.IsColumnEditable</c>: a column is editable only when its write target maps to
	/// one the delegating <see cref="ClerkBrowseEditContext"/> supports SAFELY (a non-null
	/// <see cref="EditField"/>). Everything else stays read-only rather than risk a wrong-object write.
	/// </summary>
	internal sealed class BrowseColumnEditSpec
	{
		private BrowseColumnEditSpec(string editField, string writingSystemTag, string rawField,
			string rawWritingSystem, string transduce)
		{
			EditField = editField;
			WritingSystemTag = writingSystemTag;
			RawField = rawField;
			RawWritingSystem = rawWritingSystem;
			Transduce = transduce;
		}

		/// <summary>The entry-anchored write token the edit context can write SAFELY, or null (read-only).</summary>
		public string EditField { get; }

		/// <summary>The normalized writing-system tag the keyboard/write seam resolves (vern/anal or concrete).</summary>
		public string WritingSystemTag { get; }

		/// <summary>The raw legacy <c>field</c> attribute (diagnostics / future classification).</summary>
		public string RawField { get; }

		/// <summary>The raw legacy <c>ws</c> attribute, possibly a magic "$ws=..." spec.</summary>
		public string RawWritingSystem { get; }

		/// <summary>The raw legacy <c>transduce</c> target.</summary>
		public string Transduce { get; }

		/// <summary>True when the column maps to a supported, entry-anchored inline write.</summary>
		public bool IsEditable => EditField != null;

		/// <summary>
		/// The write token for a bulk LIST-CHOICE target (Phase 1), or null when the column is not one.
		/// Unlike inline editing (driven by <c>transduce</c>), the legacy bulk List Choice columns are
		/// possibility-reference columns identified by a list <c>field</c> with no transduce — today the
		/// entry-anchored, unambiguous Morph Type (<c>field="LexEntry.LexemeForm"</c>), whose write target
		/// is <c>Entry.LexemeFormOA.MorphTypeRA</c> exactly as <see cref="ClerkBrowseEditContext.TrySetOption"/>
		/// (field "MorphType") sets it. The sense-path text columns (Form/Gloss, transduce-driven) are NOT
		/// list-choice targets, and any multi-sense/ambiguous possibility column maps to no token here — so
		/// this conservative guard never selects a column whose correct object is ambiguous, avoiding a
		/// wrong-object bulk write.
		/// </summary>
		public string ListChoiceField { get; private set; }

		/// <summary>True when the column is a valid bulk List-Choice (possibility-reference) target.</summary>
		public bool IsListChoiceTarget => ListChoiceField != null;

		/// <summary>
		/// True when the column is a valid bulk-COPY TARGET (Phase 2): an entry-anchored, editable TEXT
		/// column the delegating edit context can write SAFELY with <c>TrySetText</c>. This is deliberately
		/// STRICTER than <see cref="IsEditable"/>: it admits only the entry-anchored text write (the Lexeme
		/// Form, <c>EditField == "Form"</c>) and EXCLUDES the sense-path Gloss — whose correct object is a
		/// sense of a possibly multi-sense row, the same wrong-object hazard the inline-edit rule guards
		/// against. So bulk copy never targets a column whose write object is ambiguous. The COPY SOURCE is
		/// any readable column (see the row source), only the TARGET is constrained.
		/// </summary>
		public bool IsCopyTarget => EditField == "Form";

		/// <summary>
		/// True when the column is a valid bulk-CLEAR TARGET (Phase 3): the SAME conservative set of safe,
		/// entry-anchored editable TEXT columns Bulk Copy writes (today the Lexeme Form). Clear empties the
		/// target via the same <c>TrySetText</c> path, so it admits exactly the columns the edit context can
		/// write safely and EXCLUDES the sense-path Gloss (and any multi-sense/ambiguous text column) — the
		/// same wrong-object hazard the other tabs guard against, so a clear can never empty the wrong object.
		/// </summary>
		public bool IsClearTarget => IsCopyTarget;

		/// <summary>
		/// True when the column is a valid bulk-REPLACE TARGET (Find/Replace Phase 1): the SAME conservative
		/// set of safe, entry-anchored editable TEXT columns Bulk Copy/Clear write (today the Lexeme Form).
		/// Replace reads the current cell string, applies the find/replace pattern, and writes the result via
		/// the same <c>TrySetText</c> path, so it admits exactly the columns the edit context can write safely
		/// and EXCLUDES the sense-path Gloss (and any multi-sense/ambiguous text column) — the same
		/// wrong-object hazard the other tabs guard against, so a replace can never write the wrong object.
		/// </summary>
		public bool IsReplaceTarget => IsCopyTarget;

		/// <summary>
		/// True when the column is a valid bulk-TRANSDUCE (Process) TARGET: the SAME conservative set of safe,
		/// entry-anchored editable TEXT columns Bulk Copy/Clear/Replace write (today the Lexeme Form). Transduce
		/// reads the SOURCE cell, runs the chosen converter over it, computes the target value per the mode, and
		/// writes the result via the same <c>TrySetText</c> path, so it admits exactly the columns the edit
		/// context can write safely and EXCLUDES the sense-path Gloss (and any multi-sense/ambiguous text column)
		/// — the same wrong-object hazard the other tabs guard against, so a transduce can never write the wrong
		/// object. The SOURCE is any readable column (constrained only on the TARGET, like Bulk Copy).
		/// </summary>
		public bool IsTransduceTarget => IsCopyTarget;

		/// <summary>
		/// The ONE place the legacy XML column vocabulary (<c>field</c>/<c>ws</c>/<c>transduce</c>) is
		/// classified into the typed edit spec. Keeping the string matching here (rather than scattered
		/// across the adapter) is what lets new editor kinds be added by extending this function instead of
		/// adding more inline string tests at the seam.
		/// </summary>
		public static BrowseColumnEditSpec FromColumnAttributes(string field, string ws, string transduce)
		{
			return new BrowseColumnEditSpec(
				editField: MapEditableField(field, transduce),
				writingSystemTag: NormalizeWs(ws),
				rawField: field,
				rawWritingSystem: ws,
				transduce: transduce)
			{
				ListChoiceField = MapListChoiceField(field, transduce)
			};
		}

		// Maps a legacy column's possibility-reference attributes to the option-key write token the
		// delegating edit context can set SAFELY against the row's entry. Only the unambiguous,
		// entry-anchored Morph Type is enabled in Phase 1: its column carries the lexeme-form list field
		// (field="LexEntry.LexemeForm") and no transduce, and its write target is the entry's lexeme form
		// morph-type reference. Anything else — including multi-sense possibility columns whose correct
		// object is ambiguous — returns null and is never offered as a bulk target.
		internal static string MapListChoiceField(string field, string transduce)
		{
			// A transduce-driven column is an inline TEXT cell, never a list-choice possibility target.
			if (!string.IsNullOrEmpty(transduce))
				return null;
			if (field == "LexEntry.LexemeForm")
				return "MorphType";
			return null;
		}

		// Maps a legacy column write spec (a plain `field`, or a `transduce` target) to the field token the
		// delegating LexicalEditRegionEditContext can write SAFELY against the row's entry. Only
		// entry-anchored writes are enabled: the lexeme form (unambiguous) and the primary-sense gloss (the
		// same target the detail pane writes). Sense-path targets like LexSense.Definition — and anything
		// whose correct object is an arbitrary sense of a multi-sense row — return null and stay read-only;
		// writing the wrong sense is a data-safety hazard, and resolving the sort-item path to the right
		// sense is the deferred Stage-3c work. Returns null for any unsupported spec.
		internal static string MapEditableField(string field, string transduce)
		{
			switch (field)
			{
				case "Form":
				case "Gloss":
				case "MorphType":
					return field;
			}
			switch (transduce)
			{
				case "LexEntry.LexemeForm.Form":
					return "Form";
				case "LexSense.Gloss":
					return "Gloss";
				default:
					return null;
			}
		}

		// A column ws attribute may be a magic "$ws=..." spec; the delegating context only resolves
		// concrete ws ids/abbreviations (or the vern/anal aliases), so an unresolved spec simply yields
		// no write. Map the common vernacular/analysis magic specs to the legacy aliases.
		internal static string NormalizeWs(string ws)
		{
			if (string.IsNullOrEmpty(ws))
				return "vern";
			if (ws.Contains("anal"))
				return "anal";
			if (ws.Contains("vern"))
				return "vern";
			return ws;
		}
	}
}

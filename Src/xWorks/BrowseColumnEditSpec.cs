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
				transduce: transduce);
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

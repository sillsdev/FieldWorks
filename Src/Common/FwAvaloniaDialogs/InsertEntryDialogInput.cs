// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free input to the reusable Avalonia Insert Entry dialog — the Phase 1 replacement for the
	/// legacy <c>InsertEntryDlg</c> in New-UI mode. The product edge (the LexText launcher) builds this from the
	/// live cache so the Avalonia layer never sees an <c>ICmObject</c>: the lexeme form / gloss fields are
	/// projected as <see cref="LexicalEditRegionField"/>s (one writing-system row per current vernacular /
	/// analysis WS, seeded empty unless <see cref="LexemeForm"/>'s values carry an initial form), the morph
	/// types are flat <see cref="RegionChoiceOption"/>s (key = morph-type guid string), and the live
	/// affix-marker → morph-type derivation rides the <see cref="DeriveMorphType"/> delegate (the launcher wraps
	/// <c>MorphServices.GetTypeIfMatchesPrefix</c>/<c>FindMorphType</c>).
	///
	/// Scope: lexeme form + gloss + morph type, plus the duplicate-detection "matching entries" pane (P2, via
	/// <see cref="SearchMatches"/>). The complex-form / MSA / Create-and-Edit affordances (P3) remain out of scope.
	/// </summary>
	public sealed class InsertEntryDialogInput
	{
		/// <summary>
		/// The lexeme-form field (one row per current vernacular writing system). The launcher seeds the row
		/// values from an optional initial form (e.g. the word the user double-clicked in interlinear); rows are
		/// otherwise empty. The owned <c>FwMultiWsTextField</c> edits these through the in-memory edit context.
		/// </summary>
		public LexicalEditRegionField LexemeForm { get; set; }

		/// <summary>The gloss field (one row per current analysis writing system); rows start empty.</summary>
		public LexicalEditRegionField Gloss { get; set; }

		/// <summary>The selectable morph types (flat; key = morph-type guid string, name = display name).</summary>
		public IReadOnlyList<RegionChoiceOption> MorphTypes { get; set; } = Array.Empty<RegionChoiceOption>();

		/// <summary>The morph-type key (guid string) selected on open — the legacy default of "stem".</summary>
		public string InitialMorphTypeKey { get; set; }

		/// <summary>
		/// The live affix-marker → morph-type derivation: given the current best lexeme form it returns the
		/// derived morph-type key (guid string) plus the marker-adjusted form (e.g. typing "-ed" derives the
		/// suffix morph type and keeps the "-ed" marker). The launcher supplies this by wrapping
		/// <c>MorphServices.GetTypeIfMatchesPrefix</c>/<c>FindMorphType</c>; a null typeKey means "leave the
		/// current morph-type selection". Null delegate disables live derivation (the picker stays manual).
		/// </summary>
		public Func<string, (string typeKey, string adjustedForm)> DeriveMorphType { get; set; }

		/// <summary>The prompt shown above the fields (localized by the caller); null/empty hides it.</summary>
		public string Prompt { get; set; }

		/// <summary>The help topic id for the dialog's Help button (null/empty hides Help). Phase 1 carries it only.</summary>
		public string HelpTopic { get; set; }

		/// <summary>
		/// The duplicate-detection ("matching entries") search delegate — the P2 lift of the legacy
		/// <c>InsertEntryDlg.UpdateMatches</c> / <c>MatchingObjectsBrowser</c>. Given the current best lexeme form it
		/// returns the EXISTING entries whose lexeme/citation/alternate form matches (each a lightweight
		/// <see cref="EntryGoSearchResult"/>: id = entry hvo string, text = headword, subText/description = gloss), so
		/// the user can pick an existing entry rather than create a duplicate. The launcher supplies this by wrapping
		/// the SAME matching the legacy dialog uses (the shared <c>EntryGoSearchEngine</c>) over the live entry
		/// repository. Re-run as the lexeme form changes; an empty form clears the list. Null disables the matches
		/// pane entirely (it is hidden), so existing consumers that never set it are unaffected.
		/// </summary>
		public Func<string, IReadOnlyList<EntryGoSearchResult>> SearchMatches { get; set; }
	}
}

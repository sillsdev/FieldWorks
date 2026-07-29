// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free input to the reusable Avalonia Insert Entry dialog — the Avalonia analog of the
	/// legacy <c>InsertEntryDlg</c> in New-UI mode. The product edge (the LexText launcher) builds this from the
	/// live cache so the Avalonia layer never sees an <c>ICmObject</c>: the lexeme form / gloss fields are
	/// projected as <see cref="RegionField"/>s (one writing-system row per current vernacular /
	/// analysis WS, seeded empty unless <see cref="LexemeForm"/>'s values carry an initial form), the morph
	/// types are flat <see cref="RegionChoiceOption"/>s (key = morph-type guid string), and the live
	/// affix-marker → morph-type derivation rides the <see cref="DeriveMorphType"/> delegate (the launcher wraps
	/// <c>MorphServices.GetTypeIfMatchesPrefix</c>/<c>FindMorphType</c>).
	/// </summary>
	public sealed class InsertEntryDlgInput
	{
		/// <summary>
		/// The lexeme-form field (one row per current vernacular writing system). The launcher seeds the row
		/// values from an optional initial form (e.g. the word the user double-clicked in interlinear); rows are
		/// otherwise empty. The owned <c>FwMultiWsTextField</c> edits these through the in-memory edit context.
		/// </summary>
		public RegionField LexemeForm { get; set; }

		/// <summary>The gloss field (one row per current analysis writing system); rows start empty.</summary>
		public RegionField Gloss { get; set; }

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

		/// <summary>The help topic id for the dialog's Help button (null/empty hides Help).</summary>
		public string HelpTopic { get; set; }

		/// <summary>
		/// The duplicate-detection ("matching entries") search delegate — the lift of the legacy
		/// <c>InsertEntryDlg.UpdateMatches</c> / <c>MatchingObjectsBrowser</c>. Given the current best lexeme form AND
		/// the current best gloss it returns the EXISTING entries whose lexeme/citation/alternate form OR gloss matches
		/// (each a lightweight <see cref="EntryGoSearchResult"/>: id = entry hvo string, text = headword,
		/// subText/description = gloss), so the user can pick an existing entry rather than create a duplicate. The
		/// launcher supplies this by wrapping the SAME matching the legacy dialog uses (the
		/// <c>InsertEntrySearchEngine</c>, which searches the vernacular citation/lexeme/alternate forms AND the
		/// analysis gloss — legacy <c>GetFields</c>) over the live entry repository. Re-run as the lexeme form OR the
		/// gloss changes; both empty clears the list. Null disables the matches pane entirely (it is hidden), so
		/// existing consumers that never set it are unaffected.
		/// </summary>
		public Func<string, string, IReadOnlyList<EntryGoSearchResult>> SearchMatches { get; set; }

		/// <summary>
		/// The launcher-supplied morphology validation — the LCModel-aware lift of the legacy
		/// <c>InsertEntryDlg.CheckMorphType</c> (:1439) + <c>CircumfixProblem</c> (:1494) + the <c>ksInvalidForm</c>
		/// parse (:1681) run on OK. Given the staged per-writing-system lexeme forms (keyed by WS tag) and the chosen
		/// morph-type key it returns which morphology problem (if any) the form/type combination has, so the dialog can
		/// gate OK AND surface the matching localized message inline (the CreateFeature pattern). Null leaves the
		/// dialog with only the empty-form OK gate (older-caller parity).
		/// </summary>
		public Func<IReadOnlyDictionary<string, string>, string, InsertEntryMorphValidation> ValidateMorphology { get; set; }

		/// <summary>
		/// The launcher-supplied "re-mark the lexeme form with the morph type's markers" delegate — the LCModel-aware
		/// lift of the legacy <c>cbMorphType_SelectedIndexChanged</c> (:1706) <c>BestForm = m_morphType.FormWithMarkers(BestForm)</c>.
		/// Given the chosen morph-type key and the current best lexeme form it returns the form re-marked with that
		/// type's affix markers (e.g. picking "suffix" turns "ed" into "-ed"); a circumfix returns the form unchanged
		/// (as the legacy does, since a circumfix mixes prefix/infix/suffix). Null leaves the form untouched on an
		/// explicit morph-type pick.
		/// </summary>
		public Func<string, string, string> ApplyMorphTypeMarkers { get; set; }

		/// <summary>
		/// Which field takes the initial focus (legacy <c>SetInitialFocus</c> :514): the lexeme form normally, the
		/// gloss when the dialog was seeded from an analysis-writing-system initial string (legacy SetDlgInfo :864-871).
		/// </summary>
		public InsertEntryInitialFocus InitialFocus { get; set; } = InsertEntryInitialFocus.LexemeForm;

		// ----- grammatical-info (MSA) section -----

		/// <summary>
		/// The project's parts-of-speech hierarchy as a flat, document-order, depth-tagged <see cref="FwPosNode"/>
		/// list (the launcher builds it from <c>cache.LangProject.PartsOfSpeechOA</c>). Fed to BOTH POS choosers
		/// inside the hosted <see cref="MSAGroupBox"/>. Empty (the default) leaves the MSA section's POS choosers
		/// with only the "&lt;Any&gt;" row — so existing consumers that never set it are unaffected.
		/// </summary>
		public IReadOnlyList<FwPosNode> PosNodes { get; set; } = Array.Empty<FwPosNode>();

		/// <summary>
		/// The morph-type → grammatical-info class map (key = morph-type guid string, value = <see cref="FwMsaType"/>),
		/// mirroring the WinForms <c>MSAGroupBox.MorphTypePreference</c> switch. The dialog uses it to drive the MSA
		/// box's <see cref="MSAGroupBox.MsaType"/> LIVE as the user's morph-type selection changes, so the kit stays
		/// LCModel-free. A morph type absent from the map (or a null map) falls back to <see cref="InitialMsaType"/>.
		/// </summary>
		public IReadOnlyDictionary<string, FwMsaType> MorphTypeToMsaType { get; set; }

		/// <summary>The MSA class the box opens in (the legacy default the initial morph type implies; usually Stem).</summary>
		public FwMsaType InitialMsaType { get; set; } = FwMsaType.Stem;

		/// <summary>The main-POS id (guid string) selected on open, or null for the "&lt;Any&gt;" pick.</summary>
		public string InitialMainPosId { get; set; }

		/// <summary>
		/// Builds the inflectional-affix slot options (the legacy <c>MSAGroupBox.ResetSlotCombo</c>/<c>GetSlots</c>)
		/// for a given main-POS id (guid string) — the launcher wraps the domain slot services
		/// (<c>pos.AllAffixSlots</c> filtered by the prefixal/suffixal morph type). The dialog re-runs it whenever the
		/// MSA box's main POS changes while the box is inflectional, refeeding <see cref="MSAGroupBox.SetSlots"/>.
		/// Null leaves the slot list empty.
		/// </summary>
		public Func<string, IReadOnlyList<FwInflectionSlot>> SlotsForPos { get; set; }

		/// <summary>
		/// Builds the inflection-class options for a given main-POS id (guid string) — the launcher wraps
		/// <c>IPartOfSpeech.InflectionClassesOC</c> (incl. nested <c>SubclassesOC</c>, depth-tagged). The dialog re-runs
		/// it whenever the MSA box's MAIN POS changes (the parity of the WinForms POS-change path that resets the
		/// inflection-class tree), refeeding <see cref="MSAGroupBox.SetInflectionClasses"/>. Null leaves the list
		/// empty (only the "&lt;None&gt;" row).
		/// </summary>
		public Func<string, IReadOnlyList<FwInflectionClass>> InflectionClassesForPos { get; set; }

		/// <summary>The inflection-class id selected on open (stem/root only), or null for "&lt;None&gt;".</summary>
		public string InitialInflectionClassId { get; set; }

		/// <summary>
		/// Builds the inflection-feature SYSTEM (a flat, document-order, depth-tagged <see cref="FwFeatureNode"/> list)
		/// for a given main-POS id (guid string) — the launcher wraps the POS's <c>InflectableFeatsRC</c> (incl. its
		/// parent POSes', the lift of <c>MsaInflectionFeatureListDlg.PopulateTreeFromPos</c>). The dialog re-runs it
		/// whenever the MSA box's MAIN POS changes (infl/deriv), refeeding
		/// <see cref="MSAGroupBox.SetInflectionFeatureNodes"/>. Null leaves the editor empty.
		/// </summary>
		public Func<string, IReadOnlyList<FwFeatureNode>> InflectionFeaturesForPos { get; set; }

		/// <summary>The inflection-feature assignments selected on open (infl/deriv only); empty/null for none.</summary>
		public IReadOnlyList<FwFeatureValueAssignment> InitialInflectionFeatures { get; set; }

		// ----- complex-form type picker (WinForms m_cbComplexFormType parity, LT-21666) -----

		/// <summary>
		/// The selectable complex-form types (flat; key = complex-entry-type guid string, name = display name),
		/// in sorted display order — the legacy <c>m_cbComplexFormType</c> items after its leading "&lt;Not
		/// Applicable&gt;" entry (the kit prepends that itself). The launcher builds this from
		/// <c>LexDbOA.ComplexEntryTypesOA.ReallyReallyAllPossibilities</c>. Empty (the default) leaves the picker
		/// with only the "&lt;Not Applicable&gt;" row, so existing consumers that never set it are unaffected.
		/// </summary>
		public IReadOnlyList<RegionChoiceOption> ComplexFormTypes { get; set; } = Array.Empty<RegionChoiceOption>();

		/// <summary>The complex-form type key (guid string) selected on open, or null for "&lt;Not Applicable&gt;".</summary>
		public string InitialComplexFormTypeKey { get; set; }

		/// <summary>
		/// The morph-type-guid → <see cref="ComplexFormGating"/> map — the data lift of the WinForms
		/// <c>EnableComplexFormTypeCombo</c> switch, supplied so the kit stays LCModel-free. As the user's
		/// morph-type selection changes the dialog reads this to set the complex-form picker's enabled state and,
		/// for the bound-root/root case, force the selection back to "&lt;Not Applicable&gt;". A morph type absent
		/// from the map (or a null map) defaults to <see cref="ComplexFormGating.EnabledNotApplicable"/> (the
		/// WinForms <c>default</c> branch: enabled, selection reset to Not-Applicable).
		/// </summary>
		public IReadOnlyDictionary<string, ComplexFormGating> ComplexFormGatingByMorphType { get; set; }
	}

	/// <summary>
	/// How the morph-type selection gates the Insert Entry dialog's Complex Form Type picker — the data lift of the
	/// WinForms <c>InsertEntryDlg.EnableComplexFormTypeCombo</c> switch (LT-21666). The launcher supplies one of
	/// these per morph type so the LCModel-free kit can drive the picker without referencing morph-type guids.
	/// </summary>
	public enum ComplexFormGating
	{
		/// <summary>
		/// Bound-root / root: disable the picker AND force its selection to "&lt;Not Applicable&gt;" (a complex
		/// form type makes no sense for a root). The WinForms bound-root/root branch.
		/// </summary>
		DisabledNotApplicable,

		/// <summary>
		/// Phrase / discontiguous-phrase: enable the picker but LEAVE the current selection (do not reset to
		/// Not-Applicable — LT-21666). The WinForms phrase branch.
		/// </summary>
		EnabledKeepSelection,

		/// <summary>
		/// Default (every other morph type): enable the picker and reset the selection to "&lt;Not Applicable&gt;".
		/// The WinForms <c>default</c> branch.
		/// </summary>
		EnabledNotApplicable
	}

	/// <summary>
	/// The morphology-validation verdict the launcher-supplied <see cref="InsertEntryDlgInput.ValidateMorphology"/>
	/// returns for the current lexeme form + morph-type combination — the data lift of the legacy
	/// <c>InsertEntryDlg</c> OK-time checks. Each non-<see cref="Valid"/> verdict maps to a localized inline message
	/// that also gates OK.
	/// </summary>
	public enum InsertEntryMorphValidation
	{
		/// <summary>No morphology problem (or the delegate had nothing to check).</summary>
		Valid,

		/// <summary>The typed affix markers do not match the chosen morph type (legacy <c>CheckMorphType</c> / <c>ksInvalidLexForm</c>).</summary>
		InvalidLexForm,

		/// <summary>A circumfix is missing its left or right part in some writing system (legacy <c>CircumfixProblem</c> / <c>ksCompleteCircumfix</c>).</summary>
		IncompleteCircumfix,

		/// <summary>The form could not be parsed into a morpheme type (legacy <c>FindMorphType</c> throw / <c>ksInvalidForm</c>).</summary>
		InvalidForm
	}

	/// <summary>Which field the Insert Entry dialog focuses on open (legacy <c>SetInitialFocus</c>).</summary>
	public enum InsertEntryInitialFocus
	{
		/// <summary>Focus the lexeme form (the normal create flow).</summary>
		LexemeForm,

		/// <summary>Focus the gloss (the dialog was seeded from an analysis-WS initial string).</summary>
		Gloss
	}
}

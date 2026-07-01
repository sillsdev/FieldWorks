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
	/// The LCModel-free input to the reusable Avalonia Add New Sense dialog — the MSA-port Stage 5 replacement for
	/// the legacy <c>AddNewSenseDlg</c> in New-UI mode. The product edge (the LexText launcher) builds this from the
	/// live cache so the Avalonia layer never sees an <c>ICmObject</c>: the read-only CITATION FORM is a plain
	/// display string (the legacy <c>m_fwtbCitationForm</c>, never edited), the editable GLOSS is projected as a
	/// per-analysis-WS <see cref="LexicalEditRegionField"/> (the legacy <c>m_fwtbGloss</c>), and the
	/// grammatical-info section is fed exactly as the Insert Entry dialog feeds its <see cref="FwMsaGroupBox"/>
	/// (the POS hierarchy, slot provider, and the initial MsaType the entry's morph type implies — the lift of the
	/// legacy <c>MSAGroupBox.MorphTypePreference</c>).
	///
	/// Mirrors <see cref="InsertEntryDialogInput"/>'s shape for the gloss + MSA fields so the kit stays LCModel-free.
	/// </summary>
	public sealed class AddNewSenseDialogInput
	{
		/// <summary>
		/// The read-only citation form of the entry the sense is being added to (the legacy <c>m_fwtbCitationForm</c>,
		/// shown but never edited). Display-only; null/empty hides the row.
		/// </summary>
		public string CitationForm { get; set; }

		/// <summary>
		/// The gloss field (one row per current analysis writing system); rows start empty. The owned
		/// <c>FwMultiWsTextField</c> edits these through the in-memory edit context — the legacy <c>m_fwtbGloss</c>.
		/// </summary>
		public LexicalEditRegionField Gloss { get; set; }

		/// <summary>The prompt shown above the fields (localized by the caller); null/empty hides it.</summary>
		public string Prompt { get; set; }

		/// <summary>The help topic id for the dialog's Help button (null/empty hides Help). Phase 1 carries it only.</summary>
		public string HelpTopic { get; set; }

		// ----- grammatical-info (MSA) section (mirrors InsertEntryDialogInput) -----

		/// <summary>
		/// The project's parts-of-speech hierarchy as a flat, document-order, depth-tagged <see cref="FwPosNode"/>
		/// list, fed to BOTH POS choosers inside the hosted <see cref="FwMsaGroupBox"/>. Empty leaves the choosers
		/// with only the "&lt;Any&gt;" row.
		/// </summary>
		public IReadOnlyList<FwPosNode> PosNodes { get; set; } = Array.Empty<FwPosNode>();

		/// <summary>The MSA class the box opens in (the legacy default the entry's morph type implies; usually Stem).</summary>
		public FwMsaType InitialMsaType { get; set; } = FwMsaType.Stem;

		/// <summary>The main-POS id (guid string) selected on open, or null for the "&lt;Any&gt;" pick.</summary>
		public string InitialMainPosId { get; set; }

		/// <summary>
		/// Builds the inflectional-affix slot options (the legacy <c>MSAGroupBox.ResetSlotCombo</c>/<c>GetSlots</c>)
		/// for a given main-POS id (guid string). The dialog re-runs it whenever the MSA box's main POS changes while
		/// the box is inflectional, refeeding <see cref="FwMsaGroupBox.SetSlots"/>. Null leaves the slot list empty.
		/// </summary>
		public Func<string, IReadOnlyList<FwInflectionSlot>> SlotsForPos { get; set; }

		/// <summary>
		/// Builds the inflection-class options for a given main-POS id (guid string) — the launcher wraps
		/// <c>IPartOfSpeech.InflectionClassesOC</c> (incl. nested subclasses, depth-tagged). Re-run whenever the MSA
		/// box's MAIN POS changes, refeeding <see cref="FwMsaGroupBox.SetInflectionClasses"/>. Null leaves the list
		/// empty (only the "&lt;None&gt;" row).
		/// </summary>
		public Func<string, IReadOnlyList<FwInflectionClass>> InflectionClassesForPos { get; set; }

		/// <summary>The inflection-class id selected on open (stem/root only), or null for "&lt;None&gt;".</summary>
		public string InitialInflectionClassId { get; set; }

		/// <summary>
		/// Builds the inflection-feature SYSTEM (a flat, document-order, depth-tagged <see cref="FwFeatureNode"/> list)
		/// for a given main-POS id (guid string) — the launcher wraps the POS's <c>InflectableFeatsRC</c> (the lift of
		/// <c>MsaInflectionFeatureListDlg.PopulateTreeFromPos</c>). Re-run when the MSA box's MAIN POS changes
		/// (infl/deriv), refeeding <see cref="FwMsaGroupBox.SetInflectionFeatureNodes"/>. Null leaves the editor empty
		/// (Phase-1 §19b Stage 2).
		/// </summary>
		public Func<string, IReadOnlyList<FwFeatureNode>> InflectionFeaturesForPos { get; set; }

		/// <summary>The inflection-feature assignments selected on open (infl/deriv only); empty/null for none.</summary>
		public IReadOnlyList<FwFeatureValueAssignment> InitialInflectionFeatures { get; set; }
	}
}

// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free input to the reusable Avalonia "Create New Grammatical Info." dialog — the MSA-port Stage 5
	/// replacement for the legacy <c>MsaCreatorDlg</c> in New-UI mode. The product edge (the LexText launcher)
	/// builds this from the live cache so the Avalonia layer never sees an <c>ICmObject</c>: the read-only LEXICAL
	/// ENTRY headword (<c>m_fwtbCitationForm</c>) and the read-only SENSES summary (<c>m_fwtbSenses</c>) are plain
	/// display strings, and the grammatical-info section is fed exactly as the Insert Entry / Add New Sense dialogs
	/// feed their <see cref="FwMsaGroupBox"/> — seeded from the existing MSA / morph type (the legacy
	/// <c>m_msaGroupBox.Initialize(..., sandboxMsa)</c>).
	///
	/// The dialog is essentially the <see cref="FwMsaGroupBox"/> hosted over the entry's read-only context.
	/// </summary>
	public sealed class MsaCreatorDialogInput
	{
		/// <summary>The dialog window title — "Create New Grammatical Info." for create, or an edit-context title.</summary>
		public string Title { get; set; }

		/// <summary>The read-only headword of the lexical entry (the legacy m_fwtbCitationForm); null/empty hides the row.</summary>
		public string LexicalEntry { get; set; }

		/// <summary>
		/// The read-only senses summary that share the MSA being edited (the legacy m_fwtbSenses, populated only when
		/// editing an existing MSA — the senses whose MorphoSyntaxAnalysisRA is the original). Null/empty hides the row.
		/// </summary>
		public string Senses { get; set; }

		/// <summary>The help topic id for the dialog's Help button (null/empty hides Help).</summary>
		public string HelpTopic { get; set; }

		// ----- grammatical-info (MSA) section (mirrors InsertEntryDialogInput) -----

		/// <summary>
		/// The project's parts-of-speech hierarchy as a flat, document-order, depth-tagged <see cref="FwPosNode"/>
		/// list, fed to BOTH POS choosers inside the hosted <see cref="FwMsaGroupBox"/>.
		/// </summary>
		public IReadOnlyList<FwPosNode> PosNodes { get; set; } = Array.Empty<FwPosNode>();

		/// <summary>
		/// The MSA class the box opens in — the type of the existing MSA being edited, or the entry's desired type for
		/// a fresh create (the legacy <c>sandboxMsa.MsaType</c>).
		/// </summary>
		public FwMsaType InitialMsaType { get; set; } = FwMsaType.Stem;

		/// <summary>The main-POS id (guid string) selected on open, or null for the "&lt;Any&gt;" pick.</summary>
		public string InitialMainPosId { get; set; }

		/// <summary>The secondary ("changes to") POS id selected on open (derivational only), or null.</summary>
		public string InitialSecondaryPosId { get; set; }

		/// <summary>The inflectional-affix slot id selected on open (inflectional only), or null.</summary>
		public string InitialSlotId { get; set; }

		/// <summary>
		/// Builds the inflectional-affix slot options for a given main-POS id (guid string). The dialog re-runs it
		/// whenever the MSA box's main POS changes while inflectional, refeeding <see cref="FwMsaGroupBox.SetSlots"/>.
		/// Null leaves the slot list empty.
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

		/// <summary>
		/// The inflection-feature assignments selected on open (infl/deriv only) — the launcher reads them from the
		/// MSA's existing <c>IFsFeatStruc</c> (<c>InflFeatsOA</c> / <c>FromMsFeaturesOA</c>). Empty/null for none.
		/// </summary>
		public IReadOnlyList<FwFeatureValueAssignment> InitialInflectionFeatures { get; set; }
	}
}

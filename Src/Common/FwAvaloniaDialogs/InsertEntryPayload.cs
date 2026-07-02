// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free snapshot the Insert Entry view-model writes on OK — the per-writing-system lexeme-form
	/// and gloss values (keyed by writing-system tag) plus the chosen morph-type key (guid string). The
	/// LCModel-aware launcher reads this back to build the <c>LexEntryComponents</c> and create the entry in one
	/// undoable step.
	/// </summary>
	public sealed class InsertEntryPayload
	{
		public InsertEntryPayload(IReadOnlyDictionary<string, string> lexemeFormByWs,
			IReadOnlyDictionary<string, string> glossByWs, string morphTypeKey,
			string chosenExistingEntryId = null, FwSandboxMsa msa = null, string complexFormTypeKey = null)
		{
			LexemeFormByWs = lexemeFormByWs ?? new Dictionary<string, string>();
			GlossByWs = glossByWs ?? new Dictionary<string, string>();
			MorphTypeKey = morphTypeKey;
			ChosenExistingEntryId = chosenExistingEntryId;
			Msa = msa;
			ComplexFormTypeKey = complexFormTypeKey;
		}

		/// <summary>The lexeme-form alternatives, keyed by writing-system tag (only non-empty alternatives).</summary>
		public IReadOnlyDictionary<string, string> LexemeFormByWs { get; }

		/// <summary>The gloss alternatives, keyed by writing-system tag (only non-empty alternatives).</summary>
		public IReadOnlyDictionary<string, string> GlossByWs { get; }

		/// <summary>The chosen morph-type key (guid string); the launcher resolves it back to the IMoMorphType.</summary>
		public string MorphTypeKey { get; }

		/// <summary>
		/// The id (entry hvo string) of an EXISTING entry the user chose from the matching-entries pane — the legacy
		/// "use similar entry" outcome (<c>InsertEntryDlg</c> returning <c>m_fNewlyCreated = false</c> with the
		/// selected <c>ILexEntry</c>). Non-null means the launcher must JUMP TO that existing entry instead of creating
		/// a duplicate; null means create a new entry from the form/gloss/morph-type values above.
		/// </summary>
		public string ChosenExistingEntryId { get; }

		/// <summary>
		/// The chosen grammatical info (MSA) — the LCModel-free <see cref="FwSandboxMsa"/> the hosted
		/// <see cref="FwMsaGroupBox"/> emitted (MsaType + main/secondary POS ids + slot id). The launcher resolves
		/// the ids back to LCModel objects, builds a real <c>SandboxGenericMSA</c>, and find-or-creates the MSA on the
		/// new entry's first sense (exactly as the WinForms <c>InsertEntryDlg</c> does). Null when the dialog had no
		/// MSA section configured (the launcher then falls back to the morph-type's default MSA).
		/// </summary>
		public FwSandboxMsa Msa { get; }

		/// <summary>
		/// The chosen complex-form type key (complex-entry-type guid string) — the WinForms <c>m_complexType</c>
		/// parity (LT-21666). Null/empty when the user left the picker at "&lt;Not Applicable&gt;": the launcher then
		/// adds NO <c>ILexEntryRef</c>. When non-null the launcher creates a complex-form <c>ILexEntryRef</c> on the
		/// new entry (<c>RefType = krtComplexForm</c>) and adds the resolved <c>ILexEntryType</c> to its
		/// <c>ComplexEntryTypesRS</c>, exactly as <c>CreateNewEntryInternal</c> does. (No components are added.)
		/// </summary>
		public string ComplexFormTypeKey { get; }

		/// <summary>An empty payload (no values, no morph type) for a cancelled dialog.</summary>
		public static InsertEntryPayload Empty =>
			new InsertEntryPayload(new Dictionary<string, string>(), new Dictionary<string, string>(), null);
	}
}

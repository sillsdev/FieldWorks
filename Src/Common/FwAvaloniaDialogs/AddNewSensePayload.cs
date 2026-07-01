// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free snapshot the Add New Sense view-model writes on OK — the per-analysis-WS gloss values
	/// (keyed by writing-system tag) plus the chosen grammatical info (the <see cref="FwSandboxMsa"/> the hosted
	/// <see cref="FwMsaGroupBox"/> emitted). The LCModel-aware launcher reads this back to create the new
	/// <c>ILexSense</c> (gloss + find-or-created MSA) in one undoable step — the lift of <c>AddNewSenseDlg</c>'s OK
	/// branch (set the gloss; <c>lsNew.SandboxMSA = m_msaGroupBox.SandboxMSA</c>).
	/// </summary>
	public sealed class AddNewSensePayload
	{
		public AddNewSensePayload(IReadOnlyDictionary<string, string> glossByWs, FwSandboxMsa msa)
		{
			GlossByWs = glossByWs ?? new Dictionary<string, string>();
			Msa = msa;
		}

		/// <summary>The gloss alternatives, keyed by writing-system tag (only non-empty alternatives).</summary>
		public IReadOnlyDictionary<string, string> GlossByWs { get; }

		/// <summary>
		/// The chosen grammatical info (MSA) — the LCModel-free <see cref="FwSandboxMsa"/> (MsaType + main/secondary
		/// POS ids + slot id). The launcher resolves the ids back to LCModel objects and find-or-creates the MSA on
		/// the new sense (exactly as <c>AddNewSenseDlg</c> assigns <c>lsNew.SandboxMSA</c>). Null when no MSA section.
		/// </summary>
		public FwSandboxMsa Msa { get; }

		/// <summary>An empty payload (no gloss, no MSA) for a cancelled dialog.</summary>
		public static AddNewSensePayload Empty => new AddNewSensePayload(new Dictionary<string, string>(), null);
	}
}

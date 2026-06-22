// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free snapshot the "Create New Grammatical Info." view-model writes on OK — the chosen grammatical
	/// info (the <see cref="FwSandboxMsa"/> the hosted <see cref="FwMsaGroupBox"/> emitted). The LCModel-aware
	/// launcher reads this back to find-or-create (or update) the matching MSA — the lift of <c>MsaCreatorDlg</c>'s
	/// <c>SandboxMSA</c> property + its consumers (<c>m_sense.SandboxMSA = dlg.SandboxMSA</c> /
	/// <c>originalMsa.UpdateOrReplace(dlg.SandboxMSA)</c>).
	/// </summary>
	public sealed class MsaCreatorPayload
	{
		public MsaCreatorPayload(FwSandboxMsa msa)
		{
			Msa = msa;
		}

		/// <summary>
		/// The chosen grammatical info (MSA) — the LCModel-free <see cref="FwSandboxMsa"/> (MsaType + main/secondary
		/// POS ids + slot id). The launcher resolves the ids back to LCModel objects and builds a real
		/// <c>SandboxGenericMSA</c> to find-or-create / update the MSA. Null when the box was unconfigured.
		/// </summary>
		public FwSandboxMsa Msa { get; }

		/// <summary>An empty payload (no MSA) for a cancelled dialog.</summary>
		public static MsaCreatorPayload Empty => new MsaCreatorPayload(null);
	}
}

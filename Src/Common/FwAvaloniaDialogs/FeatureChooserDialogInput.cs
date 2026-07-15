// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-FREE input DTO for the standalone feature-structure chooser dialog
	/// (<see cref="FeatureChooserDialogViewModel"/>, Phase-1 §19b Stage 3) — the Avalonia replacement for the
	/// WinForms <c>MsaInflectionFeatureListDlg</c> (inflection-feature variant) and
	/// <c>PhonologicalFeatureChooserDlg</c> (phonological variant). The LCModel-aware launcher builds this from the
	/// live feature system (the depth-tagged <see cref="FwFeatureNode"/> list — the lift of
	/// <c>FeatureStructureTreeView.AddNode</c> / <c>PopulateTreeFromPos</c>) and the current assignment set (read from
	/// an existing <c>IFsFeatStruc</c>); on OK the launcher rebuilds the <c>IFsFeatStruc</c> from the chosen
	/// <see cref="FeatureChooserPayload.Assignments"/>. The dialog itself holds NO model reference.
	/// </summary>
	public sealed class FeatureChooserDialogInput
	{
		/// <summary>The dialog window title (e.g. "Inflection Feature Information").</summary>
		public string Title { get; set; }

		/// <summary>An optional instruction prompt shown above the feature tree.</summary>
		public string Prompt { get; set; }

		/// <summary>The stable, nonlocalized AutomationId stem for the hosted editor (e.g. "InflFeatures").</summary>
		public string AutomationId { get; set; } = "Features";

		/// <summary>
		/// The feature system as a flat, document-order, depth-tagged <see cref="FwFeatureNode"/> list (closed
		/// features + their values, complex features + their nested features). Fed verbatim to
		/// <see cref="FwFeatureStructureEditor.SetNodes"/>.
		/// </summary>
		public IReadOnlyList<FwFeatureNode> Nodes { get; set; } = Array.Empty<FwFeatureNode>();

		/// <summary>
		/// The current chosen assignment set (read from the existing <c>IFsFeatStruc</c>), seeded silently into the
		/// editor. Empty / null when there is no feature structure yet (the create path).
		/// </summary>
		public IReadOnlyList<FwFeatureValueAssignment> InitialAssignments { get; set; }

		/// <summary>The help topic id for the Help button; null hides Help.</summary>
		public string HelpTopic { get; set; }
	}
}

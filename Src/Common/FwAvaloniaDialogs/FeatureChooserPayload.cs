// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-FREE result of the standalone feature-structure chooser dialog (Phase-1 §19b Stage 3): the flat
	/// <c>(closedFeatureId, valueId)</c> assignment set the hosted <see cref="FwFeatureStructureEditor"/> emitted on
	/// OK. The LCModel-aware launcher rebuilds the nested <c>IFsFeatStruc</c> from this set (recursive-ascent
	/// <c>GetOrCreateValue</c>), and an EMPTY set is the legacy "delete the FS / unspecified" case (LT-13596).
	/// </summary>
	public sealed class FeatureChooserPayload
	{
		public FeatureChooserPayload(IReadOnlyList<FwFeatureValueAssignment> assignments)
		{
			Assignments = assignments == null
				? (IReadOnlyList<FwFeatureValueAssignment>)Array.Empty<FwFeatureValueAssignment>()
				: assignments.Where(a => a != null).ToList();
		}

		/// <summary>The chosen feature→value assignment set (one entry per closed feature with a real value). Never null.</summary>
		public IReadOnlyList<FwFeatureValueAssignment> Assignments { get; }

		/// <summary>An empty payload (no features chosen — the unspecified / delete-the-FS case).</summary>
		public static FeatureChooserPayload Empty { get; } = new FeatureChooserPayload(null);
	}
}

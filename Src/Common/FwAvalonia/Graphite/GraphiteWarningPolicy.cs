// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.Graphite
{
	/// <summary>
	/// How a classification should be presented on an Avalonia surface right now.
	/// </summary>
	public enum GraphiteWarningPresentation
	{
		/// <summary>Nothing to present (G0, or already presented this session).</summary>
		None,

		/// <summary>Record as a diagnostic only (G1); visible in setup/support surfaces, never a popup.</summary>
		LogOnly,

		/// <summary>Show as a standard warning (G2), once per writing system per project session.</summary>
		Warning,

		/// <summary>Show prominently before first render (G3); re-presented every session while the condition holds.</summary>
		ProminentWarning
	}

	/// <summary>
	/// Per-project-session rate limiting for Graphite warnings (graphite-transition-support task 2.1):
	/// G1 never produces UI, G2/G3 present once per writing system per session, and a G3 condition is
	/// never permanently suppressed — a new session (new policy instance) presents it again.
	/// Instances are per project session; not thread-safe by design (UI-thread use).
	/// </summary>
	public sealed class GraphiteWarningPolicy
	{
		private readonly HashSet<string> _presentedWsIds = new HashSet<string>(StringComparer.Ordinal);

		/// <summary>
		/// Decides the presentation for a classification, recording G2/G3 presentations so each
		/// writing system warns at most once per session.
		/// </summary>
		public GraphiteWarningPresentation Decide(GraphiteWsClassification classification)
		{
			if (classification == null) throw new ArgumentNullException(nameof(classification));

			switch (classification.Tier)
			{
				case GraphiteTier.G0:
					return GraphiteWarningPresentation.None;
				case GraphiteTier.G1:
					return GraphiteWarningPresentation.LogOnly;
				case GraphiteTier.G2:
					return _presentedWsIds.Add(classification.WsId)
						? GraphiteWarningPresentation.Warning
						: GraphiteWarningPresentation.None;
				case GraphiteTier.G3:
					return _presentedWsIds.Add(classification.WsId)
						? GraphiteWarningPresentation.ProminentWarning
						: GraphiteWarningPresentation.None;
				default:
					return GraphiteWarningPresentation.None;
			}
		}
	}
}

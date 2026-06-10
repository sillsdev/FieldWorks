// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Graphite
{
	/// <summary>
	/// The graded Graphite-impact tier of a writing system on an Avalonia surface
	/// (graphite-transition-support, design.md classification table). The tier predicts actual
	/// rendering damage from font-table evidence, not just the user preference flag.
	/// </summary>
	public enum GraphiteTier
	{
		/// <summary>Unaffected: Graphite disabled, or the resolved font carries no Graphite tables.</summary>
		G0,

		/// <summary>Dual-engine font (OpenType + Graphite tables), no Graphite feature settings: OpenType shaping is expected to be equivalent; log-only diagnostic.</summary>
		G1,

		/// <summary>Dual-engine font with Graphite feature settings: features will not apply; once-per-session warning.</summary>
		G2,

		/// <summary>Graphite-only font (no functional OpenType shaping): rendering will be wrong; prominent pre-render warning.</summary>
		G3
	}

	/// <summary>
	/// Font-table evidence for one resolved font, produced by <see cref="FontTableSniffer"/>.
	/// <c>Silf</c> is the definitive Graphite marker; <c>GSUB</c>/<c>GPOS</c> indicate OpenType shaping.
	/// </summary>
	public sealed class GraphiteFontTableEvidence
	{
		public GraphiteFontTableEvidence(bool hasGraphiteTables, bool hasOpenTypeShapingTables)
		{
			HasGraphiteTables = hasGraphiteTables;
			HasOpenTypeShapingTables = hasOpenTypeShapingTables;
		}

		/// <summary>Evidence for a font that could not be inspected: assume no Graphite tables (yields G0, no false alarms).</summary>
		public static GraphiteFontTableEvidence Unknown { get; } = new GraphiteFontTableEvidence(false, false);

		public bool HasGraphiteTables { get; }

		public bool HasOpenTypeShapingTables { get; }
	}

	/// <summary>
	/// One writing system's classification: the tier plus the deterministic, user-facing message and
	/// the structured-diagnostic identity (spec: every G1–G3 determination is auditable).
	/// </summary>
	public sealed class GraphiteWsClassification
	{
		public GraphiteWsClassification(GraphiteTier tier, string wsId, string wsDisplayName, string fontName, string message)
		{
			Tier = tier;
			WsId = wsId ?? "";
			WsDisplayName = wsDisplayName ?? wsId ?? "";
			FontName = fontName ?? "";
			Message = message ?? "";
		}

		public GraphiteTier Tier { get; }

		/// <summary>Stable writing-system identifier (rate-limit key and diagnostic identity).</summary>
		public string WsId { get; }

		public string WsDisplayName { get; }

		public string FontName { get; }

		/// <summary>Deterministic user-facing message naming the writing system and font.</summary>
		public string Message { get; }

		/// <summary>Stable diagnostic code, e.g. <c>graphite-g2</c>.</summary>
		public string DiagnosticCode => "graphite-" + Tier.ToString().ToLowerInvariant();
	}

	/// <summary>
	/// Classifies a writing system G0–G3 from immutable inputs: the user's Graphite preference, the
	/// stored Graphite feature settings, and font-table evidence for the resolved default font.
	/// Deterministic: identical inputs always produce identical classifications (spec requirement).
	/// </summary>
	public static class GraphiteWsClassifier
	{
		public static GraphiteWsClassification Classify(
			bool isGraphiteEnabled,
			string defaultFontFeatures,
			GraphiteFontTableEvidence evidence,
			string wsId,
			string wsDisplayName,
			string fontName)
		{
			if (evidence == null) throw new ArgumentNullException(nameof(evidence));

			// Graphite off, or no Graphite tables in the font: legacy would not Graphite-shape this
			// writing system either, so the Avalonia surface renders equivalently.
			if (!isGraphiteEnabled || !evidence.HasGraphiteTables)
			{
				return new GraphiteWsClassification(GraphiteTier.G0, wsId, wsDisplayName, fontName, "");
			}

			// Graphite-enabled + Graphite tables but no OpenType shaping: OpenType/fallback shaping
			// will produce visibly wrong text (e.g. Awami Nastaliq class of fonts).
			if (!evidence.HasOpenTypeShapingTables)
			{
				return new GraphiteWsClassification(GraphiteTier.G3, wsId, wsDisplayName, fontName,
					string.Format(GraphiteWarningStrings.GraphiteOnlyFontWarningFormat, wsDisplayName, fontName));
			}

			// Dual-engine font with stored Graphite feature settings: text shapes, but the features
			// will not apply on the Avalonia surface.
			if (!string.IsNullOrWhiteSpace(defaultFontFeatures))
			{
				return new GraphiteWsClassification(GraphiteTier.G2, wsId, wsDisplayName, fontName,
					string.Format(GraphiteWarningStrings.GraphiteFeaturesWarningFormat, wsDisplayName, fontName));
			}

			// Dual-engine, no feature settings: OpenType shaping is expected to be equivalent.
			return new GraphiteWsClassification(GraphiteTier.G1, wsId, wsDisplayName, fontName,
				string.Format(GraphiteWarningStrings.DualEngineInfoFormat, wsDisplayName, fontName));
		}
	}

	/// <summary>
	/// Centralized user-facing warning strings. NOTE (lexical-edit 6.11): these must move to
	/// localized .resx resources with the rest of the product-facing FwAvalonia strings before broad
	/// rollout; AutomationIds stay nonlocalized regardless.
	/// </summary>
	public static class GraphiteWarningStrings
	{
		public const string GraphiteOnlyFontWarningFormat =
			"The writing system '{0}' uses the Graphite-only font '{1}'. The new editor cannot shape this font, so text will not display correctly. Switch to the Legacy UI for full Graphite rendering, or choose a replacement font.";

		public const string GraphiteFeaturesWarningFormat =
			"The writing system '{0}' uses Graphite font features of '{1}' that do not apply in the new editor. Text will render with OpenType shaping and may look different.";

		public const string DualEngineInfoFormat =
			"The writing system '{0}' has Graphite enabled for '{1}'; the new editor renders it with OpenType shaping.";

		public const string SwitchToLegacyAction = "Switch to Legacy UI";
	}
}

// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-interlinear-editor (task 1.3) — the Morphology/xWorks adapter that projects a
	/// <c>WfiAnalysis</c> (and its owning <c>WfiWordform</c>) into the LCModel-free
	/// <see cref="InterlinearAnalysisModel"/> the Avalonia interlinear control binds to. This is the READ
	/// side of design Decision 1: ALL LCModel reads live here in the plugin/adapter, never in the FwAvalonia
	/// view. The write side (per-bundle edit + MSA prune) is task 3.2 and lives in the plugin's edit context.
	/// <para>The legacy <c>InterlinearSlice</c> hosts an <c>AnalysisInterlinearRs</c> over ONE
	/// <c>IWfiAnalysis</c> (its <c>Object</c> is cast <c>(IWfiAnalysis)</c> in <c>FinishInit</c>), and the
	/// composer invokes the plugin once per <c>InterlinearAnal</c> row with that analysis as the target — so
	/// the projection produces a single <see cref="InterlinearLine"/> (the wordform line over this analysis's
	/// morpheme bundles), wrapped in the wordform-shaped model the control renders.</para>
	/// <para>The interlinear lines mirror the legacy <c>InterlinVc</c> morph-bundle lines: the morpheme form
	/// (the bundle's <c>MorphRA</c> form, falling back to the bundle's own stored <c>Form</c>), the lex-gloss
	/// (<c>SenseRA.Gloss</c>), and the grammatical-info abbreviation (<c>MsaRA.InterlinearAbbr</c> — the same
	/// abbreviation the LexPos line shows). Each bundle carries the morph/sense/MSA GUIDs the write-back maps
	/// edits onto.</para>
	/// </summary>
	public static class InterlinearAnalysisProjector
	{
		/// <summary>
		/// Projects one <paramref name="analysis"/> into the interlinear model (a single analysis line over
		/// the wordform). Tolerates a null analysis / wordform (returns a bare-wordform model with no
		/// bundles), so a half-built or deleted analysis degrades to the empty state rather than throwing.
		/// </summary>
		public static InterlinearAnalysisModel ProjectAnalysis(IWfiAnalysis analysis, LcmCache cache)
		{
			if (cache == null)
				throw new ArgumentNullException(nameof(cache));

			var wordform = analysis?.Wordform;
			var wordformText = BestVernacular(wordform?.Form?.BestVernacularAlternative);

			var bundles = new List<InterlinearBundle>();
			if (analysis != null && analysis.IsValidObject)
			{
				foreach (var mb in analysis.MorphBundlesOS)
					bundles.Add(ProjectBundle(mb));
			}

			var line = new InterlinearLine(wordformText, bundles, analysis?.Guid);
			return new InterlinearAnalysisModel(wordformText, new[] { line }, wordform?.Guid);
		}

		/// <summary>
		/// Projects one morph bundle into a <see cref="InterlinearBundle"/>: morph form / lex-gloss /
		/// grammatical-info abbreviation, plus the morph/sense/MSA GUIDs the write-back keys edits on.
		/// </summary>
		private static InterlinearBundle ProjectBundle(IWfiMorphBundle mb)
		{
			// Morph form: the referenced MoForm's vernacular form WITH its morph-type affix markers (legacy
			// kflidMorphemes renders e.g. a prefix as "ka-", a suffix as "-a" via the MoMorphType prefix/
			// postfix). Falls back to the bundle's own stored Form (the legacy "unknown morph" case where
			// MorphRA is null but Form text was reinstated — OneAnalysisSandbox.UpdateRealAnalysisMethod).
			var bareForm = BestVernacular(mb.MorphRA?.Form?.BestVernacularAlternative);
			var morph = ApplyMorphTypeMarkers(bareForm, mb.MorphRA?.MorphTypeRA);
			if (string.IsNullOrEmpty(morph))
				morph = BestVernacular(mb.Form?.BestVernacularAlternative);

			// The lex-entry headword line (legacy kflidLexEntries via LexEntryVc): the headword of the entry
			// the morph belongs to — distinct from the morpheme form on the line above it.
			var lexEntry = (mb.MorphRA?.Owner as ILexEntry)?.HeadWord?.Text ?? string.Empty;

			var sense = mb.SenseRA;
			var gloss = sense?.Gloss?.BestAnalysisAlternative?.Text ?? string.Empty;

			var msa = mb.MsaRA;
			var grammaticalInfo = msa?.InterlinearAbbr ?? string.Empty;

			return new InterlinearBundle(morph, gloss, grammaticalInfo,
				mb.MorphRA?.Guid, sense?.Guid, msa?.Guid, lexEntry);
		}

		private static string BestVernacular(SIL.LCModel.Core.KernelInterfaces.ITsString tss)
			=> tss?.Text ?? string.Empty;

		// Wrap the bare form with its morph-type affix markers (prefix "ka-", suffix "-a", infix "-mu-", …),
		// the legacy kflidMorphemes presentation. A stem's markers are empty, so the bare form passes through.
		private static string ApplyMorphTypeMarkers(string form, IMoMorphType morphType)
		{
			if (string.IsNullOrEmpty(form) || morphType == null)
				return form;
			return (morphType.Prefix ?? string.Empty) + form + (morphType.Postfix ?? string.Empty);
		}
	}
}

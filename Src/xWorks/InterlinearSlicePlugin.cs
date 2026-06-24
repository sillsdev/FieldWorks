// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-interlinear-editor (tasks 2.2 + 3.1) — the native Avalonia interlinear editor: claims the
	/// legacy <c>SIL.FieldWorks.XWorks.MorphologyEditor.InterlinearSlice</c> layout identity through the D1
	/// plugin contract and renders a <c>WfiAnalysis</c>'s morph-bundle interlinear at the slice's real
	/// in-tree position (the Words Analyses detail pane), retiring the §20.1.3 "unsupported" fallback for it.
	/// <para>EDITABILITY (legacy parity): the legacy slice is editable only for HUMAN-APPROVED analyses
	/// (WFIParts.xml: the Normal-layout <c>InterlinearAnal</c> carries <c>deParams editable="true"</c>; the
	/// parser/disapproved <c>InterlinearParse</c> is <c>editable="false"</c>). Approval state IS the layout
	/// selector, so this plugin gates editing on the user agent's approval opinion. When editable, each
	/// bundle's gloss becomes a sense chooser; the write-back + Sandbox-parity MSA prune run through
	/// <see cref="InterlinearAnalysisWriteBack"/> on the region's SHARED fenced UOW. Architecture (design
	/// Decision 1): the FwAvalonia control is LCModel-free — this plugin owns ALL LCModel reads/writes; no
	/// Sandbox, no native Views, no LCModel mutation in the view.</para>
	/// </summary>
	public sealed class InterlinearSlicePlugin : IRegionEditorPlugin
	{
		/// <summary>The legacy slice class this plugin claims (WFIParts.xml InterlinearAnal/InterlinearParse).</summary>
		public const string InterlinearSliceClassName =
			"SIL.FieldWorks.XWorks.MorphologyEditor.InterlinearSlice";

		public string LegacyClassName => InterlinearSliceClassName;

		public Control BuildControl(RegionEditorBuildContext context)
		{
			var analysis = context?.Target as IWfiAnalysis;
			var cache = context?.Cache;
			if (analysis == null || cache == null)
				return null;

			try
			{
				var model = InterlinearAnalysisProjector.ProjectAnalysis(analysis, cache);
				ResolveLineFonts(cache, out var rightToLeft, out var vernacularFont, out var analysisFont);
				var automationId = context.Node?.AutomationId ?? "InterlinearEditor";

				// W-5: editable only for human-approved analyses (legacy deParams editable="true" lives on the
				// Normal/approved layout). The host edit context (resolved lazily) carries the region's fenced
				// session, so an interlinear edit lands as ONE undo step alongside every other row.
				Func<int, int, InterlinearBundleEditChoices> editChoices = null;
				if (IsApproved(analysis, cache) && context.EditContext != null)
				{
					var writeBack = new InterlinearAnalysisWriteBack(cache, context.EditContext, analysis);
					editChoices = (lineIndex, bundleIndex) =>
						BuildBundleChoices(analysis, cache, lineIndex, bundleIndex, writeBack);
				}

				return new InterlinearRegionEditor(model, automationId, rightToLeft, vernacularFont, analysisFont,
					editChoices);
			}
			catch (Exception e)
			{
				// Graceful degradation, same policy as the other plugins: a broken projection degrades to the
				// unsupported row (the view's null-factory guard), never the whole pane.
				Logger.WriteEvent($"InterlinearSlicePlugin: interlinear editor unavailable for analysis '{analysis.Guid}': {e}");
				return null;
			}
		}

		// True when the user agent approves this analysis (the editable Normal-layout case).
		private static bool IsApproved(IWfiAnalysis analysis, LcmCache cache)
		{
			try
			{
				return analysis.GetAgentOpinion(cache.LangProject.DefaultUserAgent) == Opinions.approves;
			}
			catch (Exception)
			{
				return false;
			}
		}

		// The per-bundle choosers for the single projected analysis line (lineIndex 0): the SENSE line
		// (lex-gloss) and the GRAMMATICAL-INFO line (MSA), the two editable interlinear lines the legacy
		// Sandbox offers as combos. Candidates are the senses / MSAs of the morpheme's owning lex entry
		// (the realistic in-bundle re-gloss / re-category); picking a sense derives the bundle's MSA from it,
		// picking an MSA re-points only the MSA — both route to the write-back, which prunes any orphaned MSA.
		// PARITY: the morph line is not editable (re-segmentation is the deferred morpheme-breaker path).
		// Returns null for an out-of-range cell or a bundle with no resolvable entry (that cell stays read-only).
		private static InterlinearBundleEditChoices BuildBundleChoices(IWfiAnalysis analysis, LcmCache cache,
			int lineIndex, int bundleIndex, InterlinearAnalysisWriteBack writeBack)
		{
			if (lineIndex != 0 || bundleIndex < 0 || bundleIndex >= analysis.MorphBundlesOS.Count)
				return null;

			var bundle = analysis.MorphBundlesOS[bundleIndex];
			var entry = bundle.MorphRA?.Owner as ILexEntry;
			if (entry == null)
				return null;

			var senseOptions = new List<RegionChoiceOption>();
			foreach (var sense in entry.AllSenses)
			{
				var gloss = sense.Gloss?.BestAnalysisAlternative?.Text;
				if (string.IsNullOrEmpty(gloss))
					gloss = sense.ShortName;
				senseOptions.Add(new RegionChoiceOption(sense.Guid.ToString(), gloss ?? string.Empty));
			}

			var msaOptions = new List<RegionChoiceOption>();
			foreach (var msa in entry.MorphoSyntaxAnalysesOC)
			{
				var abbr = msa.InterlinearAbbr;
				msaOptions.Add(new RegionChoiceOption(msa.Guid.ToString(),
					string.IsNullOrEmpty(abbr) ? msa.ShortName : abbr));
			}

			// (a) morph/entry re-pointing: the other lex entries/allomorphs that share this morpheme's surface
			// form (legacy combo lookup MorphServices.GetMatchingMorphs). Picking one re-points the bundle to
			// that lexical item; the surface form is unchanged (re-segmentation is the deferred (b) path).
			var morphOptions = BuildMorphOptions(bundle, cache);

			if (senseOptions.Count == 0 && msaOptions.Count == 0 && morphOptions.Count == 0)
				return null;

			var capturedIndex = bundleIndex;
			return new InterlinearBundleEditChoices(
				senseOptions, key => writeBack.ChooseSense(capturedIndex, key),
				msaOptions, key => writeBack.ChooseMsa(capturedIndex, key),
				morphOptions, key => writeBack.ChooseMorph(capturedIndex, key));
		}

		// The candidate morphs/entries for this morpheme's Lex. Entries line: every MoForm in the lexicon
		// whose surface form matches the bundle's morpheme (same prefix/form/postfix), via the legacy lookup
		// MorphServices.GetMatchingMorphs. Each option's key is the MoForm GUID and its display is the owning
		// entry's headword. The bundle's current morph is excluded (no self-option). Empty when there is no
		// chosen morph or no alternative entry shares the form (the line then stays read-only).
		private static List<RegionChoiceOption> BuildMorphOptions(IWfiMorphBundle bundle, LcmCache cache)
		{
			var options = new List<RegionChoiceOption>();
			var moForm = bundle.MorphRA;
			var bareForm = moForm?.Form?.BestVernacularAlternative;
			if (moForm == null || bareForm == null || string.IsNullOrEmpty(bareForm.Text))
				return options;

			var morphType = moForm.MorphTypeRA;
			var prefix = morphType?.Prefix ?? string.Empty;
			var postfix = morphType?.Postfix ?? string.Empty;
			foreach (var candidate in MorphServices.GetMatchingMorphs(cache, prefix, bareForm, postfix))
			{
				var candidateEntry = candidate.Owner as ILexEntry;
				var label = candidateEntry?.HeadWord?.Text;
				options.Add(new RegionChoiceOption(candidate.Guid.ToString(),
					string.IsNullOrEmpty(label) ? candidate.ShortName : label));
			}
			return options;
		}

		// The interlinear lines' writing-system presentation: the vernacular WS drives the wordform/morph
		// lines (RTL + font), the analysis WS the gloss/grammatical-info lines. Kept here in the plugin (not
		// the view) so the FwAvalonia control stays WS/LCModel-free.
		private static void ResolveLineFonts(LcmCache cache, out bool rightToLeft, out string vernacularFont,
			out string analysisFont)
		{
			rightToLeft = false;
			vernacularFont = null;
			analysisFont = null;
			var wsManager = cache.ServiceLocator.WritingSystemManager;

			var vernWs = wsManager.Get(cache.DefaultVernWs);
			if (vernWs != null)
			{
				rightToLeft = vernWs.RightToLeftScript;
				vernacularFont = vernWs.DefaultFontName;
			}

			var analWs = wsManager.Get(cache.DefaultAnalWs);
			if (analWs != null)
				analysisFont = analWs.DefaultFontName;
		}
	}
}

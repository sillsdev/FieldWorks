// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-interlinear-editor (task 3.2) — the WRITE side of design Decision 1/4: applies a per-bundle
	/// edit to the real <c>WfiAnalysis</c> with the Sandbox-parity MSA prune, all inside the region's SHARED
	/// fenced UOW (so an interlinear edit lands as ONE undoable step on the same fence as every other row).
	/// The FwAvalonia view never touches LCModel — it calls <see cref="ChooseSense"/> with a bundle index and
	/// a sense GUID; this helper owns all LCModel reads/writes.
	/// <para>WRITE-BACK FIDELITY (THE risk — design Decision 4): mirrors the legacy
	/// <c>AnalysisInterlinearRs.SaveChanges</c> exactly — collect the MSAs the analysis references BEFORE the
	/// edit (<see cref="WfiAnalysisServices"/>-style <c>CollectReferencedMsas</c>), re-point the bundle, then
	/// delete each previously-referenced MSA that no surviving sense uses (<c>CanDelete</c>). Choosing a sense
	/// derives the bundle's MSA from that sense (<c>SenseRA.MorphoSyntaxAnalysisRA</c>), the same coupling the
	/// legacy combo handlers enforce; clearing the sense (null key) clears both sense and MSA.</para>
	/// </summary>
	internal sealed class InterlinearAnalysisWriteBack
	{
		private readonly LcmCache _cache;
		private readonly IRegionEditContext _host;
		private readonly IWfiAnalysis _analysis;

		public InterlinearAnalysisWriteBack(LcmCache cache, IRegionEditContext host, IWfiAnalysis analysis)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_host = host;
			_analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
		}

		/// <summary>
		/// Stages a sense choice for bundle <paramref name="bundleIndex"/> on the host's fenced session (the
		/// region's shared undo step), naming the undo label "Gloss". <paramref name="senseGuidKey"/> is the
		/// chosen sense's GUID string, or null/empty to clear the bundle's sense. Returns false (and closes a
		/// session this call opened) when nothing applies.
		/// </summary>
		public bool ChooseSense(int bundleIndex, string senseGuidKey)
		{
			if (_host is RegionEditContextBase fenced)
				return fenced.Stage(() => ApplySenseChoice(bundleIndex, senseGuidKey), "Gloss");
			// No fenced host (a test fake / non-fenced context): apply directly. Callers that need the undo
			// fence supply a RegionEditContextBase; the MSA-prune unit test wraps ApplySenseChoice in its own UOW.
			return ApplySenseChoice(bundleIndex, senseGuidKey);
		}

		/// <summary>
		/// Stages a grammatical-info (MSA) choice for bundle <paramref name="bundleIndex"/> on the host's
		/// fenced session, naming the undo label "Category". <paramref name="msaGuidKey"/> is the chosen MSA's
		/// GUID string, or null/empty to clear the bundle's MSA. The legacy interlinear renders grammatical
		/// info as its OWN line with its own combo, so this gesture is independent of the sense — it re-points
		/// only the MSA and leaves <c>SenseRA</c> untouched.
		/// </summary>
		public bool ChooseMsa(int bundleIndex, string msaGuidKey)
		{
			if (_host is RegionEditContextBase fenced)
				return fenced.Stage(() => ApplyMsaChoice(bundleIndex, msaGuidKey), "Category");
			return ApplyMsaChoice(bundleIndex, msaGuidKey);
		}

		/// <summary>
		/// Stages a morph/lex-entry choice for bundle <paramref name="bundleIndex"/> on the host's fenced
		/// session, naming the undo label "Morpheme". <paramref name="morphGuidKey"/> is the chosen MoForm's
		/// GUID string (an allomorph/lexeme-form of a different entry that has the SAME surface form — so the
		/// segmentation stays valid; re-segmentation is the deferred morpheme-breaker path). Picking it
		/// re-points the bundle to that lexical item and resets the sense/MSA to the new entry's default,
		/// mirroring the legacy combo's "choose a different entry" gesture.
		/// </summary>
		public bool ChooseMorph(int bundleIndex, string morphGuidKey)
		{
			if (_host is RegionEditContextBase fenced)
				return fenced.Stage(() => ApplyMorphChoice(bundleIndex, morphGuidKey), "Morpheme");
			return ApplyMorphChoice(bundleIndex, morphGuidKey);
		}

		/// <summary>
		/// Re-points bundle <paramref name="bundleIndex"/>'s morph to the chosen MoForm and resets its sense
		/// to the new entry's default sense (and the MSA from that sense), then prunes any MSA the analysis
		/// previously referenced that no surviving sense uses. MUST run inside a UOW. Returns true on change.
		/// </summary>
		public bool ApplyMorphChoice(int bundleIndex, string morphGuidKey)
		{
			if (!TryGetBundle(bundleIndex, out var bundle))
				return false;
			var newMorph = ResolveMoForm(morphGuidKey);
			if ((bundle.MorphRA?.Hvo ?? 0) == (newMorph?.Hvo ?? 0))
				return false; // nothing changed

			// The chosen entry's default sense + its MSA (the legacy "pick a different entry" cascade).
			var newEntry = newMorph?.Owner as ILexEntry;
			var newSense = newEntry?.SensesOS.FirstOrDefault();

			var candidates = CollectPruneCandidates(bundle);
			bundle.MorphRA = newMorph;
			bundle.SenseRA = newSense;
			bundle.MsaRA = newSense?.MorphoSyntaxAnalysisRA;
			PruneOrphans(candidates);
			return true;
		}

		/// <summary>
		/// Re-points bundle <paramref name="bundleIndex"/> to the chosen sense (deriving its MSA from the
		/// sense) and prunes any MSA the analysis previously referenced that no surviving sense now uses —
		/// the legacy <c>SaveChanges</c> sequence (collect-before → re-point → delete-if-CanDelete). MUST run
		/// inside a UOW (the host's fenced <see cref="RegionEditContextBase.Stage"/> opens one; the unit test
		/// supplies its own). Returns true when the analysis changed.
		/// </summary>
		public bool ApplySenseChoice(int bundleIndex, string senseGuidKey)
		{
			if (!TryGetBundle(bundleIndex, out var bundle))
				return false;
			var newSense = ResolveSense(senseGuidKey);
			var newMsa = newSense?.MorphoSyntaxAnalysisRA;
			if ((bundle.SenseRA?.Hvo ?? 0) == (newSense?.Hvo ?? 0)
				&& (bundle.MsaRA?.Hvo ?? 0) == (newMsa?.Hvo ?? 0))
				return false; // nothing changed

			var candidates = CollectPruneCandidates(bundle);
			bundle.SenseRA = newSense;
			bundle.MsaRA = newMsa;
			PruneOrphans(candidates);
			return true;
		}

		/// <summary>
		/// Re-points ONLY bundle <paramref name="bundleIndex"/>'s MSA (leaving its sense), then prunes any MSA
		/// the analysis previously referenced that no surviving sense uses — same Sandbox-parity prune as the
		/// sense path. MUST run inside a UOW. Returns true when the analysis changed.
		/// </summary>
		public bool ApplyMsaChoice(int bundleIndex, string msaGuidKey)
		{
			if (!TryGetBundle(bundleIndex, out var bundle))
				return false;
			var newMsa = ResolveMsa(msaGuidKey);
			if ((bundle.MsaRA?.Hvo ?? 0) == (newMsa?.Hvo ?? 0))
				return false; // nothing changed

			var candidates = CollectPruneCandidates(bundle);
			bundle.MsaRA = newMsa;
			PruneOrphans(candidates);
			return true;
		}

		private bool TryGetBundle(int bundleIndex, out IWfiMorphBundle bundle)
		{
			bundle = null;
			if (!_analysis.IsValidObject || bundleIndex < 0 || bundleIndex >= _analysis.MorphBundlesOS.Count)
				return false;
			bundle = _analysis.MorphBundlesOS[bundleIndex];
			return true;
		}

		// Collect the analysis's currently-referenced MSAs BEFORE re-pointing, so we can prune any that no
		// surviving sense uses afterward (legacy AnalysisInterlinearRs.SaveChanges parity). The bundle's own
		// current MSA is the one this edit most directly risks orphaning, so include it explicitly — the
		// candidate set is a superset of the legacy collect, never a subset.
		private HashSet<IMoMorphSynAnalysis> CollectPruneCandidates(IWfiMorphBundle bundle)
		{
			var candidates = new HashSet<IMoMorphSynAnalysis>();
			_analysis.CollectReferencedMsas(candidates);
			if (bundle.MsaRA != null)
				candidates.Add(bundle.MsaRA);
			return candidates;
		}

		// Delete each candidate MSA that no surviving sense/analysis now references (CanDelete) — the legacy
		// "delete MSAs no surviving sense uses" step, run AFTER the bundle re-points.
		private void PruneOrphans(HashSet<IMoMorphSynAnalysis> candidates)
		{
			foreach (var msa in candidates)
			{
				if (msa != null && msa.IsValidObject && msa.CanDelete)
					_cache.MainCacheAccessor.DeleteObj(msa.Hvo);
			}
		}

		// Resolve a sense GUID string to its LexSense; null/blank or an unknown/invalid GUID clears the
		// bundle's sense (returns null) rather than throwing — defensive against a stale option key.
		private ILexSense ResolveSense(string senseGuidKey)
		{
			if (string.IsNullOrEmpty(senseGuidKey) || !Guid.TryParse(senseGuidKey, out var guid))
				return null;
			return _cache.ServiceLocator.ObjectRepository.TryGetObject(guid, out var obj)
				? obj as ILexSense
				: null;
		}

		// Resolve an MSA GUID string to its MoMorphSynAnalysis; null/blank or an unknown/invalid GUID clears
		// the bundle's MSA (returns null) rather than throwing — defensive against a stale option key.
		private IMoMorphSynAnalysis ResolveMsa(string msaGuidKey)
		{
			if (string.IsNullOrEmpty(msaGuidKey) || !Guid.TryParse(msaGuidKey, out var guid))
				return null;
			return _cache.ServiceLocator.ObjectRepository.TryGetObject(guid, out var obj)
				? obj as IMoMorphSynAnalysis
				: null;
		}

		// Resolve a MoForm (allomorph / lexeme form) GUID string; null/blank or unknown/invalid clears the
		// bundle's morph (returns null) rather than throwing — defensive against a stale option key.
		private IMoForm ResolveMoForm(string morphGuidKey)
		{
			if (string.IsNullOrEmpty(morphGuidKey) || !Guid.TryParse(morphGuidKey, out var guid))
				return null;
			return _cache.ServiceLocator.ObjectRepository.TryGetObject(guid, out var obj)
				? obj as IMoForm
				: null;
		}
	}
}

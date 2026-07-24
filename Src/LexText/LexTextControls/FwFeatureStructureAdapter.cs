// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using FwAvaloniaDialogs;
using SIL.LCModel;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-AWARE adapter that round-trips a morpheme MSA's inflection-feature structure
	/// (<c>IFsFeatStruc</c>) to/from the LCModel-free <see cref="FwFeatureStructureEditor"/> seam (Phase-1 §19b
	/// Stage 2). It is the feature-editor analogue of the launchers' POS-node / slot / inflection-class feeds: the
	/// Avalonia layer never sees an <c>ICmObject</c>; this adapter builds the depth-tagged <see cref="FwFeatureNode"/>
	/// system from a part of speech's <c>InflectableFeatsRC</c>, reads the current assignment set from an existing
	/// <c>IFsFeatStruc</c>, and — on commit — REBUILDS the nested <c>IFsFeatStruc</c> from the editor's flat
	/// <c>(closedFeatureId, valueId)</c> assignment set.
	///
	/// Every behaviour mirrors the WinForms truth verbatim:
	///   * <see cref="BuildNodes"/> mirrors <c>MsaInflectionFeatureListDlg.PopulateTreeFromPos</c> +
	///     <c>FeatureStructureTreeView.AddNode(IFsFeatDefn)</c>: a closed feature emits a Closed node followed by its
	///     <c>ValuesSorted</c> as Value children; a complex feature emits a Complex node followed by its
	///     <c>TypeRA.FeaturesRS</c> recursively. The POS chain (the POS and its owning parent POSes) is walked exactly
	///     as <c>PopulateTreeFromPos</c> does. An <c>AlreadyInTree</c>-style guard (by guid, along the current ancestor
	///     path) prevents the infinite loop a self-referential complex type would cause.
	///   * <see cref="ReadAssignments"/> mirrors <c>FeatureStructureTreeView.PopulateTreeFromFeatureStructure</c>: walk
	///     the FS's closed values to <c>(closedFeatureGuid, valueGuid)</c>, recursing into complex values.
	///   * <see cref="WriteFeatures"/> mirrors <c>MsaInflectionFeatureListDlg_Closing</c> +
	///     <c>BuildFeatureStructure</c>: clear the FS's <c>FeatureSpecsOC</c>, then for each assignment walk the
	///     recursive-ascent path (the complex ancestors of the closed feature, derived from the SAME node list the
	///     editor used) calling <c>GetOrCreateValue</c> on each complex feature (creating the nested
	///     <c>IFsFeatStruc</c>) and the closed feature, then set the closed value's <c>ValueRA</c> — setting
	///     <c>TypeRA</c> at each level exactly as the WinForms code does. The caller runs this inside its own UOW.
	///
	/// All methods are static + LCModel-aware so the launchers (Insert Entry / Add New Sense / MSA Creator) share one
	/// implementation and it is unit-testable against a real cache.
	/// </summary>
	internal static class FwFeatureStructureAdapter
	{
		// ----- node feed (POS InflectableFeatsRC -> depth-tagged FwFeatureNode list) -----

		/// <summary>
		/// Builds the inflection-feature SYSTEM for an MSA's part of speech (and its owning parent POSes, the legacy
		/// <c>PopulateTreeFromPos</c> walk) as a flat, document-order, depth-tagged <see cref="FwFeatureNode"/> list:
		/// a closed feature → a Closed node + its <c>ValuesSorted</c> Value children; a complex feature → a Complex
		/// node + its <c>TypeRA.FeaturesRS</c> recursively. Each node's id is the defn's/value's guid string
		/// (round-tripped on commit). A null POS yields an empty list (the editor shows nothing).
		/// </summary>
		public static IReadOnlyList<FwFeatureNode> BuildNodes(IPartOfSpeech pos)
		{
			var nodes = new List<FwFeatureNode>();
			if (pos == null)
				return nodes;

			// Walk the POS and its owning parent POSes (the legacy PopulateTreeFromPos loop), de-duplicating defns
			// already emitted at the top level (AlreadyInTree at parentNode == null).
			var topLevelSeen = new HashSet<Guid>();
			IPartOfSpeech cur = pos;
			while (cur != null)
			{
				foreach (var defn in cur.InflectableFeatsRC)
					AddDefnNode(defn, depth: 0, nodes, ancestors: new List<Guid>(), topLevelSeen);
				cur = cur.Owner as IPartOfSpeech;
			}
			return nodes;
		}

		// Mirrors FeatureStructureTreeView.AddNode(IFsFeatDefn): emit the defn node then its children. `ancestors`
		// is the guid path from the root to (not including) this node — the AlreadyInTree cycle guard.
		private static void AddDefnNode(IFsFeatDefn defn, int depth, List<FwFeatureNode> nodes, List<Guid> ancestors,
			HashSet<Guid> topLevelSeen)
		{
			if (defn == null)
				return;

			if (defn is IFsClosedFeature closed)
			{
				// Avoid the legacy duplicate at the top level (a closed feature shared by two POSes in the chain).
				if (depth == 0 && !topLevelSeen.Add(closed.Guid))
					return;
				if (ancestors.Contains(closed.Guid))
					return; // cycle guard
				nodes.Add(new FwFeatureNode(closed.Guid.ToString(), Name(closed), FwFeatureNodeKind.Closed, depth));
				foreach (var val in closed.ValuesSorted)
				{
					nodes.Add(new FwFeatureNode(val.Guid.ToString(), ValueName(val),
						FwFeatureNodeKind.Value, depth + 1));
				}
				return;
			}

			if (defn is IFsComplexFeature complex)
			{
				if (depth == 0 && !topLevelSeen.Add(complex.Guid))
					return;
				if (ancestors.Contains(complex.Guid))
					return; // AlreadyInTree: avoid the infinite loop a self-referential complex type would cause
				nodes.Add(new FwFeatureNode(complex.Guid.ToString(), Name(complex), FwFeatureNodeKind.Complex, depth));
				var type = complex.TypeRA;
				if (type == null)
					return;
				var nextAncestors = new List<Guid>(ancestors) { complex.Guid };
				foreach (var sub in type.FeaturesRS)
					AddDefnNode(sub, depth + 1, nodes, nextAncestors, topLevelSeen);
			}
		}

		/// <summary>
		/// Builds the PHONOLOGICAL feature system as a flat, document-order, depth-tagged <see cref="FwFeatureNode"/>
		/// list (Phase-1 §19b Stage 3) — the degenerate flat case the WinForms <c>PhonologicalFeatureChooserDlg</c>
		/// shows: every <c>IFsClosedFeature</c> in <c>PhFeatureSystemOA.FeaturesOC</c> (sorted by name, as the legacy
		/// dialog sorts) with its <c>ValuesSorted</c> symbolic values as Value children. No complex features / nesting
		/// (the phonological system is all closed features). A null cache yields an empty list.
		/// </summary>
		public static IReadOnlyList<FwFeatureNode> BuildPhonologicalNodes(LcmCache cache)
		{
			var nodes = new List<FwFeatureNode>();
			if (cache == null)
				return nodes;
			var featSys = cache.LanguageProject.PhFeatureSystemOA;
			if (featSys == null)
				return nodes;
			foreach (var closed in featSys.FeaturesOC.OfType<IFsClosedFeature>()
				.OrderBy(f => Name(f), StringComparer.CurrentCulture))
			{
				nodes.Add(new FwFeatureNode(closed.Guid.ToString(), Name(closed), FwFeatureNodeKind.Closed, 0));
				foreach (var val in closed.ValuesSorted)
					nodes.Add(new FwFeatureNode(val.Guid.ToString(), ValueName(val), FwFeatureNodeKind.Value, 1));
			}
			return nodes;
		}

		// ----- read (existing IFsFeatStruc -> flat assignment set) -----

		/// <summary>
		/// Reads the flat <c>(closedFeatureId, valueId)</c> assignment set from an existing inflection-feature
		/// structure (the legacy <c>PopulateTreeFromFeatureStructure</c> seeding): each closed value contributes one
		/// assignment; complex values are recursed (their nested FS's closed values contribute their own). A null FS
		/// (the create path / no features yet) yields an empty set. The nesting is IMPLICIT in the feature system, so
		/// the assignment is keyed only by the closed feature's guid (the same key the editor and <see cref="WriteFeatures"/>
		/// use); a closed feature that appears under two complex parents is an unusual case the WinForms tree also
		/// flattens by feature, so the last write wins (matching the editor's per-closed-feature radio group).
		/// </summary>
		public static IReadOnlyList<FwFeatureValueAssignment> ReadAssignments(IFsFeatStruc fs)
		{
			var result = new List<FwFeatureValueAssignment>();
			if (fs != null)
				CollectAssignments(fs, result);
			return result;
		}

		private static void CollectAssignments(IFsFeatStruc fs, List<FwFeatureValueAssignment> into)
		{
			foreach (var spec in fs.FeatureSpecsOC)
			{
				switch (spec)
				{
					case IFsClosedValue closed when closed.FeatureRA != null && closed.ValueRA != null:
						into.Add(new FwFeatureValueAssignment(
							closed.FeatureRA.Guid.ToString(), closed.ValueRA.Guid.ToString()));
						break;
					case IFsComplexValue complex when complex.ValueOA is IFsFeatStruc nested:
						CollectAssignments(nested, into);
						break;
				}
			}
		}

		// ----- write (flat assignment set -> rebuilt nested IFsFeatStruc) -----

		/// <summary>
		/// Rebuilds <paramref name="fs"/>'s feature specs from the editor's flat assignment set (the legacy
		/// <c>MsaInflectionFeatureListDlg_Closing</c> + <c>BuildFeatureStructure</c> recursive-ascent), using
		/// <paramref name="nodes"/> — the SAME depth-tagged node list the editor was fed — to recover each closed
		/// feature's complex-feature ancestry. Clears the existing specs first (the legacy "clean out any extant
		/// features" loop), then for each assignment walks the ancestor chain calling <c>GetOrCreateValue</c> on each
		/// complex feature (descending into its nested FS) and the closed feature, finally setting the closed value's
		/// <c>ValueRA</c> + the <c>TypeRA</c> at each level. Must run inside the caller's UOW. No-op when the FS is
		/// null. Returns true when at least one spec was written (the caller may delete an emptied FS, LT-13596).
		/// </summary>
		public static bool WriteFeatures(LcmCache cache, IFsFeatStruc fs,
			IReadOnlyList<FwFeatureNode> nodes, IReadOnlyList<FwFeatureValueAssignment> assignments)
		{
			if (cache == null || fs == null)
				return false;

			// Clear extant specs (the legacy loop). Snapshot first since we mutate the collection.
			foreach (var spec in fs.FeatureSpecsOC.ToList())
				fs.FeatureSpecsOC.Remove(spec);

			if (assignments == null || assignments.Count == 0)
				return false;

			// Recover the complex-feature ancestry of every closed feature node by depth-folding the node list (the
			// same fold the editor uses): a node attaches under the nearest shallower node, so a closed feature's
			// ancestors are the Complex nodes shallower than it on the running stack.
			var ancestryByClosedId = BuildClosedFeatureAncestry(nodes);

			var wroteAny = false;
			var closedRepo = cache.ServiceLocator.GetInstance<IFsClosedFeatureRepository>();
			var complexRepo = cache.ServiceLocator.GetInstance<IFsComplexFeatureRepository>();
			var symValRepo = cache.ServiceLocator.GetInstance<IFsSymFeatValRepository>();

			foreach (var assignment in assignments)
			{
				if (assignment?.ClosedFeatureId == null || assignment.ValueId == null)
					continue;
				if (!Guid.TryParse(assignment.ClosedFeatureId, out var closedGuid)
					|| !Guid.TryParse(assignment.ValueId, out var valueGuid))
					continue;

				IFsClosedFeature closedFeat;
				IFsSymFeatVal symVal;
				try
				{
					closedFeat = closedRepo.GetObject(closedGuid);
					symVal = symValRepo.GetObject(valueGuid);
				}
				catch
				{
					continue; // an unresolvable id is simply dropped (the <Any>/stale case)
				}

				// Descend through the complex ancestors (outermost first), find-or-creating each nested FS — the
				// recursive-ascent of BuildFeatureStructure, but driven from the node list rather than a tree node.
				var currentFs = fs;
				if (ancestryByClosedId.TryGetValue(assignment.ClosedFeatureId, out var ancestors))
				{
					foreach (var complexId in ancestors)
					{
						if (!Guid.TryParse(complexId, out var complexGuid))
							continue;
						IFsComplexFeature complexFeat;
						try { complexFeat = complexRepo.GetObject(complexGuid); }
						catch { complexFeat = null; }
						if (complexFeat == null)
							continue;

						var complexValue = currentFs.GetOrCreateValue(complexFeat);
						complexValue.FeatureRA = complexFeat;
						if (currentFs.TypeRA == null)
						{
							currentFs.TypeRA = cache.LanguageProject.MsFeatureSystemOA.TypesOC
								.FirstOrDefault(t => t.FeaturesRS.Contains(complexFeat));
						}
						var nestedFs = (IFsFeatStruc)((IFsComplexValue)complexValue).ValueOA;
						if (nestedFs.TypeRA == null && complexFeat.TypeRA != null)
							nestedFs.TypeRA = complexFeat.TypeRA;
						currentFs = nestedFs;
					}
				}

				// The closed feature + its symbolic value (BuildFeatureStructure's Closed + SymFeatValue cases).
				var closedValue = (IFsClosedValue)currentFs.GetOrCreateValue(closedFeat);
				closedValue.FeatureRA = closedFeat;
				if (currentFs.TypeRA == null)
				{
					currentFs.TypeRA = cache.LanguageProject.MsFeatureSystemOA.TypesOC
						.FirstOrDefault(t => t.FeaturesRS.Contains(closedFeat));
				}
				closedValue.ValueRA = symVal;
				wroteAny = true;
			}
			return wroteAny;
		}

		// Depth-fold the node list to a closed-feature-id -> ordered (outermost-first) complex-ancestor-id list. A
		// node at depth d attaches under the nearest shallower node; the complex nodes on the running ancestor stack
		// shallower than a closed node are its ancestors (the editor's fold + BuildFeatureStructure's parent chain).
		private static Dictionary<string, IReadOnlyList<string>> BuildClosedFeatureAncestry(
			IReadOnlyList<FwFeatureNode> nodes)
		{
			var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			if (nodes == null)
				return result;
			// stack[d] = the id of the most recent COMPLEX node at depth d (closed/value nodes are not ancestors).
			var complexByDepth = new List<string>();
			foreach (var node in nodes)
			{
				if (node == null)
					continue;
				var depth = node.Depth < 0 ? 0 : node.Depth;
				// Trim the stack to this depth (deeper entries belong to a finished branch).
				while (complexByDepth.Count > depth)
					complexByDepth.RemoveAt(complexByDepth.Count - 1);

				if (node.Kind == FwFeatureNodeKind.Closed)
				{
					// Its ancestors are the complex ids currently on the stack (depths 0..depth-1), in order.
					result[node.Id] = complexByDepth.Where(id => id != null).ToList();
				}
				else if (node.Kind == FwFeatureNodeKind.Complex)
				{
					while (complexByDepth.Count <= depth)
						complexByDepth.Add(null);
					complexByDepth[depth] = node.Id;
				}
			}
			return result;
		}

		/// <summary>
		/// Applies the chosen assignment set to a standalone feature structure that lives on an owner object+flid (the
		/// surface the WinForms <c>MsaInflectionFeatureListDlg</c> / <c>PhonologicalFeatureChooserDlg</c> edit): the
		/// existing <paramref name="fs"/> may be null (none yet) in which case one is created on
		/// <paramref name="owner"/>.<paramref name="owningFlid"/> when there are assignments. Rebuilds the FS specs from
		/// the assignment set via <see cref="WriteFeatures"/>; when <paramref name="deleteWhenEmpty"/> is true and the
		/// result is empty, deletes the FS (the inflection LT-13596 path) — the phonological dialog passes false (it
		/// leaves an empty FS as-is). Returns the resulting FS (null when deleted/none). Runs inside the caller's UOW.
		/// </summary>
		public static IFsFeatStruc ApplyFeaturesToOwner(LcmCache cache, IFsFeatStruc fs, ICmObject owner, int owningFlid,
			IReadOnlyList<FwFeatureNode> nodes, IReadOnlyList<FwFeatureValueAssignment> assignments,
			bool deleteWhenEmpty)
		{
			if (cache == null)
				return fs;
			var hasAssignments = assignments != null && assignments.Count > 0;

			if (!hasAssignments)
			{
				if (fs != null && deleteWhenEmpty && fs.CanDelete)
				{
					fs.Delete();
					return null;
				}
				if (fs != null)
				{
					// Clear extant specs but keep the (now empty) FS (the phonological "leave it" path).
					foreach (var spec in fs.FeatureSpecsOC.ToList())
						fs.FeatureSpecsOC.Remove(spec);
				}
				return fs;
			}

			if (fs == null && owner != null && owningFlid != 0)
			{
				var where = cache.MetaDataCacheAccessor.GetFieldType(owningFlid)
					== (int)SIL.LCModel.Core.Cellar.CellarPropertyType.OwningAtomic ? -2 : -1;
				var hvoNew = cache.DomainDataByFlid.MakeNewObject(FsFeatStrucTags.kClassId, owner.Hvo, owningFlid, where);
				fs = cache.ServiceLocator.GetInstance<IFsFeatStrucRepository>().GetObject(hvoNew);
			}
			if (fs == null)
				return null;

			var wrote = WriteFeatures(cache, fs, nodes, assignments);
			if (!wrote && deleteWhenEmpty && fs.CanDelete)
			{
				fs.Delete();
				return null;
			}
			return fs;
		}

		// ----- MSA owning-FS access (create / read / clear the inflection-feature structure) -----

		/// <summary>
		/// Reads the existing inflection-feature structure off a morpheme MSA (Phase-1 §19b Stage 2 scope): an
		/// <c>IMoInflAffMsa</c>'s <c>InflFeatsOA</c>, or an <c>IMoDerivAffMsa</c>'s <c>FromMsFeaturesOA</c> (the surface
		/// the legacy <c>PopulateTreeFromPosInEntry</c> edits). Null for any other MSA flavour or when none exists.
		/// </summary>
		public static IFsFeatStruc GetInflectionFeatures(IMoMorphSynAnalysis msa)
		{
			switch (msa)
			{
				case IMoInflAffMsa infl:
					return infl.InflFeatsOA;
				case IMoDerivAffMsa deriv:
					return deriv.FromMsFeaturesOA;
				default:
					return null;
			}
		}

		/// <summary>
		/// Resolves the part of speech whose inflectable features apply to a (potentially) owning object + the flid in
		/// which it owns the feature structure — the full lift of
		/// <c>MsaInflectionFeatureListDlg.GetPosFromCmObjectAndFlid</c> (the standalone inflection-feature slice path):
		/// an MSA's POS (infl/deriv-from/to/stem), a stem-name's owning POS, or an affix-allomorph's entry's first
		/// sense's MSA POS. Null when none resolves. Used by the standalone <see cref="LcmInflectionFeatureChooserLauncher"/>.
		/// </summary>
		public static IPartOfSpeech GetInflectionFeaturePos(ICmObject cobj, int owningFlid)
		{
			if (cobj == null)
				return null;
			switch (cobj)
			{
				case IMoInflAffMsa infl:
					return infl.PartOfSpeechRA;
				case IMoDerivAffMsa deriv:
					if (owningFlid == MoDerivAffMsaTags.kflidFromMsFeatures)
						return deriv.FromPartOfSpeechRA;
					if (owningFlid == MoDerivAffMsaTags.kflidToMsFeatures)
						return deriv.ToPartOfSpeechRA;
					return deriv.FromPartOfSpeechRA;
				case IMoStemMsa stem:
					return stem.PartOfSpeechRA;
				case IMoStemName sn:
					return sn.Owner as IPartOfSpeech;
				case IMoAffixAllomorph _:
					var entry = cobj.Owner as ILexEntry;
					if (entry == null || entry.SensesOS.Count == 0)
						return null;
					var sense = entry.SensesOS[0];
					return sense?.MorphoSyntaxAnalysisRA != null
						? GetInflectionFeaturePos(sense.MorphoSyntaxAnalysisRA, MoDerivAffMsaTags.kflidFromMsFeatures)
						: null;
				default:
					return null;
			}
		}

		/// <summary>
		/// Returns the part of speech whose inflectable features apply to a morpheme MSA — the lift of
		/// <c>MsaInflectionFeatureListDlg.GetPosFromCmObjectAndFlid</c> for the infl/deriv-FROM surface: an
		/// <c>IMoInflAffMsa</c>'s <c>PartOfSpeechRA</c>, or an <c>IMoDerivAffMsa</c>'s <c>FromPartOfSpeechRA</c>.
		/// </summary>
		public static IPartOfSpeech GetInflectionFeaturePos(IMoMorphSynAnalysis msa)
		{
			switch (msa)
			{
				case IMoInflAffMsa infl:
					return infl.PartOfSpeechRA;
				case IMoDerivAffMsa deriv:
					return deriv.FromPartOfSpeechRA;
				default:
					return null;
			}
		}

		/// <summary>
		/// Applies the chosen inflection-feature assignment set to a morpheme MSA, rebuilding (or deleting) the
		/// inflection <c>IFsFeatStruc</c> — the create-side parity of <c>MsaInflectionFeatureListDlg_Closing</c>'s OK
		/// branch. Creates the owning FS when there are assignments and none exists; rebuilds it from the assignment
		/// set via <see cref="WriteFeatures"/>; deletes it (clears the owning property) when the set is empty
		/// (LT-13596). Scoped to <c>IMoInflAffMsa.InflFeatsOA</c> / <c>IMoDerivAffMsa.FromMsFeaturesOA</c>; any other
		/// MSA flavour is a no-op. The <paramref name="nodes"/> are the feature system the editor used (needed to
		/// recover complex ancestry). Runs inside the caller's UOW. Static so both create paths share it and it is
		/// unit-testable.
		/// </summary>
		public static void ApplyInflectionFeatures(LcmCache cache, IMoMorphSynAnalysis msa,
			IReadOnlyList<FwFeatureNode> nodes, IReadOnlyList<FwFeatureValueAssignment> assignments)
		{
			if (cache == null || msa == null)
				return;
			var hasAssignments = assignments != null && assignments.Count > 0;

			IFsFeatStruc fs = GetInflectionFeatures(msa);

			if (!hasAssignments)
			{
				// No features chosen: delete any existing FS (the legacy LT-13596 empty-FS delete).
				if (fs != null)
					ClearInflectionFeatures(msa);
				return;
			}

			if (fs == null)
				fs = CreateInflectionFeatures(cache, msa);
			if (fs == null)
				return; // not an infl/deriv MSA

			var wrote = WriteFeatures(cache, fs, nodes, assignments);
			// If, after resolving, nothing was actually written (every id stale), drop the empty FS (LT-13596).
			if (!wrote)
				ClearInflectionFeatures(msa);
		}

		// Creates the owning inflection-feature FsFeatStruc on the MSA (InflFeatsOA / FromMsFeaturesOA).
		private static IFsFeatStruc CreateInflectionFeatures(LcmCache cache, IMoMorphSynAnalysis msa)
		{
			var factory = cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>();
			switch (msa)
			{
				case IMoInflAffMsa infl:
					infl.InflFeatsOA = factory.Create();
					return infl.InflFeatsOA;
				case IMoDerivAffMsa deriv:
					deriv.FromMsFeaturesOA = factory.Create();
					return deriv.FromMsFeaturesOA;
				default:
					return null;
			}
		}

		// Clears (deletes) the owning inflection-feature FsFeatStruc on the MSA.
		private static void ClearInflectionFeatures(IMoMorphSynAnalysis msa)
		{
			switch (msa)
			{
				case IMoInflAffMsa infl:
					infl.InflFeatsOA = null;
					break;
				case IMoDerivAffMsa deriv:
					deriv.FromMsFeaturesOA = null;
					break;
			}
		}

		// ----- small helpers -----

		private static string Name(IFsFeatDefn defn)
			=> defn.Name?.BestAnalysisAlternative?.Text ?? defn.ShortName ?? defn.Guid.ToString();

		private static string ValueName(IFsSymFeatVal val)
			=> val.Name?.BestAnalysisAlternative?.Text ?? val.ShortName ?? val.Guid.ToString();
	}
}

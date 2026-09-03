// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Projects a language project's grammatical info -- parts of speech, inflectional affix
	/// slots, inflection classes, and the inflectable-feature system -- into the LCModel-free
	/// shapes the shared Avalonia MSA section consumes (<see cref="FwPosNode"/>, <see
	/// cref="FwInflectionSlot"/>, <see cref="FwInflectionClass"/>, <see cref="FwFeatureNode"/>,
	/// <see cref="FwMsaType"/>), and applies a chosen <see cref="FwSandboxMsa"/> back to the
	/// model inside the caller's unit of work.
	///
	/// Projection and apply together are the lift of the WinForms <c>MSAGroupBox</c> (its
	/// <c>MorphTypePreference</c> switch, <c>GetSlots</c>/<c>ResetSlotCombo</c>, and
	/// <c>SandboxMSA</c>) plus <c>MsaInflectionFeatureListDlg</c>. This is the one place that
	/// projection and that apply happen, so two MSA sections over the same project offer the same
	/// choices and commit them the same way.
	///
	/// The module owns no dialog and opens no modal, so needing the projection never means
	/// depending on one. Rules that need no cache live in <see cref="MsaTypeRules"/> and are
	/// called from here, so "which fields does this MsaType carry?" has exactly one answer.
	/// </summary>
	internal static class GrammaticalInfoProjection
	{
		/// <summary>
		/// Projects the language project's parts-of-speech hierarchy as a flat, document-order,
		/// depth-tagged <see cref="FwPosNode"/> list from
		/// <c>cache.LangProject.PartsOfSpeechOA</c>. Each id is the POS guid string, which round-
		/// trips back through the repository on commit; the display name uses the best-analysis
		/// fallback and the abbreviation rides along for the row.
		/// </summary>
		internal static IReadOnlyList<FwPosNode> BuildPosNodes(LcmCache cache)
		{
			var nodes = new List<FwPosNode>();
			void Add(IPartOfSpeech pos, int depth)
			{
				nodes.Add(new FwPosNode(pos.Guid.ToString(), PosName(pos), depth,
					pos.Abbreviation?.BestAnalysisAlternative?.Text));
				foreach (var sub in pos.SubPossibilitiesOS.OfType<IPartOfSpeech>())
					Add(sub, depth + 1);
			}

			foreach (var pos in cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.OfType<IPartOfSpeech>())
				Add(pos, 0);
			return nodes;
		}

		private static string PosName(IPartOfSpeech pos)
			=> pos.Name.BestAnalysisAlternative?.Text ?? pos.ShortName ?? pos.Guid.ToString();

		/// <summary>
		/// Maps every morph-type guid string in the project to the <see cref="FwMsaType"/> its
		/// MSA section opens in, so the shared dialog can drive the MSA box's layout with no
		/// LCModel. It mirrors the WinForms <c>MSAGroupBox.MorphTypePreference</c> switch:
		/// stem/bound-stem/phrase to Stem; the clitic and root family to Root; the affix family
		/// to Unclassified, which the user then refines through the affix-type combo.
		/// </summary>
		internal static IReadOnlyDictionary<string, FwMsaType> BuildMorphTypeToMsaTypeMap(LcmCache cache)
		{
			var map = new Dictionary<string, FwMsaType>(StringComparer.Ordinal);
			foreach (var type in cache.LanguageProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities
				.OfType<IMoMorphType>())
			{
				map[type.Guid.ToString()] = MorphTypeGuidToMsaType(type.Guid.ToString());
			}
			return map;
		}

		/// <summary>
		/// The morph-type-guid to <see cref="FwMsaType"/> rule on its own (the lift of
		/// <c>MSAGroupBox.MorphTypePreference</c>'s switch), for seeding an MSA box's initial
		/// class from an entry's morph type.
		/// </summary>
		internal static FwMsaType MorphTypeGuidToMsaType(string morphTypeGuid)
		{
			switch (morphTypeGuid)
			{
				case MoMorphTypeTags.kMorphStem:
				case MoMorphTypeTags.kMorphBoundStem:
				case MoMorphTypeTags.kMorphPhrase:
				case MoMorphTypeTags.kMorphDiscontiguousPhrase:
					return FwMsaType.Stem;
				case MoMorphTypeTags.kMorphProclitic:
				case MoMorphTypeTags.kMorphClitic:
				case MoMorphTypeTags.kMorphEnclitic:
				case MoMorphTypeTags.kMorphParticle:
				case MoMorphTypeTags.kMorphRoot:
				case MoMorphTypeTags.kMorphBoundRoot:
					return FwMsaType.Root;
				default:
					// The affix family (prefix/suffix/infix/...): the box opens Unclassified,
					// then the user refines.
					return FwMsaType.Unclassified;
			}
		}

		/// <summary>
		/// Projects the inflectional-affix slot options for a main POS (guid string), filtered by
		/// the morph type's prefixal/suffixal nature -- the lift of
		/// <c>MSAGroupBox.GetSlots</c>/<c>ResetSlotCombo</c>. A prefixal-and-suffixal (or
		/// unknown) morph type yields every affix slot; otherwise the matching subset. Each
		/// slot's id is its guid string, which round-trips back on commit.
		/// </summary>
		internal static IReadOnlyList<FwInflectionSlot> BuildSlots(LcmCache cache, string posId, string morphTypeGuid)
		{
			if (string.IsNullOrEmpty(posId) || !Guid.TryParse(posId, out var posGuid))
				return Array.Empty<FwInflectionSlot>();
			IPartOfSpeech pos;
			try
			{
				pos = cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(posGuid);
			}
			catch
			{
				return Array.Empty<FwInflectionSlot>();
			}

			IEnumerable<IMoInflAffixSlot> slots;
			IMoMorphType morphType = null;
			if (!string.IsNullOrEmpty(morphTypeGuid) && Guid.TryParse(morphTypeGuid, out var mtGuid))
			{
				try { morphType = cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(mtGuid); }
				catch { morphType = null; }
			}

			if (morphType == null)
			{
				slots = pos.AllAffixSlots;
			}
			else
			{
				var isPrefixal = MorphServices.IsPrefixishType(cache, morphType.Hvo);
				var isSuffixal = MorphServices.IsSuffixishType(cache, morphType.Hvo);
				slots = (isPrefixal && isSuffixal)
					? pos.AllAffixSlots
					: DomainObjectServices.GetSomeSlots(cache, pos.AllAffixSlots, isPrefixal);
			}

			return slots
				.Select(s => new FwInflectionSlot(s.Guid.ToString(), s.Name.BestAnalysisAlternative?.Text ?? s.ShortName))
				.Where(s => !string.IsNullOrEmpty(s.Name))
				.ToList();
		}

		/// <summary>
		/// Projects the inflection-class options for a main POS (guid string) -- the lift of the
		/// WinForms <c>InflectionClassPopupTreeManager</c> tree, scoped to the SINGLE selected
		/// POS. Walks <c>IPartOfSpeech.InflectionClassesOC</c> and the nested
		/// <c>IMoInflClass.SubclassesOC</c> in document order, tagging each with its nesting
		/// depth so the picker can indent subclasses. Each class's id is its guid string, which
		/// round-trips back on commit. An empty or unknown POS yields no classes, and the box
		/// still shows its "&lt;None&gt;" row.
		/// </summary>
		internal static IReadOnlyList<FwInflectionClass> BuildInflectionClasses(LcmCache cache, string posId)
		{
			if (string.IsNullOrEmpty(posId) || !Guid.TryParse(posId, out var posGuid))
				return Array.Empty<FwInflectionClass>();
			IPartOfSpeech pos;
			try
			{
				pos = cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(posGuid);
			}
			catch
			{
				return Array.Empty<FwInflectionClass>();
			}

			var result = new List<FwInflectionClass>();
			void Add(IMoInflClass cls, int depth)
			{
				var name = cls.Name.BestAnalysisAlternative?.Text ?? cls.ShortName;
				if (!string.IsNullOrEmpty(name))
					result.Add(new FwInflectionClass(cls.Guid.ToString(), name, depth));
				foreach (var sub in cls.SubclassesOC)
					Add(sub, depth + 1);
			}

			foreach (var cls in pos.InflectionClassesOC)
				Add(cls, 0);
			return result;
		}

		/// <summary>
		/// Projects the inflection-feature SYSTEM for a main POS (guid string) as a flat,
		/// document-order, depth-tagged <see cref="FwFeatureNode"/> list -- the lift of
		/// <c>MsaInflectionFeatureListDlg.PopulateTreeFromPos</c> via <see
		/// cref="FwFeatureStructureAdapter.BuildNodes"/>: the POS's (and its parent POSes')
		/// <c>InflectableFeatsRC</c>, closed features expanded to their values, complex features
		/// expanded to their nested features. An empty or unknown POS yields no nodes.
		/// </summary>
		internal static IReadOnlyList<FwFeatureNode> BuildInflectionFeatures(LcmCache cache, string posId)
		{
			var pos = ResolvePos(cache, posId);
			return FwFeatureStructureAdapter.BuildNodes(pos);
		}

		/// <summary>
		/// Rebuilds the inflection <c>IFsFeatStruc</c> on a sense's morpheme MSA from the chosen
		/// inflection-feature assignment set -- the create-side parity of
		/// <c>MsaInflectionFeatureListDlg_Closing</c>. Scoped to <c>IMoInflAffMsa.InflFeatsOA</c>
		/// and <c>IMoDerivAffMsa.FromMsFeaturesOA</c>, the surface the box edits; other MSA
		/// flavours are a no-op. Resolves the feature-system nodes from the MSA's own POS, so the
		/// commit need not carry the live node list, then writes or clears the FS in the caller's
		/// unit of work.
		/// </summary>
		internal static void ApplyInflectionFeatures(LcmCache cache, ILexSense sense, FwSandboxMsa chosen)
		{
			if (sense == null || chosen == null)
				return;
			ApplyInflectionFeatures(cache, sense.MorphoSyntaxAnalysisRA, chosen);
		}

		/// <summary>
		/// Rebuilds the inflection <c>IFsFeatStruc</c> on a morpheme MSA from the chosen
		/// assignment set; see the sense overload for the rules.
		/// </summary>
		internal static void ApplyInflectionFeatures(LcmCache cache, IMoMorphSynAnalysis msa, FwSandboxMsa chosen)
		{
			if (cache == null || msa == null || chosen == null)
				return;
			var pos = FwFeatureStructureAdapter.GetInflectionFeaturePos(msa);
			var nodes = FwFeatureStructureAdapter.BuildNodes(pos);
			FwFeatureStructureAdapter.ApplyInflectionFeatures(cache, msa, nodes, chosen.InflectionFeatures);
		}

		/// <summary>
		/// Sets the chosen inflection class on the entry's first sense, if it has one. A no-op
		/// when the entry has no sense or the descriptor carries no class.
		/// </summary>
		internal static void ApplyInflectionClass(LcmCache cache, ILexEntry entry, FwSandboxMsa chosen)
		{
			if (entry == null || chosen == null || string.IsNullOrEmpty(chosen.InflectionClassId))
				return;
			if (entry.SensesOS.Count == 0)
				return;
			ApplyInflectionClass(cache, entry.SensesOS[0], chosen);
		}

		/// <summary>
		/// Sets the chosen inflection class on a sense's STEM MSA -- the lift of
		/// <c>InsertEntryDlg.SetEntryMsa</c>'s <c>IMoStemMsa</c> branch. Resolves the id to an
		/// <c>IMoInflClass</c> and assigns <c>InflectionClassRA</c>; a non-stem MSA or an
		/// unresolvable id is a no-op. Runs in the caller's unit of work.
		/// </summary>
		internal static void ApplyInflectionClass(LcmCache cache, ILexSense sense, FwSandboxMsa chosen)
		{
			if (sense == null || chosen == null || string.IsNullOrEmpty(chosen.InflectionClassId))
				return;
			if (sense.MorphoSyntaxAnalysisRA is IMoStemMsa stemMsa)
			{
				var inflClass = ResolveInflectionClass(cache, chosen.InflectionClassId);
				if (inflClass != null)
					stemMsa.InflectionClassRA = inflClass;
			}
		}

		private static IMoInflClass ResolveInflectionClass(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
				return null;
			try { return cache.ServiceLocator.GetInstance<IMoInflClassRepository>().GetObject(guid); }
			catch { return null; }
		}

		/// <summary>
		/// Resolves the LCModel-free <see cref="FwSandboxMsa"/> (MsaType plus POS/slot ids) into
		/// a real <c>SandboxGenericMSA</c> the factory uses to find-or-create the sense's MSA --
		/// the parity of <c>MSAGroupBox.SandboxMSA</c>. The POS/slot ids resolve back through the
		/// repositories by guid and an unresolvable id is simply dropped (the &lt;Any&gt; pick),
		/// while <see cref="MsaTypeRules.BuildSandboxMsa"/> decides which fields the chosen
		/// MsaType carries. A null descriptor falls back to the morph-type's default MSA flavor.
		/// </summary>
		internal static SandboxGenericMSA BuildSandboxMsa(LcmCache cache, FwSandboxMsa chosen, IMoMorphType morphType)
		{
			if (chosen == null)
				return new SandboxGenericMSA { MsaType = DefaultMsaType(morphType) };

			// Resolve the ids here; MsaTypeRules decides which fields each MsaType carries. The
			// slot is narrowed to kInfl below and category validity is not checked on this path,
			// so slotValid is true.
			var type = ToLcmMsaType(chosen.MsaType, morphType);
			var slot = type == MsaType.kInfl ? ResolveSlot(cache, chosen.SlotId) : null;
			var secondaryPos = type == MsaType.kDeriv ? ResolvePos(cache, chosen.SecondaryPosId) : null;
			return MsaTypeRules.BuildSandboxMsa(type, ResolvePos(cache, chosen.MainPosId), secondaryPos, slot,
				slotValid: true);
		}

		// Maps the shared FwMsaType to the LCModel MsaType. FwMsaType.NotSet falls back to the
		// morph-type default.
		private static MsaType ToLcmMsaType(FwMsaType type, IMoMorphType morphType)
		{
			switch (type)
			{
				case FwMsaType.Stem: return MsaType.kStem;
				case FwMsaType.Root: return MsaType.kRoot;
				case FwMsaType.Inflectional: return MsaType.kInfl;
				case FwMsaType.Derivational: return MsaType.kDeriv;
				case FwMsaType.Unclassified: return MsaType.kUnclassified;
				default: return DefaultMsaType(morphType);
			}
		}

		private static IPartOfSpeech ResolvePos(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
				return null;
			try { return cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(guid); }
			catch { return null; }
		}

		private static IMoInflAffixSlot ResolveSlot(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out var guid))
				return null;
			try { return cache.ServiceLocator.GetInstance<IMoInflAffixSlotRepository>().GetObject(guid); }
			catch { return null; }
		}

		// The default MSA flavor a morph type implies (parity with MSAGroupBox's
		// morph-type-driven default): roots
		// take a root/stem MSA, affixes an unclassified-affix MSA, everything else a stem MSA.
		private static MsaType DefaultMsaType(IMoMorphType morphType)
		{
			if (morphType == null)
				return MsaType.kStem;
			switch (morphType.Guid.ToString())
			{
				case MoMorphTypeTags.kMorphPrefix:
				case MoMorphTypeTags.kMorphInfix:
				case MoMorphTypeTags.kMorphSuffix:
				case MoMorphTypeTags.kMorphSimulfix:
				case MoMorphTypeTags.kMorphSuprafix:
				case MoMorphTypeTags.kMorphCircumfix:
				case MoMorphTypeTags.kMorphInfixingInterfix:
				case MoMorphTypeTags.kMorphPrefixingInterfix:
				case MoMorphTypeTags.kMorphSuffixingInterfix:
					return MsaType.kUnclassified;
				default:
					return MsaType.kStem;
			}
		}
	}
}

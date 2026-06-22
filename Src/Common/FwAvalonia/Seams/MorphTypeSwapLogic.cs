// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// Morph type categories, named to match the <c>MoMorphTypeTags.kguidMorphType*</c> model GUIDs.
	/// </summary>
	public enum MorphTypeKind
	{
		Root,
		Stem,
		BoundRoot,
		BoundStem,
		Particle,
		Clitic,
		Proclitic,
		Enclitic,
		Phrase,
		DiscontiguousPhrase,
		Prefix,
		Suffix,
		Infix,
		Simulfix,
		Suprafix,
		Circumfix,
		PrefixingInterfix,
		InfixingInterfix,
		SuffixingInterfix
	}

	/// <summary>
	/// Pure humble object extracted from <c>MorphTypeAtomicLauncher</c> (task 3.4): the stem/affix
	/// classification and swap data-loss decision, with no WinForms dependency. The stem-type set
	/// mirrors <c>MorphTypeAtomicLauncher.IsStemType</c> exactly (bound root/stem, enclitic, particle,
	/// proclitic, root, stem, clitic, phrase, discontiguous phrase). Swapping across the stem/affix
	/// boundary is what triggers the legacy affix/stem data-loss checks.
	/// </summary>
	public static class MorphTypeSwapLogic
	{
		private static readonly HashSet<MorphTypeKind> StemTypes = new HashSet<MorphTypeKind>
		{
			MorphTypeKind.BoundRoot,
			MorphTypeKind.BoundStem,
			MorphTypeKind.Enclitic,
			MorphTypeKind.Particle,
			MorphTypeKind.Proclitic,
			MorphTypeKind.Root,
			MorphTypeKind.Stem,
			MorphTypeKind.Clitic,
			MorphTypeKind.Phrase,
			MorphTypeKind.DiscontiguousPhrase
		};

		/// <summary>True if the morph type is a stem-type (mirrors the legacy <c>IsStemType</c>).</summary>
		public static bool IsStemType(MorphTypeKind type) => StemTypes.Contains(type);

		/// <summary>True if a swap from <paramref name="from"/> to <paramref name="to"/> crosses the stem/affix boundary.</summary>
		public static bool WouldCrossStemAffixBoundary(MorphTypeKind from, MorphTypeKind to)
			=> IsStemType(from) != IsStemType(to);

		/// <summary>
		/// Classifies a swap into a data-loss risk decision. Crossing the stem/affix boundary risks data
		/// loss (the affix-only or stem-only data on the allomorph/MSA cannot survive); same-side swaps do not.
		/// </summary>
		public static MorphSwapDecision Analyze(MorphTypeKind from, MorphTypeKind to)
		{
			if (from == to)
			{
				return new MorphSwapDecision(false, MorphSwapDirection.None, "No change.");
			}

			if (!WouldCrossStemAffixBoundary(from, to))
			{
				return new MorphSwapDecision(false, MorphSwapDirection.None,
					"Same side of the stem/affix boundary; no data-loss prompt required.");
			}

			if (IsStemType(from))
			{
				return new MorphSwapDecision(true, MorphSwapDirection.StemToAffix,
					"Changing a stem-type to an affix-type may lose stem/MSA data.");
			}

			return new MorphSwapDecision(true, MorphSwapDirection.AffixToStem,
				"Changing an affix-type to a stem-type may lose affix/inflection data.");
		}
	}

	/// <summary>Direction of a stem/affix boundary crossing.</summary>
	public enum MorphSwapDirection
	{
		None,
		StemToAffix,
		AffixToStem
	}

	/// <summary>Result of analyzing a morph type swap.</summary>
	public sealed class MorphSwapDecision
	{
		public MorphSwapDecision(bool requiresDataLossPrompt, MorphSwapDirection direction, string reason)
		{
			RequiresDataLossPrompt = requiresDataLossPrompt;
			Direction = direction;
			Reason = reason;
		}

		/// <summary>Whether the legacy data-loss confirmation prompt would be required.</summary>
		public bool RequiresDataLossPrompt { get; }

		public MorphSwapDirection Direction { get; }

		public string Reason { get; }
	}
}

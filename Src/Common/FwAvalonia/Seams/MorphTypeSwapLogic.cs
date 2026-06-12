// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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

		// Review consolidation (morph-type GUID knowledge): the ONE GUID → kind table. The seam is
		// the cleaner home because it already owns MorphTypeKind and the stem/affix decision, and
		// both the xWorks composer and any future surface can consume it without dragging WinForms
		// along. This project is deliberately LCModel-free, so the fixed MoMorphTypeTags model GUIDs
		// are mirrored as System.Guid literals; MorphTypeGuidConsolidationTests (xWorksTests, which
		// references both assemblies) pins every literal to its MoMorphTypeTags constant so the
		// mirror cannot drift. The legacy WinForms MorphTypeAtomicLauncher.IsStemType still carries
		// its own guid list (DetailControls cannot reference FwAvalonia today); the same test pins
		// that set too, and the launcher retires with its surface.
		private static readonly IReadOnlyDictionary<Guid, MorphTypeKind> KindByGuid =
			new Dictionary<Guid, MorphTypeKind>
			{
				{ new Guid("d7f713e5-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Root },
				{ new Guid("d7f713e8-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Stem },
				{ new Guid("d7f713e4-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.BoundRoot },
				{ new Guid("d7f713e7-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.BoundStem },
				{ new Guid("56db04bf-3d58-44cc-b292-4c8aa68538f4"), MorphTypeKind.Particle },
				{ new Guid("c2d140e5-7ca9-41f4-a69a-22fc7049dd2c"), MorphTypeKind.Clitic },
				{ new Guid("d7f713e2-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Proclitic },
				{ new Guid("d7f713e1-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Enclitic },
				{ new Guid("a23b6faa-1052-4f4d-984b-4b338bdaf95f"), MorphTypeKind.Phrase },
				{ new Guid("0cc8c35a-cee9-434d-be58-5d29130fba5b"), MorphTypeKind.DiscontiguousPhrase },
				{ new Guid("d7f713db-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Prefix },
				{ new Guid("d7f713dd-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Suffix },
				{ new Guid("d7f713da-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Infix },
				{ new Guid("d7f713dc-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Simulfix },
				{ new Guid("d7f713de-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Suprafix },
				{ new Guid("d7f713df-e8cf-11d3-9764-00c04f186933"), MorphTypeKind.Circumfix },
				{ new Guid("af6537b0-7175-4387-ba6a-36547d37fb13"), MorphTypeKind.PrefixingInterfix },
				{ new Guid("18d9b1c3-b5b6-4c07-b92c-2fe1d2281bd4"), MorphTypeKind.InfixingInterfix },
				{ new Guid("3433683d-08a9-4bae-ae53-2a7798f64068"), MorphTypeKind.SuffixingInterfix }
			};

		/// <summary>
		/// Classifies one of the fixed morph-type model GUIDs onto its <see cref="MorphTypeKind"/>.
		/// False for anything else (a user-created morph type has no fixed GUID and no kind).
		/// </summary>
		public static bool TryClassify(Guid morphTypeGuid, out MorphTypeKind kind)
			=> KindByGuid.TryGetValue(morphTypeGuid, out kind);

		/// <summary>True if the morph type is a stem-type (mirrors the legacy <c>IsStemType</c>).</summary>
		public static bool IsStemType(MorphTypeKind type) => StemTypes.Contains(type);

		/// <summary>
		/// True if the morph-type GUID classifies as a stem-type — the guid-level twin of the
		/// legacy <c>MorphTypeAtomicLauncher.IsStemType</c> (an unknown guid is not a stem type,
		/// exactly like the legacy null/guard behavior).
		/// </summary>
		public static bool IsStemType(Guid morphTypeGuid)
			=> TryClassify(morphTypeGuid, out var kind) && IsStemType(kind);

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

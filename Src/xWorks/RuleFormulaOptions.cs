// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 2.2) — projects the phonological inventory a rule cell can
	/// reference (phonemes, natural classes, boundary markers) into the <see cref="RegionChoiceOption"/>
	/// list the shared <c>FwOptionPicker</c> renders. Each option's <see cref="RegionChoiceOption.Key"/> is a
	/// <see cref="RuleCellSpec"/> codec key (kind prefix + GUID), so the picked option decodes straight back
	/// to the spec the <see cref="RuleFormulaEditSink"/> inserts. LCModel reads live here (design Decision 1);
	/// the view only sees the LCModel-free option list. Display text mirrors the projector/legacy Vc.
	/// </summary>
	public static class RuleFormulaOptions
	{
		/// <summary>
		/// Build the insertable-cell options for a rule, in the legacy chooser order: phonemes (vernacular
		/// name), then natural classes (analysis abbreviation), then boundary markers. Each carries a codec
		/// key (<see cref="RuleCellSpec.ToOptionKey"/>) the view decodes on commit.
		/// </summary>
		public static IReadOnlyList<RegionChoiceOption> BuildCellOptions(LcmCache cache)
		{
			var options = new List<RegionChoiceOption>();
			if (cache == null)
				return options;
			var phonData = cache.LangProject?.PhonologicalDataOA;
			if (phonData == null)
				return options;

			foreach (var phonemeSet in phonData.PhonemeSetsOS)
				foreach (var phoneme in phonemeSet.PhonemesOC)
					options.Add(Option(RuleCellKind.Phoneme, phoneme.Guid, Vernacular(phoneme)));

			foreach (var nc in phonData.NaturalClassesOS)
				options.Add(Option(RuleCellKind.NaturalClass, nc.Guid, NaturalClassText(nc)));

			foreach (var phonemeSet in phonData.PhonemeSetsOS)
				foreach (var bdry in phonemeSet.BoundaryMarkersOC)
					options.Add(Option(RuleCellKind.Boundary, bdry.Guid, Vernacular(bdry)));

			return options;
		}

		private static RegionChoiceOption Option(RuleCellKind kind, System.Guid guid, string name)
			=> new RegionChoiceOption(new RuleCellSpec(kind, guid).ToOptionKey(),
				string.IsNullOrEmpty(name) ? "?" : name, 0);

		private static string Vernacular(IPhTerminalUnit tu)
			=> tu?.Name?.BestVernacularAlternative?.Text;

		private static string NaturalClassText(IPhNaturalClass nc)
		{
			var abbr = nc?.Abbreviation?.BestAnalysisAlternative?.Text;
			if (!string.IsNullOrEmpty(abbr))
				return abbr;
			return nc?.Name?.BestAnalysisAlternative?.Text;
		}
	}
}

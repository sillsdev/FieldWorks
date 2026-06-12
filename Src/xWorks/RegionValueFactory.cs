// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Review task 12 — the ONE home for the two value-projection recipes that
	/// <see cref="FullEntryRegionComposer"/> and <see cref="LexicalEditRegionBuilder"/> had each
	/// grown separately: the per-writing-system value rows and the possibility-list option
	/// flattening. Sharing them here is what keeps the two surfaces from drifting — they HAD
	/// drifted: the builder's option-name fallback walked analysis → vernacular while the
	/// composer's walked analysis → ShortName (see <see cref="BuildPossibilityOptions"/> for the
	/// deliberate resolution).
	/// </summary>
	internal static class RegionValueFactory
	{
		/// <summary>
		/// One <see cref="RegionWsValue"/> per writing system, in list order, carrying the
		/// project's per-ws display metadata (abbreviation, default font, RTL, IETF tag);
		/// <paramref name="readText"/> supplies each alternative's text (null reads as empty).
		/// </summary>
		internal static IReadOnlyList<RegionWsValue> BuildMultiWsValues(
			IEnumerable<CoreWritingSystemDefinition> systems,
			Func<CoreWritingSystemDefinition, string> readText,
			double fontSize = 0, bool boldEmphasis = false)
		{
			var values = new List<RegionWsValue>();
			foreach (var ws in systems)
			{
				values.Add(new RegionWsValue(ws.Abbreviation, readText(ws) ?? string.Empty,
					ws.DefaultFontName, fontSize, ws.RightToLeftScript, ws.Id, boldEmphasis));
			}
			return values;
		}

		/// <summary>
		/// B8/B7: walks a possibility list's tree in document order (parent before children) into
		/// chooser options, hierarchy carried as <see cref="RegionChoiceOption.Depth"/> — exactly
		/// the indented tree the legacy chooser shows. <paramref name="flat"/> (a chooserInfo
		/// "FlatList" guicontrol spec, e.g. PeopleFlatList) keeps the order but suppresses the
		/// hierarchy, like the legacy flat chooser. Option names use the composer's fallback rule
		/// — Name.BestAnalysisAlternative, then <c>ShortName</c>, then the guid — chosen over the
		/// builder's old explicit analysis→vernacular walk because <c>CmPossibility.ShortName</c>
		/// already performs the legacy best-analysis-then-vernacular resolution itself
		/// (ShortNameTSS), so the vernacular fallback is subsumed, not lost.
		/// </summary>
		internal static IReadOnlyList<RegionChoiceOption> BuildPossibilityOptions(
			ICmPossibilityList list, bool flat)
		{
			var options = new List<RegionChoiceOption>();
			void Add(ICmPossibility possibility, int depth)
			{
				options.Add(new RegionChoiceOption(possibility.Guid.ToString(),
					possibility.Name.BestAnalysisAlternative?.Text ?? possibility.ShortName ?? possibility.Guid.ToString(),
					flat ? 0 : depth));
				foreach (var sub in possibility.SubPossibilitiesOS)
					Add(sub, depth + 1);
			}

			foreach (var possibility in list.PossibilitiesOS)
				Add(possibility, 0);
			return options;
		}
	}
}

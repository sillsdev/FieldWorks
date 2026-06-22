// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
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
		/// One <see cref="RegionWsValue"/> per writing system from an <c>ITsString</c>-backed value,
		/// preserving the source rich-text runs in a neutral Avalonia-facing shape while keeping the
		/// common project free of any LCModel dependency.
		/// </summary>
		internal static IReadOnlyList<RegionWsValue> BuildMultiWsValues(
			IEnumerable<CoreWritingSystemDefinition> systems,
			Func<CoreWritingSystemDefinition, ITsString> readText,
			ILgWritingSystemFactory writingSystemFactory,
			double fontSize = 0, bool boldEmphasis = false)
		{
			var values = new List<RegionWsValue>();
			foreach (var ws in systems)
			{
				var tss = readText(ws);
				var richText = RegionRichTextAdapter.FromTsString(tss, writingSystemFactory);
				values.Add(new RegionWsValue(ws.Abbreviation, tss?.Text ?? string.Empty,
					ws.DefaultFontName, fontSize, ws.RightToLeftScript, ws.Id, boldEmphasis, richText));
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

	internal static class RegionRichTextAdapter
	{
		internal static RegionRichTextValue FromTsString(ITsString tss, ILgWritingSystemFactory writingSystemFactory)
		{
			if (tss == null)
				return null;

			var runs = new List<RegionTextRun>();
			var lossyProperties = false;
			for (var irun = 0; irun < tss.RunCount; irun++)
			{
				var props = tss.get_Properties(irun);
				var wsHandle = TsStringUtils.GetWsOfRun(tss, irun);
				var wsTag = wsHandle > 0 ? writingSystemFactory.GetStrFromWs(wsHandle) : null;
				var namedStyle = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				var fontFamily = props.GetStrPropValue((int)FwTextPropType.ktptFontFamily);
				var objectData = props.GetStrPropValue((int)FwTextPropType.ktptObjData);
				var fontSize = props.GetIntPropValues((int)FwTextPropType.ktptFontSize, out _);
				var bold = props.GetIntPropValues((int)FwTextPropType.ktptBold, out _) > 0;
				var italic = props.GetIntPropValues((int)FwTextPropType.ktptItalic, out _) > 0;
				var underline = props.GetIntPropValues((int)FwTextPropType.ktptUnderline, out _) > 0;

				lossyProperties |= RunCarriesUnsupportedProperty(props);

				runs.Add(new RegionTextRun(tss.get_RunText(irun), wsTag, namedStyle, fontFamily,
					fontSize > 0 ? fontSize : 0, bold, italic, underline, objectData));
			}

			return new RegionRichTextValue(
				tss.Text ?? string.Empty,
				runs,
				TsStringUtils.GetXmlRep(tss, writingSystemFactory, 0),
				RequiresRichEditor(tss),
				canEditRichText: runs.TrueForAll(run => string.IsNullOrEmpty(run.ObjectData)),
				lossyProperties: lossyProperties);
		}

		// The TsString text properties the RegionTextRun model captures in FromTsString AND re-emits in
		// ToTsString's run-replay path. Any other int/string property on a run is silently dropped the
		// first time the plain text changes (the edit skips the lossless RichXml fast-path), so a run
		// carrying one outside this set is flagged lossy and the value is held read-only. ktptWs is
		// supported (replayed as the run's writing system) and so is ktptObjData (its presence already
		// forces read-only via CanEditRichText, but it is "round-tripped" enough not to count as lossy).
		private static readonly HashSet<int> SupportedIntProps = new HashSet<int>
		{
			(int)FwTextPropType.ktptWs,
			(int)FwTextPropType.ktptFontSize,
			(int)FwTextPropType.ktptBold,
			(int)FwTextPropType.ktptItalic,
			(int)FwTextPropType.ktptUnderline,
		};

		private static readonly HashSet<int> SupportedStrProps = new HashSet<int>
		{
			(int)FwTextPropType.ktptNamedStyle,
			(int)FwTextPropType.ktptFontFamily,
			(int)FwTextPropType.ktptObjData,
		};

		/// <summary>
		/// Enumerates a run's int and string TsString text properties (<see cref="ITsTextProps"/>) and
		/// returns true when ANY of them is outside the set the adapter both reads and replays — i.e. a
		/// property the run-replay path would drop on the first edit (e.g. ktptForeColor, ktptBackColor,
		/// ktptOffset, ktptSuperscript). The lossless RichXml still preserves it for full-fidelity display.
		/// </summary>
		private static bool RunCarriesUnsupportedProperty(ITsTextProps props)
		{
			for (var i = 0; i < props.IntPropCount; i++)
			{
				props.GetIntProp(i, out var tpt, out _);
				if (!SupportedIntProps.Contains(tpt))
					return true;
			}

			for (var i = 0; i < props.StrPropCount; i++)
			{
				props.GetStrProp(i, out var tpt);
				if (!SupportedStrProps.Contains(tpt))
					return true;
			}

			return false;
		}

		internal static ITsString ToTsString(RegionRichTextValue richText,
			ILgWritingSystemFactory writingSystemFactory, int fallbackWs)
		{
			if (richText == null)
				return TsStringUtils.MakeString(string.Empty, fallbackWs);

			if (!string.IsNullOrEmpty(richText.RichXml))
			{
				var roundTripped = TsStringSerializer.DeserializeTsStringFromXml(richText.RichXml,
					writingSystemFactory);
				if (roundTripped != null && roundTripped.Text == richText.PlainText)
					return roundTripped;
			}

			if (richText.Runs == null || richText.Runs.Count == 0)
				return TsStringUtils.MakeString(richText.PlainText ?? string.Empty, fallbackWs);

			var builder = TsStringUtils.MakeIncStrBldr();
			foreach (var run in richText.Runs)
			{
				var wsHandle = fallbackWs;
				if (!string.IsNullOrEmpty(run.WritingSystemTag))
				{
					var resolved = writingSystemFactory.GetWsFromStr(run.WritingSystemTag);
					if (resolved > 0)
						wsHandle = resolved;
				}

				builder.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, wsHandle);
				builder.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, run.NamedStyle);
				builder.SetStrPropValue((int)FwTextPropType.ktptFontFamily, run.FontFamily);
				builder.SetStrPropValue((int)FwTextPropType.ktptObjData, run.ObjectData);
				builder.SetIntPropValues((int)FwTextPropType.ktptBold, -1, -1);
				builder.SetIntPropValues((int)FwTextPropType.ktptItalic, -1, -1);
				builder.SetIntPropValues((int)FwTextPropType.ktptUnderline, -1, -1);
				builder.SetIntPropValues((int)FwTextPropType.ktptFontSize, -1, -1);

				if (run.Bold)
					builder.SetIntPropValues((int)FwTextPropType.ktptBold,
						(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				if (run.Italic)
					builder.SetIntPropValues((int)FwTextPropType.ktptItalic,
						(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				if (run.Underline)
					builder.SetIntPropValues((int)FwTextPropType.ktptUnderline,
						(int)FwTextPropVar.ktpvEnum, 1);
				if (run.FontSizeMilliPoints > 0)
					builder.SetIntPropValues((int)FwTextPropType.ktptFontSize,
						(int)FwTextPropVar.ktpvMilliPoint, run.FontSizeMilliPoints);

				builder.Append(run.Text ?? string.Empty);
			}

			return builder.GetString();
		}

		internal static bool RequiresRichEditor(ITsString tss)
		{
			if (tss == null || tss.Length == 0)
				return false;
			if (tss.RunCount > 1)
				return true;

			var props = tss.get_Properties(0);
			if (props.StrPropCount > 0)
				return true;

			for (var i = 0; i < props.IntPropCount; i++)
			{
				props.GetIntProp(i, out var tpt, out _);
				if (tpt != (int)FwTextPropType.ktptWs)
					return true;
			}

			return false;
		}
	}
}

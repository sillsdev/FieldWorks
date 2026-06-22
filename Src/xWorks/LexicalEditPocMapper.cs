// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Poc;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Maps the current Lexical Edit record (`LexEntry`) into the detached DTO consumed by the
	/// Avalonia POC surface. This is intentionally small and lossy: it only projects the three fields
	/// the current POC renders (lexeme form, morph type, first-sense gloss). The legacy WinForms
	/// DataTree remains the full-fidelity path; this mapper exists only for the feature-flagged
	/// in-app spike.
	/// </summary>
	public static class LexicalEditPocMapper
	{
		public static PocEntryDto CreateDto(ICmObject obj, LcmCache cache)
		{
			var entry = obj as ILexEntry;
			if (entry == null)
			{
				return null;
			}

			return new PocEntryDto(
				BuildLexemeForm(entry),
				BuildMorphTypeOptions(),
				GetMorphTypeKey(entry),
				BuildFirstSenseGloss(entry, cache));
		}

		private static IList<WsAlternative> BuildLexemeForm(ILexEntry entry)
		{
			var values = new List<WsAlternative>();
			var lexemeText = entry.LexemeFormOA != null && entry.LexemeFormOA.Form != null
				? entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text
				: string.Empty;
			if (string.IsNullOrEmpty(lexemeText))
			{
				lexemeText = entry.CitationForm.VernacularDefaultWritingSystem.Text;
			}

			values.Add(new WsAlternative("vern", lexemeText));
			return values;
		}

		private static IList<WsAlternative> BuildFirstSenseGloss(ILexEntry entry, LcmCache cache)
		{
			var values = new List<WsAlternative>();
			if (entry.SensesOS.Count > 0)
			{
				var gloss = entry.SensesOS[0].Gloss.AnalysisDefaultWritingSystem.Text;
				values.Add(new WsAlternative("anal", gloss, "Times New Roman"));
			}
			else
			{
				values.Add(new WsAlternative("anal", string.Empty, "Times New Roman"));
			}

			return values;
		}

		private static IList<MorphTypeOption> BuildMorphTypeOptions()
		{
			return new List<MorphTypeOption>
			{
				new MorphTypeOption("stem", "stem"),
				new MorphTypeOption("root", "root"),
				new MorphTypeOption("prefix", "prefix"),
				new MorphTypeOption("suffix", "suffix")
			};
		}

		private static string GetMorphTypeKey(ILexEntry entry)
		{
			var type = entry.LexemeFormOA != null ? entry.LexemeFormOA.MorphTypeRA : null;
			if (type == null)
			{
				return "stem";
			}

			if (type.Guid == MoMorphTypeTags.kguidMorphPrefix)
			{
				return "prefix";
			}
			if (type.Guid == MoMorphTypeTags.kguidMorphSuffix)
			{
				return "suffix";
			}
			if (type.Guid == MoMorphTypeTags.kguidMorphRoot || type.Guid == MoMorphTypeTags.kguidMorphBoundRoot)
			{
				return "root";
			}

			return "stem";
		}
	}
}

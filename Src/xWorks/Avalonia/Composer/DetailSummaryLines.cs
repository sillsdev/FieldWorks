// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The header lines of the objects whose detail layout opens with a summary row carrying a
	/// collapsed variant: a sense, an etymology, an extended note and a notebook record. Each
	/// pair follows that class's Summary and SummaryCollapsed parts in the shipped
	/// configuration (LexSenseParts.xml, LexEntryParts.xml, NotebookParts.xml), with their
	/// literal text localized through the string table.
	/// </summary>
	internal sealed class DetailSummaryLines
	{
		private readonly LcmCache _cache;

		/// <summary>Creates the line builder for <paramref name="cache"/>'s project.</summary>
		public DetailSummaryLines(LcmCache cache)
		{
			_cache = cache;
		}

		/// <summary>True when <paramref name="obj"/>'s class has a summary line pair.</summary>
		public static bool Covers(ICmObject obj)
			=> obj is ILexSense || obj is ILexEtymology || obj is ILexExtendedNote || obj is IRnGenericRec;

		/// <summary>
		/// True when <paramref name="obj"/>'s layout opens with a NormalSummary row rather than
		/// a HeavySummary one: an etymology or an extended note, not a sense.
		/// </summary>
		public static bool IsNormalWeight(ICmObject obj) => obj is ILexEtymology || obj is ILexExtendedNote;

		/// <summary>
		/// The header text of <paramref name="obj"/> while expanded and while collapsed. Returns
		/// false, with both null, when its class has no summary line pair.
		/// </summary>
		public bool TryGet(ICmObject obj, out string expanded, out string collapsed)
		{
			switch (obj)
			{
				case ILexSense sense:
					expanded = Lit("Sense ") + sense.LexSenseOutline?.Text;
					collapsed = expanded + Lit(" - ") + DefinitionOrGloss(sense) + Lit(" - ")
						+ GrammaticalInfo(sense.MorphoSyntaxAnalysisRA);
					return true;
				case ILexEtymology etymology:
					expanded = Lit("Etymology");
					collapsed = expanded + Lit(" - ") + (AllEmpty(etymology.LanguageNotes)
						? BestAnalysis(etymology.Gloss)
						: BestAnalysis(etymology.LanguageNotes) + Lit(" - ") + BestAnalysis(etymology.Gloss));
					return true;
				case ILexExtendedNote note:
					expanded = Lit("Extended Note");
					collapsed = expanded + Lit(" - ") + Abbreviation(note.ExtendedNoteTypeRA) + Lit(" - ")
						+ BestAnalysis(note.Discussion);
					return true;
				case IRnGenericRec record:
					expanded = IndexInOwner(record) + Lit(" ") + Name(record.TypeRA);
					collapsed = expanded + Lit(" - ") + record.Title?.Text;
					return true;
				default:
					expanded = collapsed = null;
					return false;
			}
		}

		// The definition, or the gloss when the definition is empty in every analysis
		// writing system.
		private string DefinitionOrGloss(ILexSense sense)
			=> AllEmpty(sense.Definition) ? BestAnalysis(sense.Gloss) : BestAnalysis(sense.Definition);

		// The MorphoSyntaxAnalysis "Basic" layout of each analysis class.
		private string GrammaticalInfo(IMoMorphSynAnalysis msa)
		{
			switch (msa)
			{
				case IMoStemMsa stem:
					return Abbreviation(stem.PartOfSpeechRA);
				case IMoUnclassifiedAffixMsa unclassified:
					return Abbreviation(unclassified.PartOfSpeechRA);
				case IMoDerivAffMsa derivational:
					if (derivational.FromPartOfSpeechRA == null)
					{
						return derivational.ToPartOfSpeechRA == null
							? Lit("Unclassified affix")
							: Lit("???") + Lit(" > ") + Abbreviation(derivational.ToPartOfSpeechRA);
					}
					return Abbreviation(derivational.FromPartOfSpeechRA) + Lit(" > ")
						+ (derivational.ToPartOfSpeechRA == null
							? Lit("???.")
							: Abbreviation(derivational.ToPartOfSpeechRA));
				case IMoInflAffMsa inflectional:
					if (inflectional.PartOfSpeechRA == null)
						return Lit("Unmk. infl. afx slot");
					return Abbreviation(inflectional.PartOfSpeechRA) + ":"
						+ string.Join("/", inflectional.SlotsRC.Select(slot => slot.ShortName));
				default:
					return string.Empty;
			}
		}

		// A record's IndexInOwnerTSS (its number among its owner's subrecords).
		private string IndexInOwner(IRnGenericRec record)
		{
			var mdc = (IFwMetaDataCacheManaged)_cache.MetaDataCacheAccessor;
			if (mdc.FieldExists(RnGenericRecTags.kClassName, "IndexInOwnerTSS", true))
			{
				var flid = mdc.GetFieldId(RnGenericRecTags.kClassName, "IndexInOwnerTSS", true);
				return _cache.DomainDataByFlid.get_StringProp(record.Hvo, flid)?.Text ?? string.Empty;
			}
			return (record.IndexInOwner + 1).ToString();
		}

		private string Abbreviation(ICmPossibility possibility)
			=> possibility == null ? string.Empty : BestAnalysis(possibility.Abbreviation);

		private string Name(ICmPossibility possibility)
			=> possibility == null
				? string.Empty
				: Best(possibility.Name, AnalysisWritingSystems.Concat(VernacularWritingSystems));

		private IEnumerable<CoreWritingSystemDefinition> AnalysisWritingSystems
			=> _cache.LangProject.CurrentAnalysisWritingSystems;

		private IEnumerable<CoreWritingSystemDefinition> VernacularWritingSystems
			=> _cache.LangProject.CurrentVernacularWritingSystems;

		// The first non-empty alternative in the analysis writing systems, in project order.
		private string BestAnalysis(IMultiAccessorBase value) => Best(value, AnalysisWritingSystems);

		private static string Best(IMultiAccessorBase value, IEnumerable<CoreWritingSystemDefinition> writingSystems)
		{
			if (value == null)
				return string.Empty;
			foreach (var ws in writingSystems)
			{
				var text = value.get_String(ws.Handle)?.Text;
				if (!string.IsNullOrEmpty(text))
					return text;
			}
			return string.Empty;
		}

		private bool AllEmpty(IMultiAccessorBase value) => Best(value, AnalysisWritingSystems).Length == 0;

		private static string Lit(string text) => StringTable.Table.LocalizeLiteralValue(text);
	}
}

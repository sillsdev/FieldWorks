// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Turns a template's placeholder usage into the InterlinLineChoices that drives the existing
	/// (unmodified) InterlinearExporter/InterlinVc export pipeline. InterlinLineChoices already
	/// supports several lines of the same field at different writing systems (see
	/// InterlinLineChoices.Add), so LT-22712's multi-writing-system word/morpheme lines need no
	/// change to that pipeline -- only the right set of specs requested up front.
	/// </summary>
	public static class InterlinearTemplateLineChoicesBuilder
	{
		public static InterlinLineChoices Build(LcmCache cache, IReadOnlyCollection<InterlinearTemplateField> usedFields,
			InterlinearTemplateWritingSystemMapping wsMapping, int analysisWs)
		{
			var choices = new InterlinLineChoices(cache, cache.DefaultVernWs, analysisWs);
			int ipaWs = ResolveWs(cache, wsMapping.IpaIcuCode);
			int translitWs = ResolveWs(cache, wsMapping.TransliterationIcuCode);

			// words_source is mandatory (InterlinearTemplateValidator enforces it), so it's
			// always requested.
			choices.Add(choices.CreateSpec(InterlinLineChoices.kflidWord, cache.DefaultVernWs));
			AddIfUsed(choices, usedFields, InterlinearTemplateField.WordsIpa, InterlinLineChoices.kflidWord, ipaWs);
			AddIfUsed(choices, usedFields, InterlinearTemplateField.WordsTransliteration, InterlinLineChoices.kflidWord, translitWs);

			AddIfUsed(choices, usedFields, InterlinearTemplateField.MorphemesSource, InterlinLineChoices.kflidMorphemes, cache.DefaultVernWs);
			AddIfUsed(choices, usedFields, InterlinearTemplateField.MorphemesIpa, InterlinLineChoices.kflidMorphemes, ipaWs);
			AddIfUsed(choices, usedFields, InterlinearTemplateField.MorphemesTransliteration, InterlinLineChoices.kflidMorphemes, translitWs);

			if (usedFields.Contains(InterlinearTemplateField.Gloss))
				choices.Add(choices.CreateSpec(InterlinLineChoices.kflidLexGloss, analysisWs));
			AddIfUsed(choices, usedFields, InterlinearTemplateField.FreeTranslation, InterlinLineChoices.kflidFreeTrans, analysisWs);
			AddIfUsed(choices, usedFields, InterlinearTemplateField.LiteralTranslation, InterlinLineChoices.kflidLitTrans, analysisWs);
			AddIfUsed(choices, usedFields, InterlinearTemplateField.Note, InterlinLineChoices.kflidNote, analysisWs);

			return choices;
		}

		// ws == 0 means the project has no writing system configured for this role; the field is
		// simply not requested, and InterlinearTemplateResolver drops its line automatically.
		private static void AddIfUsed(InterlinLineChoices choices, IReadOnlyCollection<InterlinearTemplateField> usedFields,
			InterlinearTemplateField field, int flid, int ws)
		{
			if (ws != 0 && usedFields.Contains(field))
				choices.Add(choices.CreateSpec(flid, ws));
		}

		private static int ResolveWs(LcmCache cache, string icuCode)
		{
			return string.IsNullOrEmpty(icuCode) ? 0 : cache.WritingSystemFactory.GetWsFromStr(icuCode);
		}
	}
}

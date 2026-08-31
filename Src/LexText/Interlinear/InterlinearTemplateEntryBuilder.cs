// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SIL.LCModel;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Reads the flextext-shaped XML that InterlinearExporter/InterlinVc already produce (built
	/// from the InterlinLineChoices InterlinearTemplateLineChoicesBuilder constructs) and turns
	/// each phrase into an InterlinearTemplateEntry, selecting each item by writing system
	/// (item/@lang) rather than by position -- the one thing the old single-writing-system
	/// xml2LaTeX.xsl (LT-22645) never needed to do. The token-alignment rules themselves (one
	/// token per word/morpheme, brace-grouping a multi-word span, "{}" for a missing value,
	/// punctuation attaching to its neighbor with no space) are a direct port of that XSLT,
	/// generalized to run once per requested writing system instead of exactly once.
	/// </summary>
	public static class InterlinearTemplateEntryBuilder
	{
		public static IEnumerable<InterlinearTemplateEntry> BuildEntries(XDocument exported, LcmCache cache,
			IReadOnlyCollection<InterlinearTemplateField> usedFields, InterlinearTemplateWritingSystemMapping wsMapping, int analysisWs)
		{
			string sourceLang = cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(cache.DefaultVernWs);
			string ipaLang = ToIcuCode(cache, wsMapping.IpaIcuCode);
			string translitLang = ToIcuCode(cache, wsMapping.TransliterationIcuCode);
			string analysisLang = cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(analysisWs);
			// Which vernacular writing system decides a morph's boundary marker (- vs =) for the
			// gloss line: whichever morphemes_* form the template actually uses, preferring the
			// primary (source) form when more than one is present at once (LT-22712).
			string boundaryLang = usedFields.Contains(InterlinearTemplateField.MorphemesSource) ? sourceLang
				: usedFields.Contains(InterlinearTemplateField.MorphemesIpa) ? ipaLang
				: translitLang;

			foreach (var phrase in exported.Descendants("phrase"))
			{
				var words = phrase.Element("words")?.Elements("word").ToList() ?? new List<XElement>();
				var entry = new InterlinearTemplateEntry();

				entry.Set(InterlinearTemplateField.WordsSource, BuildWordLine(words, sourceLang));
				if (usedFields.Contains(InterlinearTemplateField.WordsIpa) && ipaLang != null)
					entry.Set(InterlinearTemplateField.WordsIpa, BuildWordLine(words, ipaLang));
				if (usedFields.Contains(InterlinearTemplateField.WordsTransliteration) && translitLang != null)
					entry.Set(InterlinearTemplateField.WordsTransliteration, BuildWordLine(words, translitLang));

				if (usedFields.Contains(InterlinearTemplateField.MorphemesSource))
					entry.Set(InterlinearTemplateField.MorphemesSource, BuildMorphLine(words, sourceLang));
				if (usedFields.Contains(InterlinearTemplateField.MorphemesIpa) && ipaLang != null)
					entry.Set(InterlinearTemplateField.MorphemesIpa, BuildMorphLine(words, ipaLang));
				if (usedFields.Contains(InterlinearTemplateField.MorphemesTransliteration) && translitLang != null)
					entry.Set(InterlinearTemplateField.MorphemesTransliteration, BuildMorphLine(words, translitLang));

				if (usedFields.Contains(InterlinearTemplateField.Gloss))
					entry.Set(InterlinearTemplateField.Gloss, BuildGlossLine(words, boundaryLang, analysisLang));
				if (usedFields.Contains(InterlinearTemplateField.FreeTranslation))
					entry.Set(InterlinearTemplateField.FreeTranslation, InterlinearTemplateEscaping.EscapeProse(GetItemText(phrase, "gls", analysisLang)));
				if (usedFields.Contains(InterlinearTemplateField.LiteralTranslation))
					entry.Set(InterlinearTemplateField.LiteralTranslation, InterlinearTemplateEscaping.EscapeProse(GetItemText(phrase, "lit", analysisLang)));
				if (usedFields.Contains(InterlinearTemplateField.Note))
					entry.Set(InterlinearTemplateField.Note, InterlinearTemplateEscaping.EscapeProse(GetItemText(phrase, "note", analysisLang)));

				yield return entry;
			}
		}

		// Null unless icuCode names a writing system currently configured in this project; a null
		// result leaves the corresponding words_*/morphemes_* fields with no data, so their lines
		// drop.
		private static string ToIcuCode(LcmCache cache, string icuCode)
		{
			return !string.IsNullOrEmpty(icuCode) && cache.WritingSystemFactory.GetWsFromStr(icuCode) != 0 ? icuCode : null;
		}

		private static string BuildWordLine(List<XElement> words, string lang)
		{
			return BuildAlignedLine(words, word => InterlinearTemplateEscaping.EscapeToken(GetItemText(word, "txt", lang) ?? string.Empty));
		}

		private static string BuildMorphLine(List<XElement> words, string lang)
		{
			return BuildAlignedLine(words, word => BuildMorphToken(word, lang));
		}

		private static string BuildMorphToken(XElement word, string lang)
		{
			var morphs = word.Element("morphemes")?.Elements("morph").ToList();
			if (morphs == null || morphs.Count == 0)
				return InterlinearTemplateEscaping.EscapeToken(GetItemText(word, "txt", lang) ?? string.Empty);

			var sb = new StringBuilder();
			foreach (var morph in morphs)
				sb.Append(InterlinearTemplateEscaping.EscapeToken(GetItemText(morph, "txt", lang) ?? string.Empty));
			return sb.ToString();
		}

		// One token per real word; a punctuation mark attaches with no space to the nearest word
		// token.
		private static string BuildAlignedLine(List<XElement> words, System.Func<XElement, string> tokenForWord)
		{
			var sb = new StringBuilder();
			var sawRealWord = false;
			foreach (var word in words)
			{
				var punct = GetItemText(word, "punct", null);
				if (punct != null)
				{
					sb.Append(InterlinearTemplateEscaping.EscapeToken(punct));
					continue;
				}
				if (sawRealWord)
					sb.Append(' ');
				var token = tokenForWord(word);
				sb.Append(string.IsNullOrEmpty(token) ? "{}" : token);
				sawRealWord = true;
			}
			return sb.ToString();
		}

		private static string BuildGlossLine(List<XElement> words, string boundaryLang, string analysisLang)
		{
			var sb = new StringBuilder();
			var first = true;
			foreach (var word in words)
			{
				if (GetItemText(word, "punct", null) != null)
					continue;
				if (!first)
					sb.Append(' ');
				first = false;

				var morphs = word.Element("morphemes")?.Elements("morph").ToList();
				if (morphs == null || morphs.Count == 0)
				{
					sb.Append("{}");
					continue;
				}
				for (var i = 0; i < morphs.Count; i++)
				{
					if (i > 0)
					{
						var followingTxt = GetItemText(morphs[i], "txt", boundaryLang) ?? string.Empty;
						sb.Append(followingTxt.StartsWith("=") ? "=" : "-");
					}
					sb.Append(EmitGlossOrPlaceholder(GetItemText(morphs[i], "gls", analysisLang)));
				}
			}
			return sb.ToString();
		}

		private static string EmitGlossOrPlaceholder(string gls)
		{
			return string.IsNullOrWhiteSpace(gls) ? "{}" : InterlinearTemplateEscaping.EscapeToken(gls);
		}

		private static string GetItemText(XElement parent, string type, string lang)
		{
			var item = parent.Elements("item").FirstOrDefault(e =>
				(string)e.Attribute("type") == type && (lang == null || (string)e.Attribute("lang") == lang));
			return item?.Value;
		}
	}
}

// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// The closed set of fields a "LaTeX Interlinear for Publication" template may reference,
	/// each written in template text as a <c>{{name}}</c> placeholder (see
	/// <see cref="InterlinearTemplateFields"/> for the name mapping).
	/// </summary>
	public enum InterlinearTemplateField
	{
		WordsSource,
		WordsIpa,
		WordsTransliteration,
		MorphemesSource,
		MorphemesIpa,
		MorphemesTransliteration,
		Gloss,
		FreeTranslation,
		LiteralTranslation,
		Note
	}

	/// <summary>
	/// The name<->field mapping for the closed placeholder vocabulary, plus the groupings the
	/// validator and the export pipeline both need (which fields share one underlying writing
	/// system-selectable source, and which are mandatory).
	/// </summary>
	public static class InterlinearTemplateFields
	{
		private static readonly Dictionary<string, InterlinearTemplateField> NameToField = new Dictionary<string, InterlinearTemplateField>
		{
			{ "words_source", InterlinearTemplateField.WordsSource },
			{ "words_ipa", InterlinearTemplateField.WordsIpa },
			{ "words_transliteration", InterlinearTemplateField.WordsTransliteration },
			{ "morphemes_source", InterlinearTemplateField.MorphemesSource },
			{ "morphemes_ipa", InterlinearTemplateField.MorphemesIpa },
			{ "morphemes_transliteration", InterlinearTemplateField.MorphemesTransliteration },
			{ "gloss", InterlinearTemplateField.Gloss },
			{ "free_translation", InterlinearTemplateField.FreeTranslation },
			{ "literal_translation", InterlinearTemplateField.LiteralTranslation },
			{ "note", InterlinearTemplateField.Note }
		};

		/// <summary>Word-level, vernacular-writing-system-selectable fields (words_source is
		/// mandatory; the rest are optional companions, LT-22712).</summary>
		public static readonly IReadOnlyList<InterlinearTemplateField> WordsGroup = new[]
		{
			InterlinearTemplateField.WordsSource, InterlinearTemplateField.WordsIpa, InterlinearTemplateField.WordsTransliteration
		};

		/// <summary>Morpheme-level, vernacular-writing-system-selectable fields; at least one is
		/// always required.</summary>
		public static readonly IReadOnlyList<InterlinearTemplateField> MorphemesGroup = new[]
		{
			InterlinearTemplateField.MorphemesSource, InterlinearTemplateField.MorphemesIpa, InterlinearTemplateField.MorphemesTransliteration
		};

		/// <summary>Segment-level fields that resolve against the per-export analysis-language
		/// choice.</summary>
		public static readonly IReadOnlyList<InterlinearTemplateField> AnalysisLanguageGroup = new[]
		{
			InterlinearTemplateField.Gloss, InterlinearTemplateField.FreeTranslation,
			InterlinearTemplateField.LiteralTranslation, InterlinearTemplateField.Note
		};

		public static bool TryGetField(string placeholderName, out InterlinearTemplateField field)
		{
			return NameToField.TryGetValue(placeholderName, out field);
		}

		public static string NameOf(InterlinearTemplateField field)
		{
			return NameToField.First(kvp => kvp.Value == field).Key;
		}
	}

	/// <summary>
	/// Validates a template's <c>{{name}}</c> placeholders against the closed vocabulary and the
	/// LT-22712 mandatory-field rules. Deliberately does not attempt to validate the surrounding
	/// LaTeX itself -- per the ticket, the validator's job is that "the {{entry}} fields are
	/// correct," not that the template is good LaTeX.
	/// </summary>
	public static class InterlinearTemplateValidator
	{
		private static readonly Regex PlaceholderPattern = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

		/// <returns>Empty if the template is valid; otherwise one message per problem
		/// found.</returns>
		public static IReadOnlyList<string> Validate(string templateText)
		{
			var errors = new List<string>();
			var body = InterlinearTemplateHelpText.RemoveHelpText(templateText);
			var usedFields = new HashSet<InterlinearTemplateField>();
			foreach (Match match in PlaceholderPattern.Matches(body))
			{
				var name = match.Groups[1].Value;
				if (InterlinearTemplateFields.TryGetField(name, out var field))
					usedFields.Add(field);
				else
					errors.Add($"Unknown placeholder {{{{{name}}}}}.");
			}

			if (!usedFields.Contains(InterlinearTemplateField.WordsSource))
				errors.Add("Template must include {{words_source}}.");
			if (!usedFields.Overlaps(InterlinearTemplateFields.MorphemesGroup))
				errors.Add("Template must include at least one of {{morphemes_source}}, {{morphemes_ipa}}, {{morphemes_transliteration}}.");
			if (!usedFields.Contains(InterlinearTemplateField.Gloss))
				errors.Add("Template must include {{gloss}}.");

			return errors;
		}

		public static bool IsValid(string templateText)
		{
			return Validate(templateText).Count == 0;
		}
	}

	/// <summary>
	/// The already-assembled, already-aligned, already-escaped data for one interlinear entry
	/// (one phrase/segment), keyed by the same closed vocabulary the template placeholders use.
	/// A field with no value for this entry (not selected by the template, or genuinely absent
	/// in the source data) is simply missing from the dictionary.
	/// </summary>
	public sealed class InterlinearTemplateEntry
	{
		private readonly Dictionary<InterlinearTemplateField, string> m_values = new Dictionary<InterlinearTemplateField, string>();

		public void Set(InterlinearTemplateField field, string value)
		{
			m_values[field] = value;
		}

		public string Get(InterlinearTemplateField field)
		{
			return m_values.TryGetValue(field, out var value) ? value : string.Empty;
		}
	}

	/// <summary>
	/// Resolves a template's placeholders against one entry's data: plain substitution, plus the
	/// "a line whose only dynamic content is empty disappears entirely" rule that lets an entry
	/// with no literal translation (say) not leave a stray "\glt  \\" line behind.
	/// </summary>
	public static class InterlinearTemplateResolver
	{
		private static readonly Regex PlaceholderPattern = new Regex(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

		public static string Resolve(string templateText, InterlinearTemplateEntry entry)
		{
			var body = InterlinearTemplateHelpText.RemoveHelpText(templateText);
			var lines = body.Replace("\r\n", "\n").Split('\n');
			var resultLines = new List<string>(lines.Length);
			foreach (var line in lines)
			{
				var matches = PlaceholderPattern.Matches(line);
				if (matches.Count > 0 && matches.Cast<Match>().All(m => IsEmptyPlaceholder(m, entry)))
					continue; // every placeholder on this line resolved empty: drop the whole line, static text included.
				resultLines.Add(PlaceholderPattern.Replace(line, m => ResolveOne(m, entry)));
			}
			return string.Join("\n", resultLines);
		}

		// An unknown placeholder counts as non-empty (it renders as its own literal {{name}}
		// text),
		// so a still-being-edited template with a typo doesn't silently lose whole lines in
		// preview.
		private static bool IsEmptyPlaceholder(Match match, InterlinearTemplateEntry entry)
		{
			return InterlinearTemplateFields.TryGetField(match.Groups[1].Value, out var field) && string.IsNullOrWhiteSpace(entry.Get(field));
		}

		private static string ResolveOne(Match match, InterlinearTemplateEntry entry)
		{
			return InterlinearTemplateFields.TryGetField(match.Groups[1].Value, out var field) ? entry.Get(field) : match.Value;
		}
	}

	/// <summary>
	/// A template file ends with an optional, never-exported help block explaining the
	/// placeholder vocabulary to whoever edits the template. Everything from and including the
	/// sentinel line onward is stripped before validation or resolution ever sees the text.
	/// </summary>
	public static class InterlinearTemplateHelpText
	{
		public const string Sentinel = "%% ===== Template help (not exported below this line) ===== %%";

		public static string RemoveHelpText(string templateText)
		{
			var index = templateText.IndexOf(Sentinel, System.StringComparison.Ordinal);
			return index < 0 ? templateText : templateText.Substring(0, index);
		}
	}
}

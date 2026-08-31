// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using NUnit.Framework;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Unit tests for the LT-22712 "LaTeX Interlinear for Publication" template engine: closed
	/// placeholder vocabulary, mandatory-field validation, and plain-substitution resolution
	/// (including the drop-the-whole-line-when-empty rule). Pure logic, no LCM cache needed.
	/// </summary>
	[TestFixture]
	public class InterlinearTemplateTests
	{
		private const string MinimalValidTemplate =
			"\\ea\n{{words_source}} \\\\\n\\gll {{morphemes_source}} \\\\\n{{gloss}} \\\\\n\\glt {{free_translation}}\n\\z";

		[Test]
		public void Validate_MinimalValidTemplate_HasNoErrors()
		{
			Assert.That(InterlinearTemplateValidator.Validate(MinimalValidTemplate), Is.Empty);
			Assert.That(InterlinearTemplateValidator.IsValid(MinimalValidTemplate), Is.True);
		}

		[Test]
		public void Validate_UnknownPlaceholder_ReportsIt()
		{
			var errors = InterlinearTemplateValidator.Validate(MinimalValidTemplate.Replace("{{gloss}}", "{{glosss}}"));
			Assert.That(errors, Has.Some.Contains("{{glosss}}"));
		}

		[Test]
		public void Validate_MissingWordsSource_IsAnError()
		{
			var template = "\\gll {{morphemes_source}} \\\\\n{{gloss}} \\\\";
			var errors = InterlinearTemplateValidator.Validate(template);
			Assert.That(errors, Has.Some.Contains("{{words_source}}"));
		}

		[Test]
		public void Validate_MissingMorphemeForm_IsAnError()
		{
			var template = "{{words_source}} \\\\\n{{gloss}} \\\\";
			var errors = InterlinearTemplateValidator.Validate(template);
			Assert.That(errors, Has.Some.Contains("morphemes_source"));
		}

		[Test]
		public void Validate_MissingGloss_IsAnError()
		{
			var template = "{{words_source}} \\\\\n{{morphemes_ipa}} \\\\";
			var errors = InterlinearTemplateValidator.Validate(template);
			Assert.That(errors, Has.Some.Contains("{{gloss}}"));
		}

		[Test]
		public void Validate_AnyOneMorphemeFormSatisfiesTheRequirement()
		{
			foreach (var name in new[] { "morphemes_source", "morphemes_ipa", "morphemes_transliteration" })
			{
				var template = $"{{{{words_source}}}} \\\\\n{{{{{name}}}}} \\\\\n{{{{gloss}}}} \\\\";
				Assert.That(InterlinearTemplateValidator.IsValid(template), Is.True, $"template using only {name} should be valid");
			}
		}

		[Test]
		public void Validate_AllOptionalFieldsTogether_IsStillValid()
		{
			var template = string.Join(" \\\\\n", new[]
			{
				"{{words_source}}", "{{words_ipa}}", "{{words_transliteration}}",
				"{{morphemes_source}}", "{{morphemes_ipa}}", "{{morphemes_transliteration}}",
				"{{gloss}}", "{{free_translation}}", "{{literal_translation}}", "{{note}}"
			});
			Assert.That(InterlinearTemplateValidator.IsValid(template), Is.True);
		}

		[Test]
		public void Validate_IgnoresPlaceholdersInsideHelpText()
		{
			var template = MinimalValidTemplate + "\n" + InterlinearTemplateHelpText.Sentinel + "\n{{not_a_real_field}}";
			Assert.That(InterlinearTemplateValidator.IsValid(template), Is.True);
		}

		private static InterlinearTemplateEntry BuildEntry(params (InterlinearTemplateField Field, string Value)[] values)
		{
			var entry = new InterlinearTemplateEntry();
			foreach (var (field, value) in values)
				entry.Set(field, value);
			return entry;
		}

		[Test]
		public void Resolve_SubstitutesEveryPlaceholder()
		{
			var entry = BuildEntry(
				(InterlinearTemplateField.WordsSource, "na-g-red1-Curu-tahob"),
				(InterlinearTemplateField.MorphemesSource, "na g red1 Curu tahob"),
				(InterlinearTemplateField.Gloss, "del imperf ATN cover"),
				(InterlinearTemplateField.FreeTranslation, "They sinned against their patron."));

			var resolved = InterlinearTemplateResolver.Resolve(MinimalValidTemplate, entry);

			Assert.That(resolved, Does.Contain("na-g-red1-Curu-tahob"));
			Assert.That(resolved, Does.Contain("del imperf ATN cover"));
			Assert.That(resolved, Does.Contain("They sinned against their patron."));
			Assert.That(resolved, Does.Not.Contain("{{"));
		}

		[Test]
		public void Resolve_LineWithOnlyAnEmptyPlaceholder_IsDroppedEntirely()
		{
			var template = MinimalValidTemplate + "\n{{note}} \\\\";
			var entry = BuildEntry(
				(InterlinearTemplateField.WordsSource, "x"),
				(InterlinearTemplateField.MorphemesSource, "x"),
				(InterlinearTemplateField.Gloss, "x"),
				(InterlinearTemplateField.FreeTranslation, "x"));
			// note is deliberately left unset (empty).

			var resolved = InterlinearTemplateResolver.Resolve(template, entry);

			Assert.That(resolved, Does.Not.Contain("note"));
			var lines = resolved.Split('\n');
			Assert.That(lines.Length, Is.EqualTo(MinimalValidTemplate.Split('\n').Length), "the note-only line should vanish, not just its placeholder");
		}

		[Test]
		public void Resolve_LineWithStaticTextAndOnlyEmptyPlaceholders_DropsTheStaticTextToo()
		{
			var entry = BuildEntry(
				(InterlinearTemplateField.WordsSource, "x"),
				(InterlinearTemplateField.MorphemesSource, "x"),
				(InterlinearTemplateField.Gloss, "x"));
			// free_translation deliberately left unset.

			var resolved = InterlinearTemplateResolver.Resolve(MinimalValidTemplate, entry);

			Assert.That(resolved, Does.Not.Contain("\\glt"));
		}

		[Test]
		public void Resolve_UnknownPlaceholder_IsLeftLiteralAndKeepsItsLineAlive()
		{
			var template = "{{typo_field}} \\\\";
			var resolved = InterlinearTemplateResolver.Resolve(template, new InterlinearTemplateEntry());
			Assert.That(resolved, Does.Contain("{{typo_field}}"));
		}

		[Test]
		public void Resolve_StripsHelpTextBeforeResolving()
		{
			var template = MinimalValidTemplate + "\n" + InterlinearTemplateHelpText.Sentinel + "\nThis is only for humans, not LaTeX.";
			var entry = BuildEntry(
				(InterlinearTemplateField.WordsSource, "x"),
				(InterlinearTemplateField.MorphemesSource, "x"),
				(InterlinearTemplateField.Gloss, "x"));

			var resolved = InterlinearTemplateResolver.Resolve(template, entry);

			Assert.That(resolved, Does.Not.Contain("only for humans"));
		}

		[Test]
		public void NameOf_RoundTripsWithTryGetField()
		{
			foreach (InterlinearTemplateField field in System.Enum.GetValues(typeof(InterlinearTemplateField)))
			{
				var name = InterlinearTemplateFields.NameOf(field);
				Assert.That(InterlinearTemplateFields.TryGetField(name, out var roundTripped), Is.True);
				Assert.That(roundTripped, Is.EqualTo(field));
			}
		}
	}
}

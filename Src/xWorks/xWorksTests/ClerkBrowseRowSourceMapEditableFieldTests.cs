// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Unit coverage for the browse adapter's transduce→edit-field allowlist (the pure decision behind
	/// which lexicon-browse cells are inline-editable), now owned by the typed <see cref="BrowseColumnEditSpec"/>.
	/// The contract is data-safety: only entry-anchored writes the delegating edit context supports are
	/// enabled (lexeme Form, primary-sense Gloss); every other column — Definition, citation form,
	/// arbitrary sense paths — maps to null and stays read-only rather than risk writing the wrong object.
	/// </summary>
	[TestFixture]
	public class ClerkBrowseRowSourceMapEditableFieldTests
	{
		[TestCase("Form")]
		[TestCase("Gloss")]
		[TestCase("MorphType")]
		public void ExplicitSupportedField_PassesThrough(string field)
			=> Assert.That(BrowseColumnEditSpec.MapEditableField(field, null), Is.EqualTo(field));

		[Test]
		public void LexemeFormTransduce_MapsToForm()
			=> Assert.That(BrowseColumnEditSpec.MapEditableField(null, "LexEntry.LexemeForm.Form"),
				Is.EqualTo("Form"));

		[Test]
		public void SenseGlossTransduce_MapsToGloss()
			=> Assert.That(BrowseColumnEditSpec.MapEditableField(null, "LexSense.Gloss"),
				Is.EqualTo("Gloss"));

		[TestCase("LexSense.Definition")]   // edit context has no Definition write
		[TestCase("LexEntry.CitationForm")] // headword editing not yet wired safely
		[TestCase("LexSense.ScientificName")]
		[TestCase("LexEtymology.Form")]
		public void UnsupportedTransduce_MapsToNull_StaysReadOnly(string transduce)
			=> Assert.That(BrowseColumnEditSpec.MapEditableField(null, transduce), Is.Null);

		[Test]
		public void NoFieldAndNoTransduce_MapsToNull()
			=> Assert.That(BrowseColumnEditSpec.MapEditableField(null, null), Is.Null);

		// ----- the typed value object that replaces the (field, ws, transduce) out-param trio -----

		[Test]
		public void FromColumnAttributes_SupportedTransduce_IsEditable_CarriesTypedTokens()
		{
			var spec = BrowseColumnEditSpec.FromColumnAttributes(null, "$ws=vernacular", "LexEntry.LexemeForm.Form");
			Assert.That(spec.IsEditable, Is.True);
			Assert.That(spec.EditField, Is.EqualTo("Form"));
			Assert.That(spec.WritingSystemTag, Is.EqualTo("vern"), "the magic ws spec normalizes to the vern alias");
			Assert.That(spec.Transduce, Is.EqualTo("LexEntry.LexemeForm.Form"), "the raw transduce is preserved for diagnostics");
		}

		[Test]
		public void FromColumnAttributes_UnsupportedColumn_NotEditable_ButStillNormalizesWs()
		{
			var spec = BrowseColumnEditSpec.FromColumnAttributes(null, "$ws=analysis", "LexSense.Definition");
			Assert.That(spec.IsEditable, Is.False, "Definition has no safe entry-anchored write");
			Assert.That(spec.EditField, Is.Null);
			Assert.That(spec.WritingSystemTag, Is.EqualTo("anal"));
		}
	}
}

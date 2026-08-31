// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Exercises InterlinearTemplateEntryBuilder's ported token-alignment/escaping logic against
	/// a hand-built flextext-shaped XDocument, independent of any real text data -- the multi-
	/// writing-system selection and alignment rules are the riskiest part of LT-22712's pipeline,
	/// and no existing XML test fixture in this project has rich enough word/morpheme content to
	/// exercise them (see InterlinearTemplateExporterTests, which only checks the pipeline wiring
	/// against a fixture that happens to have no words at all).
	/// </summary>
	[TestFixture]
	public class InterlinearTemplateEntryBuilderTests : InterlinearTestBase
	{
		private const string SourceIcu = "qaa-x-kal";
		private const string IpaIcu = "qaa-fonipa-x-kal";
		private const string AnalysisIcu = "en";

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			CoreWritingSystemDefinition wsSource;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet(SourceIcu, out wsSource);

			// The entry builder resolves words_source/morphemes_source against the project's
			// default vernacular writing system, so this fixture needs SourceIcu to actually be
			// that default, same as in a real project.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(wsSource);
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Insert(0, wsSource);
			});
		}

		private XDocument BuildSamplePhrase()
		{
			// Word 2's IPA form is built from numeric code points, not typed literally, so this
			// source file never carries a raw zero-width space or replacement character itself.
			var corruptIpaForm = "na-t" + (char)0x200B + "u" + (char)0xFFFD + "gu";

			// Word 1 is unanalyzed; word 2 has two morphemes (one missing a gloss) and its IPA
			// form needs cleaning; its second morpheme's IPA form needs brace-grouping.
			return XDocument.Parse($@"
<document>
  <interlinear-text>
    <item type=""title"" lang=""{SourceIcu}"">Sample</item>
    <paragraphs><paragraph><phrases><phrase>
      <item type=""gls"" lang=""{AnalysisIcu}"">The dog ran.</item>
      <item type=""lit"" lang=""{AnalysisIcu}"">dog-the run-past.</item>
      <item type=""note"" lang=""{AnalysisIcu}"">a note</item>
      <words>
        <word>
          <item type=""txt"" lang=""{SourceIcu}"">asu</item>
          <item type=""txt"" lang=""{IpaIcu}"">asu</item>
        </word>
        <word>
          <item type=""txt"" lang=""{SourceIcu}"">na-tugu</item>
          <item type=""txt"" lang=""{IpaIcu}"">{corruptIpaForm}</item>
          <morphemes>
            <morph>
              <item type=""txt"" lang=""{SourceIcu}"">na-</item>
              <item type=""txt"" lang=""{IpaIcu}"">na-</item>
              <item type=""gls"" lang=""{AnalysisIcu}"">past</item>
            </morph>
            <morph>
              <item type=""txt"" lang=""{SourceIcu}"">tugu</item>
              <item type=""txt"" lang=""{IpaIcu}"">go up</item>
              <item type=""gls"" lang=""{AnalysisIcu}""></item>
            </morph>
          </morphemes>
        </word>
        <word>
          <item type=""punct"">.</item>
        </word>
      </words>
    </phrase></phrases></paragraph></paragraphs>
  </interlinear-text>
</document>");
		}

		private InterlinearTemplateEntry BuildEntry(System.Collections.Generic.HashSet<InterlinearTemplateField> usedFields)
		{
			CoreWritingSystemDefinition wsIpa;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet(IpaIcu, out wsIpa);
			var wsEng = Cache.ServiceLocator.WritingSystemManager.Get(AnalysisIcu);
			var wsMapping = new InterlinearTemplateWritingSystemMapping { IpaIcuCode = IpaIcu };

			return InterlinearTemplateEntryBuilder.BuildEntries(BuildSamplePhrase(), Cache, usedFields, wsMapping, wsEng.Handle).Single();
		}

		private static System.Collections.Generic.HashSet<InterlinearTemplateField> AllFields()
		{
			return new System.Collections.Generic.HashSet<InterlinearTemplateField>((System.Collections.Generic.IEnumerable<InterlinearTemplateField>)
				System.Enum.GetValues(typeof(InterlinearTemplateField)));
		}

		[Test]
		public void WordsSource_OneTokenPerWord_PunctuationAttachesWithNoSpace()
		{
			var entry = BuildEntry(AllFields());
			Assert.That(entry.Get(InterlinearTemplateField.WordsSource), Is.EqualTo("asu na-tugu."));
		}

		[Test]
		public void WordsIpa_CleansZeroWidthSpaceAndReplacementCharacter()
		{
			var entry = BuildEntry(AllFields());
			// U+200B dropped, U+FFFD -> '?'; "na-tu?gu" contains no space so it is not
			// brace-grouped.
			Assert.That(entry.Get(InterlinearTemplateField.WordsIpa), Is.EqualTo("asu na-tu?gu."));
		}

		[Test]
		public void MorphemesSource_UnanalyzedWordFallsBackToItsOwnForm_ConcatenatesAnalyzedWordsMorphs()
		{
			var entry = BuildEntry(AllFields());
			Assert.That(entry.Get(InterlinearTemplateField.MorphemesSource), Is.EqualTo("asu na-tugu."));
		}

		[Test]
		public void MorphemesIpa_BraceGroupsAMultiWordMorphForm()
		{
			var entry = BuildEntry(AllFields());
			Assert.That(entry.Get(InterlinearTemplateField.MorphemesIpa), Is.EqualTo("asu na-{go up}."));
		}

		[Test]
		public void Gloss_UsesBoundaryMarkerFromSourceMorphemesAndPlaceholdersAMissingGloss()
		{
			var entry = BuildEntry(AllFields());
			Assert.That(entry.Get(InterlinearTemplateField.Gloss), Is.EqualTo("{} past-{}"));
		}

		[Test]
		public void FreeformFields_AreEscapedProseFromThePhraseLevelItems()
		{
			var entry = BuildEntry(AllFields());
			Assert.That(entry.Get(InterlinearTemplateField.FreeTranslation), Is.EqualTo("The dog ran."));
			Assert.That(entry.Get(InterlinearTemplateField.LiteralTranslation), Is.EqualTo("dog-the run-past."));
			Assert.That(entry.Get(InterlinearTemplateField.Note), Is.EqualTo("a note"));
		}

		[Test]
		public void UnrequestedFields_AreNotPopulated()
		{
			var usedFields = new System.Collections.Generic.HashSet<InterlinearTemplateField>
			{
				InterlinearTemplateField.WordsSource, InterlinearTemplateField.MorphemesSource, InterlinearTemplateField.Gloss
			};
			var entry = BuildEntry(usedFields);
			Assert.That(entry.Get(InterlinearTemplateField.WordsIpa), Is.Empty);
			Assert.That(entry.Get(InterlinearTemplateField.FreeTranslation), Is.Empty);
		}
	}
}

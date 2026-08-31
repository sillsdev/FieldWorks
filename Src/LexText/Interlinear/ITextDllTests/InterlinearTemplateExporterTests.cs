// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using NUnit.Framework;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// End-to-end check that LT-22712's pipeline (line-choices builder,
	/// InterlinearExporter/InterlinVc, entry builder, resolver) runs against a real LCM cache and
	/// produces a well-formed document. InterlinearExporterTests.xml (this fixture's text data)
	/// has phrases but no actual word content, so this only proves the wiring runs cleanly end to
	/// end; InterlinearTemplateEntryBuilderTests covers the token-alignment/escaping rules
	/// themselves against hand-built word/morpheme data.
	/// </summary>
	[TestFixture]
	public class InterlinearTemplateExporterTests : InterlinearExporterTestsBase
	{
		private const string QaaXKal = "qaa-x-kal";
		private int m_wsEngHandle;

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			CoreWritingSystemDefinition wsXkal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet(QaaXKal, out wsXkal);

			// words_source resolves against the project's default vernacular writing system, so
			// qaa-x-kal needs to actually be that default here, same as in a real project.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(wsXkal);
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Insert(0, wsXkal);
			});
		}

		[SetUp]
		public void BeforeEachTest()
		{
			CoreWritingSystemDefinition wsXkal;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet(QaaXKal, out wsXkal);
			var wsEng = Cache.ServiceLocator.WritingSystemManager.Get("en");
			m_wsEngHandle = wsEng.Handle;
			m_text1 = SetupDataForText1();
			m_choices = new InterlinLineChoices(Cache.LanguageProject, wsXkal.Handle, wsEng.Handle);
		}

		[Test]
		public void Export_DefaultTemplate_ProducesAWellFormedDocument()
		{
			var result = InterlinearTemplateExporter.Export(Cache, new[] { m_text1.ContentsOA },
				InterlinearTemplateDefault.Text, new InterlinearTemplateWritingSystemMapping(), m_wsEngHandle);

			Assert.That(result, Does.Contain("\\documentclass"));
			Assert.That(result, Does.Contain("\\begin{document}"));
			Assert.That(result, Does.Contain("\\end{document}"));
			Assert.That(result, Does.Contain("\\ea"));
			Assert.That(result, Does.Contain("\\z"));
			Assert.That(result, Does.Not.Contain("{{"), "every placeholder should have been resolved");
			Assert.That(result, Does.Contain("LaTeX Interlinear for Publication export."));
			Assert.That(result, Does.Contain("Test Text1 for InterlinearExporterTexts"), "the text's title should appear as a comment");
		}

		[Test]
		public void PreviewFirstEntry_ReturnsOneResolvedEaBlock()
		{
			var preview = InterlinearTemplateExporter.PreviewFirstEntry(Cache, m_text1.ContentsOA,
				InterlinearTemplateDefault.Text, new InterlinearTemplateWritingSystemMapping(), m_wsEngHandle);

			Assert.That(preview, Does.Contain("\\ea"));
			Assert.That(preview, Does.Contain("\\z"));
			Assert.That(preview, Does.Not.Contain("{{"));
		}

		[Test]
		public void Export_InvalidTemplate_Throws()
		{
			Assert.That(() => InterlinearTemplateExporter.Export(Cache, new[] { m_text1.ContentsOA },
					"{{gloss}}", new InterlinearTemplateWritingSystemMapping(), m_wsEngHandle),
				Throws.InvalidOperationException);
		}
	}
}

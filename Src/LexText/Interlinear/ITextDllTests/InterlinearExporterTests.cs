using System;
using System.Collections;
using System.Xml;
using System.IO;
using System.Xml.Schema;
using Palaso.TestUtilities;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// </summary>
	[TestFixture]
	public class InterlinearExporterTestsBase : InterlinearTestBase
	{
		protected FDO.IText m_text1;
		protected InterlinLineChoices m_choices;
		private XmlDocument m_textsDefn;


		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_textsDefn = new XmlDocument();
		}

		protected virtual FDO.IText SetupDataForText1()
		{
			return LoadTestText("LexText/Interlinear/ITextDllTests/InterlinearExporterTests.xml", 1, m_textsDefn);
		}

		[TearDown]
		public void Exit()
		{
			m_text1 = null;
			m_choices = null;
		}

		protected XmlDocument ExportToXml(string mode)
		{
			XmlDocument exportedXml = new XmlDocument();
			using (var vc = new InterlinVc(Cache))
			using (var stream = new MemoryStream())
			using (var writer = new XmlTextWriter(stream, System.Text.Encoding.UTF8))
			{
				var exporter = InterlinearExporter.Create(mode, Cache, writer, m_text1.ContentsOA, m_choices, vc);
				exporter.WriteBeginDocument();
				exporter.ExportDisplay();
				exporter.WriteEndDocument();
				writer.Flush();
				stream.Seek(0, SeekOrigin.Begin);
				exportedXml.Load(stream);
			}
#pragma warning disable 219
			string xml = exportedXml.InnerXml;
#pragma warning restore 219
			return exportedXml;
		}

		protected XmlDocument ExportToXml()
		{
			return ExportToXml("xml");
		}
		public class InterlinearExporterTests : InterlinearExporterTestsBase
		{
			[SetUp]
			public void BeforeEachTest()
			{
				m_text1 = SetupDataForText1();
				m_choices = new InterlinLineChoices(Cache.LanguageProject, Cache.DefaultVernWs, Cache.DefaultAnalWs);
			}

			[TearDown]
			public void AfterEachTest()
			{
				Cache.LanguageProject.TextsOC.Remove(m_text1);
			}

			[Test]
			public void AlternateCaseAnalyses_Baseline_LT5385()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);

				ParagraphAnnotator pa = new ParagraphAnnotator(m_text1.ContentsOA[0]);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml();
				AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//paragraph");
				AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//word/item[@type=\"txt\" and text()=\"Xxxpus\"]");

				//// Set alternate case endings.
				ParagraphAnnotator ta = new ParagraphAnnotator(m_text1.ContentsOA[0]);
				string altCaseForm;
				ta.SetAlternateCase(0, 0, StringCaseStatus.allLower, out altCaseForm);
				ta.ReparseParagraph();
				Assert.AreEqual("xxxpus", altCaseForm);
				exportedDoc = ExportToXml();
				AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//word/item[@type=\"txt\" and text()=\"Xxxpus\"]");
			}

			[Test]
			public void ExportBasicInformation()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries);
				m_choices.Add(InterlinLineChoices.kflidLexGloss);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml();

				string formLexEntry = "went";
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "go.PST", null);
				pa.BreakIntoMorphs(0, 1, new ArrayList{ leGo.LexemeFormOA });
				pa.SetMorphSense(0, 1, 0, leGo.SensesOS[0]);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//word[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='cf']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='go.PST']", 1);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='variantTypes']", 0);
			}

			[Test]
			public void ExportVariantTypeInformation_LT9374()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries);
				m_choices.Add(InterlinLineChoices.kflidLexGloss);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml();

				AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("//morph/item[@type=\"cf\"]");
				string formLexEntry = "go";

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "go.PST", null);
				var freeVarType =
					Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypFreeVar);
				freeVarType.ReverseAbbr.SetAnalysisDefaultWritingSystem("fr. var.");
				pa.SetVariantOf(0, 1, leGo, freeVarType);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//word[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='cf']='go']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='variantTypes']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='variantTypes']='+fr. var.']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='go.PST']", 1);
			}

			[Test]
			[Ignore("to enable soon.")]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsAppend()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries);
				m_choices.Add(InterlinLineChoices.kflidLexGloss);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml();

				AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("//morph/item[@type=\"cf\"]");
				string formLexEntry = "go";

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "glossgo", null);
				var pastVar =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPastVar);
				pastVar.GlossAppend.SetAnalysisDefaultWritingSystem(".pst");

				pa.SetVariantOf(0, 1, leGo, pastVar);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='cf']='go']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='glossgo']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='glsAppend']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='glsAppend']='.pst']", 1);

				// variantTypes item isn't really needed since it is empty, but don't see any harm in keeping it.
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='variantTypes']='']", 1);
			}

			[Test]
			[Ignore("to enable soon.")]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsPrepend()
			{
			}


			[Test]
			public void ValidateExport()
			{
				//NOTE: The new test paragraphs need to have all new words w/o duplicates so we can predict the guesses
				//xxxcrayzee xxxyouneek xxxsintents.

				// copy a text of first paragraph into a new paragraph to generate guesses.


				// collect expected guesses from the glosses in the first paragraph.


				// then verify we've created guesses for the new text.

				// export the paragraph and test the Display results

			}

		}

		public class InterlinearExporterTestsForELAN : InterlinearExporterTestsBase
		{
			private string recGuid;

			protected override FDO.IText SetupDataForText1()
			{
				Cache.LanguageProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();

				//person not found create one and add it.
				var newPerson = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
				Cache.LanguageProject.PeopleOA.PossibilitiesOS.Add(newPerson);
				newPerson.Name.set_String(Cache.DefaultVernWs, "Hiro Protaganist");

				FDO.IText text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				Cache.LangProject.TextsOC.Add(text);
				text.ContentsOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				text.MediaFilesOA = Cache.ServiceLocator.GetInstance<ICmMediaContainerFactory>().Create();
				ICmMediaURI recording = Cache.ServiceLocator.GetInstance<ICmMediaURIFactory>().Create();
				text.MediaFilesOA.MediaURIsOC.Add(recording);
				recGuid = recording.Guid.ToString();
				IStTxtPara para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(text.ContentsOA, 0, "special");
				para.Contents = Cache.ServiceLocator.GetInstance<ITsStrFactory>().MakeString("This is a text. It has two segments.", Cache.LanguageProject.DefaultVernacularWritingSystem.Handle);
				ISegment seg = para.SegmentsOS[0];
				seg.BeginTimeOffset = "Timeslot 1";
				seg.EndTimeOffset = "Timeslot 2";
				seg.MediaURIRA = recording;
				seg.SpeakerRA = newPerson;
				return text;
			}

			[SetUp]
			public void BeforeEachTest()
			{
				m_text1 = SetupDataForText1();
				m_choices = new InterlinLineChoices(Cache.LanguageProject, Cache.DefaultVernWs, Cache.DefaultAnalWs);
			}

			[TearDown]
			public void AfterEachTest()
			{
				Cache.LanguageProject.TextsOC.Remove(m_text1);
			}

			[Test]
			public void ValidateMultipleTitlesAndAbbreviations()
			{
				m_text1.Name.set_String(Cache.WritingSystemFactory.GetWsFromStr("en"), "english");
				m_text1.Name.set_String(Cache.WritingSystemFactory.GetWsFromStr("fr"), "french");
				m_text1.Abbreviation.set_String(Cache.WritingSystemFactory.GetWsFromStr("en"), "english");
				m_text1.Abbreviation.set_String(Cache.WritingSystemFactory.GetWsFromStr("fr"), "french");
				XmlDocument exportedDoc = ExportToXml();
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath("//interlinear-text/item[@type=\"title\"]", 2);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath("//interlinear-text/item[@type=\"title-abbreviation\"]", 2);
			}

			[Test]
			public void ValidateMultipleSources()
			{
				m_text1.Source.set_String(Cache.WritingSystemFactory.GetWsFromStr("en"), "english");
				m_text1.Source.set_String(Cache.WritingSystemFactory.GetWsFromStr("fr"), "french");
				XmlDocument exportedDoc = ExportToXml();
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath("//interlinear-text/item[@type=\"source\"]", 2);
			}

			[Test]
			public void ValidateMultipleComments()
			{
				m_text1.Description.set_String(Cache.WritingSystemFactory.GetWsFromStr("en"), "english");
				m_text1.Description.set_String(Cache.WritingSystemFactory.GetWsFromStr("fr"), "french");
				XmlDocument exportedDoc = ExportToXml();
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath("//interlinear-text/item[@type=\"comment\"]", 2);
			}

			/// <summary>
			/// Create two paragraphs with two identical sentences. The first paragraph has real analyses, the second has only guesses.
			/// Validate that the guids for each paragraph, and each phrase and word annotation are unique.
			/// </summary>
			[Test]
			[Platform(Exclude = "Linux", Reason = "Need to fix validation (FWNX-852)")]
			public void ValidateELANExport()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries);
				m_choices.Add(InterlinLineChoices.kflidLexGloss);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				// create two paragraphs with two identical sentences.
				ParagraphAnnotator pa = new ParagraphAnnotator((IStTxtPara)m_text1.ContentsOA.ParagraphsOS[0]);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml("elan");

				//validate export xml against schema
				var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };

				string p = Path.Combine(DirectoryFinder.FlexFolder, Path.Combine("Export Templates", "Interlinear"));
				string file = Path.Combine(p, "FlexInterlinear.xsd");
				settings.Schemas.Add("", file);
				exportedDoc.Schemas.Add("", new Uri(file).AbsoluteUri);

				//validate export against schema.
				Assert.DoesNotThrow(() =>
				{
					exportedDoc.Validate(DontIgnore);
				});

				//validate segment reference to MediaURI
				AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//phrase[@media-file=\"" + recGuid + "\"]");
				AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//media-files/media[@guid=\"" + recGuid + "\"]");
				AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//phrase[@speaker=\"Hiro Protaganist\"]");

				//validate guid present on Interlin-text, phrase, word, and morpheme
				AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("document/interlinear-text[not(@guid)]");
				AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("//phrase[not(@guid)]");
				AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("//word[not(@guid) and not(item[@type=\"punct\"])]");
				AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("//morpheme[not(@guid)]");
			}

			private void DontIgnore(object sender, ValidationEventArgs e)
			{
				throw e.Exception;
			}
		}
	}
}

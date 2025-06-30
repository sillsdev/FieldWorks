// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.PlatformUtilities;
using SIL.TestUtilities;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// </summary>
	[TestFixture]
	public class InterlinearExporterTestsBase : InterlinearTestBase
	{
		protected LCModel.IText m_text1;
		protected InterlinLineChoices m_choices;
		private XmlDocument m_textsDefn;
		private const string QaaXKal = "qaa-x-kal";


		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_textsDefn = new XmlDocument();
		}

		protected virtual LCModel.IText SetupDataForText1()
		{
			string path = Path.Combine(FwDirectoryFinder.SourceDirectory, "LexText", "Interlinear", "ITextDllTests",
				"InterlinearExporterTests.xml");
			return LoadTestText(path, 1, m_textsDefn);
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
				vc.LineChoices = m_choices;
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


		public delegate T StreamFactory<T>();
		private delegate void ExtractStream<T>(T stream);

		private static void TransformDoc<T>(XmlDocument usxDocument,
			StreamFactory<T> createStream, ExtractStream<T> extractStream, string fileXsl)
			where T : Stream
		{
			const bool omitXmlDeclaration = true;
			string xslt = File.ReadAllText(fileXsl);
			var xform = new XslCompiledTransform(false);
			using (var stringReader = new StringReader(xslt))
				using (var xmlReader = XmlReader.Create(stringReader))
					xform.Load(xmlReader);

			using (var stream = createStream())
			{
				var writerSettings = new XmlWriterSettings();
				writerSettings.OmitXmlDeclaration = omitXmlDeclaration;
				if (writerSettings.OmitXmlDeclaration)
					writerSettings.ConformanceLevel = ConformanceLevel.Auto;
				using (var xmlWriter = XmlWriter.Create(stream, writerSettings))
				{
					xform.Transform(usxDocument.CreateNavigator(), new XsltArgumentList(), xmlWriter);
					xmlWriter.Flush();
					extractStream(stream);
				}
			}
		}

		private static XmlDocument TransformDoc(XmlDocument usxDocument, string fileXsl)
		{
			StreamFactory<MemoryStream> createStream = () => new MemoryStream();
			var transformedDoc = new XmlDocument();
			ExtractStream<MemoryStream> extractStream = delegate(MemoryStream stream)
															{
																stream.Seek(0, SeekOrigin.Begin);
																transformedDoc.Load(stream);
															};
			TransformDoc(usxDocument, createStream, extractStream, fileXsl);
			return transformedDoc;
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
				CoreWritingSystemDefinition wsXkal;
				Cache.ServiceLocator.WritingSystemManager.GetOrSet(QaaXKal, out wsXkal);
				var wsEng = Cache.ServiceLocator.WritingSystemManager.Get("en");
				m_text1 = SetupDataForText1();
				m_choices = new InterlinLineChoices(Cache.LanguageProject, wsXkal.Handle, wsEng.Handle);
			}

			private void DontIgnore(object sender, ValidationEventArgs e)
			{
				throw e.Exception;
			}

			[TearDown]
			public void AfterEachTest()
			{
				m_text1.Delete();
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
				ExportToXml();

				string formLexEntry = "went";
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "go.PST", null);
				pa.BreakIntoMorphs(0, 1, new ArrayList { leGo.LexemeFormOA });
				pa.SetMorphSense(0, 1, 0, leGo.SensesOS[0]);
				pa.ReparseParagraph();
				var exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//word[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='cf']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='go.PST']", 1);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='variantTypes']", 0);
			}

			[Test]
			public void ExportBasicInformation_FormSansMorph()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes, WritingSystemServices.kwsVernInParagraph);
				m_choices.Add(InterlinLineChoices.kflidLexEntries);
				m_choices.Add(InterlinLineChoices.kflidLexGloss);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				var para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				var pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				ExportToXml();

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				var formGo = "go";
				var formEn = "en";
				var tssGoForm = TsStringUtils.MakeString(formGo, wsXkal.Handle);
				var tssEnForm = TsStringUtils.MakeString(formEn, wsXkal.Handle);
				int clsidForm;
				var leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formGo, out clsidForm), tssGoForm, "go", null);
				var leEn = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formEn, out clsidForm), tssEnForm, ".PP", null);
				pa.BreakIntoMorphs(0, 2, new ArrayList{ leGo.LexemeFormOA, tssEnForm });
				pa.SetMorphSense(0, 2, 0, leGo.SensesOS[0]);
				pa.SetMorphSense(0, 2, 1, leEn.SensesOS[0]);
				pa.ReparseParagraph();
				var exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath($@"//morph[item[@type='txt' and @lang='{QaaXKal}']='en']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath($@"//word[item[@type='txt' and @lang='{QaaXKal}']='gone']", 1);
				// The guesser adds an analysis for "go".
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath($@"//morphemes[@analysisStatus='guess']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath($@"//morph[item[@type='txt' and @lang='{QaaXKal}']='go']", 2);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath($@"//morph[item[@type='txt' and @lang='{QaaXKal}']]", 3);
			}

			/// <summary>
			/// LT-13016. Exported texts were missing Notes and Literal Translation lines.
			/// </summary>
			[Test]
			public void ExportFreeLiteralNoteLines_xml()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidFreeTrans);
				m_choices.Add(InterlinLineChoices.kflidLitTrans);
				m_choices.Add(InterlinLineChoices.kflidNote);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml();

				ITsString tssFreeTranslation;
				pa.SetDefaultFreeTranslation(0, out tssFreeTranslation);
				ITsString tssLitTranslation;
				pa.SetDefaultLiteralTranslation(0, out tssLitTranslation);
				ITsString tssNote1;
				pa.SetDefaultNote(0, out tssNote1);

				var segment0 = para1.SegmentsOS[0];
				var note2 = Cache.ServiceLocator.GetInstance<INoteFactory>().Create();
				segment0.NotesOS.Add(note2);
				note2.Content.set_String(Cache.DefaultAnalWs, "second note");
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//phrase[item[@type='gls']='" + tssFreeTranslation.Text + @"']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//phrase[item[@type='lit']='" + tssLitTranslation.Text + @"']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//phrase/item[@type='note']", 2);
				Assert.That(exportedDoc.SelectSingleNode("//phrase/item[@type='note'][1]").InnerText, Is.EqualTo(tssNote1.Text));
				Assert.That(exportedDoc.SelectSingleNode("//phrase/item[@type='note'][2]").InnerText, Is.EqualTo("second note"));
			}

			/// <summary>
			/// LT-13016. Exported texts were missing Notes and Literal Translation lines.
			/// </summary>
			[Test]
			public void ExportFreeLiteralNoteLines_html()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidFreeTrans);
				m_choices.Add(InterlinLineChoices.kflidLitTrans);
				m_choices.Add(InterlinLineChoices.kflidNote);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml();

				ITsString tssFreeTranslation;
				pa.SetDefaultFreeTranslation(0, out tssFreeTranslation);
				ITsString tssLitTranslation;
				pa.SetDefaultLiteralTranslation(0, out tssLitTranslation);
				ITsString tssNote1;
				pa.SetDefaultNote(0, out tssNote1);

				var segment0 = para1.SegmentsOS[0];
				var note2 = Cache.ServiceLocator.GetInstance<INoteFactory>().Create();
				segment0.NotesOS.Add(note2);
				note2.Content.set_String(Cache.DefaultAnalWs, "second note");
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();
				var htmlDoc = TransformDocXml2Html(exportedDoc);

				AssertThatXmlIn.Dom(htmlDoc).HasSpecifiedNumberOfMatchesForXpath(@"//*[@class='itx_Freeform_gls']", 3 + 1); // "Should be 3 empty and 1 non-empty freeform Free (gloss) annotations"
				AssertThatXmlIn.Dom(htmlDoc).HasSpecifiedNumberOfMatchesForXpath(@"//*[@class='itx_Freeform_lit']", 3 + 1); // "Should be 3 empty and 1 non-empty freeform literal annotations"
				AssertThatXmlIn.Dom(htmlDoc).HasSpecifiedNumberOfMatchesForXpath(@"//*[@class='itx_Freeform_note']", 2); // "Should be 2 freeform note annotations"
				// (Free, Literal, and two Notes).
				Assert.That(htmlDoc.SelectNodes(@"//*[@class='itx_Freeform_gls']")[3].InnerText, Is.EqualTo(tssFreeTranslation.Text));
				Assert.That(htmlDoc.SelectNodes(@"//*[@class='itx_Freeform_lit']")[3].InnerText, Is.EqualTo(tssLitTranslation.Text));
				Assert.That(htmlDoc.SelectNodes(@"//*[@class='itx_Freeform_note']")[0].InnerText, Is.EqualTo(tssNote1.Text));
				Assert.That(htmlDoc.SelectNodes(@"//*[@class='itx_Freeform_note']")[1].InnerText, Is.EqualTo("second note"));
				AssertThatXmlIn.Dom(htmlDoc).HasSpecifiedNumberOfMatchesForXpath(@"//*[@class='itx_Words']", 4); // "Should be 4 phrases."
				AssertThatXmlIn.Dom(htmlDoc).HasSpecifiedNumberOfMatchesForXpath(@"//*[@class='itx_Frame_Number']", 4); // "Should be 4 phrase numbers."
				AssertThatXmlIn.Dom(htmlDoc).HasSpecifiedNumberOfMatchesForXpath(@"//*[@class='itx_Frame_Word']", 4); // "Should be 4 words (including punct)."
			}

			[Test]
			public void ExportBasicInformation_xml2html_fixMisc()
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
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "go.PST", null);
				pa.BreakIntoMorphs(0, 1, new ArrayList { leGo.LexemeFormOA });
				pa.SetMorphSense(0, 1, 0, leGo.SensesOS[0]);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();


				XmlDocument transformedDoc = TransformDocXml2Html(exportedDoc);

				// /html/body/p[4]/span[5]/table/tr/td
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//item", 0);
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//languages", 0);
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[5]/table/tr/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[5]/table/tr/td").InnerText, Is.EqualTo(".\u00A0"));

			}

			[Test]
			public void ExportBasicInformation_multipleWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				var wsEs = Cache.ServiceLocator.WritingSystemManager.Get("es");
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsEs.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsXkal.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml();

				string formLexEntry = "went";

				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "go.PST", null);
				leGo.LexemeFormOA.Form.set_String(wsEs.Handle, "eswent");
				pa.BreakIntoMorphs(0, 1, new ArrayList { leGo.LexemeFormOA });
				pa.SetMorphSense(0, 1, 0, leGo.SensesOS[0]);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//word[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='cf']", 2);
				Assert.That(exportedDoc.SelectSingleNode("//morph/item[@type='cf'][1]/text()").Value, Is.EqualTo("eswent"));
				Assert.That(exportedDoc.SelectSingleNode("//morph/item[@type='cf'][2]/text()").Value, Is.EqualTo("went"));
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='go.PST']", 1);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='variantTypes']", 0);
			}

			private void ValidateInterlinearXml(XmlDocument exportedDoc)
			{
				var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };

				string p = Path.Combine(FwDirectoryFinder.FlexFolder, Path.Combine("Export Templates", "Interlinear"));
				string file = Path.Combine(p, "FlexInterlinear.xsd");
				settings.Schemas.Add("", file);
				exportedDoc.Schemas.Add("", new Uri(file).AbsoluteUri);

				//validate export against schema.
				Assert.DoesNotThrow(() => exportedDoc.Validate(DontIgnore));
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

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "go.PST", null);
				var freeVarType =
					Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypFreeVar);
				freeVarType.ReverseAbbr.SetAnalysisDefaultWritingSystem("fr. var.");
				pa.SetVariantOf(0, 1, leGo, freeVarType);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//word[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				// The guesser adds an analysis for "go".
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath($@"//morphemes[@analysisStatus='guess']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='cf']='go']", 2);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='variantTypes']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='variantTypes']='+fr. var.']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='go.PST']", 1);
			}

			[Test]
			public void ExportVariantTypeInformation_LT9374_xml2html_multipleWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				var wsEs = Cache.ServiceLocator.WritingSystemManager.Get("es");
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsEs.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsXkal.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml();

				AssertThatXmlIn.Dom(exportedDoc).HasNoMatchForXpath("//morph/item[@type=\"cf\"]");
				string formLexEntry = "go";

				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "go.PST", null);
				string formLexEntryEs = "es" + formLexEntry;
				leGo.LexemeFormOA.Form.set_String(wsEs.Handle, formLexEntryEs);
				var freeVarType =
					Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypFreeVar);
				freeVarType.ReverseAbbr.SetAnalysisDefaultWritingSystem("fr. var.");
				pa.SetVariantOf(0, 1, leGo, freeVarType);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);

				XmlDocument transformedDoc = TransformDocXml2Html(exportedDoc);

				// NOTE: the whitespace after "+fr. var." is &#160;
				// /html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[2]/td
				// "esgo 1+fr. var.\u00A0"  has no homograph number after Ls-13615
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[2]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[2]/td").InnerText, Is.EqualTo(formLexEntryEs + "+fr. var.\u00A0"));
				//AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[2]/td/sub[@class='Interlin_Homograph']", 1);
				//Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[2]/td/sub[@class='Interlin_Homograph']").InnerText, Is.EqualTo("1"));
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[2]/td/span[@class='itx_VariantTypes']", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[2]/td/span[@class='itx_VariantTypes']").InnerText, Is.EqualTo("+fr. var."));

				// "go 1+fr. var.\u00A0"  has no homograph number after Ls-13615
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td").InnerText, Is.EqualTo(formLexEntry + "+fr. var.\u00A0"));
				//AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/sub[@class='Interlin_Homograph']", 1);
				//Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/sub[@class='Interlin_Homograph']").InnerText, Is.EqualTo("1"));
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/span[@class='itx_VariantTypes']", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/span[@class='itx_VariantTypes']").InnerText, Is.EqualTo("+fr. var."));
			}

			[Test]
			public void ExportVariantTypeInformation_LT9374_xml2OO_multipleWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				var wsEs = Cache.ServiceLocator.WritingSystemManager.Get("es");
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsEs.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsXkal.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				XmlDocument exportedDoc = ExportToXml();

				string formLexEntry = "go";

				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "go.PST", null);
				string formLexEntryEs = "es" + formLexEntry;
				leGo.LexemeFormOA.Form.set_String(wsEs.Handle, formLexEntryEs);
				var freeVarType =
					Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypFreeVar);
				freeVarType.ReverseAbbr.SetAnalysisDefaultWritingSystem("fr. var.");
				pa.SetVariantOf(0, 1, leGo, freeVarType);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);

				XmlDocument transformedDocOO = TransformDocXml2OO(exportedDoc);
				XmlNamespaceManager nsmgr = LoadNsmgrForDoc(transformedDocOO);
				// TODO: enhance AssertThatXmlIn to handle all prefixes
				//AssertThatXmlIn.Dom(transformedDocOO).HasSpecifiedNumberOfMatchesForXpath(@"/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]", 1);
				// "esgo 1+fr. var.\u00A0"  has no homograph number after Ls-13615
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]", nsmgr).InnerText, Is.EqualTo(formLexEntryEs + "+fr. var."));
				//Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]/text:span[@text:style-name='Interlin_Homograph']", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]/text:span[@text:style-name='Interlin_VariantTypes']", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]/text:span[@text:style-name='Interlin_VariantTypes']", nsmgr).InnerText, Is.EqualTo("+fr. var."));

				// "go 1+fr. var.\u00A0"  has no homograph number after Ls-13615
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]", nsmgr).InnerText, Is.EqualTo(formLexEntry + "+fr. var."));
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]/text:span[@text:style-name='Interlin_VariantTypes']", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]/text:span[@text:style-name='Interlin_VariantTypes']", nsmgr).InnerText, Is.EqualTo("+fr. var."));
			}

			[Test]
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

				string formLexEntry = "go";

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "glossgo", null);
				var pastVar =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPastVar);
				pastVar.GlossAppend.SetAnalysisDefaultWritingSystem(".pst");

				pa.SetVariantOf(0, 1, leGo, pastVar);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				// The guesser adds an analysis for "go".
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath($@"//morphemes[@analysisStatus='guess']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='cf']='go']", 2);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='glossgo']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='glsAppend']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='glsAppend']='.pst']", 1);

				// variantTypes item isn't really needed since it is empty, but don't see any harm in keeping it.
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='variantTypes']='']", 1);
			}

			[Test]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsPrepend()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, Cache.DefaultAnalWs);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				var exportedDoc = ExportToXml();

				string formLexEntry = "go";

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "glossgo", null);
				var pastVar =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPastVar);
				pastVar.GlossPrepend.SetAnalysisDefaultWritingSystem("Prepend.");

				pa.SetVariantOf(0, 1, leGo, pastVar);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);
				var transformedDoc = TransformDocXml2Html(exportedDoc);

				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td").InnerText, Is.EqualTo("Prepend.glossgo\u00A0"));
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/span[1][@class='itx_VariantTypes']", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/span[1][@class='itx_VariantTypes']").InnerText, Is.EqualTo("Prepend."));
			}

			[Test]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsAppend_xml2htm()
			{
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, Cache.DefaultAnalWs);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				var exportedDoc = ExportToXml();

				string formLexEntry = "go";

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "glossgo", null);
				var pastVar =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPastVar);
				pastVar.GlossAppend.SetAnalysisDefaultWritingSystem(".pst");

				pa.SetVariantOf(0, 1, leGo, pastVar);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);
				var transformedDoc = TransformDocXml2Html(exportedDoc);

				// NOTE: the whitespace after "glossgo.pst" is &#160;
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td").InnerText, Is.EqualTo("glossgo.pst\u00A0"));
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/span[@class='itx_VariantTypes']", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/span[@class='itx_VariantTypes']").InnerText, Is.EqualTo(".pst"));
			}

			[Test]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsAppend_xml2htm_multipleWss()
			{
				var wsfr = Cache.ServiceLocator.WritingSystemManager.Get("fr");
				var wsEn = Cache.ServiceLocator.WritingSystemManager.Get("en");
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsfr.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsEn.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				var exportedDoc = ExportToXml();

				string formLexEntry = "go";

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "glossgo", null);
				var pastVar =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPastVar);
				leGo.SensesOS[0].Gloss.set_String(wsfr.Handle, "frglossgo");
				pastVar.GlossAppend.SetAnalysisDefaultWritingSystem(".pst");

				pa.SetVariantOf(0, 1, leGo, pastVar);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);
				XmlDocument transformedDoc = TransformDocXml2Html(exportedDoc);
				// NOTE: The following test in the comment fails whenever a namespace has been defined because it requires you add a
				// namespace via XmlNamespaceManager (http://mymemorysucks.wordpress.com/2007/08/17/xmldocumentselectnodes-selects-nothing/)
				/*
				var testDoc = new XmlDocument();
				testDoc.LoadXml(@"<html xmlns='http://www.w3.org/1999/xhtml'></html>");
				AssertThatXmlIn.Dom(testDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html", 1);
				 */

				// NOTE: the whitespace after "glossgo.pst" is &#160;
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td").InnerText, Is.EqualTo("frglossgo.pst\u00A0"));
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/span[@class='itx_VariantTypes']", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/span[@class='itx_VariantTypes']").InnerText, Is.EqualTo(".pst"));

				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[4]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[4]/td").InnerText, Is.EqualTo("glossgo.pst\u00A0"));
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[4]/td/span[@class='itx_VariantTypes']", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[4]/td/span[@class='itx_VariantTypes']").InnerText, Is.EqualTo(".pst"));
			}

			[Test]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsAppend_xml2OO_multipleWss()
			{
				var wsfr = Cache.ServiceLocator.WritingSystemManager.Get("fr");
				var wsEn = Cache.ServiceLocator.WritingSystemManager.Get("en");
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsfr.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsEn.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				var exportedDoc = ExportToXml();

				string formLexEntry = "go";

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "glossgo", null);
				var pastVar =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPastVar);
				leGo.SensesOS[0].Gloss.set_String(wsfr.Handle, "frglossgo");
				pastVar.GlossAppend.SetAnalysisDefaultWritingSystem(".pst");

				pa.SetVariantOf(0, 1, leGo, pastVar);
				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);
				var transformedDocOO = TransformDocXml2OO(exportedDoc);
				XmlNamespaceManager nsmgr = LoadNsmgrForDoc(transformedDocOO);

				// TODO: enhance AssertThatXmlIn to handle all prefixes
				// /text:span[@text:style-name='Interlin_VariantTypes']
				//AssertThatXmlIn.Dom(transformedDocOO).HasSpecifiedNumberOfMatchesForXpath(@"/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]", 1);
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]", nsmgr).InnerText, Is.EqualTo("frglossgo.pst"));
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]/text:span[@text:style-name='Interlin_VariantTypes']", nsmgr),
					Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]/text:span[@text:style-name='Interlin_VariantTypes']", nsmgr).InnerText,
					Is.EqualTo(".pst"));

				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[4]", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[4]", nsmgr).InnerText, Is.EqualTo("glossgo.pst"));
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[4]/text:span[@text:style-name='Interlin_VariantTypes']", nsmgr),
					Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[4]/text:span[@text:style-name='Interlin_VariantTypes']", nsmgr).InnerText,
					Is.EqualTo(".pst"));
				Assert.That(transformedDocOO.SelectNodes("//text:p[text()='.pst']", nsmgr), Has.Count.EqualTo(0));
			}


			/// <summary>
			/// Ideally, this should be broken up into two or three tests as we have done for the other exports,
			/// however, I (EricP) got tired of making so many tests, so lumped it all into one.
			/// </summary>
			[Test]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsAppend_varianttypes_xml2Word_multipleWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				var wsEs = Cache.ServiceLocator.WritingSystemManager.Get("es");
				var wsfr = Cache.ServiceLocator.WritingSystemManager.Get("fr");
				var wsEn = Cache.ServiceLocator.WritingSystemManager.Get("en");
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsEs.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsXkal.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsfr.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsEn.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				var exportedDoc = ExportToXml();

				string formLexEntry = "go";

				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "glossgo", null);
				const string formLexEntryEs = "eswent";
				leGo.LexemeFormOA.Form.set_String(wsEs.Handle, formLexEntryEs);
				var pastVar =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPastVar);
				leGo.SensesOS[0].Gloss.set_String(wsfr.Handle, "frglossgo");
				pastVar.GlossAppend.SetAnalysisDefaultWritingSystem(".pst");

				var ler = pa.SetVariantOf(0, 1, leGo, pastVar);
				var freeVar = Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypFreeVar);
				freeVar.ReverseAbbr.SetAnalysisDefaultWritingSystem("fr. var.");
				ler.VariantEntryTypesRS.Add(freeVar);

				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				var transformedDocWord = TransformDocXml2Word(exportedDoc);
				XmlNamespaceManager nsmgr = LoadNsmgrForDoc(transformedDocWord);

				// TODO: enhance AssertThatXmlIn to handle all prefixes
				//AssertThatXmlIn.Dom(transformedDocOO).HasSpecifiedNumberOfMatchesForXpath(@"/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]", 1);
				Assert.That(transformedDocWord.SelectNodes("/w:wordDocument/w:body/w:p[5]/w:pict[3]/v:shape/v:textbox/w:txbxContent/w:tbl/w:tr[2]/w:tc/w:p/w:r[2]/w:t", nsmgr), Has.Count.EqualTo(1), "test homograph number for lex entry: " + formLexEntryEs);
				// w:p = "eswent+fr. var. "  has no homograph after Ls-13615
				Assert.That(transformedDocWord.SelectSingleNode("/w:wordDocument/w:body/w:p[5]/w:pict[3]/v:shape/v:textbox/w:txbxContent/w:tbl/w:tr[2]/w:tc/w:p/w:r[2]/w:t", nsmgr).InnerText, Is.Not.EqualTo("1"), "Inaccurate homograph number for lex entry: " + formLexEntryEs);
				Assert.That(transformedDocWord.SelectNodes("/w:wordDocument/w:body/w:p[5]/w:pict[3]/v:shape/v:textbox/w:txbxContent/w:tbl/w:tr[2]/w:tc/w:p/w:r[2]/w:t", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocWord.SelectSingleNode("/w:wordDocument/w:body/w:p[5]/w:pict[3]/v:shape/v:textbox/w:txbxContent/w:tbl/w:tr[2]/w:tc/w:p/w:r[2]/w:t", nsmgr).InnerText, Is.EqualTo("+fr. var."));
				// /w:worddocument/w:body/w:p[5]/w:pict[3]/v:shape/v:textbox/w:txbxcontent/w:tbl/w:tr[5]/w:tc/w:p/w:r/w:t
				Assert.That(transformedDocWord.SelectNodes("//*[text()='.pst']", nsmgr), Has.Count.EqualTo(2), "Irregularly inflected types should only match the number of lines for LexGloss");
				// w:pStyle
				Assert.That(transformedDocWord.SelectNodes("//w:pStyle[starts-with(@w:val, '\n') or starts-with(@w:val, ' ')]", nsmgr), Has.Count.EqualTo(0), "style values should not start with whitespace");
			}

			/// <summary>
			/// Ideally, this should be broken up into two or three tests as we have done for the other exports,
			/// however, I (EricP) got tired of making so many tests, so lumped it all into one.
			/// </summary>
			[Test]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsAppend_varianttypes_xml2Word2007_multipleWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				var wsEs = Cache.ServiceLocator.WritingSystemManager.Get("es");
				var wsfr = Cache.ServiceLocator.WritingSystemManager.Get("fr");
				var wsEn = Cache.ServiceLocator.WritingSystemManager.Get("en");
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsEs.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsXkal.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsfr.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsEn.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				var exportedDoc = ExportToXml();

				string formLexEntry = "go";

				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "glossgo", null);
				const string formLexEntryEs = "eswent";
				leGo.LexemeFormOA.Form.set_String(wsEs.Handle, formLexEntryEs);
				var pastVar =
					Cache.ServiceLocator.GetInstance<ILexEntryInflTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypPastVar);
				leGo.SensesOS[0].Gloss.set_String(wsfr.Handle, "frglossgo");
				pastVar.GlossAppend.SetAnalysisDefaultWritingSystem(".pst");

				var ler = pa.SetVariantOf(0, 1, leGo, pastVar);
				var freeVar = Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>().GetObject(LexEntryTypeTags.kguidLexTypFreeVar);
				freeVar.ReverseAbbr.SetAnalysisDefaultWritingSystem("fr. var.");
				ler.VariantEntryTypesRS.Add(freeVar);

				pa.ReparseParagraph();
				exportedDoc = ExportToXml();

				var transformedDocWord = TransformDocXml2Word2007(exportedDoc);
				XmlNamespaceManager nsmgr = LoadNsmgrForDoc(transformedDocWord);

				// TODO: enhance AssertThatXmlIn to handle all prefixes
				//AssertThatXmlIn.Dom(transformedDocOO).HasSpecifiedNumberOfMatchesForXpath(@"/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]", 1);
				// m:mr[2]/m:e/m:r = "eswent+fr. var. " has no homograph after Ls-13615
				//Assert.That(transformedDocWord.SelectNodes("/pkg:package/pkg:part[3]/pkg:xmlData/w:document/w:body/w:p[5]/m:oMath[2]/m:m/m:mr[2]/m:e/m:m/m:mr[2]/m:e/m:r/m:t[2]", nsmgr), Has.Count.EqualTo(1), "test homograph number for lex entry: " + formLexEntryEs);
				Assert.That(transformedDocWord.SelectSingleNode("/pkg:package/pkg:part[3]/pkg:xmlData/w:document/w:body/w:p[5]/m:oMath[2]/m:m/m:mr[2]/m:e/m:m/m:mr[2]/m:e/m:r/m:t[2]", nsmgr).InnerText, Is.Not.EqualTo("1"), "Inaccurate homograph number for lex entry: " + formLexEntryEs);
				Assert.That(transformedDocWord.SelectNodes("/pkg:package/pkg:part[3]/pkg:xmlData/w:document/w:body/w:p[5]/m:oMath[2]/m:m/m:mr[2]/m:e/m:m/m:mr[2]/m:e/m:r/m:t[2]", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocWord.SelectSingleNode("/pkg:package/pkg:part[3]/pkg:xmlData/w:document/w:body/w:p[5]/m:oMath[2]/m:m/m:mr[2]/m:e/m:m/m:mr[2]/m:e/m:r/m:t[2]", nsmgr).InnerText, Is.EqualTo("+fr. var."));
				Assert.That(transformedDocWord.SelectNodes("//*[text()='.pst']", nsmgr), Has.Count.EqualTo(2), "Irregularly inflected types should only match the number of lines for LexGloss");
				Assert.That(transformedDocWord.SelectNodes("//w:rStyle[starts-with(@w:val, '\n') or starts-with(@w:val, ' ')]", nsmgr), Has.Count.EqualTo(0), "style values should not start with whitespace");
			}

			[Test]
			[Ignore("This is a bug that might need to be fixed if users notice it. low priority since the user could just not display lines with same ws")]
			public void ExportIrrInflVariantTypeInformation_LT7581_gls_multiEngWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get(QaaXKal);
				var wsEn = Cache.ServiceLocator.WritingSystemManager.Get("en");
				m_choices.Add(InterlinLineChoices.kflidWord);
				m_choices.Add(InterlinLineChoices.kflidMorphemes);
				m_choices.Add(InterlinLineChoices.kflidLexEntries, wsXkal.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsEn.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexGloss, wsEn.Handle);
				m_choices.Add(InterlinLineChoices.kflidLexPos);

				IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
				ParagraphAnnotator pa = new ParagraphAnnotator(para1);
				pa.ReparseParagraph();
				var exportedDoc = ExportToXml();

				string formLexEntry = "go";

				ITsString tssLexEntryForm = TsStringUtils.MakeString(formLexEntry, wsXkal.Handle);
				int clsidForm;
				ILexEntry leGo = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(
					MorphServices.FindMorphType(Cache, ref formLexEntry, out clsidForm), tssLexEntryForm, "glossgo", null);
				pa.BreakIntoMorphs(0, 1, new ArrayList() {leGo.LexemeFormOA});
				pa.SetMorphSense(0, 1, 0, leGo.SensesOS[0]);

				pa.ReparseParagraph();
				exportedDoc = ExportToXml();
				var transformedDocWord = TransformDocXml2Word(exportedDoc);
				XmlNamespaceManager nsmgr = LoadNsmgrForDoc(transformedDocWord);

				Assert.That(transformedDocWord.SelectNodes("//*[text()='glossgo']", nsmgr), Has.Count.EqualTo(2), "Should only have one LexGloss per line");
			}
			private string CombineFilenameWithExportFolders(string filename)
			{
				string p = Path.Combine(FwDirectoryFinder.FlexFolder, Path.Combine("Export Templates", "Interlinear"));
				return Path.Combine(p, filename);
			}

			private XmlDocument TransformDocXml2Html(XmlDocument exportedDoc)
			{
				return TransformDoc(exportedDoc, CombineFilenameWithExportFolders("xml2htm.xsl"));
			}

			private XmlDocument TransformDocXml2OO(XmlDocument exportedDoc)
			{
				return TransformDoc(exportedDoc, CombineFilenameWithExportFolders("xml2OO.xsl"));
			}

			private XmlDocument TransformDocXml2Word(XmlDocument exportedDoc)
			{
				return TransformDoc(exportedDoc, CombineFilenameWithExportFolders("xml2Word.xsl"));
			}

			private XmlDocument TransformDocXml2Word2007(XmlDocument exportedDoc)
			{
				return TransformDoc(exportedDoc, CombineFilenameWithExportFolders("xml2Word2007.xsl"));
			}

			/// <summary>
			/// Search through an entire document for "xmlns" attributes that define prefixes,
			/// returns an XmlNamespaceManager able to interpret xpath for those prefixes.
			/// </summary>
			/// <param name="doc"></param>
			/// <returns></returns>
			private static XmlNamespaceManager LoadNsmgrForDoc(XmlDocument doc)
			{
				var rootNode = doc.DocumentElement;
				var nsmgr = new XmlNamespaceManager(doc.NameTable);
				foreach (XmlNode node in rootNode.SelectNodes("//*"))
				{
					foreach (XmlAttribute attr in node.Attributes)
					{
						if (attr.Prefix != "xmlns")
							continue;
						var prefix = attr.LocalName;
						var urn = attr.Value;
						if (prefix.Length > 0)
						{
							var urnDefined = nsmgr.LookupNamespace(prefix);
							if (String.IsNullOrEmpty(urnDefined))
							{
								nsmgr.AddNamespace(prefix, urn);
							}

						}
					}
				}
				return nsmgr;
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

			protected override LCModel.IText SetupDataForText1()
			{
				Cache.LanguageProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();

				//person not found create one and add it.
				var newPerson = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
				Cache.LanguageProject.PeopleOA.PossibilitiesOS.Add(newPerson);
				newPerson.Name.set_String(Cache.DefaultVernWs, "Hiro Protaganist");

				LCModel.IText text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				//Cache.LangProject.TextsOC.Add(text);
				text.ContentsOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				text.MediaFilesOA = Cache.ServiceLocator.GetInstance<ICmMediaContainerFactory>().Create();
				ICmMediaURI recording = Cache.ServiceLocator.GetInstance<ICmMediaURIFactory>().Create();
				text.MediaFilesOA.MediaURIsOC.Add(recording);
				recGuid = recording.Guid.ToString();
				IStTxtPara para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(text.ContentsOA, 0, "special");
				para.Contents = TsStringUtils.MakeString("This is a text. It has two segments.", Cache.LanguageProject.DefaultVernacularWritingSystem.Handle);
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
				m_text1.Delete();
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

				string p = Path.Combine(FwDirectoryFinder.FlexFolder, Path.Combine("Export Templates", "Interlinear"));
				string file = Path.Combine(p, "FlexInterlinear.xsd");
				settings.Schemas.Add("", file);
				exportedDoc.Schemas.Add("", new Uri(file).AbsoluteUri);

				// The Mono implementation of XmlDocument.Validate is buggy (Xamarin Bug 8381).
				// But the other validation checks below are still worthwhile.
				if (!Platform.IsMono)
				{
					//validate export against schema.
					Assert.DoesNotThrow(() =>
					{
						exportedDoc.Validate(DontIgnore);
					});
				}

				//validate segment reference to MediaURI
				AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//phrase[@media-file=\"" + recGuid + "\"]");
				AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//media-files/media[@guid=\"" + recGuid + "\"]");
				AssertThatXmlIn.Dom(exportedDoc).HasAtLeastOneMatchForXpath("//phrase[@speaker=\"Hiro Protaganist\"]");
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath("//phrase/item[@type=\"txt\"]", 2);

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

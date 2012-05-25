using System;
using System.Collections;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Schema;
using System.Xml.Xsl;
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
			xform.Load(XmlReader.Create(new StringReader(xslt)));

			using (var stream = createStream())
			{
				var writerSettings = new XmlWriterSettings();
				writerSettings.OmitXmlDeclaration = omitXmlDeclaration;
				if (writerSettings.OmitXmlDeclaration)
					writerSettings.ConformanceLevel = ConformanceLevel.Auto;
				var xmlWriter = XmlWriter.Create(stream, writerSettings);

				xform.Transform(usxDocument.CreateNavigator(), new XsltArgumentList(), xmlWriter);
				xmlWriter.Flush();
				extractStream(stream);
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
				IWritingSystem wsXkal;
				Cache.ServiceLocator.WritingSystemManager.GetOrSet("qaa-x-kal", out wsXkal);
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

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//word[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='cf']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='go.PST']", 1);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='variantTypes']", 0);
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
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
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
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[5]/table/tr/td").InnerText, Is.EqualTo(". "));

			}

			[Test]
			public void ExportBasicInformation_multipleWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
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

				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
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

				string p = Path.Combine(DirectoryFinder.FlexFolder, Path.Combine("Export Templates", "Interlinear"));
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

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//word[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='cf']='go']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='variantTypes']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='variantTypes']='+fr. var.']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='go.PST']", 1);
			}

			[Test]
			public void ExportVariantTypeInformation_LT9374_xml2html_multipleWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
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

				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
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
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[2]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[2]/td").InnerText, Is.EqualTo(formLexEntryEs + "1+fr. var. "));
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td").InnerText, Is.EqualTo(formLexEntry + "1+fr. var. "));
			}

			[Test]
			public void ExportVariantTypeInformation_LT9374_xml2OO_multipleWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
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

				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
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
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]", nsmgr).InnerText, Is.EqualTo(formLexEntryEs + "1+fr. var."));
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]", nsmgr).InnerText, Is.EqualTo(formLexEntry + "1+fr. var."));
			}

			private XmlNamespaceManager LoadNsmgrForDoc(XmlDocument transformedDocOO)
			{
				var rootNode = transformedDocOO.DocumentElement;
				var nsmgr = new XmlNamespaceManager(transformedDocOO.NameTable);
				foreach (XmlAttribute attr in rootNode.Attributes)
				{
					if (attr.Prefix != "xmlns")
						continue;
					var prefix = attr.LocalName;
					var urn = attr.Value;
					if (prefix.Length > 0)
					{
						var urnDefined = nsmgr.LookupNamespace(prefix);
						if (String.IsNullOrEmpty(urnDefined))
							nsmgr.AddNamespace(prefix, urn);
					}

				}
				return nsmgr;
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

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);

				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='txt']='went']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='cf']='go']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='gls']='glossgo']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph/item[@type='glsAppend']", 1);
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='glsAppend']='.pst']", 1);

				// variantTypes item isn't really needed since it is empty, but don't see any harm in keeping it.
				AssertThatXmlIn.Dom(exportedDoc).HasSpecifiedNumberOfMatchesForXpath(@"//morph[item[@type='variantTypes']='']", 1);
			}

			[Test]
			[Ignore("TODO")]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsPrepend()
			{
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

				//validate export xml against schema
				ValidateInterlinearXml(exportedDoc);
				var transformedDoc = TransformDocXml2Html(exportedDoc);
				// NOTE: The following test in the comment fails whenever a namespace has been defined because it requires you add a
				// namespace via XmlNamespaceManager (http://mymemorysucks.wordpress.com/2007/08/17/xmldocumentselectnodes-selects-nothing/)
				/*
				var testDoc = new XmlDocument();
				testDoc.LoadXml(@"<html xmlns='http://www.w3.org/1999/xhtml'></html>");
				AssertThatXmlIn.Dom(testDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html", 1);
				 */

				// NOTE: the whitespace after "glossgo.pst" is &#160;
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/text()").Value, Is.EqualTo("glossgo.pst "));
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

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
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
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[3]/td/text()").Value, Is.EqualTo("frglossgo.pst "));
				AssertThatXmlIn.Dom(transformedDoc).HasSpecifiedNumberOfMatchesForXpath(@"/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[4]/td", 1);
				Assert.That(transformedDoc.SelectSingleNode("/html/body/p[4]/span[3]/table/tr[2]/td/span/table/tr[4]/td/text()").Value, Is.EqualTo("glossgo.pst "));
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

				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
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
				//AssertThatXmlIn.Dom(transformedDocOO).HasSpecifiedNumberOfMatchesForXpath(@"/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[2]", 1);
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[3]", nsmgr).InnerText, Is.EqualTo("frglossgo.pst"));
				Assert.That(transformedDocOO.SelectNodes("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[4]", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocOO.SelectSingleNode("/office:document-content/office:body/office:text/text:p[5]/draw:frame[3]/draw:text-box/text:p[2]/draw:frame/draw:text-box/text:p[4]", nsmgr).InnerText, Is.EqualTo("glossgo.pst"));
				Assert.That(transformedDocOO.SelectNodes("//text:p[text()='.pst']", nsmgr), Has.Count.EqualTo(0));
			}


			[Test]
			public void ExportIrrInflVariantTypeInformation_LT7581_glsAppend_varianttypes_xml2Word_multipleWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
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

				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
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
				Assert.That(transformedDocWord.SelectSingleNode("/w:wordDocument/w:body/w:p[5]/w:pict[3]/v:shape/v:textbox/w:txbxContent/w:tbl/w:tr[2]/w:tc/w:p/w:r[2]/w:t", nsmgr).InnerText, Is.EqualTo("1"), "Inaccurate homograph number for lex entry: " + formLexEntryEs);
				Assert.That(transformedDocWord.SelectNodes("/w:wordDocument/w:body/w:p[5]/w:pict[3]/v:shape/v:textbox/w:txbxContent/w:tbl/w:tr[2]/w:tc/w:p/w:r[3]/w:t", nsmgr), Has.Count.EqualTo(1));
				Assert.That(transformedDocWord.SelectSingleNode("/w:wordDocument/w:body/w:p[5]/w:pict[3]/v:shape/v:textbox/w:txbxContent/w:tbl/w:tr[2]/w:tc/w:p/w:r[3]/w:t", nsmgr).InnerText, Is.EqualTo("+fr. var."));
				// /w:worddocument/w:body/w:p[5]/w:pict[3]/v:shape/v:textbox/w:txbxcontent/w:tbl/w:tr[5]/w:tc/w:p/w:r/w:t
				Assert.That(transformedDocWord.SelectNodes("//*[text()='.pst']", nsmgr), Has.Count.EqualTo(2), "Irregularly inflected types should only match the number of lines for LexGloss");
				// w:pStyle
				Assert.That(transformedDocWord.SelectNodes("//w:pStyle[starts-with(@w:val, '\n') or starts-with(@w:val, ' ')]", nsmgr), Has.Count.EqualTo(0), "style values should not start with whitespace");
			}


			[Test]
			[Ignore("This is a bug that might need to be fixed if users notice it. low priority since the user could just not display lines with same ws")]
			public void ExportIrrInflVariantTypeInformation_LT7581_gls_multiEngWss()
			{
				var wsXkal = Cache.ServiceLocator.WritingSystemManager.Get("qaa-x-kal");
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

				ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, wsXkal.Handle);
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
				string p = Path.Combine(DirectoryFinder.FlexFolder, Path.Combine("Export Templates", "Interlinear"));
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

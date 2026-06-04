// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.WritingSystems;
using SIL.TestUtilities;
using XCore;
using static SIL.FieldWorks.XWorks.LcmWordGenerator;
using W14 = DocumentFormat.OpenXml.Office2010.Word;
// ReSharper disable StringLiteralTypo

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class LcmWordGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private FwXApp m_application;
		private FwXWindow m_window;
		private PropertyTable m_propertyTable;
		private Mediator m_mediator;
		private RecordClerk m_Clerk;

		// Character Styles
		private const string DictionaryNormal = "Dictionary-Normal-Char";
		private const string DictionaryGlossStyleName = "Dictionary-Gloss-Char";

		// Paragraph Styles
		private const string MainEntryParagraphStyleName = "Dictionary-Main-Para";
		private const string MainEntryParagraphDisplayName = "Main Entry Display Name";
		private const string SensesParagraphStyleName = "Dictionary-Senses-Para";
		private const string SensesParagraphDisplayName = "Senses Display Name";
		private const string SubSensesParagraphStyleName = "Dictionary-SubSenses-Para";
		private const string SubSensesParagraphDisplayName = "SubSenses Display Name";
		private const string BulletParagraphStyleName = "Dictionary-Bullet-Para";
		private const string BulletParagraphDisplayName = "Bullet Display Name";
		private const string NumberParagraphStyleName = "Dictionary-Number-Para";
		private const string NumberParagraphDisplayName = "Number Display Name";

		private ConfiguredLcmGenerator.GeneratorSettings DefaultSettings;

		private static XmlNamespaceManager WordNamespaceManager;

		static LcmWordGeneratorTests()
		{
			var openXmlSchema = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
			WordNamespaceManager = new XmlNamespaceManager(new NameTable());
			WordNamespaceManager.AddNamespace("w", openXmlSchema);
			WordNamespaceManager.AddNamespace("r", openXmlSchema);
			WordNamespaceManager.AddNamespace("wp", openXmlSchema);
			WordNamespaceManager.AddNamespace("w14", "http://schemas.microsoft.com/office/word/2010/wordml");
		}

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
			m_mediator = m_window.Mediator;
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet
			var wordGenerator = new LcmWordGenerator(Cache);
			wordGenerator.Init(new ReadOnlyPropertyTable(m_propertyTable));
			DefaultSettings = new ConfiguredLcmGenerator.GeneratorSettings(Cache,
					new ReadOnlyPropertyTable(m_propertyTable), true, false, null)
				{ ContentGenerator = wordGenerator, StylesGenerator = wordGenerator };

			var styles = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable).Styles;

			// Add character styles
			if (!styles.Contains(DictionaryNormal))
				styles.Add(new BaseStyleInfo { Name = DictionaryNormal });
			if (!styles.Contains("Dictionary-Headword"))
				styles.Add(new BaseStyleInfo { Name = "Dictionary-Headword", IsParagraphStyle = false});
			if (!styles.Contains(WordStylesGenerator.BeforeAfterBetweenStyleName))
				styles.Add(new BaseStyleInfo { Name = WordStylesGenerator.BeforeAfterBetweenStyleName, IsParagraphStyle = false });
			if (!styles.Contains("Abbreviation"))
			{
				var baseStyle = new BaseStyleInfo { Name = "Abbreviation", IsParagraphStyle = false };
				baseStyle.SetExplicitFontIntProp(3, 1);  // Bold
				styles.Add(baseStyle);
			}
			if (!styles.Contains("Dictionary-SenseNumber"))
				styles.Add(new BaseStyleInfo { Name = "Dictionary-SenseNumber", IsParagraphStyle = false });
			if (!styles.Contains("Style1"))
				styles.Add(new BaseStyleInfo { Name = "Style1", IsParagraphStyle = false });
			if (!styles.Contains(DictionaryGlossStyleName))
				styles.Add(new BaseStyleInfo { Name = DictionaryGlossStyleName, IsParagraphStyle = false });

			// Add character styles that are used in Classified Dictionary
			if (!styles.Contains(WordStylesGenerator.Abbreviation))
				styles.Add(new BaseStyleInfo { Name = WordStylesGenerator.Abbreviation, IsParagraphStyle = false });
			if (!styles.Contains(WordStylesGenerator.Abbreviation + WordStylesGenerator.BeforeAfterBetween))
				styles.Add(new BaseStyleInfo { Name = WordStylesGenerator.Abbreviation + WordStylesGenerator.BeforeAfterBetween, IsParagraphStyle = false });
			if (!styles.Contains(WordStylesGenerator.Name))
				styles.Add(new BaseStyleInfo { Name = WordStylesGenerator.Name, IsParagraphStyle = false });

			// Add paragraph styles
			if (!styles.Contains(WordStylesGenerator.NormalParagraphStyleName))
				styles.Add(new BaseStyleInfo { Name = WordStylesGenerator.NormalParagraphStyleName, IsParagraphStyle = true });
			if (!styles.Contains(MainEntryParagraphStyleName))
				styles.Add(new BaseStyleInfo { Name = MainEntryParagraphStyleName, IsParagraphStyle = true });
			if (!styles.Contains(WordStylesGenerator.LetterHeadingStyleName))
				styles.Add(new BaseStyleInfo { Name = WordStylesGenerator.LetterHeadingStyleName, IsParagraphStyle = true });
			if (!styles.Contains(SensesParagraphStyleName))
				styles.Add(new BaseStyleInfo { Name = SensesParagraphStyleName, IsParagraphStyle = true });
			if (!styles.Contains(SubSensesParagraphStyleName))
				styles.Add(new BaseStyleInfo { Name = SubSensesParagraphStyleName, IsParagraphStyle = true });
			if (!styles.Contains(BulletParagraphStyleName))
			{
				var bulletinfo = new BulletInfo
				{
					m_numberScheme = VwBulNum.kvbnBulletBase + 1,
				};
				var inherbullet = new InheritableStyleProp<BulletInfo>(bulletinfo);
				var bulletStyle = new TestStyle(inherbullet, Cache) { Name = BulletParagraphStyleName, IsParagraphStyle = true };
				styles.Add(bulletStyle);
			}
			if (!styles.Contains(NumberParagraphStyleName))
			{
				var bulletinfo = new BulletInfo
				{
					m_numberScheme = VwBulNum.kvbnArabic,
				};
				var inherbullet = new InheritableStyleProp<BulletInfo>(bulletinfo);
				var bulletStyle = new TestStyle(inherbullet, Cache) { Name = NumberParagraphStyleName, IsParagraphStyle = true };
				styles.Add(bulletStyle);
			}

			m_Clerk = CreateClerk();
			m_propertyTable.SetProperty("ActiveClerk", m_Clerk, false);

			m_propertyTable.SetProperty("currentContentControl", "lexiconDictionary", false);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/");
		}

		private RecordClerk CreateClerk()
		{
			const string entryClerk = @"<?xml version='1.0' encoding='UTF-8'?>
			<root>
				<clerks>
					<clerk id='entries'>
						<recordList owner='LexDb' property='Entries'/>
					</clerk>
				</clerks>
				<tools>
					<tool label='Dictionary' value='lexiconDictionary' icon='DocumentView'>
						<control>
							<dynamicloaderinfo assemblyPath='xWorks.dll' class='SIL.FieldWorks.XWorks.XhtmlDocView'/>
							<parameters area='lexicon' clerk='entries' layout='Bartholomew' layoutProperty='DictionaryPublicationLayout' editable='false' configureObjectName='Dictionary'/>
						</control>
					</tool>
				</tools>
			</root>";
			var doc = new XmlDocument();
			doc.LoadXml(entryClerk);
			XmlNode clerkNode = doc.SelectSingleNode("//tools/tool[@label='Dictionary']//parameters[@area='lexicon']");
			RecordClerk clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false, false);
			clerk.SortName = "Headword";
			return clerk;
		}

		private RecordClerk CreateClassifiedClerk()
		{
			const string classifiedClerk = @"<?xml version='1.0' encoding='UTF-8'?>
			<root>
				<clerks>
					<clerk id='SemanticDomainList'>
						<recordList owner='LangProject' property='SemanticDomainList'/>
					</clerk>
				</clerks>
				<tools>
					<tool label='Classified Dictionary' value='lexiconClassifiedDictionary' icon='DocumentView'>
						<control>
							<dynamicloaderinfo assemblyPath='xWorks.dll' class='SIL.FieldWorks.XWorks.XhtmlDocView'/>
							<parameters area='lexicon' clerk='SemanticDomainList' layout='classifiedDict' layoutProperty='ClassifiedDictionaryPublicationLayout' editable='false' configureObjectName='Classified Dictionary'/>
						</control>
					</tool>
				</tools>
			</root>";
			var doc = new XmlDocument();
			doc.LoadXml(classifiedClerk);
			XmlNode clerkNode = doc.SelectSingleNode("//tools/tool[@label='Classified Dictionary']//parameters[@area='lexicon']");
			RecordClerk clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false, false);
			clerk.SortName = "Name";
			return clerk;
		}

		private ICmSemanticDomain CreateSemanticDomain(LcmCache cache)
		{
			var domain = cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>().Create();
			// Add domain to the semantic domain list before setting its name & abbreviation
			cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Add(domain);
			int ws = cache.WritingSystemFactory.GetWsFromStr("en");
			domain.Name.set_String(ws, "Test Domain");
			domain.Abbreviation.set_String(ws, "1.0");
			return domain;
		}

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				m_Clerk?.Dispose();
				m_application?.Dispose();
				m_window?.Dispose();
				m_mediator?.Dispose();
				m_propertyTable?.Dispose();
			}
		}

		~LcmWordGeneratorTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		#endregion disposal

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			Dispose();
		}

		[SetUp]
		public void Setup()
		{
			LcmWordGenerator.ClearStyleCollection();
			DefaultSettings.StylesGenerator.AddGlobalStyles(null, new ReadOnlyPropertyTable(m_propertyTable));
		}

		[Test]
		public void GenerateCharacterStyleFromLcmStyleSheet_OpenTypeFontFeatures_AddsWordTypographyProperties()
		{
			var styleName = "WordFeatureStyle" + Guid.NewGuid().ToString("N");
			var fontInfo = new FontInfo { m_features = { ExplicitValue = "liga=0,lnum=1,pnum=1,calt=0,ss02=0,cv01=2" } };
			var projectStyle = new TestStyle(fontInfo, Cache) { Name = styleName, IsParagraphStyle = false };
			FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable).Styles.Add(projectStyle);

			var style = WordStylesGenerator.GenerateCharacterStyleFromLcmStyleSheet(styleName, Cache.DefaultVernWs,
				new ReadOnlyPropertyTable(m_propertyTable));

			var runProps = style.GetFirstChild<StyleRunProperties>();
			AssertWordTypographyProperties(runProps, W14.LigaturesValues.None, W14.NumberFormValues.Lining,
				W14.NumberSpacingValues.Proportional, false, 2U, false);
		}

		[Test]
		public void GetExplicitFontProperties_OpenTypeFontFeatures_AddsWordTypographyProperties()
		{
			var fontInfo = new FontInfo { m_features = { ExplicitValue = "liga=1,clig=1,onum=1,tnum=1,calt=1,ss03=1,cv01=2" } };

			var runProps = WordStylesGenerator.GetExplicitFontProperties(fontInfo);

			AssertWordTypographyProperties(runProps, W14.LigaturesValues.StandardContextual, W14.NumberFormValues.OldStyle,
				W14.NumberSpacingValues.Tabular, true, 3U, true);
		}

		[Test]
		public void GenerateCharacterStyleFromLcmStyleSheet_NormalStyle_UsesWritingSystemDefaultFontFeatures()
		{
			var vernWs = Cache.ServiceLocator.WritingSystemManager.Get(Cache.DefaultVernWs);
			vernWs.DefaultFont = new FontDefinition("Charis SIL") { Features = "ss11=1,ss12=1" };

			var style = WordStylesGenerator.GenerateCharacterStyleFromLcmStyleSheet(
				WordStylesGenerator.NormalParagraphStyleName,
				vernWs.Handle,
				new ReadOnlyPropertyTable(m_propertyTable));

			var runProps = style.GetFirstChild<StyleRunProperties>();
			Assert.That(runProps, Is.Not.Null);

			var runFonts = runProps.GetFirstChild<RunFonts>();
			Assert.That(runFonts, Is.Not.Null);
			Assert.That(runFonts.Ascii?.Value, Is.EqualTo("Charis SIL"));

			var stylisticSets = runProps.GetFirstChild<W14.StylisticSets>();
			Assert.That(stylisticSets, Is.Not.Null);

			var styleSets = stylisticSets.Elements<W14.StyleSet>().OrderBy(styleSet => styleSet.Id?.Value).ToList();
			Assert.That(styleSets.Count, Is.EqualTo(2));
			Assert.That(styleSets.Select(styleSet => styleSet.Id?.Value), Is.EqualTo(new uint?[] { 11U, 12U }));
			Assert.That(styleSets.Select(styleSet => styleSet.Val?.Value),
				Is.EqualTo(new[] { W14.OnOffValues.True, W14.OnOffValues.True }));
		}

		[Test]
		[Category("ManualDocx")]
		public void GenerateManualDocxArtifact_CharisBaseline_NoFontOptions()
		{
			var docxPath = GenerateManualDocxArtifact("charis-baseline-no-font-options.docx", null);

			Assert.That(new FileInfo(docxPath).Length, Is.GreaterThan(0));
			Assert.That(GetDocxStyleSetIds(docxPath), Is.Empty);
		}

		[Test]
		[Category("ManualDocx")]
		public void GenerateManualDocxArtifact_CharisSs11Ss12()
		{
			var docxPath = GenerateManualDocxArtifact("charis-ss11-ss12.docx", "ss11=1,ss12=1");

			Assert.That(new FileInfo(docxPath).Length, Is.GreaterThan(0));
			var styleSetIds = GetDocxStyleSetIds(docxPath);
			Assert.That(styleSetIds, Does.Contain(11U));
			Assert.That(styleSetIds, Does.Contain(12U));
		}

		private static void AssertWordTypographyProperties(OpenXmlCompositeElement runProps,
			W14.LigaturesValues ligaturesValue, W14.NumberFormValues numberFormValue,
			W14.NumberSpacingValues numberSpacingValue, bool contextualAlternativesValue,
			uint stylisticSetId, bool stylisticSetValue)
		{
			Assert.That(runProps, Is.Not.Null);
			var ligatures = runProps.GetFirstChild<W14.Ligatures>();
			Assert.That(ligatures, Is.Not.Null);
			Assert.That(ligatures.Val.Value, Is.EqualTo(ligaturesValue));

			var numberForm = runProps.GetFirstChild<W14.NumberingFormat>();
			Assert.That(numberForm, Is.Not.Null);
			Assert.That(numberForm.Val.Value, Is.EqualTo(numberFormValue));

			var numberSpacing = runProps.GetFirstChild<W14.NumberSpacing>();
			Assert.That(numberSpacing, Is.Not.Null);
			Assert.That(numberSpacing.Val.Value, Is.EqualTo(numberSpacingValue));

			var contextualAlternatives = runProps.GetFirstChild<W14.ContextualAlternatives>();
			Assert.That(contextualAlternatives, Is.Not.Null);
			Assert.That(contextualAlternatives.Val.Value, Is.EqualTo(GetOnOffValue(contextualAlternativesValue)));

			var stylisticSets = runProps.GetFirstChild<W14.StylisticSets>();
			Assert.That(stylisticSets, Is.Not.Null);
			var styleSet = stylisticSets.Elements<W14.StyleSet>().Single();
			Assert.That(styleSet.Id.Value, Is.EqualTo(stylisticSetId));
			Assert.That(styleSet.Val.Value, Is.EqualTo(GetOnOffValue(stylisticSetValue)));
		}

		private static W14.OnOffValues GetOnOffValue(bool value)
		{
			return value ? W14.OnOffValues.True : W14.OnOffValues.False;
		}

		private string GenerateManualDocxArtifact(string fileName, string fontFeatures)
		{
			var outputDir = EnsureManualDocxArtifactOutputDirectory();
			var filePath = Path.Combine(outputDir, fileName);
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}

			ConfigureManualDocxWritingSystem(Cache.DefaultVernWs, fontFeatures);
			ConfigureManualDocxWritingSystem(Cache.DefaultAnalWs, fontFeatures);
			EnsureManualDocxStylesAvailable();

			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "agaga", "again agaga");
			var configuration = CreateManualDocxConfiguration();
			var publicationDecorator = new DictionaryPublicationDecorator(Cache, m_Clerk.VirtualListPublisher, m_Clerk.VirtualFlid);

			LcmWordGenerator.SavePublishedDocx(new[] { entry.Hvo }, m_Clerk, publicationDecorator, int.MaxValue,
				configuration, m_propertyTable, filePath);

			TestContext.WriteLine("Generated manual DOCX artifact: " + filePath);
			Assert.That(File.Exists(filePath), Is.True);
			return filePath;
		}

		private DictionaryConfigurationModel CreateManualDocxConfiguration()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "headword",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				Style = "Dictionary-Headword"
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" }),
				Style = DictionaryGlossStyleName
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = SensesParagraphStyleName
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode, sensesNode },
				CSSClassNameOverride = "entry",
				FieldDescription = "LexEntry",
				Style = MainEntryParagraphStyleName
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var configuration = new DictionaryConfigurationModel(true)
			{
				Label = "Manual DOCX",
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			DictionaryConfigurationModel.SpecifyParentsAndReferences(configuration.Parts, configuration, configuration.SharedItems);
			return configuration;
		}

		private void EnsureManualDocxStylesAvailable()
		{
			var styles = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable).Styles;

			if (!styles.Contains("Dictionary-Normal"))
				styles.Add(new BaseStyleInfo { Name = "Dictionary-Normal", IsParagraphStyle = true });
			if (!styles.Contains(DictionaryNormal))
				styles.Add(new BaseStyleInfo { Name = DictionaryNormal });
			if (!styles.Contains("Dictionary-Headword"))
				styles.Add(new BaseStyleInfo { Name = "Dictionary-Headword", IsParagraphStyle = false });
			if (!styles.Contains(DictionaryGlossStyleName))
				styles.Add(new BaseStyleInfo { Name = DictionaryGlossStyleName, IsParagraphStyle = false });
			if (!styles.Contains(MainEntryParagraphStyleName))
				styles.Add(new BaseStyleInfo { Name = MainEntryParagraphStyleName, IsParagraphStyle = true });
			if (!styles.Contains(SensesParagraphStyleName))
				styles.Add(new BaseStyleInfo { Name = SensesParagraphStyleName, IsParagraphStyle = true });
		}

		private void ConfigureManualDocxWritingSystem(int wsHandle, string fontFeatures)
		{
			var writingSystem = Cache.ServiceLocator.WritingSystemManager.Get(wsHandle);
			writingSystem.DefaultFont = new FontDefinition("Charis SIL") { Features = fontFeatures };
		}

		private static IReadOnlyCollection<uint> GetDocxStyleSetIds(string filePath)
		{
			using (var archive = ZipFile.OpenRead(filePath))
			{
				var stylesEntry = archive.GetEntry("word/styles.xml");
				Assert.That(stylesEntry, Is.Not.Null);

				var stylesDocument = new XmlDocument();
				using (var stylesStream = stylesEntry.Open())
				{
					stylesDocument.Load(stylesStream);
				}

				var namespaceManager = new XmlNamespaceManager(stylesDocument.NameTable);
				namespaceManager.AddNamespace("w14", "http://schemas.microsoft.com/office/word/2010/wordml");

				return stylesDocument.SelectNodes("//w14:styleSet", namespaceManager)
					.Cast<XmlNode>()
					.Select(node => node.Attributes?["id", "http://schemas.microsoft.com/office/word/2010/wordml"]?.Value)
					.Where(value => uint.TryParse(value, out _))
					.Select(value => uint.Parse(value))
					.Distinct()
					.OrderBy(id => id)
					.ToArray();
			}
		}

		private static string EnsureManualDocxArtifactOutputDirectory()
		{
			if (!string.Equals(Environment.GetEnvironmentVariable("FW_RUN_MANUAL_DOCX_EXPORT_TESTS"), "1",
				StringComparison.Ordinal))
			{
				Assert.Ignore("Set FW_RUN_MANUAL_DOCX_EXPORT_TESTS=1 to generate manual DOCX artifacts.");
			}

			var outputDir = Environment.GetEnvironmentVariable("FW_MANUAL_DOCX_OUTPUT_DIR");
			if (string.IsNullOrWhiteSpace(outputDir))
			{
				outputDir = Path.Combine(Path.GetDirectoryName(typeof(LcmWordGeneratorTests).Assembly.Location),
					"ManualDocxArtifacts");
			}

			Directory.CreateDirectory(outputDir);
			return outputDir;
		}


		[Test]
		public void GenerateWordDocForEntry_OneSenseWithGlossGeneratesCorrectResult()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = "Dictionary-Headword"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = DictionaryNormal
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			Console.WriteLine(result);
			AssertThatXmlIn.String(result.DocBody.OuterXml).HasSpecifiedNumberOfMatchesForXpath(
				"/w:body/w:p/w:r/w:t[text()='gloss']",
				1,
				WordNamespaceManager);
		}

		[Test]
		public void GenerateWordDocForEntry_LineBreaksInBeforeContentWork()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = "Dictionary-Headword",
				Before = "\\Abefore\\0A"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = DictionaryNormal
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry",
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			Console.WriteLine(result);
			AssertThatXmlIn.String(result?.DocBody?.OuterXml).HasSpecifiedNumberOfMatchesForXpath(
				"/w:body/w:p/w:r/w:br[@w:type='textWrapping']",
				2,
				WordNamespaceManager);
		}

		[Test]
		public void GenerateUniqueStyleName()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = "Dictionary-Headword"
			};
			var glossNode2 = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = "Abbreviation"
			};
			var subSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode2 },
				Style = DictionaryNormal
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSensesNode },
				Style = DictionaryNormal
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			int wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			ConfiguredXHTMLGeneratorTests.AddSenseAndTwoSubsensesToEntry(entry, "second gloss", Cache, wsEn);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;

			Assert.That(result.DocBody.OuterXml.Contains("Gloss[lang=en]"), Is.True);
			Assert.That(result.DocBody.OuterXml.Contains("Gloss2[lang=en]"), Is.True);
		}

		[Test]
		public void GenerateSenseNumberData()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var senseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "BEF",
				AfterNumber = "AFT",
				NumberingStyle = "%d",
				NumberEvenASingleSense = true
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = "Dictionary-Headword"
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = DictionaryNormal
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode },
				Style = DictionaryNormal
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			int wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			ConfiguredXHTMLGeneratorTests.AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss", Cache, wsEn);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;

			// The four important pieces of data contained in the run:
			// 1. Sense style:					Sense Number[lang=en]
			// 2. Sense number before text:		BEF
			// 3. Sense number:					2
			// 4. Sense number after text:		AFT
			const string senseNumberTwoRun = "<w:t xml:space=\"preserve\">BEF</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number-Context[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">AFT</w:t></w:r>";
			Assert.That(result.DocBody.OuterXml.Contains(senseNumberTwoRun), Is.True);
		}

		[Test]
		public void BeforeBetweenAfterContent()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var senseOptions = new DictionaryNodeSenseOptions
			{
				NumberingStyle = "%d",
				NumberEvenASingleSense = true
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = "Dictionary-Headword"
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = DictionaryNormal,
				Before = "BE2",
				Between = "TW2",
				After = "AF2"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode },
				Style = DictionaryNormal,
				Before = "BE1",
				Between = "TW1",
				After = "AF1"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			int wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			ConfiguredXHTMLGeneratorTests.AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss", Cache, wsEn);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			var outXml = result.DocBody.OuterXml;

			// Before text 'BE1' is before sense number '1' for 'gloss'.
			const string beforeFirstSense =
				"<w:t xml:space=\"preserve\">BE1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">gloss</w:t>";
			Assert.That(outXml.Contains(beforeFirstSense), Is.True);

			// Between text 'TW1' is before sense number '2' for 'second gloss'.
			const string betweenSenses =
				"<w:t xml:space=\"preserve\">TW1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">second gloss</w:t>";
			Assert.That(outXml.Contains(betweenSenses), Is.True);

			// Before text 'BE2' is before sense number '2' for 'second gloss2.1'.
			const string beforeFirstSubSense =
				"<w:t xml:space=\"preserve\">BE2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number2[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss2[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">second gloss2.1</w:t>";
			Assert.That(outXml.Contains(beforeFirstSubSense), Is.True);

			// Between text 'TW2' is before sense number '2' for 'second gloss2.2'.
			const string betweenSubSenses =
				"<w:t xml:space=\"preserve\">TW2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number2[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss2[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">second gloss2.2</w:t>";
			Assert.That(outXml.Contains(betweenSubSenses), Is.True);

			// After text 'AF2' is after 'second gloss2.2'.
			const string afterSubSenses =
				"<w:t xml:space=\"preserve\">second gloss2.2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"SensesOS-Context\" /></w:rPr><w:t xml:space=\"preserve\">AF2</w:t>";
			Assert.That(outXml.Contains(afterSubSenses), Is.True);

			// After text 'AF1' is after 'AF2'.
			const string afterSenses =
				"<w:t xml:space=\"preserve\">AF2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"SensesOS-Context2\" /></w:rPr><w:t xml:space=\"preserve\">AF1</w:t>";
			Assert.That(outXml.Contains(afterSenses), Is.True);
		}

		[Test]
		public void BeforeBetweenAfterContentWithWSAbbreviation()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en" },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "fr" }
				},
				DisplayWritingSystemAbbreviations = true
			};

			var senseOptions = new DictionaryNodeSenseOptions
			{
				NumberingStyle = "%d",
				NumberEvenASingleSense = true
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = "Dictionary-Headword",
				Before = "BE3",
				Between = "TW3",
				After = "AF3"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = DictionaryNormal,
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			testEntry.SensesOS.First().Gloss.set_String(wsFr, TsStringUtils.MakeString("glossFR", wsFr));

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			var outXml = result.DocBody.OuterXml;

			// Before text 'BE3' is after the sense number '1' and before the english abbreviation, which is before 'gloss'.
			const string beforeAbbreviation =
				"<w:t xml:space=\"preserve\">1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss-Context\" /></w:rPr><w:t xml:space=\"preserve\">BE3</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Writing System Abbreviation[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">Eng </w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang=en]\" /></w:rPr><w:t xml:space=\"preserve\">gloss</w:t>";
			Assert.That(outXml.Contains(beforeAbbreviation), Is.True);

			// Between text 'TW3' is before the french abbreviation, which is before 'glossFR'.
			const string betweenAbbreviation =
				"<w:t xml:space=\"preserve\">TW3</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Writing System Abbreviation[lang=fr]\" /></w:rPr><w:t xml:space=\"preserve\">Fre </w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang=fr]\" /></w:rPr><w:t xml:space=\"preserve\">glossFR</w:t>";
			Assert.That(outXml.Contains(betweenAbbreviation), Is.True);

			// After text 'AF3' is after 'glossFR'.
			const string afterAbbreviation =
				"<w:t xml:space=\"preserve\">glossFR</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss-Context\" /></w:rPr><w:t xml:space=\"preserve\">AF3</w:t>";
			Assert.That(outXml.Contains(afterAbbreviation), Is.True);
		}

		[Test]
		public void BetweenContentOnceForMultipleVariantTypesGroups()
		{
			// LT-22517: When an entry has variants of two different types, the Between text on the
			// VariantEntryTypesRS node should appear exactly once — between the two type groups —
			// and not before the first group.
			const string betweenText = "BETWEEN_VARIANT_TYPES";
			const string secondVariantType = "Spelling Variant";

			// Create entries BEFORE building list options so both types are captured in the snapshot.
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantForm1 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantForm2 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			// Two variants of different types → two type groups in the output.
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantForm1); // "Crazy Variant" (TestVariantName)
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantForm2, secondVariantType);

			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" })
			};
			// Between is on the VariantEntryTypesRS node because that is what nodeList.Last()
			// resolves to inside AddLexReferences when factoring by type.
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Between = betweenText,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode }
			};
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(
					DictionaryNodeListOptions.ListIds.Variant, Cache),
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			var outXml = result.DocBody.OuterXml;

			// Between text should appear exactly once — between the two groups, not before the first.
			Assert.That(Regex.Matches(outXml, betweenText).Count, Is.EqualTo(1),
				"Between text should appear exactly once, between the two variant type groups");
		}

		[Test]
		public void BetweenContentAbsentForSingleVariantTypeGroup()
		{
			// LT-22517: When an entry has variants of only one type, no Between text should appear.
			const string betweenText = "BETWEEN_VARIANT_TYPES";

			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantForm1 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantForm1); // single type group

			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" })
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Between = betweenText,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode }
			};
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(
					DictionaryNodeListOptions.ListIds.Variant, Cache),
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			var outXml = result.DocBody.OuterXml;

			// No Between text should appear when there is only one type group.
			Assert.That(outXml, Does.Not.Contain(betweenText),
				"Between text should not appear when there is only one variant type group");
		}

		[Test]
		public void BeforeContentSuppressedOnSubsequentVariantTypeGroupsWhenBetweenPresent()
		{
			// LT-22517: When Between text separates variant type groups, the Before text should
			// appear only before the first group. It should NOT be re-emitted after the Between
			// text on subsequent groups (which would produce e.g. "; (TypeB)" instead of "; TypeB").
			// This keeps the Word Export consistent with the display in Flex.
			const string beforeText = "BEFORE_VARIANT_TYPES";
			const string betweenText = "BETWEEN_VARIANT_TYPES";
			const string secondVariantType = "Spelling Variant";

			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantForm1 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantForm2 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantForm1); // "Crazy Variant" (TestVariantName)
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantForm2, secondVariantType);

			var variantFormTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" })
			};
			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Before = beforeText,
				Between = betweenText,
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNameNode }
			};
			var formNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				SubField = "MLHeadWord",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var variantsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(
					DictionaryNodeListOptions.ListIds.Variant, Cache),
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode, formNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantsNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			// SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			var outXml = result.DocBody.OuterXml;

			// Before text should appear exactly once — only before the first group.
			Assert.That(Regex.Matches(outXml, beforeText).Count, Is.EqualTo(1),
				"Before text should appear exactly once, only before the first variant type group");
			// Between text should appear exactly once — between the two groups.
			Assert.That(Regex.Matches(outXml, betweenText).Count, Is.EqualTo(1),
				"Between text should appear exactly once, between the two variant type groups");
			// Before text must appear earlier in the output than Between text:
			// Before belongs to the first group; Between separates the first from the second.
			// If Before appeared after Between it would mean it was (incorrectly) re-emitted on the second group.
			Assert.That(outXml.IndexOf(beforeText), Is.LessThan(outXml.IndexOf(betweenText)),
				"Before text should precede Between text in the output — it belongs to the first group only");
		}

		[Test]
		public void GeneratePropertyData()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });

			// Test with the 'DateModified' property.
			var dateModifiedNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "DateModified",
				Label = "DisplayNameBase",
				Style = "Style1",
				Before = "BE4",
				After = "AF4"
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = "Dictionary-Headword"
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = DictionaryNormal
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode, dateModifiedNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			var outXml = result.DocBody.OuterXml;

			// The property before text 'BE4' is first, followed by the style that is applied to the property, 'DisplayNameBase'.
			const string beforeAndStyle = "<w:t xml:space=\"preserve\">BE4</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"DisplayNameBase[lang=en]\" /></w:rPr>";
			Assert.That(outXml.Contains(beforeAndStyle), Is.True);

			// The property after text 'AF4' was written.
			Assert.That(outXml.Contains("AF4"), Is.True);
		}
		[Test]
		public void EmbeddedStylesHaveNoExtraSpace()
		{
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en", "fr" }),
				Between = "AREYOUCRAZY"
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				CSSClassNameOverride = "translationcontents",
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				CSSClassNameOverride = "examplescontents",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode },
				Style = DictionaryNormal
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { examplesNode },
				Style = DictionaryNormal
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			const string example = "Jones and Schneider";
			const string translation = "Overwritten with actual SUT data";
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			ConfiguredXHTMLGeneratorTests.AddExampleToSense(testEntry.SensesOS[0], example, Cache, wsFr, wsEn, translation);
			var enTrans = MakeMuliStyleTss(new [] { "don't", "go", "between" });
			var frTrans = MakeMuliStyleTss(new[] { "aller", "entre", "eux" });
			testEntry.SensesOS[0].ExamplesOS[0].TranslationsOC.First().Translation.set_String(wsEn, enTrans);
			testEntry.SensesOS[0].ExamplesOS[0].TranslationsOC.First().Translation.set_String(wsFr, frTrans);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			var outXml = result.DocBody.OuterXml;
			// Verify that AREYOUCRAZY appears only once in the output.
			var betweenCount = Regex.Matches(outXml, "AREYOUCRAZY").Count;

			Assert.That(betweenCount, Is.EqualTo(1)); // The between should not separate runs in a single translation
		}

		[Test]
		public void ReferenceParagraphDisplayNames()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var senseOptions = new DictionaryNodeSenseOptions
			{
				NumberingStyle = "%d",
				NumberEvenASingleSense = true,
				DisplayEachSenseInAParagraph = true
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = DictionaryGlossStyleName
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = SensesParagraphStyleName,
				Label = SensesParagraphDisplayName
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				Style = MainEntryParagraphStyleName,
				Label = MainEntryParagraphDisplayName
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			int wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;

			// Assert that the references to the paragraph styles use the display names, not the style names.
			Assert.That(result.DocBody.OuterXml.Contains(MainEntryParagraphDisplayName), Is.True);
			Assert.That(result.DocBody.OuterXml.Contains(SensesParagraphDisplayName), Is.True);
		}

		[Test]
		public void GenerateParagraphForSensesAndSubSenses()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var subSenseOptions = new DictionaryNodeSenseOptions
			{
				NumberingStyle = "%d",
				NumberEvenASingleSense = true,
				DisplayEachSenseInAParagraph = true
			};
			var senseOptions = new DictionaryNodeSenseOptions
			{
				NumberingStyle = "%d",
				NumberEvenASingleSense = true,
				DisplayEachSenseInAParagraph = true
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = DictionaryGlossStyleName
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				IsEnabled = true,
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = SubSensesParagraphStyleName,
				Label = SubSensesParagraphDisplayName
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				IsEnabled = true,
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode },
				Style = SensesParagraphStyleName,
				Label = SensesParagraphDisplayName
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true,
				Style = MainEntryParagraphStyleName,
				Label = MainEntryParagraphDisplayName
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			int wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			ConfiguredXHTMLGeneratorTests.AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss", Cache, wsEn);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;

			// There should be 5 paragraphs, one for the main entry, one for each sense, and one for each subsense.
			AssertThatXmlIn.String(result.DocBody.OuterXml).HasSpecifiedNumberOfMatchesForXpath(
				"/w:body/w:p",
				5,
				WordNamespaceManager);
		}

		[Test]
		public void GenerateBulletsAndNumbering()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var subSenseOptions = new DictionaryNodeSenseOptions
			{
				DisplayEachSenseInAParagraph = true
			};
			var senseOptions = new DictionaryNodeSenseOptions
			{
				DisplayEachSenseInAParagraph = true
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = DictionaryGlossStyleName
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				IsEnabled = true,
				DictionaryNodeOptions = subSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				StyleType = ConfigurableDictionaryNode.StyleTypes.Paragraph,
				Style = NumberParagraphStyleName,
				Label = NumberParagraphDisplayName
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				IsEnabled = true,
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode, subSenseNode },
				StyleType = ConfigurableDictionaryNode.StyleTypes.Paragraph,
				Style = BulletParagraphStyleName,
				Label = BulletParagraphDisplayName
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				IsEnabled = true,
				StyleType = ConfigurableDictionaryNode.StyleTypes.Paragraph,
				Style = MainEntryParagraphStyleName,
				Label = MainEntryParagraphDisplayName
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			int wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			ConfiguredXHTMLGeneratorTests.AddSenseAndTwoSubsensesToEntry(testEntry, "second gloss", Cache, wsEn);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;

			// There should be two instances of the bulletId and one instance for each of the numberId's.
			string resultStr = result.DocBody.OuterXml;
			int count1 = Regex.Matches(resultStr, "<w:numId w:val=\"1\" />").Count;
			int count2 = Regex.Matches(resultStr, "<w:numId w:val=\"2\" />").Count;
			int count3 = Regex.Matches(resultStr, "<w:numId w:val=\"3\" />").Count;
			int bulletId = 0;
			if (count1 == 2)
			{
				bulletId = 1;
				Assert.That(count2 == 1 && count3 == 1, Is.True);
			}
			else if (count2 == 2)
			{
				bulletId = 2;
				Assert.That(count1 == 1 && count3 == 1, Is.True);
			}
			else if (count3 == 2)
			{
				bulletId = 3;
				Assert.That(count1 == 1 && count2 == 1, Is.True);
			}
			Assert.That(bulletId != 0, Is.True);

			// Make sure both instances of the bulletId are associated with the bullet style.
			string bulletStyleStr = "w:pStyle w:val=\"Bullet Display Name\" /><w:numPr><w:ilvl w:val=\"0\" /><w:numId w:val=\"" + bulletId;
			Assert.That(Regex.Matches(resultStr, bulletStyleStr).Count == 2, Is.True);
		}

		[Test]
		public void GenerateContinueParagraph()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var senseOptions = new DictionaryNodeSenseOptions
			{
				NumberingStyle = "%d",
				NumberEvenASingleSense = true,
				DisplayEachSenseInAParagraph = true
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = DictionaryGlossStyleName
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = senseOptions,
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = SensesParagraphStyleName,
				Label = SensesParagraphDisplayName
			};
			var dateModifiedNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "DateModified",
				Label = "Date Modified",
				Before = " Modified on: ",
				IsEnabled = true,
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode, dateModifiedNode },
				Style = MainEntryParagraphStyleName,
				Label = MainEntryParagraphDisplayName
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;

			// There should be 3 paragraph styles, one for the main entry, one for the sense, and one for the continuation of the main entry.
			AssertThatXmlIn.String(result.DocBody.OuterXml).HasSpecifiedNumberOfMatchesForXpath(
				"/w:body/w:p/w:pPr/w:pStyle",
				3,
				WordNamespaceManager);

			// Assert that the continuation paragraph uses the continuation style.
			Assert.That(result.DocBody.OuterXml.Contains(MainEntryParagraphDisplayName + WordStylesGenerator.EntryStyleContinue), Is.True);
		}

		[Test]
		public void GetGuidewordStyleForConfiguredDictionary()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Gloss",
				DictionaryNodeOptions = wsOpts,
				Style = "Dictionary-Headword",
				Label = WordStylesGenerator.HeadwordDisplayName
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode },
				Style = DictionaryNormal
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				Style = MainEntryParagraphStyleName,
				Label = MainEntryParagraphDisplayName
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;

			//SUT
			List<string> firstHeadwordStyles = LcmWordGenerator.GetFirstGuidewordStylesList(result, DictionaryConfigurationModel.ConfigType.Root);

			Assert.That(firstHeadwordStyles.Count == 1, Is.True);
			Assert.That(firstHeadwordStyles[0] == "Headword[lang=en]", Is.True);
		}

		[Test]
		public void GetGuidewordStyleForClassifiedDictionary()
		{
			var classifiedClerk = CreateClassifiedClerk();
			m_propertyTable.SetProperty("ActiveClerk", classifiedClerk, false);
			m_propertyTable.SetProperty("currentContentControl", "lexiconClassifiedDictionary", false);

			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var domainNumberNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Abbreviation",
				After = " - ",
				DictionaryNodeOptions = wsOpts,
				Style = "Classified-Abbreviation",
				Label = WordStylesGenerator.Abbreviation
			};
			var domainNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				DictionaryNodeOptions = wsOpts,
				Style = "Classified-Name",
				Label = WordStylesGenerator.Name
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "CmSemanticDomain",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { domainNumberNode, domainNameNode },
				Style = MainEntryParagraphStyleName,
				Label = MainEntryParagraphDisplayName
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var domain = CreateSemanticDomain(Cache);
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(domain, mainEntryNode, null, DefaultSettings, 0) as DocFragment;

			//SUT
			List<string> firstGuidewordStyles = LcmWordGenerator.GetFirstGuidewordStylesList(result, DictionaryConfigurationModel.ConfigType.Lexeme);

			// For Classified Dictionary, the guidewords should consist of the following three pieces:
			// semantic domain number (abbreviation), after content associated with the semantic domain number, semantic domain name.
			Assert.That(firstGuidewordStyles.Count, Is.EqualTo(3));
			Assert.That(firstGuidewordStyles[0], Is.EqualTo(WordStylesGenerator.Abbreviation+"[lang=en]"));
			Assert.That(firstGuidewordStyles[1], Is.EqualTo(WordStylesGenerator.Abbreviation+WordStylesGenerator.BeforeAfterBetween+"[lang=en]"));
			Assert.That(firstGuidewordStyles[2], Is.EqualTo(WordStylesGenerator.Name + "[lang=en]"));

			// Reset activeclerk and currentContentControl to avoid affecting other tests.
			m_propertyTable.SetProperty("ActiveClerk", m_Clerk, false);
			m_propertyTable.SetProperty("currentContentControl", "lexiconDictionary", false);
		}

		private ITsString MakeMuliStyleTss(IEnumerable<string> content)
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var tsFact = TsStringUtils.TsStrFactory;
			var builder = tsFact.GetIncBldr();
			var lastStyle = "Dictionary-Gloss-Char";
			foreach (var runContent in content)
			{
				builder.AppendTsString(TsStringUtils.MakeString(runContent, wsEn, lastStyle));
				lastStyle = lastStyle.Equals("Dictionary-Gloss-Char") ? "Dictionary-Normal-Char" : "Dictionary-Gloss-Char";
			}
			return builder.GetString();
		}
	}
}

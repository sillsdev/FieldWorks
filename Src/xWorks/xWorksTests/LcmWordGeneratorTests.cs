// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.TestUtilities;
using XCore;
using static SIL.FieldWorks.XWorks.LcmWordGenerator;
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
				styles.Add(new BaseStyleInfo { Name = "Abbreviation", IsParagraphStyle = false });
			if (!styles.Contains("Dictionary-SenseNumber"))
				styles.Add(new BaseStyleInfo { Name = "Dictionary-SenseNumber", IsParagraphStyle = false });
			if (!styles.Contains("Style1"))
				styles.Add(new BaseStyleInfo { Name = "Style1", IsParagraphStyle = false });
			if (!styles.Contains(DictionaryGlossStyleName))
				styles.Add(new BaseStyleInfo { Name = DictionaryGlossStyleName, IsParagraphStyle = false });

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
			DefaultSettings.StylesGenerator.AddGlobalStyles(null, new ReadOnlyPropertyTable(m_propertyTable));

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
			RecordClerk clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false);
			clerk.SortName = "Headword";
			return clerk;
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
			AssertThatXmlIn.String(result.mainDocPart.RootElement.OuterXml).HasSpecifiedNumberOfMatchesForXpath(
				"/w:document/w:body/w:p/w:r/w:t[text()='gloss']",
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
			AssertThatXmlIn.String(result?.mainDocPart.RootElement?.OuterXml).HasSpecifiedNumberOfMatchesForXpath(
				"/w:document/w:body/w:p/w:r/w:br[@w:type='textWrapping']",
				2,
				WordNamespaceManager);
		}

		[Test]
		public void GenerateUniqueStyleName()
		{
			// This test needs to clear the style collection, else we may get flaky test results
			// because other tests may set the gloss Style to some other value; resulting in this
			// test getting unique style names like "Gloss3[lang='en']".
			LcmWordGenerator.ClearStyleCollection();
			// Always re-add the global styles after clearing the collection.
			DefaultSettings.StylesGenerator.AddGlobalStyles(null, new ReadOnlyPropertyTable(m_propertyTable));

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
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				Style = DictionaryNormal
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			glossNode.Style = "Abbreviation";
			var result2 = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;

			Assert.True(result.mainDocPart.RootElement.OuterXml.Contains("Gloss[lang='en']"));
			Assert.True(result2.mainDocPart.RootElement.OuterXml.Contains("Gloss2[lang='en']"));
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
			// 1. Sense style:					Sense Number[lang='en']
			// 2. Sense number before text:		BEF
			// 3. Sense number:					2
			// 4. Sense number after text:		AFT
			const string senseNumberTwoRun = "<w:t xml:space=\"preserve\">BEF</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Context : Sense Number[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">AFT</w:t></w:r>";
			Assert.True(result.mainDocPart.RootElement.OuterXml.Contains(senseNumberTwoRun));
		}

		[Test]
		public void GenerateBeforeBetweenAfterContent()
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
			var outXml = result.mainDocPart.RootElement.OuterXml;

			// Before text 'BE1' is before sense number '1' for 'gloss'.
			const string beforeFirstSense =
				"<w:t xml:space=\"preserve\">BE1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">gloss</w:t>";
			Assert.True(outXml.Contains(beforeFirstSense));

			// Between text 'TW1' is before sense number '2' for 'second gloss'.
			const string betweenSenses =
				"<w:t xml:space=\"preserve\">TW1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">second gloss</w:t>";
			Assert.True(outXml.Contains(betweenSenses));

			// Before text 'BE2' is before sense number '1' for 'second gloss2.1'.
			const string beforeFirstSubSense =
				"<w:t xml:space=\"preserve\">BE2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">second gloss2.1</w:t>";
			Assert.True(outXml.Contains(beforeFirstSubSense));

			// Between text 'TW2' is before sense number '2' for 'second gloss2.2'.
			const string betweenSubSenses =
				"<w:t xml:space=\"preserve\">TW2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Sense Number[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">second gloss2.2</w:t>";
			Assert.True(outXml.Contains(betweenSubSenses));

			// After text 'AF2' is after 'second gloss2.2'.
			const string afterSubSenses =
				"<w:t xml:space=\"preserve\">second gloss2.2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Context : SensesOS\" /></w:rPr><w:t xml:space=\"preserve\">AF2</w:t>";
			Assert.True(outXml.Contains(afterSubSenses));

			// After text 'AF1' is after 'AF2'.
			const string afterSenses =
				"<w:t xml:space=\"preserve\">AF2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Context : SensesOS\" /></w:rPr><w:t xml:space=\"preserve\">AF1</w:t>";
			Assert.True(outXml.Contains(afterSenses));
		}

		[Test]
		public void GenerateBeforeBetweenAfterContentWithWSAbbreviation()
		{
			var wsOpts = new DictionaryNodeWritingSystemOptions
			{
				Options = new List<DictionaryNodeListOptions.DictionaryNodeOption>
				{
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "en" },
					new DictionaryNodeListOptions.DictionaryNodeOption { Id = "es" }
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
			var wsEs = Cache.WritingSystemFactory.GetWsFromStr("es");
			testEntry.SensesOS.First().Gloss.set_String(wsEs, TsStringUtils.MakeString("glossES", wsEs));

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0) as DocFragment;
			var outXml = result.mainDocPart.RootElement.OuterXml;

			// Before text 'BE3' is after the sense number '1' and before the english abbreviation, which is before 'gloss'.
			const string beforeAbbreviation =
				"<w:t xml:space=\"preserve\">1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Context : Gloss[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">BE3</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Writing System Abbreviation\" /></w:rPr><w:t xml:space=\"preserve\">Eng </w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">gloss</w:t>";
			Assert.True(outXml.Contains(beforeAbbreviation));

			// Between text 'TW3' is before the spanish abbreviation, which is before 'glossES'.
			const string betweenAbbreviation =
				"<w:t xml:space=\"preserve\">TW3</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Writing System Abbreviation\" /></w:rPr><w:t xml:space=\"preserve\">Spa </w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang='es']\" /></w:rPr><w:t xml:space=\"preserve\">glossES</w:t>";
			Assert.True(outXml.Contains(betweenAbbreviation));

			// After text 'AF3' is after 'glossES'.
			const string afterAbbreviation =
				"<w:t xml:space=\"preserve\">glossES</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Context : Gloss[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">AF3</w:t>";
			Assert.True(outXml.Contains(afterAbbreviation));
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
			var outXml = result.mainDocPart.RootElement.OuterXml;

			// The property before text 'BE4' is first, followed by the style that is applied to the property, 'DisplayNameBase'.
			const string beforeAndStyle = "<w:t xml:space=\"preserve\">BE4</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"DisplayNameBase\" /></w:rPr>";
			Assert.True(outXml.Contains(beforeAndStyle));

			// The property after text 'AF4' was written.
			Assert.True(outXml.Contains("AF4"));
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
			var outXml = result.mainDocPart.RootElement.OuterXml;
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
			Assert.True(result.mainDocPart.RootElement.OuterXml.Contains(MainEntryParagraphDisplayName));
			Assert.True(result.mainDocPart.RootElement.OuterXml.Contains(SensesParagraphDisplayName));
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
			AssertThatXmlIn.String(result.mainDocPart.RootElement.OuterXml).HasSpecifiedNumberOfMatchesForXpath(
				"/w:document/w:body/w:p",
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
			string resultStr = result.mainDocPart.RootElement.OuterXml;
			int count1 = Regex.Matches(resultStr, "<w:numId w:val=\"1\" />").Count;
			int count2 = Regex.Matches(resultStr, "<w:numId w:val=\"2\" />").Count;
			int count3 = Regex.Matches(resultStr, "<w:numId w:val=\"3\" />").Count;
			int bulletId = 0;
			if (count1 == 2)
			{
				bulletId = 1;
				Assert.True(count2 == 1 && count3 == 1);
			}
			else if (count2 == 2)
			{
				bulletId = 2;
				Assert.True(count1 == 1 && count3 == 1);
			}
			else if (count3 == 2)
			{
				bulletId = 3;
				Assert.True(count1 == 1 && count2 == 1);
			}
			Assert.True(bulletId != 0);

			// Make sure both instances of the bulletId are associated with the bullet style.
			string bulletStyleStr = "w:pStyle w:val=\"Bullet Display Name\" /><w:numPr><w:ilvl w:val=\"0\" /><w:numId w:val=\"" + bulletId;
			Assert.True(Regex.Matches(resultStr, bulletStyleStr).Count == 2);
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
			AssertThatXmlIn.String(result.mainDocPart.RootElement.OuterXml).HasSpecifiedNumberOfMatchesForXpath(
				"/w:document/w:body/w:p/w:pPr/w:pStyle",
				3,
				WordNamespaceManager);

			// Assert that the continuation paragraph uses the continuation style.
			Assert.True(result.mainDocPart.RootElement.OuterXml.Contains(MainEntryParagraphDisplayName + WordStylesGenerator.EntryStyleContinue));
		}

		[Test]
		public void GetFirstHeadwordStyle()
		{
			LcmWordGenerator.ClearStyleCollection();
			DefaultSettings.StylesGenerator.AddGlobalStyles(null, new ReadOnlyPropertyTable(m_propertyTable));
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
			string firstHeadwordStyle = LcmWordGenerator.GetFirstGuidewordStyle(result, DictionaryConfigurationModel.ConfigType.Root);

			Assert.True(firstHeadwordStyle == "Headword[lang='en']");
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

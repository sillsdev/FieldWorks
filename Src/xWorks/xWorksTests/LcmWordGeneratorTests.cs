// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms.VisualStyles;
using System.Xml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainImpl;
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

		private const string DictionaryNormal = "Dictionary-Normal";

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
			if (!styles.Contains(DictionaryNormal))
				styles.Add(new BaseStyleInfo { Name = DictionaryNormal });
			if (!styles.Contains("Dictionary-Headword"))
				styles.Add(new BaseStyleInfo { Name = "Dictionary-Headword", IsParagraphStyle = false});
			if (!styles.Contains("Abbreviation"))
				styles.Add(new BaseStyleInfo { Name = "Abbreviation", IsParagraphStyle = false });
			if (!styles.Contains("Dictionary-SenseNumber"))
				styles.Add(new BaseStyleInfo { Name = "Dictionary-SenseNumber", IsParagraphStyle = false });
			if (!styles.Contains("Style1"))
				styles.Add(new BaseStyleInfo { Name = "Style1", IsParagraphStyle = false });

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
			const string senseNumberTwoRun = "<w:r><w:rPr><w:rStyle w:val=\"Sense Number[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">BEF</w:t><w:t xml:space=\"preserve\">2</w:t><w:t xml:space=\"preserve\">AFT</w:t></w:r>";
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
				"<w:t xml:space=\"preserve\">second gloss2.2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Context\" /></w:rPr><w:t xml:space=\"preserve\">AF2</w:t>";
			Assert.True(outXml.Contains(afterSubSenses));

			// After text 'AF1' is after 'AF2'.
			const string afterSenses =
				"<w:t xml:space=\"preserve\">AF2</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Context\" /></w:rPr><w:t xml:space=\"preserve\">AF1</w:t>";
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
				"<w:t xml:space=\"preserve\">1</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Context\" /></w:rPr><w:t xml:space=\"preserve\">BE3</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Writing System Abbreviation\" /></w:rPr><w:t xml:space=\"preserve\">Eng </w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang='en']\" /></w:rPr><w:t xml:space=\"preserve\">gloss</w:t>";
			Assert.True(outXml.Contains(beforeAbbreviation));

			// Between text 'TW3' is before the spanish abbreviation, which is before 'glossES'.
			const string betweenAbbreviation =
				"<w:t xml:space=\"preserve\">TW3</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Writing System Abbreviation\" /></w:rPr><w:t xml:space=\"preserve\">Spa </w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Gloss[lang='es']\" /></w:rPr><w:t xml:space=\"preserve\">glossES</w:t>";
			Assert.True(outXml.Contains(betweenAbbreviation));

			// After text 'AF3' is after 'glossES'.
			const string afterAbbreviation =
				"<w:t xml:space=\"preserve\">glossES</w:t></w:r><w:r><w:rPr><w:rStyle w:val=\"Context\" /></w:rPr><w:t xml:space=\"preserve\">AF3</w:t>";
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
	}
}

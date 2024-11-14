// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using XCore;
using Formatting = Newtonsoft.Json.Formatting;
// ReSharper disable StringLiteralTypo

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class LcmJsonGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase, IDisposable
	{
		private int m_wsEn, m_wsFr;

		private FwXApp m_application;
		private FwXWindow m_window;
		private PropertyTable m_propertyTable;
		private Mediator m_mediator;
		private RecordClerk m_Clerk;

		private const string DictionaryNormal = "Dictionary-Normal";

		private ConfiguredLcmGenerator.GeneratorSettings DefaultSettings => new ConfiguredLcmGenerator.GeneratorSettings(Cache,
			new ReadOnlyPropertyTable(m_propertyTable), true, false, null) { ContentGenerator = new LcmJsonGenerator(Cache) };

		private DictionaryPublicationDecorator DefaultDecorator => new DictionaryPublicationDecorator(Cache,
			(ISilDataAccessManaged)Cache.MainCacheAccessor, Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);

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

			var styles = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable).Styles;
			if (!styles.Contains(DictionaryNormal))
				styles.Add(new BaseStyleInfo { Name = DictionaryNormal });

			m_Clerk = CreateClerk();
			m_propertyTable.SetProperty("ActiveClerk", m_Clerk, false);

			m_propertyTable.SetProperty("currentContentControl", "lexiconDictionary", false);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/");
			m_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			m_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
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

		~LcmJsonGeneratorTests()
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
		public void GenerateJsonForEntry_OneSenseWithGlossGeneratesCorrectResult()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetSenseNodeOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0).ToString();
			Console.WriteLine(result);
			var expectedResult = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""senses"": [{""guid"":""g" + entry.Guid + @""",""gloss"": [{""lang"":""en"",""value"":""gloss""}]},]}";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResult);
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_DefinitionOrGloss_HandlePerWS()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en", "es" });
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "DefinitionOrGloss", DictionaryNodeOptions = wsOpts, CSSClassNameOverride = "definitionOrGloss"}
				},
				DictionaryNodeOptions = new DictionaryNodeSenseOptions()

			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var wsEs = ConfiguredXHTMLGeneratorTests.EnsureWritingSystemSetup(Cache, "es", false);
			entry.SensesOS.First().Definition.set_String(wsEs, "definition");
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0).ToString();

			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""senses"": [{""guid"":""g" + entry.Guid + @""", ""definitionorgloss"": [{""lang"":""en"",""value"":""gloss""},
				{""lang"":""es"",""value"":""definition""}]}]}";
			//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults);
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_TwoSensesWithSameInfoShowGramInfoFirst_Json()
		{
			var DictionaryNodeSenseOptions = new DictionaryNodeSenseOptions
			{
				ShowSharedGrammarInfoFirst = true
			};
			var categorynfo = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				Children = new List<ConfigurableDictionaryNode>(),
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" })
			};
			var gramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "msas",
				Children = new List<ConfigurableDictionaryNode> { categorynfo }
			};
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = DictionaryNodeSenseOptions,
				Children = new List<ConfigurableDictionaryNode> { gramInfoNode, glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			var posSeq = Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS;
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			posSeq.Add(pos);
			var sense = entry.SensesOS.First();

			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			sense.MorphoSyntaxAnalysisRA = msa;

			msa.PartOfSpeechRA = pos;
			msa.PartOfSpeechRA.Abbreviation.set_String(m_wsEn, "Blah");
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(entry, "second sense", m_wsEn, Cache);
			var secondMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			var secondSense = entry.SensesOS[1];
			entry.MorphoSyntaxAnalysesOC.Add(secondMsa);
			secondSense.MorphoSyntaxAnalysisRA = secondMsa;
			secondMsa.PartOfSpeechRA = pos;

			var settings = new ConfiguredLcmGenerator.GeneratorSettings(Cache, m_propertyTable, false, false, null) { ContentGenerator = new LcmJsonGenerator(Cache) };
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings, 0).ToString();
			Console.WriteLine(result);
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""msas"": {""mlpartofspeech"": [{""lang"":""en"",""value"":""Blah""}]}, ""senses"": [{""guid"":""g" + entry.Guid + @""",""gloss"": [{""lang"":""en"",""value"":""gloss""}]},
				{""guid"":""g" + entry.Guid + @""",""gloss"": [{""lang"":""en"",""value"":""second sense""}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_OneSenseWithSinglePicture()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var thumbNailNode = new ConfigurableDictionaryNode { FieldDescription = "PictureFileRA", CSSClassNameOverride = "photo" };
			var senseNumberNode = new ConfigurableDictionaryNode { FieldDescription = "SenseNumberTSS", CSSClassNameOverride = "sensenumber" };
			var captionNode = new ConfigurableDictionaryNode { FieldDescription = "Caption", DictionaryNodeOptions = wsOpts };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { thumbNailNode, senseNumberNode, captionNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Senses",
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { sensesNode, pictureNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var sense = entry.SensesOS[0];
			var sensePic = ConfiguredXHTMLGeneratorTests.CreatePicture(Cache);
			sense.PicturesOS.Add(sensePic);

			var settings = DefaultSettings;

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, settings, 0).ToString();
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""pictures"": [{""guid"":""g" + sensePic.Guid + @""",""src"":""pictures/test_auth_copy_license.jpg"",
				""sensenumber"": [{""lang"":""en"",""value"":""1""}],""caption"": [{""lang"":""en"",""value"":""caption""}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_FilterByPublication()
		{
			// Note that my HS French is nonexistent after 40+ years.  But this is only test code...
			var typeMain = ConfiguredXHTMLGeneratorTests.CreatePublicationType("main", Cache);
			var typeTest = ConfiguredXHTMLGeneratorTests.CreatePublicationType("test", Cache);

			// This entry is published for both main and test.  Its first sense (and example) are published in main, its
			// second sense(and example) are published in test.
			// The second example of the first sense should not be published at all, since it is not published in main and
			// its owner is not published in test.
			var entryCorps = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "corps", "body");
			entryCorps.DoNotPublishInRC.Remove(typeMain);
			entryCorps.DoNotPublishInRC.Remove(typeTest);
			var pronunciation = ConfiguredXHTMLGeneratorTests.AddPronunciationToEntry(entryCorps, "pronunciation", m_wsFr, Cache);
			var exampleCorpsBody1 = ConfiguredXHTMLGeneratorTests.AddExampleToSense(entryCorps.SensesOS[0], "Le corps est gros.", Cache, m_wsFr, m_wsEn, "The body is big.");
			var exampleCorpsBody2 = ConfiguredXHTMLGeneratorTests.AddExampleToSense(entryCorps.SensesOS[0], "Le corps est esprit.", Cache, m_wsFr, m_wsEn, "The body is spirit.");
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(entryCorps, "corpse", m_wsEn, Cache);
			ConfiguredXHTMLGeneratorTests.AddExampleToSense(entryCorps.SensesOS[1], "Le corps est mort.", Cache, m_wsFr, m_wsEn, "The corpse is dead.");

			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			sensePic.Caption.set_String(wsFr, TsStringUtils.MakeString("caption", wsFr));
			entryCorps.SensesOS[0].PicturesOS.Add(sensePic);

			pronunciation.DoNotPublishInRC.Add(typeTest);
			entryCorps.SensesOS[0].DoNotPublishInRC.Add(typeTest);
			exampleCorpsBody1.DoNotPublishInRC.Add(typeTest);
			exampleCorpsBody2.DoNotPublishInRC.Add(typeMain);   // should not show at all!
			sensePic.DoNotPublishInRC.Add(typeTest);

			entryCorps.SensesOS[1].DoNotPublishInRC.Add(typeMain);
			//exampleCorpsCorpse1.DoNotPublishInRC.Add(typeMain); -- should not show in main because its owner is not shown there

			// This entry is published only in main, together with its sense and example.
			var entryBras = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "bras", "arm");
			entryBras.DoNotPublishInRC.Remove(typeMain);
			ConfiguredXHTMLGeneratorTests.AddExampleToSense(entryBras.SensesOS[0], "Mon bras est casse.", Cache, m_wsFr, m_wsEn, "My arm is broken.");
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(entryBras, "hand", m_wsEn, Cache);
			ConfiguredXHTMLGeneratorTests.AddExampleToSense(entryBras.SensesOS[1], "Mon bras va bien.", Cache, m_wsFr, m_wsEn, "My arm is fine.");
			entryBras.DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[0].DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[1].DoNotPublishInRC.Add(typeTest);
			//exampleBrasArm1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there
			//exampleBrasHand1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there

			// This entry is published only in test, together with its sense and example.
			var entryOreille = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "oreille", "ear");
			ConfiguredXHTMLGeneratorTests.AddExampleToSense(entryOreille.SensesOS[0], "Lac Pend d'Oreille est en Idaho.", Cache, m_wsFr, m_wsEn, "Lake Pend d'Oreille is in Idaho.");
			entryOreille.DoNotPublishInRC.Add(typeMain);
			entryOreille.SensesOS[0].DoNotPublishInRC.Add(typeMain);
			entryOreille.DoNotPublishInRC.Remove(typeTest);
			entryOreille.SensesOS[0].DoNotPublishInRC.Remove(typeTest);
			//exampleOreille1.DoNotPublishInRC.Add(typeMain); -- should not show in main because its owner is not shown there

			var entryEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "entry", "entry");
			entryEntry.DoNotPublishInRC.Remove(typeMain);
			entryEntry.DoNotPublishInRC.Remove(typeTest);
			var entryMainsubentry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "mainsubentry", "mainsubentry");
			entryMainsubentry.DoNotPublishInRC.Remove(typeMain);
			entryMainsubentry.DoNotPublishInRC.Add(typeTest);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, entryEntry, entryMainsubentry, true);

			var entryTestsubentry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "testsubentry", "testsubentry");
			entryTestsubentry.DoNotPublishInRC.Add(typeMain);
			entryTestsubentry.DoNotPublishInRC.Remove(typeTest);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, entryEntry, entryTestsubentry, true);
			var bizarroVariant = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "bizarre", "myVariant");
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, entryEntry, bizarroVariant, "Spelling Variant");
			bizarroVariant.DoNotPublishInRC.Remove(typeMain);
			bizarroVariant.DoNotPublishInRC.Add(typeTest);

			// Note that the decorators must be created (or refreshed) *after* the data exists.
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var pubMain = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeMain);
			var pubTest = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeTest);
			//SUT
			var hvosMain = new List<int>(pubMain.GetEntriesToPublish(m_propertyTable, flidVirtual));
			Assert.AreEqual(5, hvosMain.Count, "there are five entries in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryCorps.Hvo), "corps is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryBras.Hvo), "bras is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(bizarroVariant.Hvo), "bizarre is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryOreille.Hvo), "oreille is not shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryEntry.Hvo), "entry is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryMainsubentry.Hvo), "mainsubentry is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryTestsubentry.Hvo), "testsubentry is not shown in the main publication");
			var hvosTest = new List<int>(pubTest.GetEntriesToPublish(m_propertyTable, flidVirtual));
			Assert.AreEqual(4, hvosTest.Count, "there are four entries in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryCorps.Hvo), "corps is shown in the test publication");
			Assert.IsFalse(hvosTest.Contains(entryBras.Hvo), "bras is not shown in the test publication");
			Assert.IsFalse(hvosTest.Contains(bizarroVariant.Hvo), "bizarre is not shown in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryOreille.Hvo), "oreille is shown in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryEntry.Hvo), "entry is shown in the test publication");
			Assert.IsFalse(hvosTest.Contains(entryMainsubentry.Hvo), "mainsubentry is shown in the test publication");
			Assert.IsTrue(hvosTest.Contains(entryTestsubentry.Hvo), "testsubentry is shown in the test publication");

			var variantFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				CSSClassNameOverride = "headword",
				SubField = "HeadWordRef",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "vernacular" })
			};
			var variantTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
			};
			var variantNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { variantTypeNode, variantFormNode },
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache)
			};
			var subHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLHeadWord",
				CSSClassNameOverride = "subentry",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var subentryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { subHeadwordNode }
			};
			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" }),
				CSSClassNameOverride = "translatedsentence"
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "TranslationsOC",
				Children = new List<ConfigurableDictionaryNode> { translationNode },
				CSSClassNameOverride = "translations"
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Example",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "examplesentence"
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ExamplesOS",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode },
				CSSClassNameOverride = "examples"
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "DefinitionOrGloss",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" }),
				CSSClassNameOverride = "definitionorgloss"
			};
			var captionNode = new ConfigurableDictionaryNode { FieldDescription = "Caption", DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }) };
			var pictureNode = new ConfigurableDictionaryNode
			{
				DictionaryNodeOptions = new DictionaryNodePictureOptions(),
				FieldDescription = "PicturesOfSenses",
				CSSClassNameOverride = "Pictures",
				Children = new List<ConfigurableDictionaryNode> { captionNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions(),
				Children = new List<ConfigurableDictionaryNode> { glossNode, examplesNode },
				CSSClassNameOverride = "senses"
			};
			var pronunciationForm = new ConfigurableDictionaryNode
			{
				FieldDescription = "Form",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainPronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { pronunciationForm }
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, mainPronunciationsNode, sensesNode, pictureNode, subentryNode, variantNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			var output = ConfiguredLcmGenerator.GenerateContentForEntry(entryEntry, mainEntryNode, pubMain, DefaultSettings, 0).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			var expectedResults = "{\"xhtmlTemplate\": \"lexentry\",\"guid\":\"g" + entryEntry.Guid + "\",\"letterHead\": \"e\",\"sortIndex\": 0," +
								  "\"entry\": [{\"lang\":\"fr\",\"value\":\"entry\"}],\"senses\": [{\"guid\":\"g" +
								  entryEntry.Guid + "\",\"definitionorgloss\": [{\"lang\":\"en\",\"value\":\"entry\"}]}]," +
								  "\"subentries\": [{\"subentry\": [{\"lang\":\"fr\",\"value\":\"mainsubentry\"}]}]," +
								  "\"variantformentrybackrefs\": [{\"referenceType\":\"{\\\"name\\\": [{\\\"lang\\\":\\\"en\\\"," +
								  "\\\"value\\\":\\\"Spelling Variant\\\"}],},\",\"references\":[{\"headword\": [{\"lang\":\"fr\",\"guid\": \"g" +
								  bizarroVariant.Guid + "\", \"value\":\"bizarre\"}]}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(output, expected);
		}

		[Test]
		public void GenerateJsonForEntry_TypeAfterForm()
		{
			var typeMain = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0];

			var entryEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "entry", "entry");
			var entryMainsubentry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "mainsubentry", "mainsubentry");
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, entryEntry, entryMainsubentry, true);

			var bizarroVariant = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "bizarre", "myVariant");
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, entryEntry, bizarroVariant, "Spelling Variant");

			// Note that the decorators must be created (or refreshed) *after* the data exists.
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubMain = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeMain);

			var variantFormNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwningEntry",
				CSSClassNameOverride = "headword",
				SubField = "HeadWordRef",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "vernacular" })
			};
			var variantTypeNameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Name",
				IsEnabled = true,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var variantTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { variantTypeNameNode },
			};
			var variantNodeTypeAfter = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { variantFormNode, variantTypeNode },
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Variant, Cache)
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNodeTypeAfter = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, variantNodeTypeAfter },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNodeTypeAfter);

			//SUT
			var outputTypeAfter = ConfiguredLcmGenerator.GenerateContentForEntry(entryEntry, mainEntryNodeTypeAfter, pubMain, DefaultSettings, 0).ToString();
			Assert.That(outputTypeAfter, Is.Not.Null.Or.Empty);
			var expectedResultsTypeAfter = "{\"xhtmlTemplate\": \"lexentry\",\"guid\":\"g" + entryEntry.Guid + "\",\"letterHead\": \"e\",\"sortIndex\": 0," +
								  "\"entry\": [{\"lang\":\"fr\",\"value\":\"entry\"}]," +
								  "\"variantformentrybackrefs\": [{\"references\":[{\"headword\": [{\"lang\":\"fr\",\"guid\": \"g" +
								  bizarroVariant.Guid + "\", \"value\":\"bizarre\"}]}]," +
								  "\"referenceType\":\"{\\\"name\\\": [{\\\"lang\\\":\\\"en\\\",\\\"value\\\":\\\"Spelling Variant\\\"}],},\"}]}";
			var expectedTypeAfter = (JObject)JsonConvert.DeserializeObject(expectedResultsTypeAfter, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(outputTypeAfter, expectedTypeAfter);
		}

		[Test]
		public void GenerateJsonForEntry_WsAudiowithHyperlink()
		{
			CoreWritingSystemDefinition wsEnAudio;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("en-Zxxx-x-audio", out wsEnAudio);
			Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsEnAudio);
			var headwordNode = new ConfigurableDictionaryNode
			{
				CSSClassNameOverride = "headword",
				FieldDescription = "LexemeFormOA",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en-Zxxx-x-audio" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var senseaudio = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entryOne.LexemeFormOA = senseaudio;
			const string audioFileName = "Test Audi'o.wav";
			senseaudio.Form.set_String(wsEnAudio.Handle, TsStringUtils.MakeString(audioFileName, wsEnAudio.Handle));
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, DefaultSettings, 0).ToString();

			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"": ""g" + entryOne.Guid +
								  @""",""letterHead"": ""c"",""sortIndex"": 0, ""headword"": [{""guid"": ""g" + entryOne.Guid + @""", ""lang"":""en-Zxxx-x-audio"", ""value"": {""id"": ""gTest_Audi_o"", ""src"": ""AudioVisual/Test Audi'o.wav""}}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_SensibleJsonForVideoFiles()
		{
			var entryCorps = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "corps", "body");
			var pronunciation = ConfiguredXHTMLGeneratorTests.AddPronunciationToEntry(entryCorps, "pronunciation", m_wsFr, Cache);
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(entryCorps, "corpse", m_wsEn, Cache);
			var mainMediaFile = Cache.ServiceLocator.GetInstance<ICmMediaFactory>().Create();
			pronunciation.MediaFilesOS.Add(mainMediaFile);
			var mainFile = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var localMediaFolder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(localMediaFolder);
			localMediaFolder.FilesOC.Add(mainFile);
			// InternalPath is null by default, but trying to set it to null throws an exception
			mainFile.InternalPath = "fileName.mp4";
			mainMediaFile.MediaFileRA = mainFile;

			var mediaFileNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MediaFileRA",
				IsEnabled = true
			};
			var mediaNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MediaFilesOS",
				CSSClassNameOverride = "mediafiles",
				IsEnabled = true,
				Children = new List<ConfigurableDictionaryNode> { mediaFileNode }
			};
			var mainPronunciationsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "PronunciationsOS",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { mediaNode }
			};
			var mainHeadwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "HeadWord",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" }),
				CSSClassNameOverride = "entry"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { mainHeadwordNode, mainPronunciationsNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			var output = ConfiguredLcmGenerator.GenerateContentForEntry(entryCorps, mainEntryNode, DefaultDecorator, DefaultSettings, 0).ToString();
			Assert.That(output, Is.Not.Null.Or.Empty);
			var expectedResults = "{\"xhtmlTemplate\":\"lexentry\",\"guid\":\"g" + entryCorps.Guid + "\",\"letterHead\":\"c\",\"sortIndex\":0," +
								  "\"entry\": [{\"lang\":\"fr\",\"value\":\"corps\"}]," +
								  "\"pronunciations\": [{\"mediafiles\": [{\"value\": {\"id\":\"g" + mainMediaFile.Guid + "\",\"src\": \"AudioVisual/fileName.mp4\"}}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(output, expected);
		}

		[Test]
		public void GenerateJsonForEntry_SenseNumbersGeneratedForMultipleSenses()
		{
			var wsOpts = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" });
			var glossNode = new ConfigurableDictionaryNode { FieldDescription = "Gloss", DictionaryNodeOptions = wsOpts };
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "Senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions { NumberingStyle = "%d" },
				Children = new List<ConfigurableDictionaryNode> { glossNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(testEntry, "second gloss", m_wsEn, Cache);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, DefaultDecorator, DefaultSettings, 0).ToString();
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + testEntry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,""senses"":[{""senseNumber"":""1"",
				""guid"":""g" + testEntry.Guid + @""",""gloss"":[{""lang"":""en"",""value"":""gloss""}]},
				{""senseNumber"":""2"",""guid"":""g" + testEntry.Guid + @""",""gloss"":[{""lang"":""en"",""value"":""second gloss""}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_EmbeddedWritingSystemGeneratesCorrectResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Bibliography",
				CSSClassNameOverride = "bib",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr", "en" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var englishStr = TsStringUtils.MakeString("English", m_wsEn);
			var frenchString = TsStringUtils.MakeString("French with  embedded", m_wsFr);
			var multiRunString = frenchString.Insert(12, englishStr);
			entry.Bibliography.set_String(m_wsFr, multiRunString);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0).ToString();
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,""bib"": [{""lang"":""fr"",""value"":""French with ""},
				{""lang"":""en"",""value"":""English""},{""lang"":""fr"",""value"":"" embedded""}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_UnicodeLineBreak_GeneratesValidJson()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Bibliography",
				CSSClassNameOverride = "bib",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr", "en" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var englishStr = TsStringUtils.MakeString("English\u2028with line break", m_wsEn);
			entry.Bibliography.set_String(m_wsFr, englishStr);
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings, 0).ToString();
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""bib"": [{""lang"":""en"",""value"":""English\nwith line break""}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_GeneratesForwardNameForForwardLexicalRelations()
		{
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var referencedEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			const string refTypeName = "TestRefType";
			const string refTypeRevName = "sURsyoT";
			ConfiguredXHTMLGeneratorTests.CreateLexicalReference(Cache, mainEntry.SensesOS.First(), referencedEntry, refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.That(refType, Is.Not.Null);

			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" })
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid + ":f" })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, DefaultSettings, 0).ToString();
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + mainEntry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""sensesos"": [{""lexsensereferences"": [{""ownertype_name"": [{""lang"":""en"",""value"":""TestRefType""}]}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_EmptyNameOnLexicalRelation_GeneratesEmptyButValidContent()
		{
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var referencedEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateLexicalReference(Cache, mainEntry.SensesOS.First(), referencedEntry, "");
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(pos => pos.Name.BestAnalysisAlternative.Text == "***");
			var nameNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "OwnerType",
				SubField = "Name",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" })
			};
			var referencesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexSenseReferences",
				DictionaryNodeOptions = new DictionaryNodeListOptions
				{
					ListId = DictionaryNodeListOptions.ListIds.Sense,
					Options = DictionaryDetailsControllerTests.ListOfEnabledDNOsFromStrings(new[] { refType.Guid.ToString() })
				},
				Children = new List<ConfigurableDictionaryNode> { nameNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { referencesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(mainEntry, mainEntryNode, null, DefaultSettings, 0).ToString();
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + mainEntry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""sensesos"": [{""lexsensereferences"": [{}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_HomographNumbersGeneratesCorrectResult()
		{
			var citationForm = new ConfigurableDictionaryNode
			{
				FieldDescription = "CitationForm",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var homographNum = new ConfigurableDictionaryNode { FieldDescription = "HomographNumber" };
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { homographNum, citationForm },
				FieldDescription = "LexEntry"
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var entryTwo = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, DefaultSettings, 0).ToString();
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entryOne.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,""homographnumber"": ""1"",
				""citationform"": [{""lang"":""fr"",""value"":""Citation""}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
			result = ConfiguredLcmGenerator.GenerateContentForEntry(entryTwo, mainEntryNode, null, DefaultSettings, 0).ToString();
			expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entryTwo.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,""homographnumber"": ""2"",
				""citationform"": [{""lang"":""fr"",""value"":""Citation""}]}";
			expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);

		}

		[Test]
		public void GenerateJsonForEntry_GeneratesSpecifiedSortIndex()
		{
			var citationForm = new ConfigurableDictionaryNode
			{
				FieldDescription = "CitationForm",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { citationForm },
				FieldDescription = "LexEntry"
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entryOne = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, DefaultSettings, 36).ToString();
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entryOne.Guid + @""",""letterHead"": ""c"",""sortIndex"": 36,
				""citationform"": [{""lang"":""fr"",""value"":""Citation""}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
			// default value of -1
			result = ConfiguredLcmGenerator.GenerateContentForEntry(entryOne, mainEntryNode, null, DefaultSettings).ToString();
			expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entryOne.Guid + @""",""letterHead"": ""c"",""sortIndex"": -1,
				""citationform"": [{""lang"":""fr"",""value"":""Citation""}]}";
			expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);

		}

		[Test]
		public void GenerateJsonForEntry_TwoDifferentPicturesGetUniqueWebFriendlyPaths()
		{
			var mainEntryNode = ConfiguredXHTMLGeneratorTests.CreatePictureModel();
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(testEntry, "second", m_wsEn, Cache);
			var sense = testEntry.SensesOS[0];
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			var sense2 = testEntry.SensesOS[1];
			var sensePic2 = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense2.PicturesOS.Add(sensePic2);
			var pic2 = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder2 = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			Cache.LangProject.MediaOC.Add(folder2);
			folder.FilesOC.Add(pic);
			folder2.FilesOC.Add(pic2);
			var fileName = Path.GetRandomFileName();
			var tempPath1 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempPath1);
			var tempPath2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempPath2);
			// Write a couple of jpeg header bytes (for no particular reason)
			var filePath1 = Path.Combine(tempPath1, fileName);
			File.WriteAllBytes(filePath1, new byte[] { 0xFF, 0xE0, 0x0, 0x0, 0x1 });
			var filePath2 = Path.Combine(tempPath2, fileName);
			File.WriteAllBytes(filePath2, new byte[] { 0xFF, 0xE0, 0x0, 0x0, 0x2 });
			pic.InternalPath = filePath1;
			pic2.InternalPath = filePath2;
			sensePic.PictureFileRA = pic;
			sensePic2.PictureFileRA = pic2;

			var tempFolder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ConfigDictPictureExportTest"));
			try
			{
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0).ToString();

				// Bug: The second filename should be different after the export with relative path settings (fix later)
				var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + testEntry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
					""pictures"": [{""guid"":""g" + sensePic.Guid + @""",""src"":""pictures/" + fileName + @"""},
					{""guid"":""g" + sensePic2.Guid + @""",""src"":""pictures/" + fileName + @"""}],}";
				var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
				VerifyJson(result, expected);
			}
			finally
			{
				IO.RobustIO.DeleteDirectoryAndContents(tempFolder.FullName);
				File.Delete(filePath1);
				File.Delete(filePath2);
			}
		}

		[Test]
		public void GenerateJsonForEntry_MinorComplexForm_TemplateTypeCorrect_GeneratesGloss()
		{
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var lexentry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(lexentry, "gloss2", wsEn, Cache);
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(lexentry, string.Empty, wsEn, Cache);
			lexentry.SummaryDefinition.SetAnalysisDefaultWritingSystem("MainEntrySummaryDefn");
			lexentry.SensesOS[0].Definition.SetAnalysisDefaultWritingSystem("MainEntryS1Defn");
			lexentry.SensesOS[2].Definition.SetAnalysisDefaultWritingSystem("MainEntryS3Defn");

			var subentry1 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, lexentry, subentry1, true); // subentry references main ILexEntry

			var subentry2 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, lexentry.SensesOS[1], subentry2, true); // subentry references 2nd ILexSense

			var subentry3 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, lexentry.SensesOS[2], subentry3, true); // subentry references 3rd ILexSense

			var glossOrSummDefnNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "GlossOrSummary",
				Label = "Gloss (or Summary Definition)",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "analysis" })
			};
			var refentryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { glossOrSummDefnNode },
				FieldDescription = "ConfigReferencedEntries",
				Label = "Referenced Entries",
				CSSClassNameOverride = "referencedentries"
			};
			var ComponentsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "ComplexFormEntryRefs",
				Label = "Components",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(DictionaryNodeListOptions.ListIds.Complex, Cache),
				Children = new List<ConfigurableDictionaryNode> { refentryNode }
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { ComponentsNode },
				FieldDescription = "LexEntry",
				CSSClassNameOverride = "minorentrycomplex",
			};
			CssGeneratorTests.PopulateFieldsForTesting(minorEntryNode);
;
			//SUT
			var result = ConfiguredLcmGenerator.GenerateContentForEntry(subentry1, minorEntryNode, null, DefaultSettings, 0).ToString();
			var expectedResults = @"{""xhtmlTemplate"":""minorentrycomplex"",""guid"":""g" + subentry1.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""complexformentryrefs"": [{""referencedentries"": [{""glossorsummary"": [{""lang"":""en"",""value"":""MainEntrySummaryDefn""}]}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);

			//SUT
			var result2 = ConfiguredLcmGenerator.GenerateContentForEntry(subentry2, minorEntryNode, null, DefaultSettings, 0).ToString();
			expectedResults = @"{""xhtmlTemplate"":""minorentrycomplex"",""guid"":""g" + subentry2.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""complexformentryrefs"": [{""referencedentries"": [{""glossorsummary"": [{""lang"":""en"",""value"":""gloss2""}]}]}]}";
			expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result2, expected);

			//SUT
			var result3 = ConfiguredLcmGenerator.GenerateContentForEntry(subentry3, minorEntryNode, null, DefaultSettings, 0).ToString();
			expectedResults = @"{""xhtmlTemplate"": ""minorentrycomplex"",""guid"":""g" + subentry3.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""complexformentryrefs"": [{""referencedentries"": [{""glossorsummary"": [{""lang"":""en"",""value"":""MainEntryS3Defn""}]}]}]}";
			expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result3, expected);
		}

		[Test]
		public void GenerateDictionaryMetaDataForApi_OneEntryGetsLetterHeads()
		{
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var siteName = "test";
			var json = LcmJsonGenerator.GenerateDictionaryMetaData(siteName, new[] { "mainentry.xhtml" }, new List<DictionaryConfigurationModel>(), new []{ testEntry.Hvo }, null, Cache, m_Clerk);
			var expectedResults = @"{""_id"":""" + siteName + @""",""mainLanguage"":{""title"":""French"",""lang"":""fr"",""letters"":[""c""],""cssFiles"":[""configured.css""]},""partsOfSpeech"":[],""semanticDomains"":[],
				""xhtmlTemplates"": [""mainentry.xhtml""]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(json.ToString(), expected);
		}

		[Test]
		public void GenerateDictionaryMetaDataForApi_SemDomAndPOSPopulated()
		{
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var possFact = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			Cache.LangProject.SemanticDomainListOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var domainOne = possFact.Create();
			var noun = possFact.Create();
			Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Add(domainOne);
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(noun);
			domainOne.Abbreviation.set_String(m_wsEn, "9.0");
			domainOne.Name.set_String(m_wsEn, "CustomDomain");
			noun.Abbreviation.set_String(m_wsEn, "n");
			noun.Name.set_String(m_wsEn, "noun");
			var siteName = "test";
			var json = LcmJsonGenerator.GenerateDictionaryMetaData(siteName, new []{ "mainentry.xhtml" },
				new List<DictionaryConfigurationModel>(), new[] { testEntry.Hvo }, null, Cache, m_Clerk);
			var expectedResults = @"{""_id"":""test"",""mainLanguage"":{""title"":""French"",""lang"":""fr"",""letters"":[""c""],""cssFiles"":[""configured.css""]},
				""partsOfSpeech"":[{""lang"":""en"",""abbreviation"":""n"",""name"":""noun"",""guid"":""g" + noun.Guid + @"""}],
				""semanticDomains"":[{""lang"":""en"",""abbreviation"":""9.0"",""name"":""CustomDomain"",""guid"":""g" + domainOne.Guid + @"""}],
				""xhtmlTemplates"": [""mainentry.xhtml""]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(json.ToString(), expected);
		}

		[Test]
		public void SavePublishedJsonWithStyles_DisplayXhtmlPopulated()
		{
			var citationForm = new ConfigurableDictionaryNode
			{
				FieldDescription = "CitationForm",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var homographNum = new ConfigurableDictionaryNode { FieldDescription = "HomographNumber" };
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { homographNum, citationForm },
				FieldDescription = "LexEntry"
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var results = LcmJsonGenerator.SavePublishedJsonWithStyles(new[] { testEntry.Hvo },
				DefaultDecorator, 1,
				new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } },
				m_propertyTable, "test.json", null, out int[] _);
			var expectedResults = @"{""xhtmlTemplate"":""lexentry"",""guid"":""g" + testEntry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""homographnumber"":""0"",""citationform"":[{""lang"":""fr"",""value"":""Citation""}],
				""displayXhtml"":""<div class=\""lexentry\"" nodeId=\""" + mainEntryNode.GetHashCode() +
				@"\"" id=\""g" + testEntry.Guid + @"\""><span class=\""homographnumber\"" nodeId=\""" + homographNum.GetHashCode() +
				@"\"">0</span><span class=\""citationform\""><span nodeId=\""" + citationForm.GetHashCode() + @"\"" lang=\""fr\"">Citation</span></span></div>""}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(results[0][0].ToString(Formatting.None), expected);
		}

		[Test]
		public void SavePublishedJsonWithStyles_BatchingWorks()
		{
			var citationForm = new ConfigurableDictionaryNode
			{
				FieldDescription = "CitationForm",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "fr" })
			};
			var homographNum = new ConfigurableDictionaryNode { FieldDescription = "HomographNumber" };
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { homographNum, citationForm },
				FieldDescription = "LexEntry"
			};

			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var testEntry2 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var testEntry3 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var testBatchSize = 2;
			var results = LcmJsonGenerator.SavePublishedJsonWithStyles(new[] { testEntry.Hvo, testEntry2.Hvo, testEntry3.Hvo },
				DefaultDecorator, testBatchSize,
				new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } },
				m_propertyTable, "test.json", null, out int[] _);
			Assert.That(results.Count, Is.EqualTo(2)); // 3 entries makes 2 batches at batchSize of 2
			Assert.That(results[0].Count, Is.EqualTo(testBatchSize)); // one full batch of 2
			Assert.That(results[1].Count, Is.EqualTo(1)); // one lonely entry in the last batch

			dynamic jsonResult0 = results[0].First;
			Assert.AreEqual(0, jsonResult0.sortIndex.Value);
			dynamic jsonResult1 = results[0].Last;
			Assert.AreEqual(1, jsonResult1.sortIndex.Value);
			dynamic jsonResult2 = results[1].First;
			Assert.AreEqual(2, jsonResult2.sortIndex.Value);
		}

		[Test]
		public void SavePublishedJsonWithStyles_HiddenMinorEntry_DoesNotThrow()
		{
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache, m_propertyTable);
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var minorEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, minorEntry);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(minorEntry, false);

			var result = LcmJsonGenerator.SavePublishedJsonWithStyles(new[] { minorEntry.Hvo },
				DefaultDecorator, 1, configModel, m_propertyTable, "test.json", null, out int[] _);

			Assert.AreEqual(1, result.Count, "batches");
			Assert.AreEqual(0, result[0].Count, "entries");
		}

		[Test]
		public void SavePublishedJsonWithStyles_MinorEntryNotPublished()
		{
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache, m_propertyTable);
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var minorEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, minorEntry);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(minorEntry, false);

			var result = LcmJsonGenerator.SavePublishedJsonWithStyles(new[] { mainEntry.Hvo, minorEntry.Hvo },
				DefaultDecorator, 10, configModel, m_propertyTable, "test.json", null, out int[] entryIds);

			Assert.AreEqual(1, result.Count, "batches");
			Assert.AreEqual(1, result[0].Count, "entries");
			Assert.AreEqual(result[0].Count, entryIds.Length);
		}

		[Test]
		public void GenerateXHTMLForEntry_EmbeddedWritingSystemOfOppositeDirectionGeneratesCorrectResult()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Bibliography",
				CSSClassNameOverride = "bib",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "he" })
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Children = new List<ConfigurableDictionaryNode> { headwordNode },
				FieldDescription = "LexEntry"
			};
			CssGeneratorTests.PopulateFieldsForTesting(mainEntryNode);
			var entry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var multiRunString = ConfiguredXHTMLGeneratorTests.MakeBidirectionalTss(new[] { "דוד", " et ", "דניאל" }, Cache);
			var wsHe = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("he");
			entry.Bibliography.set_String(wsHe, multiRunString);
			//SUT

			var json = ConfiguredLcmGenerator.GenerateContentForEntry(entry, mainEntryNode, null, DefaultSettings).ToString();
			var expectedResults = @"{""xhtmlTemplate"":""lexentry"",""guid"":""g" + entry.Guid + @""",""letterHead"":""c"",""sortIndex"":-1,
				""bib"": [{""lang"":""he"",""value"":""דוד""},{""lang"":""en"",""value"":"" et ""},{""lang"":""he"",""value"":""דניאל""}],}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(json, expected);
		}

		[Test]
		public void GenerateXHTMLForEntry_MultiLineCustomFieldGeneratesContent()
		{
			using (var customField = new CustomFieldForTest(Cache, "MultiplelineTest",
				Cache.MetaDataCacheAccessor.GetClassId("LexEntry"), 0,
				CellarPropertyType.OwningAtomic, Guid.Empty))
			{
				var memberNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "MultiplelineTest",
					IsCustomField = true
				};
				var rootNode = new ConfigurableDictionaryNode
				{
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { memberNode }
				};
				CssGeneratorTests.PopulateFieldsForTesting(rootNode);
				var testEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
				var text = ConfiguredXHTMLGeneratorTests.CreateMultiParaText("Custom string", Cache);
				Cache.MainCacheAccessor.SetObjProp(testEntry.Hvo, customField.Flid, text.Hvo);
				//SUT
				var result = ConfiguredLcmGenerator.GenerateContentForEntry(testEntry, rootNode, null, DefaultSettings, 0).ToString();

				var expectedResults = @"{""xhtmlTemplate"":""lexentry"",
					""guid"":""g" + testEntry.Guid + @""",
					""letterHead"":""c"",
					""sortIndex"":0,
					""multiplelinetest"": [{""lang"":""fr"",""value"":""First para Custom string""},
										   {""lang"":""fr"",""value"":""Second para Custom string""},
										  ],}";
				//This assert is dependent on the specific entry data created in CreateInterestingLexEntry
				var expected = (JObject)JsonConvert.DeserializeObject(expectedResults);
				VerifyJson(result, expected);
			}
		}

		/// <summary>
		/// Verifies the json data generated is equivalent to the expected result
		/// </summary>
		private void VerifyJson(string actual, JObject expected)
		{
			// TODO: use Json FluentAssert library
			dynamic jsonResult = JsonConvert.DeserializeObject(actual, new JsonSerializerSettings { Formatting = Formatting.None });
			string actualReformatted = JsonConvert.SerializeObject(jsonResult, Formatting.Indented);
			Assert.That(actualReformatted, Is.EqualTo(JsonConvert.SerializeObject(expected, Formatting.Indented)));
		}
	}
}

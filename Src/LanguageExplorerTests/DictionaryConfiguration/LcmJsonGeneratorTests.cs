// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageExplorer;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorer.TestUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	[TestFixture]
	class LcmJsonGeneratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private int m_wsEn, m_wsFr, m_wsHe;

		private const string DictionaryNormal = "Dictionary-Normal";

		private GeneratorSettings DefaultSettings => new GeneratorSettings(Cache,
			new ReadOnlyPropertyTable(_flexComponentParameters.PropertyTable), true, false, null) { ContentGenerator = new LcmJsonGenerator(Cache) };

		private DictionaryPublicationDecorator DefaultDecorator => new DictionaryPublicationDecorator(Cache,
			(ISilDataAccessManaged)Cache.MainCacheAccessor, Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries);

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			FwRegistrySettings.Init();
			_flexComponentParameters = TestSetupServices.SetupEverything(Cache);
			var styles = FwUtils.StyleSheetFromPropertyTable(_flexComponentParameters.PropertyTable).Styles;
			if (!styles.Contains(DictionaryNormal))
			{
				styles.Add(new BaseStyleInfo { Name = DictionaryNormal });
			}

			_flexComponentParameters.PropertyTable.SetProperty(LanguageExplorerConstants.ToolChoice, LanguageExplorerConstants.LexiconDictionaryMachineName);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests/TestData/");
			m_wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			m_wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
		}

		/// <inheritdoc />
		public override void FixtureTeardown()
		{
			try
			{
				TestSetupServices.DisposeTrash(_flexComponentParameters);
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} FixtureTeardown method.", err);
			}
			finally
			{
				base.FixtureTeardown();
			}
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
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, DefaultSettings, 0);
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
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, DefaultSettings, 0);

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

			var settings = new GeneratorSettings(Cache, _flexComponentParameters.PropertyTable, false, false, null) { ContentGenerator = new LcmJsonGenerator(Cache) };
			//SUT
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings, 0);
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
			var sensePic = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
			sense.PicturesOS.Add(sensePic);
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			sensePic.Caption.set_String(wsEn, TsStringUtils.MakeString("caption", wsEn));
			var pic = Cache.ServiceLocator.GetInstance<ICmFileFactory>().Create();
			var folder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			Cache.LangProject.MediaOC.Add(folder);
			folder.FilesOC.Add(pic);
			pic.InternalPath = "picture";
			sensePic.PictureFileRA = pic;

			var settings = DefaultSettings;

			//SUT
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, settings, 0);
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""pictures"": [{""guid"":""g" + sensePic.Guid + @""",""src"":""pictures/picture"",
				""sensenumber"": [{""lang"":""en"",""value"":""1""}],""caption"": [{""lang"":""en"",""value"":""caption""}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
		}

		[Test]
		public void GenerateJsonForEntry_FilterByPublication()
		{
			// Note that my HS French is nonexistent after 40+ years.  But this is only test code...
			var typeMain = DictionaryConfigurationImportController.AddPublicationType("main", Cache);
			var typeTest = DictionaryConfigurationImportController.AddPublicationType("test", Cache);

			// This entry is published for both main and test.  Its first sense (and example) are published in main, its
			// second sense(and example) are published in test.
			// The second example of the first sense should not be published at all, since it is not published in main and
			// its owner is not published in test.
			var entryCorps = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "corps", "body");
			var pronunciation = ConfiguredXHTMLGeneratorTests.AddPronunciationToEntry(entryCorps, "pronunciation", m_wsFr, Cache);
			var exampleCorpsBody1 = ConfiguredXHTMLGeneratorTests.AddExampleToSense(Cache, entryCorps.SensesOS[0], "Le corps est gros.", m_wsFr, "The body is big.", m_wsEn);
			var exampleCorpsBody2 = ConfiguredXHTMLGeneratorTests.AddExampleToSense(Cache, entryCorps.SensesOS[0], "Le corps est esprit.", m_wsFr, "The body is spirit.", m_wsEn);
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(entryCorps, "corpse", m_wsEn, Cache);
			ConfiguredXHTMLGeneratorTests.AddExampleToSense(Cache, entryCorps.SensesOS[1], "Le corps est mort.", m_wsFr, "The corpse is dead.", m_wsEn);

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
			ConfiguredXHTMLGeneratorTests.AddExampleToSense(Cache, entryBras.SensesOS[0], "Mon bras est casse.", m_wsFr, "My arm is broken.", m_wsEn);
			ConfiguredXHTMLGeneratorTests.AddSenseToEntry(entryBras, "hand", m_wsEn, Cache);
			ConfiguredXHTMLGeneratorTests.AddExampleToSense(Cache, entryBras.SensesOS[1], "Mon bras va bien.", m_wsFr, "My arm is fine.", m_wsEn);
			entryBras.DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[0].DoNotPublishInRC.Add(typeTest);
			entryBras.SensesOS[1].DoNotPublishInRC.Add(typeTest);
			//exampleBrasArm1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there
			//exampleBrasHand1.DoNotPublishInRC.Add(typeTest); -- should not show in test because its owner is not shown there

			// This entry is published only in test, together with its sense and example.
			var entryOreille = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "oreille", "ear");
			ConfiguredXHTMLGeneratorTests.AddExampleToSense(Cache, entryOreille.SensesOS[0], "Lac Pend d'Oreille est en Idaho.", m_wsFr, "Lake Pend d'Oreille is in Idaho.", m_wsEn);
			entryOreille.DoNotPublishInRC.Add(typeMain);
			entryOreille.SensesOS[0].DoNotPublishInRC.Add(typeMain);
			//exampleOreille1.DoNotPublishInRC.Add(typeMain); -- should not show in main because its owner is not shown there

			var entryEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "entry", "entry");
			var entryMainsubentry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "mainsubentry", "mainsubentry");
			entryMainsubentry.DoNotPublishInRC.Add(typeTest);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, entryEntry, entryMainsubentry, true);

			var entryTestsubentry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "testsubentry", "testsubentry");
			entryTestsubentry.DoNotPublishInRC.Add(typeMain);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, entryEntry, entryTestsubentry, true);
			var bizarroVariant = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache, "bizarre", "myVariant");
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, entryEntry, bizarroVariant, "Spelling Variant");
			bizarroVariant.DoNotPublishInRC.Add(typeTest);

			// Note that the decorators must be created (or refreshed) *after* the data exists.
			int flidVirtual = Cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries;
			var pubEverything = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual);
			var pubMain = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeMain);
			var pubTest = new DictionaryPublicationDecorator(Cache, (ISilDataAccessManaged)Cache.MainCacheAccessor, flidVirtual, typeTest);
			//SUT
			var hvosMain = new List<int>(pubMain.GetEntriesToPublish(_flexComponentParameters.PropertyTable, flidVirtual));
			Assert.AreEqual(5, hvosMain.Count, "there are five entries in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryCorps.Hvo), "corps is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryBras.Hvo), "bras is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(bizarroVariant.Hvo), "bizarre is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryOreille.Hvo), "oreille is not shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryEntry.Hvo), "entry is shown in the main publication");
			Assert.IsTrue(hvosMain.Contains(entryMainsubentry.Hvo), "mainsubentry is shown in the main publication");
			Assert.IsFalse(hvosMain.Contains(entryTestsubentry.Hvo), "testsubentry is not shown in the main publication");
			var hvosTest = new List<int>(pubTest.GetEntriesToPublish(_flexComponentParameters.PropertyTable, flidVirtual));
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
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, ListIds.Variant)
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
			var output = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entryEntry, mainEntryNode, pubMain, DefaultSettings, 0);
			Assert.IsNotNullOrEmpty(output);
			var expectedResults = "{\"xhtmlTemplate\": \"lexentry\",\"guid\":\"g" + entryEntry.Guid + "\",\"letterHead\": \"e\",\"sortIndex\": 0," +
								  "\"entry\": [{\"lang\":\"fr\",\"value\":\"entry\"}],\"senses\": [{\"guid\":\"g" +
								  entryEntry.Guid + "\",\"definitionorgloss\": [{\"lang\":\"en\",\"value\":\"entry\"}]}]," +
								  "\"subentries\": [{\"subentry\": [{\"lang\":\"fr\",\"value\":\"mainsubentry\"}]}]," +
								  "\"variantformentrybackrefs\": [{\"references\":[{\"headword\": [{\"lang\":\"fr\",\"guid\": \"g" + bizarroVariant.Guid +
								  "\", \"value\":\"bizarre\"}]}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(output, expected);
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
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, DefaultSettings, 0);

			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"": ""g" + entryOne.Guid +
								  @""",""letterHead"": ""c"",""sortIndex"": 0, ""headword"": [{""guid"": ""g" + entryOne.Guid + @""", ""lang"":""en-Zxxx-x-audio"", ""value"": {""id"": ""gTest_Audi_o"", ""src"": ""AudioVisual/Test Audi'o.wav""}}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
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
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, DefaultDecorator, DefaultSettings, 0);
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
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entry, mainEntryNode, null, DefaultSettings, 0);
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,""bib"": [{""lang"":""fr"",""value"":""French with ""},
				{""lang"":""en"",""value"":""English""},{""lang"":""fr"",""value"":"" embedded""}]}";
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
			ConfiguredXHTMLGeneratorTests.CreateLexicalReference(Cache, mainEntry.SensesOS.First(), referencedEntry, null, m_wsEn, refTypeName, refTypeRevName);
			var refType = Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.First(poss => poss.Name.BestAnalysisAlternative.Text == refTypeName);
			Assert.IsNotNull(refType);

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
					ListId = ListIds.Sense,
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
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(mainEntry, mainEntryNode, null, DefaultSettings, 0);
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + mainEntry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""sensesos"": [{""lexsensereferences"": [{""ownertype_name"": [{""lang"":""en"",""value"":""TestRefType""}]}]}]}";
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
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, DefaultSettings, 0);
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entryOne.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,""homographnumber"": ""1"",
				""citationform"": [{""lang"":""fr"",""value"":""Citation""}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
			result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entryTwo, mainEntryNode, null, DefaultSettings, 0);
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
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, DefaultSettings, 36);
			var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + entryOne.Guid + @""",""letterHead"": ""c"",""sortIndex"": 36,
				""citationform"": [{""lang"":""fr"",""value"":""Citation""}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);
			// default value of -1
			result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(entryOne, mainEntryNode, null, DefaultSettings);
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
				var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(testEntry, mainEntryNode, null, DefaultSettings, 0);

				// Bug: The second filename should be different after the export with relative path settings (fix later)
				var expectedResults = @"{""xhtmlTemplate"": ""lexentry"",""guid"":""g" + testEntry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
					""pictures"": [{""guid"":""g" + sensePic.Guid + @""",""src"":""pictures/" + fileName + @"""},
					{""guid"":""g" + sensePic2.Guid + @""",""src"":""pictures/" + fileName + @"""}],}";
				var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
				VerifyJson(result, expected);
			}
			finally
			{
				SIL.IO.RobustIO.DeleteDirectoryAndContents(tempFolder.FullName);
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
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, ListIds.Complex),
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
			var result = ConfiguredLcmGenerator.GenerateXHTMLForEntry(subentry1, minorEntryNode, null, DefaultSettings, 0);
			var expectedResults = @"{""xhtmlTemplate"":""minorentrycomplex"",""guid"":""g" + subentry1.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""complexformentryrefs"": [{""referencedentries"": [{""glossorsummary"": [{""lang"":""en"",""value"":""MainEntrySummaryDefn""}]}]}]}";
			var expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result, expected);

			//SUT
			var result2 = ConfiguredLcmGenerator.GenerateXHTMLForEntry(subentry2, minorEntryNode, null, DefaultSettings, 0);
			expectedResults = @"{""xhtmlTemplate"":""minorentrycomplex"",""guid"":""g" + subentry2.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""complexformentryrefs"": [{""referencedentries"": [{""glossorsummary"": [{""lang"":""en"",""value"":""gloss2""}]}]}]}";
			expected = (JObject)JsonConvert.DeserializeObject(expectedResults, new JsonSerializerSettings { Formatting = Formatting.None });
			VerifyJson(result2, expected);

			//SUT
			var result3 = ConfiguredLcmGenerator.GenerateXHTMLForEntry(subentry3, minorEntryNode, null, DefaultSettings, 0);
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
			var json = LcmJsonGenerator.GenerateDictionaryMetaData(siteName, new[] { "mainentry.xhtml" }, new List<DictionaryConfigurationModel>(), new []{ testEntry.Hvo }, null, null, Cache);
			var expectedResults = @"{""_id"":""" + siteName + @""",""mainLanguage"":{""lang"":""fr"",""letters"":[""c""],""cssFiles"":[""configured.css""]},""partsOfSpeech"":[],""semanticDomains"":[],
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
				new List<DictionaryConfigurationModel>(), new[] { testEntry.Hvo }, null, null, Cache);
			var expectedResults = @"{""_id"":""test"",""mainLanguage"":{""lang"":""fr"",""letters"":[""c""],""cssFiles"":[""configured.css""]},
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
				_flexComponentParameters.PropertyTable, "test.json", null);
			var expectedResults = @"{""xhtmlTemplate"":""lexentry"",""guid"":""g" + testEntry.Guid + @""",""letterHead"": ""c"",""sortIndex"": 0,
				""homographnumber"":""0"",""citationform"":[{""lang"":""fr"",""value"":""Citation""}],
				""displayXhtml"":""<div class=\""lexentry\"" id=\""g" + testEntry.Guid + @"\""><span class=\""homographnumber\"">0</span><span class=\""citationform\""><span lang=\""fr\"">Citation</span></span></div>""}";
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
				_flexComponentParameters.PropertyTable, "test.json", null);
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
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable);
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var minorEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, minorEntry);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(minorEntry, false);

			var result = LcmJsonGenerator.SavePublishedJsonWithStyles(new[] { minorEntry.Hvo },
				DefaultDecorator, 1, configModel, _flexComponentParameters.PropertyTable, "test.json", null);

			Assert.AreEqual(1, result.Count, "batches");
			Assert.AreEqual(0, result[0].Count, "entries");
		}

		/// <summary>
		/// Verifies the json data generated is equivalent to the expected result
		/// </summary>
		private static void VerifyJson(string actual, JObject expected)
		{
			// TODO: use Json FluentAssert library
			dynamic jsonResult = JsonConvert.DeserializeObject(actual, new JsonSerializerSettings { Formatting = Formatting.None });
			string actualReformatted = JsonConvert.SerializeObject(jsonResult, Formatting.Indented);
			Console.WriteLine(actualReformatted);
			Assert.That(actualReformatted, Is.EqualTo(JsonConvert.SerializeObject(expected, Formatting.Indented)));
		}
	}
}
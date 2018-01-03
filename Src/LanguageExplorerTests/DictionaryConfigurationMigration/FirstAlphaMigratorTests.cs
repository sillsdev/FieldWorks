// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageExplorer;
using LanguageExplorer.DictionaryConfigurationMigration;
using LanguageExplorer.Works;
using NUnit.Framework;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.IO;
using SIL.LCModel;
using LanguageExplorerTests.Works;

namespace LanguageExplorerTests.DictionaryConfigurationMigration
{
	public class FirstAlphaMigratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FirstAlphaMigrator _migrator;
		private SimpleLogger _logger;

		[SetUp]
		public void SetUp()
		{
			_logger = new SimpleLogger();
			_migrator = new FirstAlphaMigrator(string.Empty, Cache, _logger);
		}

		[TearDown]
		public void TearDown()
		{
			_logger.Dispose();
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesVersion()
		{
			var alphaModel = new DictionaryConfigurationModel { Version = PreHistoricMigrator.VersionAlpha1, Parts = new List<ConfigurableDictionaryNode>() };
			_migrator.MigrateFrom83Alpha(alphaModel); // SUT
			Assert.AreEqual(FirstAlphaMigrator.VersionAlpha3, alphaModel.Version);
		}

		[Test]
		public void MigrateFrom83Alpha_ConfigWithVerMinus1GetsMigrated()
		{
			var configChild = new ConfigurableDictionaryNode { FieldDescription = "Child", ReferenceItem = "LexEntry" };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent", Children = new List<ConfigurableDictionaryNode> { configChild } };
			var configModel = new DictionaryConfigurationModel
			{
				Version = PreHistoricMigrator.VersionPre83, // the original migration code neglected to update the version on completion
				Parts = new List<ConfigurableDictionaryNode> { configParent }
			};
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.Null(configChild.ReferenceItem, "Unused ReferenceItem should have been removed");
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesReferencedEntriesToGlossOrSummary()
		{
			var configGlossOrSummDefn = new ConfigurableDictionaryNode { Label = "Gloss (or Summary Definition)", FieldDescription = "DefinitionOrGloss" };
			var configReferencedEntries = new ConfigurableDictionaryNode
			{
				Label = "Referenced Entries",
				FieldDescription = "ConfigReferencedEntries",
				CSSClassNameOverride = "referencedentries",
				Children = new List<ConfigurableDictionaryNode> { configGlossOrSummDefn }
			};
			var configParent = new ConfigurableDictionaryNode
			{
				FieldDescription = "Parent",
				Label = "Variant Of",
				Children = new List<ConfigurableDictionaryNode> { configReferencedEntries }
			};
			var configDefnOrGloss = new ConfigurableDictionaryNode { Label = "Definition (or Gloss)", FieldDescription = "DefinitionOrGloss" };
			var configSenses = new ConfigurableDictionaryNode
			{
				Label = "Senses",
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { configDefnOrGloss }
			};
			var main = new ConfigurableDictionaryNode { FieldDescription = "Main", Children = new List<ConfigurableDictionaryNode> { configParent, configSenses } };
			var configModel = new DictionaryConfigurationModel
			{
				Version = 3,
				Parts = new List<ConfigurableDictionaryNode> { main }
			};
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("GlossOrSummary", configGlossOrSummDefn.FieldDescription,
				"'Gloss (or Summary Definition)' Field Description should have been updated");
			Assert.AreEqual("DefinitionOrGloss", configDefnOrGloss.FieldDescription,
				"'Definition (or Gloss)' should not change fields");
		}

		[Test]
		public void MigrateFrom83Alpha_RemovesDeadReferenceItems()
		{
			var configChild = new ConfigurableDictionaryNode { FieldDescription = "TestChild", ReferenceItem = "LexEntry" };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent", Children = new List<ConfigurableDictionaryNode> { configChild } };
			var configModel = new DictionaryConfigurationModel { Version = 1, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.Null(configChild.ReferenceItem, "Unused ReferenceItem should have been removed");
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesExampleSentenceLabels()
		{
			var configExampleChild = new ConfigurableDictionaryNode { Label = "Example", FieldDescription = "Example" };
			var configExampleParent = new ConfigurableDictionaryNode { Label = "Examples", FieldDescription = "ExamplesOS", Children = new List<ConfigurableDictionaryNode> { configExampleChild } };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent", Children = new List<ConfigurableDictionaryNode> { configExampleParent } };
			var configModel = new DictionaryConfigurationModel { Version = 3, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("Example Sentence", configExampleChild.Label);
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesFreshlySharedNodes()
		{
			var examplesOS = new ConfigurableDictionaryNode { Label = "Examples", FieldDescription = "ExamplesOS" };
			var subsenses = new ConfigurableDictionaryNode { Label = "Subsenses", FieldDescription = "SensesOS", Children = new List<ConfigurableDictionaryNode> { examplesOS } };
			var senses = new ConfigurableDictionaryNode { Label = "Senses", FieldDescription = "SensesOS", Children = new List<ConfigurableDictionaryNode> { subsenses } };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent", Children = new List<ConfigurableDictionaryNode> { senses } };
			var configModel = new DictionaryConfigurationModel { Version = 3, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			_migrator.MigrateFrom83Alpha(configModel); // SUT
			for (var node = examplesOS; !configModel.SharedItems.Contains(node); node = node.Parent)
				Assert.NotNull(node, "ExamplesOS should be freshly-shared (under subsenses)");
			Assert.That(examplesOS.DictionaryNodeOptions, Is.TypeOf(typeof(DictionaryNodeListAndParaOptions)), "Freshly-shared nodes should be included");
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesExampleOptions()
		{
			var configExamplesNode = new ConfigurableDictionaryNode { Label = "Examples", FieldDescription = "ExamplesOS" };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent",
				Children = new List<ConfigurableDictionaryNode> { configExamplesNode } };
			var configModel = new DictionaryConfigurationModel { Version = 3, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual(StyleTypes.Character, configExamplesNode.StyleType);
			Assert.IsTrue(configExamplesNode.DictionaryNodeOptions is DictionaryNodeListAndParaOptions, "wrong type");
			var options = (DictionaryNodeListAndParaOptions)configExamplesNode.DictionaryNodeOptions;
			Assert.IsFalse(options.DisplayEachInAParagraph, "Default is *not* in paragraph");
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesBibliographyLabels()
		{
			var configBiblioEntryNode = new ConfigurableDictionaryNode { Label = "Bibliography", FieldDescription = "Owner", SubField = "Bibliography" };
			var configBiblioSenseNode = new ConfigurableDictionaryNode { Label = "Bibliography", FieldDescription = "Bibliography" };
			var configBiblioParent = new ConfigurableDictionaryNode { Label = "Referenced Senses", FieldDescription = "ReferringSenses", Children = new List<ConfigurableDictionaryNode> { configBiblioSenseNode, configBiblioEntryNode } };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent",
				Children = new List<ConfigurableDictionaryNode> { configBiblioParent } };
			var configModel = new DictionaryConfigurationModel { Version = 3, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("Bibliography (Entry)", configBiblioEntryNode.Label);
			Assert.AreEqual("Bibliography (Sense)", configBiblioSenseNode.Label);
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesHeadWordRefs()
		{
			var cpFormChild = new ConfigurableDictionaryNode { Label = "Complex Form", FieldDescription = "OwningEntry", SubField = "MLHeadWord" };
			var referenceHwChild = new ConfigurableDictionaryNode { Label = "Referenced Headword", FieldDescription = "HeadWord" };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent",
				Children = new List<ConfigurableDictionaryNode> { referenceHwChild, cpFormChild } };
			var configModel = new DictionaryConfigurationModel { Version = 2, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("HeadWordRef", referenceHwChild.FieldDescription);
			Assert.AreEqual("HeadWordRef", cpFormChild.SubField);
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesReversalHeadwordRefs()
		{
			var cpFormChild = new ConfigurableDictionaryNode { Label = "Complex Form", FieldDescription = "OwningEntry", SubField = "MLHeadWord" };
			var referenceHwChild = new ConfigurableDictionaryNode { Label = "Referenced Headword", FieldDescription = "HeadWord" };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent",
				Children = new List<ConfigurableDictionaryNode> { referenceHwChild, cpFormChild } };
			var configModel = new DictionaryConfigurationModel
			{
				Version = 2, WritingSystem = "en",
				Parts = new List<ConfigurableDictionaryNode> { configParent },
				FilePath = Path.Combine("ReversalIndex", "English.fwdictconfig")
			};
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("ReversalName", referenceHwChild.FieldDescription);
			Assert.AreEqual("ReversalName", cpFormChild.SubField);
		}

		[Test]
		public void MigrateFrom83Alpha_NullFieldDescriptionIsLogged()
		{
			var cpFormChild = new ConfigurableDictionaryNode { Label = "Complex Form", FieldDescription = null };
			var referenceHwChild = new ConfigurableDictionaryNode { Label = "Referenced Headword", FieldDescription = "HeadWord" };
			var configParent = new ConfigurableDictionaryNode
			{
				Label = "Parent",
				FieldDescription = "Parent",
				Children = new List<ConfigurableDictionaryNode> { referenceHwChild, cpFormChild }
			};
			cpFormChild.Parent = configParent;
			var configModel = new DictionaryConfigurationModel { Version = 2, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.That(_logger.Content, Is.StringContaining("'Parent > Complex Form' reached the Alpha2 migration with a null FieldDescription."));
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesSharedItems()
		{
			var cpFormChild = new ConfigurableDictionaryNode { Label = "Complex Form", FieldDescription = "OwningEntry", SubField = "MLHeadWord" };
			var referenceHwChild = new ConfigurableDictionaryNode { Label = "Referenced Headword", FieldDescription = "HeadWord" };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent",
				Children = new List<ConfigurableDictionaryNode> { referenceHwChild, cpFormChild } };
			var configModel = new DictionaryConfigurationModel
			{
				Version = 2, WritingSystem = "en",
				Parts = new List<ConfigurableDictionaryNode>(),
				FilePath = Path.Combine("ReversalIndex", "English.fwdictconfig"),
				SharedItems = new List<ConfigurableDictionaryNode> { configParent }
			};
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("ReversalName", referenceHwChild.FieldDescription);
			Assert.AreEqual("ReversalName", cpFormChild.SubField);
		}

		[Test]
		public void MigrateFrom83Alpha_MissingReversalWsFilledIn()
		{
			CoreWritingSystemDefinition testWs;
			WritingSystemServices.FindOrCreateWritingSystem(Cache, null, "ta-fonipa", false, false, out testWs);
			Cache.LangProject.AddToCurrentAnalysisWritingSystems(testWs);
			var configModelEn = new DictionaryConfigurationModel
			{
				Version = 2,
				Parts = new List<ConfigurableDictionaryNode>(),
				Label = "English",
				FilePath = Path.Combine("ReversalIndex", "English.fwdictconfig")
			};
			var configModelTamil = new DictionaryConfigurationModel
			{
				Version = 2,
				Parts = new List<ConfigurableDictionaryNode>(),
				Label = "Tamil (International Phonetic Alphabet)",
				FilePath = Path.Combine("ReversalIndex", "Tamil.fwdictconfig")
			};
			_migrator.MigrateFrom83Alpha(configModelEn);
			Assert.AreEqual("en", configModelEn.WritingSystem);
			_migrator.MigrateFrom83Alpha(configModelTamil);
			Assert.AreEqual("ta__IPA", configModelTamil.WritingSystem);
		}

		[Test]
		public void MigrateFrom83Alpha_MissingReversalWsFilledIn_NonReversalsIgnored()
		{
			// This covers the unlikely case where a non-reversal configuration is named after a language
			var configModelRoot = new DictionaryConfigurationModel
			{
				Version = 2,
				Parts = new List<ConfigurableDictionaryNode>(),
				Label = "English",
				FilePath = Path.Combine("NotReversalIndex", "English.fwdictconfig")
			};
			_migrator.MigrateFrom83Alpha(configModelRoot);
			Assert.Null(configModelRoot.WritingSystem, "The WritingSystem should not be filled in for configurations that aren't for reversal");
		}

		[Test]
		public void MigrateFrom83Alpha_Pre83ReversalCopiesGrabNameFromFile()
		{
			// This test case handles advanced users who made copies pre 8.3 and have used the alpha
			var configModelRoot = new DictionaryConfigurationModel
			{
				Version = 2,
				Parts = new List<ConfigurableDictionaryNode>(),
				Label = "My Copy",
				FilePath = Path.Combine("ReversalIndex", "My Copy-English-#Engl464.fwdictconfig")
			};
			_migrator.MigrateFrom83Alpha(configModelRoot);
			Assert.AreEqual("en", configModelRoot.WritingSystem, "English should have been parsed out of the filename and used to set the WritingSystem");
		}

		[Test]
		public void MigrateFrom83Alpha_ExtractsWritingSystemOptionsFromReferencedSenseOptions()
		{
			DictionaryConfigurationModel model;
			using (var modelFile = new TempFile(new[]
			{
				DictionaryConfigurationModelTests.XmlOpenTagsThruHeadword, @"
				<ReferringSenseOptions>
					<WritingSystemOptions writingSystemType=""vernacular"" displayWSAbreviation=""true"">
						<Option id=""vernacular"" isEnabled=""true"" />
					</WritingSystemOptions>
					<SenseOptions numberStyle=""Sense-Reference-Number"" numberBefore="" "" numberingStyle=""%O"" numberAfter="""" numberSingleSense=""false"" showSingleGramInfoFirst=""false"" displayEachSenseInParagraph=""false"" />
				</ReferringSenseOptions>",
				DictionaryConfigurationModelTests.XmlCloseTagsFromHeadword
			}))
			{
				model = new DictionaryConfigurationModel(modelFile.Path, Cache);
			}

			// SUT
			_migrator.MigrateFrom83Alpha(model);
			var testNodeOptions = model.Parts[0].Children[0].DictionaryNodeOptions;
			Assert.IsInstanceOf(typeof(DictionaryNodeWritingSystemOptions), testNodeOptions);
			var wsOptions = (DictionaryNodeWritingSystemOptions)testNodeOptions;
			Assert.IsTrue(wsOptions.DisplayWritingSystemAbbreviations);
			Assert.AreEqual(DictionaryNodeWritingSystemOptions.WritingSystemType.Vernacular, wsOptions.WsType);
			Assert.AreEqual(1, wsOptions.Options.Count);
			Assert.AreEqual("vernacular", wsOptions.Options[0].Id);
			Assert.IsTrue(wsOptions.Options[0].IsEnabled);
		}

		[Test]
		public void MigrateFrom83Alpha_SubSubSenseReferenceNodeSharesMainEntrySense()
		{
			var subsubsenses = new ConfigurableDictionaryNode { Label = "Subsubsenses", FieldDescription = "SensesOS", ReferenceItem = null };
			var subsenses = new ConfigurableDictionaryNode { Label = "Subsenses", FieldDescription = "SensesOS", Children = new List<ConfigurableDictionaryNode> { subsubsenses } };
			var subentriesUnderSenses = new ConfigurableDictionaryNode
			{
				Label = "Subentries", FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "TestNode", Label = "TestNode"}
				}
			};
			var mainEntryHeadword = new ConfigurableDictionaryNode { FieldDescription = "HeadWord" };
			var senses = new ConfigurableDictionaryNode { Label = "Senses", FieldDescription = "SensesOS", Children = new List<ConfigurableDictionaryNode> { subsenses, subentriesUnderSenses } };
			var subentries = new ConfigurableDictionaryNode
			{
				Label = "Subentries",
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = "TestChild", Label = "TestNode" } }
			};
			var mainEntry = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { mainEntryHeadword, senses, subentries }
			};
			var model = new DictionaryConfigurationModel
			{
				Version = 1,
				Parts = new List<ConfigurableDictionaryNode> { mainEntry }
			};

			_migrator.MigrateFrom83Alpha(model);
			Assert.That(subsenses.ReferenceItem, Is.StringMatching("MainEntrySubsenses"));
			Assert.That(subsubsenses.ReferenceItem, Is.StringMatching("MainEntrySubsenses"));
			Assert.That(subentriesUnderSenses.ReferenceItem, Is.StringMatching("MainEntrySubentries"));
			Assert.Null(subsenses.Children, "Children not removed from shared nodes");
			Assert.Null(subsubsenses.Children, "Children not removed from shared nodes");
			Assert.Null(subentriesUnderSenses.Children, "Children not removed from shared nodes");
			var sharedSubsenses = model.SharedItems.FirstOrDefault(si => si.Label == "MainEntrySubsenses");
			Assert.NotNull(sharedSubsenses, "No Subsenses in SharedItems");
			Assert.AreEqual(1, sharedSubsenses.Children.Count(n => n.FieldDescription == "SensesOS"), "Should have exactly one Subsubsenses node");
		}

		[Test]
		public void MigrateFrom83Alpha_SubSenseParentSenseNumberingStyleMigrated()
		{
			var DictionaryNodeSubSenseOptions = new DictionaryNodeSenseOptions
			{
				BeforeNumber = "",
				AfterNumber = ")",
				NumberStyle = "Dictionary-SenseNumber",
				NumberingStyle = "%d",
				ParentSenseNumberingStyle = "%.",
				DisplayEachSenseInAParagraph = false,
				NumberEvenASingleSense = true,
				ShowSharedGrammarInfoFirst = false
			};
			var subsubsenses = new ConfigurableDictionaryNode { Label = "Subsubsenses", FieldDescription = "SensesOS", ReferenceItem = null };
			var subsenses = new ConfigurableDictionaryNode { Label = "Subsenses", FieldDescription = "SensesOS", DictionaryNodeOptions = DictionaryNodeSubSenseOptions, Children = new List<ConfigurableDictionaryNode> { subsubsenses } };
			var senses = new ConfigurableDictionaryNode { Label = "Senses", FieldDescription = "SensesOS", Children = new List<ConfigurableDictionaryNode> { subsenses } };
			var subentries = new ConfigurableDictionaryNode
			{
				Label = "Subentries",
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = "TestChild", Label = "TestNode" } }
			};
			var mainEntry = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses, subentries }
			};
			var model = new DictionaryConfigurationModel
			{
				Version = 1,
				Parts = new List<ConfigurableDictionaryNode> { mainEntry }
			};

			_migrator.MigrateFrom83Alpha(model);
			var subSenseNode = (DictionaryNodeSenseOptions)subsenses.DictionaryNodeOptions;
			Assert.That(subSenseNode.ParentSenseNumberingStyle, Is.StringMatching("%."));
		}

		[Test]
		public void MigrateFrom83Alpha_SubsubsensesNodeAddedIfNeeded()
		{
			var subsensesNode = new ConfigurableDictionaryNode
			{
				Label = "Subsenses",
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { Label = "TestChild", FieldDescription = "TestChild" } }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				Label = "Senses",
				FieldDescription = "SensesOS",
				Children = new List<ConfigurableDictionaryNode> { subsensesNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			var model = new DictionaryConfigurationModel { Version = PreHistoricMigrator.VersionPre83, Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };

			_migrator.MigrateFrom83Alpha(model);
			var subSenses = model.SharedItems.Find(node => node.Label == "MainEntrySubsenses");
			Assert.NotNull(subSenses);
			Assert.AreEqual(2, subSenses.Children.Count, "Subsenses children were not moved to shared");
			Assert.That(subSenses.Children[1].Label, Is.StringMatching("Subsubsenses"), "Subsubsenses not added during migration");
			Assert.Null(model.Parts[0].Children[0].Children[0].Children, "Subsenses children were left in non-shared node");
		}

		[Test]
		public void MigrateFrom83Alpha_SubSenseSettingsMigratedToSharedNodes()
		{
			var subCategNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MLPartOfSpeech",
				DictionaryNodeOptions =
					ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "analysis" }, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis),
				IsEnabled = false
			};
			var subGramInfoNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "MorphoSyntaxAnalysisRA",
				CSSClassNameOverride = "morphosyntaxanalysis",
				Children = new List<ConfigurableDictionaryNode> { subCategNode },
				IsEnabled = true
			};
			var subGlossNode = new ConfigurableDictionaryNode
			{
				Label = "Gloss",
				FieldDescription = "Gloss",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "en" }),
				IsEnabled = true
			};
			var subSenseNode = new ConfigurableDictionaryNode
			{
				Label = "Subsenses",
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					NumberEvenASingleSense = false,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { subGramInfoNode, subGlossNode }
			};
			var senseNode = new ConfigurableDictionaryNode
			{
				Label = "Senses",
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				DictionaryNodeOptions = new DictionaryNodeSenseOptions
				{
					DisplayEachSenseInAParagraph = true,
					NumberEvenASingleSense = false,
					ShowSharedGrammarInfoFirst = true
				},
				Children = new List<ConfigurableDictionaryNode> { subSenseNode }
			};
			var subentriesNode = new ConfigurableDictionaryNode
			{
				Label = "Subentries",
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = "TestChild", Label = "TestChild" } }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senseNode, subentriesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Version = PreHistoricMigrator.VersionPre83,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};

			_migrator.MigrateFrom83Alpha(model);
			var subSenseGloss =
				model.SharedItems.Find(node => node.Label == "MainEntrySubsenses").Children.Find(child => child.Label == subGlossNode.Label);
			var subGramInfo =
				model.SharedItems.Find(node => node.Label == "MainEntrySubsenses").Children.Find(child => child.Label == subGramInfoNode.Label);
			var subEntries = model.SharedItems.Find(node => node.Label == "MainEntrySubentries");
			Assert.NotNull(subSenseGloss, "Subsenses did not get moved into the shared node");
			Assert.Null(model.Parts[0].Children[1].Children, "Subsenses children were left in non-shared node");
			Assert.IsTrue(subSenseGloss.IsEnabled, "Enabled not migrated into shared nodes for direct children");
			Assert.NotNull(subGramInfo, "Subsense children were not moved into the shared node");
			Assert.IsTrue(subGramInfo.IsEnabled, "Enabled not migrated into shared nodes for descendents");
			Assert.NotNull(subEntries);
			Assert.AreEqual(1, subEntries.Children.Count, "Subentries children were not moved to shared");
			Assert.Null(model.Parts[0].Children[1].Children, "Subentries children were left in non-shared node");
			Assert.NotNull(model.Parts[0].Children[1].DictionaryNodeOptions, "Subentries complex form options not added in migration");
		}

		[Test]
		public void MigrateFrom83Alpha_ReversalSubentriesMigratedToSharedNodes()
		{
			var subentriesNode = new ConfigurableDictionaryNode
			{
				Label = "Reversal Subentries",
				FieldDescription = "SubentriesOS",
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = "TestChild", Label = "TestChild" } }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Reversal Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { subentriesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Version = PreHistoricMigrator.VersionPre83,
				WritingSystem = "en",
				FilePath = string.Empty,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};

			_migrator.MigrateFrom83Alpha(model);
			var subEntries = model.SharedItems.Find(node => node.Label == "AllReversalSubentries");
			Assert.NotNull(subEntries);
			Assert.AreEqual(2, subEntries.Children.Count, "Subentries children were not moved to shared");
			Assert.That(subEntries.Children[1].Label, Is.StringMatching("Reversal Subsubentries"), "Subsubentries not added during migration");
			Assert.Null(model.Parts[0].Children[0].Children, "Subentries children were left in non-shared node");
		}

		[Test]
		public void MigrateFrom83Alpha_ReversalSubentriesNotDuplicatedIfPresentMigratedToSharedNodes()
		{
			var subentriesNode = new ConfigurableDictionaryNode
			{
				Label = "Reversal Subentries",
				FieldDescription = "SubentriesOS",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "TestChild", Label = "TestChild" },
					new ConfigurableDictionaryNode { Label = "Reversal Subsubentries", FieldDescription = "SubentriesOS" }
				}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Reversal Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { subentriesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Version = PreHistoricMigrator.VersionAlpha1,
				WritingSystem = "en",
				FilePath = string.Empty,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};

			_migrator.MigrateFrom83Alpha(model);
			var subEntries = model.SharedItems.Find(node => node.Label == "AllReversalSubentries");
			Assert.NotNull(subEntries);
			Assert.AreEqual(2, subEntries.Children.Count, "Subentries children were not moved to shared");
			Assert.That(subEntries.Children[1].Label, Is.StringMatching("Reversal Subsubentries"), "Subsubentries not added during migration");
			Assert.Null(model.Parts[0].Children[0].Children, "Subentries children were left in non-shared node");
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesTranslationsCssClass()
		{
			var configTranslationsChild = new ConfigurableDictionaryNode { Label = "Translations", FieldDescription = "TranslationsOC" };
			var configExampleParent = new ConfigurableDictionaryNode { Label = "Examples", FieldDescription = "ExamplesOS", Children = new List<ConfigurableDictionaryNode> { configTranslationsChild } };
			var configParent = new ConfigurableDictionaryNode { FieldDescription = "Parent", Children = new List<ConfigurableDictionaryNode> { configExampleParent } };
			var configModel = new DictionaryConfigurationModel { Version = 3, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("translationcontents", configTranslationsChild.CSSClassNameOverride);
		}

		[Test]
		public void MigrateFromConfigV5toV6_SwapsReverseAbbrAndAbbreviation_Variants()
		{
			var revAbbrNode = new ConfigurableDictionaryNode {Label = "Reverse Abbreviation", FieldDescription = "ReverseAbbr"};
			var abbrNode = new ConfigurableDictionaryNode { Label = "Abbreviation", FieldDescription = "Abbreviation" };
			var nameNode = new ConfigurableDictionaryNode { Label = "Name", FieldDescription = "Name" };
			var nameNode2 = new ConfigurableDictionaryNode { Label = "Name", FieldDescription = "Name" };

			var varTypeNodeWithRevAbbr = new ConfigurableDictionaryNode
			{
				Label = "Variant Type",
				FieldDescription = "VariantEntryTypesRS",
				Children = new List<ConfigurableDictionaryNode> {revAbbrNode, nameNode}
			};

			var varTypeNodeWithRevAbbr2 = varTypeNodeWithRevAbbr.DeepCloneUnderSameParent();

			var varTypeNodeWithAbbr = new ConfigurableDictionaryNode
			{
				Label = "Variant Type",
				FieldDescription = "VariantEntryTypesRS",
				Children = new List<ConfigurableDictionaryNode> { abbrNode, nameNode2 }
			};

			var variantsNode = new ConfigurableDictionaryNode
			{
				Label = "Variant Forms",
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { varTypeNodeWithRevAbbr }
			};

			var variantOfSenseNode = new ConfigurableDictionaryNode
			{
				Label = "Variants of Sense",
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { varTypeNodeWithRevAbbr2 }
			};

			var variantOfNode = new ConfigurableDictionaryNode
			{
				Label = "Variant Of",
				FieldDescription = "VisibleVariantEntryRefs",
				Children = new List<ConfigurableDictionaryNode> { varTypeNodeWithAbbr }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { variantsNode, variantOfSenseNode, variantOfNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha2,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};

			_migrator.MigrateFrom83Alpha(model);
			var varTypeNode = variantsNode.Children.First();
			Assert.AreEqual(2, varTypeNode.Children.Count, "'Variant Forms' grandchildren should only be 'Abbreviation' and 'Name'");
			Assert.IsNotNull(varTypeNode.Children.Find(node => node.Label == "Abbreviation"),
				"Reverse Abbreviation should be changed to Abbreviation");
			Assert.IsNotNull(varTypeNode.Children.Find(node => node.Label == "Name"),
				"Name should not be changed");
			varTypeNode = variantOfSenseNode.Children.First();
			Assert.AreEqual(2, varTypeNode.Children.Count,
				"'Variants of Sense' grandchildren should only be 'Abbreviation' and 'Name'");
			Assert.IsNotNull(varTypeNode.Children.Find(node => node.Label == "Abbreviation"),
				"Reverse Abbreviation should be changed to Abbreviation");
			Assert.IsNotNull(varTypeNode.Children.Find(node => node.Label == "Name"),
				"Name should not be changed");
			Assert.AreEqual("Variant of", variantOfNode.Label, "'Variant Of' should have gotten a lowercase 'o'");
			varTypeNode = variantOfNode.Children.First();
			Assert.AreEqual(2, varTypeNode.Children.Count,
				"'Variant of' grandchildren should only be 'Reverse Abbreviation' and 'Reverse Name'");
			Assert.IsNotNull(varTypeNode.Children.Find(node => node.Label == "Reverse Abbreviation"),
				"Abbreviation should be changed to Reverse Abbreviation");
			Assert.IsNotNull(varTypeNode.Children.Find(node => node.Label == "Reverse Name"),
				"Name should be changed to ReverseName");
		}

		[Test]
		public void MigrateFromConfigV5toV6_SwapsReverseAbbrAndAbbreviation_ComplexForms()
		{
			var revAbbrNode = new ConfigurableDictionaryNode { Label = "Reverse Abbreviation", FieldDescription = "ReverseAbbr" };
			var abbrNode = new ConfigurableDictionaryNode { Label = "Abbreviation", FieldDescription = "Abbreviation" };
			var nameNode = new ConfigurableDictionaryNode { Label = "Name", FieldDescription = "Name" };
			var nameNode2 = new ConfigurableDictionaryNode { Label = "Name", FieldDescription = "Name" };

			var cfTypeNodeWithRevAbbr = new ConfigurableDictionaryNode
			{
				Label = "Complex Form Type",
				FieldDescription = "ComplexEntryTypesRS",
				Children = new List<ConfigurableDictionaryNode> { revAbbrNode, nameNode }
			};

			var cfTypeNodeWithRevAbbr2 = cfTypeNodeWithRevAbbr.DeepCloneUnderSameParent();

			var cfTypeNodeLookup = cfTypeNodeWithRevAbbr.DeepCloneUnderSameParent();
			cfTypeNodeLookup.FieldDescription = ConfiguredXHTMLGenerator.LookupComplexEntryType;

			var cfTypeNodeLookup2 = cfTypeNodeLookup.DeepCloneUnderSameParent();

			var cfTypeNodeWithAbbr = new ConfigurableDictionaryNode
			{
				Label = "Complex Form Type",
				FieldDescription = "ComplexEntryTypesRS",
				Children = new List<ConfigurableDictionaryNode> { abbrNode, nameNode2 }
			};

			var cfTypeNodeWithAbbr2 = cfTypeNodeWithAbbr.DeepCloneUnderSameParent();

			var otherRefCFNode = new ConfigurableDictionaryNode
			{
				Label = "Other Referenced Complex Forms",
				FieldDescription = "ComplexFormsNotSubentries",
				Children = new List<ConfigurableDictionaryNode> { cfTypeNodeWithRevAbbr }
			};

			var refCFNode = new ConfigurableDictionaryNode
			{
				Label = "Referenced Complex Forms",
				FieldDescription = "VisibleComplexFormBackRefs",
				Children = new List<ConfigurableDictionaryNode> { cfTypeNodeWithRevAbbr2 }
			};

			var mainEntrySubentriesNode = new ConfigurableDictionaryNode
			{
				Label = "MainEntrySubentries", // Root and Hybrid only
				FieldDescription = "mainentrysubentries",
				Children = new List<ConfigurableDictionaryNode> { cfTypeNodeLookup }
			};

			var minorSubentriesNode = new ConfigurableDictionaryNode
			{
				Label = "Minor Subentries", // Hybrid only
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode> { cfTypeNodeLookup2 }
			};

			var componentsCFNode = new ConfigurableDictionaryNode
			{
				Label = "Components",
				FieldDescription = "ComplexFormEntryRefs",
				Children = new List<ConfigurableDictionaryNode> { cfTypeNodeWithAbbr }
			};

			var componentRefsCFNode = new ConfigurableDictionaryNode
			{
				Label = "Component References",
				FieldDescription = "ComplexFormEntryRefs",
				Children = new List<ConfigurableDictionaryNode> { cfTypeNodeWithAbbr2 }
			};

			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode>
				{
					otherRefCFNode,
					refCFNode,
					mainEntrySubentriesNode,
					minorSubentriesNode,
					componentsCFNode,
					componentRefsCFNode
				}
			};
			var model = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha2,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};

			_migrator.MigrateFrom83Alpha(model);
			var cfTypeNode = otherRefCFNode.Children.First();
			Assert.AreEqual(2, cfTypeNode.Children.Count,
				"'Other Referenced Complex Forms' grandchildren should only be 'Abbreviation' and 'Name'");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Abbreviation"),
				"Reverse Abbreviation should be changed to Abbreviation");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Name"),
				"Name should not be changed");
			cfTypeNode = refCFNode.Children.First();
			Assert.AreEqual(2, cfTypeNode.Children.Count,
				"'Referenced Complex Forms' grandchildren should only be 'Abbreviation' and 'Name'");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Abbreviation"),
				"Reverse Abbreviation should be changed to Abbreviation");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Name"),
				"Name should not be changed");
			cfTypeNode = mainEntrySubentriesNode.Children.First();
			Assert.AreEqual(2, cfTypeNode.Children.Count,
				"'MainEntrySubentries' grandchildren should only be 'Abbreviation' and 'Name'");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Abbreviation"),
				"Reverse Abbreviation should be changed to Abbreviation");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Name"),
				"Name should not be changed");
			cfTypeNode = minorSubentriesNode.Children.First();
			Assert.AreEqual(2, cfTypeNode.Children.Count,
				"'Minor Subentries' grandchildren should only be 'Abbreviation' and 'Name'");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Abbreviation"),
				"Reverse Abbreviation should be changed to Abbreviation");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Name"),
				"Name should not be changed");
			cfTypeNode = componentsCFNode.Children.First();
			Assert.AreEqual(2, cfTypeNode.Children.Count,
				"'Components' grandchildren should only be 'Reverse Abbreviation' and 'Reverse Name'");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Reverse Abbreviation"),
				"Abbreviation should be changed to Reverse Abbreviation");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Reverse Name"),
				"Name should be changed to ReverseName");
			cfTypeNode = componentRefsCFNode.Children.First();
			Assert.AreEqual(2, cfTypeNode.Children.Count,
				"'Component References' grandchildren should only be 'Reverse Abbreviation' and 'Reverse Name'");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Reverse Abbreviation"),
				"Abbreviation should be changed to Reverse Abbreviation");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Reverse Name"),
				"Name should be changed to ReverseName");
		}

		[Test]
		public void MigrateFromConfigV5toV6_UpdatesReferencedHeadword()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				Label = "Headword", FieldDescription = "MLOwnerOutlineName", CSSClassNameOverride = "headword"
			};
			var referencedSensesNode = new ConfigurableDictionaryNode
			{
				Label = "Referenced Senses", FieldDescription = "ReferringSenses",
				Children = new List<ConfigurableDictionaryNode> { headwordNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Reversal Entry",
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { referencedSensesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Version = PreHistoricMigrator.VersionAlpha1,
				WritingSystem = "en",
				FilePath = string.Empty,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			_migrator.MigrateFrom83Alpha(model);
			Assert.AreEqual("Referenced Headword", headwordNode.Label);
		}

		[Test]
		public void MigrateFromConfigV5toV6_UpdatesReferencedHeadwordForSubentryUnder()
		{
			var headwordNode = new ConfigurableDictionaryNode
			{
				Label = "Form",
				FieldDescription = "HeadWordRef"
			};
			var subentryUnder = new ConfigurableDictionaryNode
			{
				Label = "Subentry Under",
				FieldDescription = "NonTrivialEntryRoots",
				Children = new List<ConfigurableDictionaryNode> { headwordNode }
			};
			var components = new ConfigurableDictionaryNode
			{
				Label = "Components",
				FieldDescription = "ComplexFormEntryRefs",
				Children = new List<ConfigurableDictionaryNode> { subentryUnder }
			};
			var minorEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Minor Entry (Complex Forms)",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { components }
			};
			var model = new DictionaryConfigurationModel
			{
				Version = PreHistoricMigrator.VersionAlpha1,
				FilePath = string.Empty,
				Parts = new List<ConfigurableDictionaryNode> { minorEntryNode }
			};
			_migrator.MigrateFrom83Alpha(model);
			Assert.AreEqual("Referenced Headword", headwordNode.Label);
		}

		[Test]
		public void MigrateFromConfigV5toV6_SwapsReverseAbbrAndAbbreviation_ReversalIndexes()
		{
			var revAbbrNode = new ConfigurableDictionaryNode { Label = "Reverse Abbreviation", FieldDescription = "ReverseAbbr" };
			var nameNode = new ConfigurableDictionaryNode { Label = "Reverse Name", FieldDescription = "ReverseName" };

			var cfTypeNodeWithRevAbbr = new ConfigurableDictionaryNode
			{
				Label = "Complex Form Type",
				FieldDescription = "ComplexEntryTypesRS",
				Children = new List<ConfigurableDictionaryNode> { revAbbrNode, nameNode }
			};

			var variantTypeNodeWithRevAbbr = cfTypeNodeWithRevAbbr.DeepCloneUnderSameParent();
			variantTypeNodeWithRevAbbr.SubField = "VariantEntryTypesRS";

			var fakeNodeForMinTest = new ConfigurableDictionaryNode
			{
				Label = "FakeParentForMinTest", // Root and Hybrid only
				FieldDescription = "typeholder",
				Children = new List<ConfigurableDictionaryNode> { cfTypeNodeWithRevAbbr, variantTypeNodeWithRevAbbr }
			};

			var model = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha2,
				Parts = new List<ConfigurableDictionaryNode> { fakeNodeForMinTest },
				WritingSystem = "en"
			};

			_migrator.MigrateFrom83Alpha(model);
			var cfTypeNode = fakeNodeForMinTest.Children.First();
			Assert.AreEqual(2, cfTypeNode.Children.Count, "Should only have two children, Abbreviation and Name");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Abbreviation"),
				"Reverse Abbreviation should be changed to Abbreviation");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Name"),
				"Reverse Name should be changed to Name");
			cfTypeNode = fakeNodeForMinTest.Children[1];
			Assert.AreEqual(2, cfTypeNode.Children.Count, "Should only have two children, Abbreviation and Name");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Abbreviation"),
				"Reverse Abbreviation should be changed to Abbreviation");
			Assert.IsNotNull(cfTypeNode.Children.Find(node => node.Label == "Name"),
				"Reverse Name should be changed to Name");
		}

		[Test]
		public void MigrateFromConfigV7toV8_AddsIsRootBased_Stem()
		{
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
			};
			var model = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha2,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode },
				FilePath = "./Lexeme" + DictionaryConfigurationModel.FileExtension
			};
			_migrator.MigrateFrom83Alpha(model);
			Assert.IsFalse(model.IsRootBased);
		}

		[Test]
		public void MigrateFromConfigV7toV8_AddsIsRootBased_Root()
		{
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
			};
			var model = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha2,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode },
				FilePath = "./Root" + DictionaryConfigurationModel.FileExtension
			};
			_migrator.MigrateFrom83Alpha(model);
			Assert.IsTrue(model.IsRootBased);
		}

		[Test]
		public void MigrateFromConfigV6toV7_UpdatesAllomorphFieldDescription()
		{
			var AllomorphNode = new ConfigurableDictionaryNode
			{
				Label = "Allomorphs",
				FieldDescription = "Owner",
				SubField = "AlternateFormsOS",
				CSSClassNameOverride = "allomorphs",
			};
			var referencedSensesNode = new ConfigurableDictionaryNode
			{
				Label = "Referenced Senses",
				FieldDescription = "ReferringSenses",
				Children = new List<ConfigurableDictionaryNode> { AllomorphNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Reversal Entry",
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { referencedSensesNode }
			};
			var model = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha2,
				WritingSystem = "en",
				FilePath = String.Empty,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			_migrator.MigrateFrom83Alpha(model);
			Assert.AreEqual("Entry", AllomorphNode.FieldDescription, "Should have changed 'Owner' field for reversal to 'Entry'");
			Assert.AreEqual("AlternateFormsOS", AllomorphNode.SubField, "Should have changed to a sequence.");
		}

		[Test]
		public void MigrateFromConfigV6toV7_ReversalPronunciationBefAft()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				Before = "[",
				Between = " ",
				After = "] ",
				Label = "Pronunciation",
				FieldDescription = "Form",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "pronunciation" },
					DictionaryNodeWritingSystemOptions.WritingSystemType.Pronunciation)
			};
			var pronunciationsNode = new ConfigurableDictionaryNode
			{
				Between = " ",
				After = " ",
				Label = "Pronunciations",
				FieldDescription = "Owner",
				SubField = "PronunciationsOS",
				CSSClassNameOverride = "pronunciations",
				Children = new List<ConfigurableDictionaryNode> { formNode }
			};
			var referencedSensesNode = new ConfigurableDictionaryNode
			{
				Label = "Referenced Senses", FieldDescription = "ReferringSenses",
				Children = new List<ConfigurableDictionaryNode> { pronunciationsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Reversal Entry",
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> {referencedSensesNode}
			};
			var model = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha2,
				WritingSystem = "en",
				FilePath = string.Empty,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			_migrator.MigrateFrom83Alpha(model);
			Assert.AreEqual("[", pronunciationsNode.Before, "Should have set Before to '['.");
			Assert.AreEqual("] ", pronunciationsNode.After, "Should have set After to '] '.");
			Assert.AreEqual(" ", pronunciationsNode.Between, "Should have set Between to one space.");
			Assert.AreEqual("", formNode.Before, "Should have set Before to empty string.");
			Assert.AreEqual(" ", formNode.After, "Should have set After to one space.");
			Assert.AreEqual("", formNode.Between, "Should have set Between to empty string.");
		}

		/// <summary>
		/// Part of LT-12572.
		/// </summary>
		[Test]
		public void MigrateFrom83Alpha_PicturesChildrenUpdated()
		{
			// Old:
			//   Pictures
			//    - Thumbnail
			//    - Sense Number
			//    - Caption
			// Should me migrated to:
			//   Pictures
			//    - Thumbnail
			//    - Caption
			//    - Headword
			//    - Gloss

			var captionNode = new ConfigurableDictionaryNode
				{ Label = "Caption", FieldDescription = "Caption" };
			var senseNumberNode = new ConfigurableDictionaryNode
				{ Label = "Sense Number", FieldDescription = "SenseNumberTSS" };
			var thumbnailNode = new ConfigurableDictionaryNode
				{ Label = "Thumbnail", FieldDescription = "PictureFieldRA" };
			var picturesNode = new ConfigurableDictionaryNode
				{
					Label = "Pictures",
					FieldDescription = "PicturesOfSenses",
					Children = new List<ConfigurableDictionaryNode>{ thumbnailNode, senseNumberNode, captionNode }
				};
			var mainEntryNode = new ConfigurableDictionaryNode
				{
					Label = "Main Entry",
					FieldDescription = "LexEntry",
					Children = new List<ConfigurableDictionaryNode> { picturesNode }
				};
			var model = new DictionaryConfigurationModel { Version = PreHistoricMigrator.VersionAlpha1, Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };

			// SUT
			_migrator.MigrateFrom83Alpha(model);

			var migratedPicturesNode=model.Parts[0].Children[0];

			// Sense Number should be gone
			Assert.That(migratedPicturesNode.Children[1].Label, Is.Not.StringMatching("Sense Number"), "Sense Number should be gone");

			// Thumbnail and Caption should still be there
			Assert.That(migratedPicturesNode.Children[0].Label, Is.StringMatching("Thumbnail"), "Thumbnail should still be present");
			Assert.That(migratedPicturesNode.Children[1].Label, Is.StringMatching("Caption"), "Caption should still be present");

			// Headword and Gloss should now be there
			Assert.That(migratedPicturesNode.Children.Count, Is.GreaterThanOrEqualTo(3), "Not enough child nodes. Maybe Headword and Gloss weren't added.");
			Assert.That(migratedPicturesNode.Children[2].Label, Is.StringMatching("Headword"), "Headword not introduced");
			Assert.That(migratedPicturesNode.Children[3].Label, Is.StringMatching("Gloss"), "Gloss not introduced");
		}
	}
}
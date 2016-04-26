using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Palaso.IO;
using Palaso.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	public class FirstAlphaMigratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FirstAlphaMigrator m_migrator;

		[SetUp]
		public void SetUp()
		{
			m_migrator = new FirstAlphaMigrator(Cache);
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesVersion()
		{
			var alphaModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode() } };
			m_migrator.MigrateFrom83Alpha(alphaModel); // SUT
			Assert.AreEqual(DictionaryConfigurationMigrator.VersionCurrent, alphaModel.Version);
		}

		[Test]
		public void MigrateFrom83Alpha_ConfigWithVerMinus1GetsMigrated()
		{
			var configChild = new ConfigurableDictionaryNode { ReferenceItem = "LexEntry" };
			var configParent = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { configChild } };
			var configModel = new DictionaryConfigurationModel
			{
				Version = PreHistoricMigrator.VersionPre83, // the original migration code neglected to update the version on completion
				Parts = new List<ConfigurableDictionaryNode> { configParent }
			};
			m_migrator.MigrateFrom83Alpha(configModel);
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
			var configModel = new DictionaryConfigurationModel
			{
				Version = 3,
				Parts = new List<ConfigurableDictionaryNode> { configParent, configSenses }
			};
			m_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("GlossOrSummary", configGlossOrSummDefn.FieldDescription,
				"'Gloss (or Summary Definition)' Field Description should have been updated");
			Assert.AreEqual("DefinitionOrGloss", configDefnOrGloss.FieldDescription,
				"'Definition (or Gloss)' should not change fields");
		}

		[Test]
		public void MigrateFrom83Alpha_RemovesDeadReferenceItems()
		{
			var configChild = new ConfigurableDictionaryNode { ReferenceItem = "LexEntry" };
			var configParent = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { configChild } };
			var configModel = new DictionaryConfigurationModel { Version = 1, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			m_migrator.MigrateFrom83Alpha(configModel);
			Assert.Null(configChild.ReferenceItem, "Unused ReferenceItem should have been removed");
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesExampleSentenceLabels()
		{
			var configExampleChild = new ConfigurableDictionaryNode { Label = "Example", FieldDescription = "Example" };
			var configExampleParent = new ConfigurableDictionaryNode { Label = "Examples", FieldDescription = "ExamplesOS", Children = new List<ConfigurableDictionaryNode> { configExampleChild } };
			var configParent = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { configExampleParent } };
			var configModel = new DictionaryConfigurationModel { Version = 3, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			m_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("Example Sentence", configExampleChild.Label);
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesExampleOptions()
		{
			var configExamplesNode = new ConfigurableDictionaryNode { Label = "Examples", FieldDescription = "ExamplesOS" };
			var configParent = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { configExamplesNode } };
			var configModel = new DictionaryConfigurationModel { Version = 3, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			m_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual(ConfigurableDictionaryNode.StyleTypes.Paragraph, configExamplesNode.StyleType);
			Assert.AreEqual("Bulleted List", configExamplesNode.Style);
			Assert.IsTrue(configExamplesNode.DictionaryNodeOptions is DictionaryNodeComplexFormOptions, "wrong type");
			var options = (DictionaryNodeComplexFormOptions)configExamplesNode.DictionaryNodeOptions;
			Assert.IsTrue(options.DisplayEachComplexFormInAParagraph, "True was not set");
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesBibliographyLabels()
		{
			var configBiblioEntryNode = new ConfigurableDictionaryNode { Label = "Bibliography", FieldDescription = "Owner", SubField = "Bibliography" };
			var configBiblioSenseNode = new ConfigurableDictionaryNode { Label = "Bibliography", FieldDescription = "Bibliography" };
			var configBiblioParent = new ConfigurableDictionaryNode { Label = "Referenced Senses", FieldDescription = "ReferringSenses", Children = new List<ConfigurableDictionaryNode> { configBiblioSenseNode, configBiblioEntryNode } };
			var configParent = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { configBiblioParent } };
			var configModel = new DictionaryConfigurationModel { Version = 3, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			m_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("Bibliography (Entry)", configBiblioEntryNode.Label);
			Assert.AreEqual("Bibliography (Sense)", configBiblioSenseNode.Label);
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesHeadWordRefs()
		{
			var cpFormChild = new ConfigurableDictionaryNode { Label = "Complex Form", FieldDescription = "OwningEntry", SubField = "MLHeadWord" };
			var referenceHwChild = new ConfigurableDictionaryNode { Label = "Referenced Headword", FieldDescription = "HeadWord" };
			var configParent = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { referenceHwChild, cpFormChild } };
			var configModel = new DictionaryConfigurationModel { Version = 2, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			m_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("HeadWordRef", referenceHwChild.FieldDescription);
			Assert.AreEqual("HeadWordRef", cpFormChild.SubField);
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesReversalHeadwordRefs()
		{
			var cpFormChild = new ConfigurableDictionaryNode { Label = "Complex Form", FieldDescription = "OwningEntry", SubField = "MLHeadWord" };
			var referenceHwChild = new ConfigurableDictionaryNode { Label = "Referenced Headword", FieldDescription = "HeadWord" };
			var configParent = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { referenceHwChild, cpFormChild } };
			var configModel = new DictionaryConfigurationModel { Version = 2, WritingSystem = "en", Parts = new List<ConfigurableDictionaryNode> { configParent } };
			m_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("ReversalName", referenceHwChild.FieldDescription);
			Assert.AreEqual("ReversalName", cpFormChild.SubField);
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesSharedItems()
		{
			var cpFormChild = new ConfigurableDictionaryNode { Label = "Complex Form", FieldDescription = "OwningEntry", SubField = "MLHeadWord" };
			var referenceHwChild = new ConfigurableDictionaryNode { Label = "Referenced Headword", FieldDescription = "HeadWord" };
			var configParent = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { referenceHwChild, cpFormChild } };
			var configModel = new DictionaryConfigurationModel
			{
				Version = 2,
				WritingSystem = "en",
				Parts = new List<ConfigurableDictionaryNode>(),
				SharedItems = new List<ConfigurableDictionaryNode> { configParent }
			};
			m_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("ReversalName", referenceHwChild.FieldDescription);
			Assert.AreEqual("ReversalName", cpFormChild.SubField);
		}

		[Test]
		public void MigrateFrom83Alpha_MissingReversalWsFilledIn()
		{
			Cache.LangProject.AddToCurrentAnalysisWritingSystems((IWritingSystem)Cache.WritingSystemFactory.get_Engine("ta-fonipa"));
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
			m_migrator.MigrateFrom83Alpha(configModelEn);
			Assert.AreEqual("en", configModelEn.WritingSystem);
			m_migrator.MigrateFrom83Alpha(configModelTamil);
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
			m_migrator.MigrateFrom83Alpha(configModelRoot);
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
			m_migrator.MigrateFrom83Alpha(configModelRoot);
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
			m_migrator.MigrateFrom83Alpha(model);
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
		public void MigrateFrom83Alpha_UpdatesTranslationsCssClass()
		{
			var configTranslationsChild = new ConfigurableDictionaryNode { Label = "Translations", FieldDescription = "TranslationsOC" };
			var configExampleParent = new ConfigurableDictionaryNode { Label = "Examples", FieldDescription = "ExamplesOS", Children = new List<ConfigurableDictionaryNode> { configTranslationsChild } };
			var configParent = new ConfigurableDictionaryNode { Children = new List<ConfigurableDictionaryNode> { configExampleParent } };
			var configModel = new DictionaryConfigurationModel { Version = 3, Parts = new List<ConfigurableDictionaryNode> { configParent } };
			m_migrator.MigrateFrom83Alpha(configModel);
			Assert.AreEqual("translationcontents", configTranslationsChild.CSSClassNameOverride);
		}
	}
}
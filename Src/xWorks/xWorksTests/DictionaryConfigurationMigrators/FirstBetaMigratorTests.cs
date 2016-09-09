// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	public class FirstBetaMigratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string KidField = "kiddo";
		private FirstBetaMigrator m_migrator;
		private SimpleLogger m_logger;

		[SetUp]
		public void SetUp()
		{
			m_logger = new SimpleLogger();
			m_migrator = new FirstBetaMigrator(Cache, m_logger);
		}

		[TearDown]
		public void TearDown()
		{
			m_logger.Dispose();
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesVersion()
		{
			var alphaModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha2,
				Parts = new List<ConfigurableDictionaryNode>()
			};
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode>() }); // SUT
			Assert.AreEqual(DictionaryConfigurationMigrator.VersionCurrent, alphaModel.Version);
		}

		[Test]
		public void MigrateFrom83Alpha_ItemsMovedIntoGroupsAreMoved()
		{
			var firstPartNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Test",
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = KidField} }
			};
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha3, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
			var group = "Group";
			var groupNode = new ConfigurableDictionaryNode
			{
				FieldDescription = group,
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions(),
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = KidField } }
			};
			var defaultModelWithGroup = new DictionaryConfigurationModel
			{
				Version = DictionaryConfigurationMigrator.VersionCurrent,
				Parts = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = "Test", Children = new List<ConfigurableDictionaryNode> { groupNode } } }
			};
			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(defaultModelWithGroup);
			// reset the kiddo state to false after using the utility methods (they set IsEnabled to true on all nodes)
			firstPartNode.Children[0].IsEnabled = false;
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, defaultModelWithGroup); // SUT
			Assert.IsFalse(alphaModel.Parts[0].Children.Any(child => child.FieldDescription == KidField), "The child should have been moved out of the parent and into the group");
			Assert.IsTrue(alphaModel.Parts[0].Children.Any(child => child.FieldDescription == group), "The group should have been added");
			Assert.IsTrue(alphaModel.Parts[0].Children[0].Children[0].FieldDescription == KidField, "The child should have ended up inside the group");
			Assert.IsFalse(alphaModel.Parts[0].Children[0].Children[0].IsEnabled, "The child keep the enabled state even though it moved");
		}

		[Test]
		public void MigrateFrom83Alpha_GroupPlacedAfterThePreceedingSiblingFromDefault()
		{
			var olderBroField = "OlderBro";
			var firstPartNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Test",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = olderBroField},
					new ConfigurableDictionaryNode { FieldDescription = "OtherBrotherBob"},
					new ConfigurableDictionaryNode { FieldDescription = KidField }
				}
			};
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha3, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
			var group = "Group";
			var groupNode = new ConfigurableDictionaryNode
			{
				FieldDescription = group,
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions(),
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = KidField } }
			};
			var olderBrother = new ConfigurableDictionaryNode {FieldDescription = olderBroField};
			var defaultModelWithGroup = new DictionaryConfigurationModel
			{
				Version = DictionaryConfigurationMigrator.VersionCurrent,
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "Test", Children = new List<ConfigurableDictionaryNode> { olderBrother, groupNode } }
				}
			};
			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(defaultModelWithGroup);
			// reset the kiddo state to false after using the utility methods (they set IsEnabled to true on all nodes)
			firstPartNode.Children[0].IsEnabled = false;
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, defaultModelWithGroup); // SUT
			Assert.IsTrue(alphaModel.Parts[0].Children[1].FieldDescription == group, "The group should have ended up following the olderBroField");
			Assert.IsTrue(alphaModel.Parts[0].Children[2].FieldDescription == "OtherBrotherBob", "The original order of unrelated fields should be retained");
		}

		[Test]
		public void MigrateFrom83Alpha_GroupPlacedAtEndIfNoPreceedingSiblingFound()
		{
			var olderBroField = "OlderBro";
			var firstPartNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Test",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "OtherBrotherBob"},
					new ConfigurableDictionaryNode { FieldDescription = KidField }
				}
			};
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha3, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
			var group = "Group";
			var groupNode = new ConfigurableDictionaryNode
			{
				FieldDescription = group,
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions(),
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = KidField } }
			};
			var olderBrother = new ConfigurableDictionaryNode { FieldDescription = olderBroField };
			var defaultModelWithGroup = new DictionaryConfigurationModel
			{
				Version = DictionaryConfigurationMigrator.VersionCurrent,
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "Test", Children = new List<ConfigurableDictionaryNode> { olderBrother, groupNode } }
				}
			};
			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(defaultModelWithGroup);
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, defaultModelWithGroup); // SUT
			Assert.IsTrue(alphaModel.Parts[0].Children[2].FieldDescription == group,
				"The group should be tacked on the end when the preceeding sibling couldn't be matched");
		}

		[Test]
		public void MigrateFrom83Alpha_ChildAndGrandChildGroupsMigrated()
		{
			var olderBroField = "OlderBro";
			var grandChildField = "GrandKid";
			var cousinField = "cuz";
			var grandKidNode = new ConfigurableDictionaryNode { FieldDescription = grandChildField };
			var cousinNode = new ConfigurableDictionaryNode { FieldDescription = cousinField };
			var firstPartNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Test",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = olderBroField, Children = new List<ConfigurableDictionaryNode> { cousinNode } },
					new ConfigurableDictionaryNode { FieldDescription = KidField, Children = new List<ConfigurableDictionaryNode> { grandKidNode } }
				}
			};
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha3, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
			var group = "Group";
			var grandKidGroup = new ConfigurableDictionaryNode
			{
				FieldDescription = group,
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions(),
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = grandChildField } }
			};
			var groupNode = new ConfigurableDictionaryNode
			{
				FieldDescription = group,
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions(),
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = KidField , Children = new List<ConfigurableDictionaryNode> { grandKidGroup} }
				}
			};
			var cousinGroup = new ConfigurableDictionaryNode
			{
				FieldDescription = group,
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions(),
				Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = cousinField } }
			};
			var olderBrother = new ConfigurableDictionaryNode { FieldDescription = olderBroField, Children = new List<ConfigurableDictionaryNode> { cousinGroup } };
			var defaultModelWithGroup = new DictionaryConfigurationModel
			{
				Version = DictionaryConfigurationMigrator.VersionCurrent,
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "Test", Children = new List<ConfigurableDictionaryNode> { olderBrother, groupNode } }
				}
			};
			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(defaultModelWithGroup);
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, defaultModelWithGroup); // SUT

			var topGroupNode = alphaModel.Parts[0].Children[1];
			var olderBroNode = alphaModel.Parts[0].Children[0];
			Assert.IsTrue(topGroupNode.FieldDescription == group, "Child group not added");
			Assert.IsTrue(olderBroNode.Children[0].FieldDescription == group, "Group under non group not added");
			Assert.IsTrue(topGroupNode.Children[0].Children[0].FieldDescription == group, "Group not added under item that was moved into a group");
			Assert.IsTrue(topGroupNode.Children[0].Children[0].Children[0].FieldDescription == grandChildField, "Grand child group contents incorrect");
			Assert.IsTrue(olderBroNode.Children[0].Children[0].FieldDescription == cousinField, "Group under non-group contents incorrect");
		}

		[Test]
		public void MigrateFrom83Alpha_GroupPropertiesClonedFromNewDefaults()
		{
			var firstPartNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Test",
				Children = new List<ConfigurableDictionaryNode>()
			};
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha3, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
			var group = "Group";
			var description = "TestDescription";
			var before = "[";
			var after = "]";
			var style = "retro";
			var label = "Integrity";
			var groupNode = new ConfigurableDictionaryNode
			{
				Before = before,
				After = after,
				Style = style,
				Label = label,
				FieldDescription = group,
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions { Description = description, DisplayGroupInParagraph = true }
			};
			var defaultModelWithGroup = new DictionaryConfigurationModel
			{
				Version = DictionaryConfigurationMigrator.VersionCurrent,
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "Test", Children = new List<ConfigurableDictionaryNode> { groupNode } }
				}
			};
			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(defaultModelWithGroup);
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, defaultModelWithGroup); // SUT
			Assert.IsTrue(alphaModel.Parts[0].Children[0].FieldDescription == group, "The group node was not properly cloned");
			Assert.IsTrue(alphaModel.Parts[0].Children[0].Label == label, "The group node was not properly cloned");
			Assert.IsTrue(alphaModel.Parts[0].Children[0].Before == before, "The group node was not properly cloned");
			Assert.IsTrue(alphaModel.Parts[0].Children[0].After == after, "The group node was not properly cloned");
			Assert.IsTrue(alphaModel.Parts[0].Children[0].Style == style, "The group node was not properly cloned");
			Assert.AreEqual(alphaModel.Parts[0], alphaModel.Parts[0].Children[0].Parent, "The group node has the wrong parent");
		}

		[Test]
		public void MigrateFrom83Alpha_DefaultConfigsFoundForEachType()
		{
			var reversalModel = new DictionaryConfigurationModel { WritingSystem = "en" };
			var reversalDefault = m_migrator.LoadBetaDefaultForAlphaConfig(reversalModel); // SUT
			Assert.IsTrue(reversalDefault.IsReversal);
			Assert.That(reversalDefault.Label, Is.StringContaining("Reversal"));

			var rootModel = new DictionaryConfigurationModel { IsRootBased = true };
			var rootDefault = m_migrator.LoadBetaDefaultForAlphaConfig(rootModel); // SUT
			Assert.IsTrue(rootDefault.IsRootBased);
			Assert.That(rootDefault.Label, Is.StringContaining("Root"));

			var subEntry = new ConfigurableDictionaryNode
			{
				Label = "Minor Subentries",
				FieldDescription = "Subentries"
			};
			var hybridModel = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						Label = "Main Entry", FieldDescription = "LexEntry", CSSClassNameOverride = "entry",
						Children = new List<ConfigurableDictionaryNode> {subEntry}
					},
					new ConfigurableDictionaryNode
					{
						Label = "Main Entry (Complex Forms)", FieldDescription = "LexEntry", CSSClassNameOverride = "mainentrycomplex"
					}
				}
			};

			var hybridDefault = m_migrator.LoadBetaDefaultForAlphaConfig(hybridModel); // SUT
			Assert.That(hybridDefault.Label, Is.StringContaining("Hybrid"));

			var stemModel = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						Label = "Main Entry", FieldDescription = "LexEntry", CSSClassNameOverride = "entry"
					},
					new ConfigurableDictionaryNode { Label = "Main Entry (Complex Forms)", FieldDescription = "LexEntry", CSSClassNameOverride = "mainentrycomplex" }
				}
			};
			var stemDefault = m_migrator.LoadBetaDefaultForAlphaConfig(stemModel); // SUT
			Assert.That(stemDefault.Label, Is.StringContaining("Stem"));
		}

		[Test]
		public void MigrateFrom83Alpha_ExtendedNoteChildrenAreMigrated()
		{
			var alphaSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode>()
			};
			var alphaMainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { alphaSensesNode }
			};
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha3, Parts = new List<ConfigurableDictionaryNode> { alphaMainEntryNode } };

			var translationNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Translation"
			};
			var translationsNode = new ConfigurableDictionaryNode
			{
				Label = "Translations",
				FieldDescription = "TranslationsOC",
				CSSClassNameOverride = "translations",
				Children = new List<ConfigurableDictionaryNode> { translationNode }
			};
			var exampleNode = new ConfigurableDictionaryNode
			{
				Label = "Example Sentence",
				FieldDescription = "Example"
			};
			var examplesNode = new ConfigurableDictionaryNode
			{
				Label = "Examples",
				FieldDescription = "ExamplesOS",
				CSSClassNameOverride = "examples",
				Children = new List<ConfigurableDictionaryNode> { exampleNode, translationsNode }
			};
			var discussionTypeNode = new ConfigurableDictionaryNode
			{
				Label = "Discussion",
				FieldDescription = "Discussion"
			};
			var noteTypeNode = new ConfigurableDictionaryNode
			{
				Label = "Note Type",
				SubField = "Name",
				FieldDescription = "ExtendedNoteTypeRA"
			};
			var extendedNoteNode = new ConfigurableDictionaryNode
			{
				Label = "Extended Note",
				FieldDescription = "ExtendedNoteOS",
				CSSClassNameOverride = "extendednotecontents",
				Children = new List<ConfigurableDictionaryNode> { noteTypeNode, discussionTypeNode, examplesNode }
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { extendedNoteNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			var defaultModel = new DictionaryConfigurationModel { Version = DictionaryConfigurationMigrator.VersionCurrent, Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };

			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(defaultModel);
			// SUT
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, defaultModel);

			var migratedExtendedNoteNode = alphaModel.Parts[0].Children[0].Children[0];

			// Parent Node is Extended Note
			Assert.That(migratedExtendedNoteNode.Label, Is.StringMatching("Extended Note"), "Extended Note not migrated");

			// Children Nodes are Note Type, Discussion, Example Sentence, Translations
			Assert.That(migratedExtendedNoteNode.Children[0].Label, Is.StringMatching("Note Type"), "Note Type not migrated");
			Assert.That(migratedExtendedNoteNode.Children[1].Label, Is.StringMatching("Discussion"), "Discussion not migrated");
			Assert.That(migratedExtendedNoteNode.Children[2].Children[0].Label, Is.StringMatching("Example Sentence"), "Example Sentence not migrated");
			Assert.That(migratedExtendedNoteNode.Children[2].Children[1].Label, Is.StringMatching("Translations"), "Translations not migrated");
		}

		[Test]
		public void MigrateFromConfig83AlphaToBeta10_UpdatesEtymologyCluster()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				Label = "Etymological Form",
				FieldDescription = "Form",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "best vernoranal" },
					DictionaryNodeWritingSystemOptions.WritingSystemType.Both)
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				Label = "Gloss",
				FieldDescription = "Gloss",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "analysis" },
					DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var commentNode = new ConfigurableDictionaryNode
			{
				Label = "Comment",
				FieldDescription = "Comment",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "analysis" },
					DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var sourceNode = new ConfigurableDictionaryNode
			{
				Label = "Source",
				FieldDescription = "Source"
			};
			var etymologyNode = new ConfigurableDictionaryNode
			{
				Label = "Etymology",
				FieldDescription = "EtymologyOA",
				CSSClassNameOverride = "etymology",
				Children = new List<ConfigurableDictionaryNode> { formNode, glossNode, commentNode, sourceNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { etymologyNode }
			};
			var alphaModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			var rootModel = m_migrator.LoadBetaDefaultForAlphaConfig(alphaModel);
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, rootModel);
			Assert.AreEqual("EtymologyOS", etymologyNode.FieldDescription, "Should have changed to a sequence.");
			Assert.AreEqual("etymologies", etymologyNode.CSSClassNameOverride, "Should have changed CSS override");
			Assert.AreEqual("(", etymologyNode.Before, "Should have set Before to '('.");
			Assert.AreEqual(") ", etymologyNode.After, "Should have set After to ') '.");
			Assert.AreEqual(" ", etymologyNode.Between, "Should have set Between to one space.");
			var etymChildren = etymologyNode.Children;
			// instead of verifying certain nodes are NOT present, we'll just verify all 7 of the expected nodes
			// and that there ARE only 7 nodes.
			Assert.AreEqual(7, etymChildren.Count);
			var configNode = etymChildren.Find(node => node.Label == "Preceding Annotation");
			Assert.IsNotNull(configNode, "Should have added Preceding Annotation node");
			Assert.That(configNode.FieldDescription, Is.EqualTo("PrecComment"));
			Assert.That(configNode.IsEnabled, Is.True, "PrecComment node should be enabled");
			TestForWritingSystemOptionsType(configNode, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis);
			configNode = etymChildren.Find(node => node.Label == "Source Language");
			Assert.IsNotNull(configNode, "Should have added Source Language node");
			Assert.That(configNode.FieldDescription, Is.EqualTo("LanguageRS"));
			Assert.That(configNode.IsEnabled, Is.True, "Language node should be enabled");
			Assert.True(configNode.IsEnabled, "Source Language node should be enabled by default");
			Assert.That(configNode.CSSClassNameOverride, Is.EqualTo("languages"), "Should have changed the css override");
			// Just checking that some 'contexts' have been filled in by the new default config.
			Assert.That(configNode.Between, Is.EqualTo(" "));
			Assert.That(configNode.After, Is.EqualTo(" "));
			Assert.That(configNode.Before, Is.Null);
			var childNodes = configNode.Children;
			Assert.That(childNodes.Count, Is.EqualTo(2), "We ought to have Abbreviation and Name nodes here");
			var abbrNode = childNodes.Find(n => n.Label == "Abbreviation");
			Assert.IsNotNull(abbrNode, "Source Language should have an Abbrevation node");
			Assert.True(abbrNode.IsEnabled, "Abbrevation node should be enabled by default");
			TestForWritingSystemOptionsType(abbrNode, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis);
			var nameNode = childNodes.Find(n => n.Label == "Name");
			Assert.IsNotNull(nameNode, "Source Language should have an Name node");
			Assert.False(nameNode.IsEnabled, "Name node should not be enabled by default");
			TestForWritingSystemOptionsType(nameNode, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis);
			var langNotesNode = etymChildren.Find(node => node.FieldDescription == "LanguageNotes");
			Assert.That(langNotesNode.IsEnabled, Is.False, "LanguageNotes node should not be enabled by default");
			TestForWritingSystemOptionsType(langNotesNode, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis);
			configNode = etymChildren.Find(node => node.Label == "Source Form");
			Assert.IsNotNull(configNode, "Should have changed the name of the old Etymological Form node");
			Assert.That(configNode.FieldDescription, Is.EqualTo("Form"));
			Assert.That(configNode.IsEnabled, Is.True, "Form node should be enabled");
			TestForWritingSystemOptionsType(configNode, DictionaryNodeWritingSystemOptions.WritingSystemType.Both);
			configNode = etymChildren.Find(node => node.Label == "Gloss");
			Assert.IsNotNull(configNode, "Should still have the Gloss node");
			Assert.That(configNode.FieldDescription, Is.EqualTo("Gloss"));
			Assert.That(configNode.IsEnabled, Is.True, "Gloss node should be enabled");
			TestForWritingSystemOptionsType(configNode, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis);
			configNode = etymChildren.Find(node => node.Label == "Following Comment");
			Assert.IsNotNull(configNode, "Should have changed the name of the old Comment node");
			Assert.That(configNode.FieldDescription, Is.EqualTo("Comment"));
			Assert.That(configNode.IsEnabled, Is.False, "Comment node should NOT be enabled");
			TestForWritingSystemOptionsType(configNode, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis);
			configNode = etymChildren.Find(node => node.Label == "Note");
			Assert.IsNull(configNode, "Should NOT add Note node to configurations");
			configNode = etymChildren.Find(node => node.Label == "Bibliographic Source");
			Assert.IsNotNull(configNode, "Should have added Bibliographic Source node");
			Assert.That(configNode.FieldDescription, Is.EqualTo("Bibliography"));
			Assert.That(configNode.IsEnabled, Is.True, "Bibliography node should be enabled");
			TestForWritingSystemOptionsType(configNode, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis);
		}

		private static void TestForWritingSystemOptionsType(ConfigurableDictionaryNode configNode,
			DictionaryNodeWritingSystemOptions.WritingSystemType expectedWsType)
		{
			var options = configNode.DictionaryNodeOptions;
			Assert.True(options is DictionaryNodeWritingSystemOptions, "Config node should have WritingSystemOptions");
			Assert.AreEqual(expectedWsType, (options as DictionaryNodeWritingSystemOptions).WsType);
		}

		[Test]
		public void MigrateFrom83AlphaToBeta10_UpdatesReversalEtymologyCluster()
		{
			var formNode = new ConfigurableDictionaryNode
			{
				Label = "Etymological Form",
				FieldDescription = "Form",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "best vernoranal" },
					DictionaryNodeWritingSystemOptions.WritingSystemType.Both)
			};
			var glossNode = new ConfigurableDictionaryNode
			{
				Label = "Gloss",
				FieldDescription = "Gloss",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "analysis" },
					DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var commentNode = new ConfigurableDictionaryNode
			{
				Label = "Comment",
				FieldDescription = "Comment",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetWsOptionsForLanguages(new[] { "analysis" },
					DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis)
			};
			var sourceNode = new ConfigurableDictionaryNode
			{
				Label = "Source",
				FieldDescription = "Source"
			};
			var etymologyNode = new ConfigurableDictionaryNode
			{
				Label = "Etymology",
				FieldDescription = "Owner",
				SubField = "EtymologyOA",
				CSSClassNameOverride = "etymology",
				Children = new List<ConfigurableDictionaryNode> { formNode, glossNode, commentNode, sourceNode }
			};
			var referencedSensesNode = new ConfigurableDictionaryNode
			{
				Label = "Referenced Senses",
				FieldDescription = "ReferringSenses",
				Children = new List<ConfigurableDictionaryNode> { etymologyNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Reversal Entry",
				FieldDescription = "ReversalIndexEntry",
				Children = new List<ConfigurableDictionaryNode> { referencedSensesNode }
			};
			var alphaModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3,
				WritingSystem = "en",
				FilePath = string.Empty,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			var rootModel = m_migrator.LoadBetaDefaultForAlphaConfig(alphaModel);
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, rootModel);
			Assert.AreEqual("EtymologyOS", etymologyNode.SubField, "Should have changed to a sequence.");
			Assert.AreEqual("Entry", etymologyNode.FieldDescription, "Should have changed 'Owner' field for reversal to 'Entry'");
			Assert.AreEqual("etymologies", etymologyNode.CSSClassNameOverride, "Should have changed CSS override");
			Assert.AreEqual(7, etymologyNode.Children.Count, "There should be 7 nodes after the conversion.");
			Assert.IsNull(etymologyNode.DictionaryNodeOptions, "Improper options added to etymology sequence node.");
		}
	}
}

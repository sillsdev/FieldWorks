// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Palaso.IO;
using SIL.CoreImpl;
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
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha2, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
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
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha2, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
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
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha2, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
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
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha2, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
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
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha2, Parts = new List<ConfigurableDictionaryNode> { firstPartNode } };
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
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha2, Parts = new List<ConfigurableDictionaryNode> { alphaMainEntryNode } };

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

			var defaultModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha2, Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };

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
	}
}

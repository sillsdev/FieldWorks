// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageExplorer;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Works;
using LanguageExplorer.Works.DictionaryConfigurationMigrators;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.IO;
using SIL.TestUtilities;

namespace LanguageExplorerTests.Works.DictionaryConfigurationMigrators
{
	public class FirstBetaMigratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private const string KidField = "kiddo";
		private const string LexEntry = "LexEntry";
		private const string ReferencedComplexForms = "VisibleComplexFormBackRefs";
		private const string OtherRefdComplexForms = "ComplexFormsNotSubentries";
		private FirstBetaMigrator m_migrator;
		private ISimpleLogger m_logger;

		public override void TestSetup()
		{
			base.TestSetup();

			_flexComponentParameters = TestSetupServices.SetupEverything(Cache);
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, FwUtils.ksFlexAppName, Cache.ProjectId.Path);
			m_logger = new SimpleLogger();
			m_migrator = new FirstBetaMigrator(Cache, m_logger);
		}

		[TearDown]
		public override void TestTearDown()
		{
			_flexComponentParameters.PropertyTable.Dispose();
			DirectoryUtilities.DeleteDirectoryRobust(Cache.ProjectId.Path);
			m_logger.Dispose();
			_flexComponentParameters = null;
			m_logger = null;
			m_migrator = null;

			base.TestTearDown();
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesVersion()
		{
			var alphaModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha2,
				IsRootBased = true,
				Parts = new List<ConfigurableDictionaryNode>()
			};
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode>() }); // SUT
			Assert.AreEqual(DictionaryConfigurationMigrator.VersionCurrent, alphaModel.Version);
		}

		[Test]
		public void MigrateFrom83Alpha_MoveStemToLexeme()
		{
			using (var tempFolder = TemporaryFolder.TrackExisting(Path.GetDirectoryName(Cache.ProjectId.Path)))
			{
				var configLocations = LcmFileHelper.GetConfigSettingsDir(tempFolder.Path);
				configLocations = Path.Combine(configLocations, "Dictionary");
				Directory.CreateDirectory(configLocations);
				const string content =
@"<DictionaryConfiguration xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'
name='Stem-based (complex forms as main entries)' version='8' lastModified='2016-10-05' allPublications='true'>
  <ConfigurationItem name='Main Entry' isEnabled='true' style='Dictionary-Normal' styleType='paragraph' field='LexEntry' cssClassNameOverride='entry'/>
</DictionaryConfiguration>";
				var actualFilePath = Path.Combine(configLocations, "Stem" + DictionaryConfigurationModel.FileExtension);
				var convertedFilePath = Path.Combine(configLocations, "Lexeme" + DictionaryConfigurationModel.FileExtension);
				File.WriteAllText(actualFilePath, content);
				m_migrator.MigrateIfNeeded(m_logger, _flexComponentParameters.PropertyTable, "Test App Version"); // SUT
				Assert.IsTrue(File.Exists(convertedFilePath));
			}
		}

		[Test]
		public void MigrateFrom83Alpha_ItemsMovedIntoGroupsAreMoved()
		{
			var firstPartNode = new ConfigurableDictionaryNode
			{
				FieldDescription = LexEntry,
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
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = LexEntry, Children = new List<ConfigurableDictionaryNode> { groupNode } }
				}
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
			const string olderBroField = "OlderBro";
			var firstPartNode = new ConfigurableDictionaryNode
			{
				FieldDescription = LexEntry,
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
					new ConfigurableDictionaryNode { FieldDescription = LexEntry, Children = new List<ConfigurableDictionaryNode> { olderBrother, groupNode } }
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
				FieldDescription = LexEntry,
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
					new ConfigurableDictionaryNode { FieldDescription = LexEntry, Children = new List<ConfigurableDictionaryNode> { olderBrother, groupNode } }
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
				FieldDescription = LexEntry,
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
					new ConfigurableDictionaryNode { FieldDescription = LexEntry, Children = new List<ConfigurableDictionaryNode> { olderBrother, groupNode } }
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
				FieldDescription = "ReversalIndexEntry",
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
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions { Description = description, DisplayEachInAParagraph = true }
			};
			var defaultModelWithGroup = new DictionaryConfigurationModel
			{
				Version = DictionaryConfigurationMigrator.VersionCurrent,
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "ReversalIndexEntry", Children = new List<ConfigurableDictionaryNode> { groupNode } }
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
		public void MigrateFrom83Alpha_ConflatesMainEntriesForLexemey([Values(true, false)] bool isHybrid)
		{
			const string kiddoBefore = "This is mine: ";
			const string componentsBefore = "Before: ";
			const string ComponentReferences = "Component References";
			var RCFsForThisConfig = isHybrid ? OtherRefdComplexForms : ReferencedComplexForms;
			var RCFLabelForThisConfig = isHybrid ? "Other Referenced Complex Forms" : "Referenced Complex Forms";
			var extantChildNode = new ConfigurableDictionaryNode { FieldDescription = KidField, Before = kiddoBefore };
			// LT-17962 was reopened for disappearing Component References nodes. What had happened was, when we split Main Entry,
			// Main Entry (NOT Complex) did not have Component References, so the corresponding legacy node was marked as Custom.
			// If this Custom node is not removed before we reconflate the two Main Entry nodes, it blocks the legitimate Component References node
			// from being added back into Main Entry (now combined).
			var customProblemNode = new ConfigurableDictionaryNode { FieldDescription = ComponentReferences, IsCustomField = true };
			var mainGroupingNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Group",
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions(),
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { Label = RCFLabelForThisConfig, FieldDescription = RCFsForThisConfig },
					customProblemNode
				}
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entries", FieldDescription = LexEntry, Children = new List<ConfigurableDictionaryNode> { extantChildNode, mainGroupingNode }
			};
			var componentsNode = new ConfigurableDictionaryNode { FieldDescription = "Components", Before = componentsBefore};
			var hiddenByCustomProblemNode = new ConfigurableDictionaryNode { Label = ComponentReferences, FieldDescription = "ComplexFormEntryRefs" };
			var extantChildUnderComplexNode = new ConfigurableDictionaryNode { FieldDescription = KidField, Before = "This is not mine: "};
			var otherUniqueChildNode =  new ConfigurableDictionaryNode { FieldDescription = "ComplexKid" };
			var complexGroupingNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "Group",
				DictionaryNodeOptions = new DictionaryNodeGroupingOptions(),
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "GroupedChild" },
					new ConfigurableDictionaryNode { Label = RCFLabelForThisConfig, FieldDescription = RCFsForThisConfig },
					hiddenByCustomProblemNode
				}
			};
			var complexMainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Complex Entries", FieldDescription = LexEntry,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, DictionaryNodeListOptions.ListIds.Complex),
				Children = new List<ConfigurableDictionaryNode> { componentsNode, complexGroupingNode, extantChildUnderComplexNode, otherUniqueChildNode }
			};
			if (isHybrid)
			{
				mainEntryNode.Children.Add(new ConfigurableDictionaryNode { FieldDescription = "Subentries" });
				complexMainEntryNode.Children.Add(new ConfigurableDictionaryNode { FieldDescription = "Subentries" });
			}
			var variantEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Variants", FieldDescription = LexEntry,
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, DictionaryNodeListOptions.ListIds.Variant)
			};
			var alphaModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3 + 1, // skip the adding of "new" grouping nodes; we already have them
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode, complexMainEntryNode, variantEntryNode }
			};

			// Beta model with entries already conflated. Shift Main Entry (Complex Forms) into Main Entry's position
			var betaModel = alphaModel.DeepClone();
			betaModel.Parts.RemoveAt(0);
			var betaMainEntryNode = betaModel.Parts[0];
			betaMainEntryNode.Label = "Main Entries";
			betaMainEntryNode.DictionaryNodeOptions = null;
			betaMainEntryNode.Children.Find(c => c.Before != null).Before = "Something completeley off the wall";

			// add an extraneous Complex node to ensure it is deleted on migration
			alphaModel.Parts.Add(complexMainEntryNode.DeepCloneUnderSameParent());
			// earlier versions of Hybrid mistakenly included all Referenced Complex Forms in their grouping node:
			if (isHybrid)
				complexGroupingNode.Children[1].FieldDescription = ReferencedComplexForms;

			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(betaModel);

			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, betaModel); // SUT
			Assert.AreEqual(2, alphaModel.Parts.Count, "All root-level Complex Form nodes should have been removed");
			var mainChildren = alphaModel.Parts[0].Children;
			Assert.AreEqual(isHybrid ? 5 : 4, mainChildren.Count, "All child nodes of Main Entry (Complex Forms) should have been copied to Main Entry");
			Assert.AreEqual("Components", mainChildren[0].FieldDescription, "Components should have been inserted at the beginning");
			Assert.AreEqual(componentsBefore, mainChildren[0].Before, "Components's Before material should have come from the user's configuration");
			Assert.AreEqual(KidField, mainChildren[1].FieldDescription, "The existing field should be in the middle");
			Assert.AreEqual(kiddoBefore, mainChildren[1].Before, "The existing node's Before should have retained its value from Main Entry proper");
			Assert.AreEqual("ComplexKid", mainChildren[2].FieldDescription, "The other child node should have been inserted after the existing one");
			Assert.AreEqual(typeof(DictionaryNodeGroupingOptions), mainChildren[3].DictionaryNodeOptions.GetType(), "The final node should be the group");
			var groupedChildren = mainChildren[3].Children;
			Assert.AreEqual(3, groupedChildren.Count, "groupedChildren.Count");
			Assert.AreEqual("GroupedChild", groupedChildren[0].FieldDescription, "Grouped child should have been copied into existing group");
			Assert.AreEqual(RCFsForThisConfig, groupedChildren[1].FieldDescription, "Subentries should not be included in *Other* Referenced Complex Forms");
			Assert.AreEqual("ComplexFormEntryRefs", groupedChildren[2].FieldDescription, "The legit node should have supplanted the placeholder Custom node");
			Assert.False(groupedChildren[isHybrid ? 1 : 2].IsCustomField, "Component References is NOT a Custom field");
		}

		[Test]
		public void MigrateFrom83Alpha_HandlesDuplicateVariantsNode()
		{
			var mainEntryNode = new ConfigurableDictionaryNode { Label = "Main Entries", FieldDescription = LexEntry };
			var complexEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Complex Entries",
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode>(),
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, DictionaryNodeListOptions.ListIds.Complex)
			};
			var variantEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Variants",
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode>(),
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, DictionaryNodeListOptions.ListIds.Variant)
			};
			var betaModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3,
				IsRootBased = true, // keep Complex its own node
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode, complexEntryNode, variantEntryNode }
			};

			// create alpha model with an extra Variants node
			var alphaModel = betaModel.DeepClone();
			alphaModel.Parts.Add(variantEntryNode.DeepCloneUnderSameParent());

			// Create a new node in the beta model that needs to be migrated in
			variantEntryNode.Children = new List<ConfigurableDictionaryNode> { new ConfigurableDictionaryNode { FieldDescription = KidField } };

			CssGeneratorTests.PopulateFieldsForTesting(betaModel);
			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);

			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, betaModel);
			var parts = alphaModel.Parts;
			Assert.AreEqual(4, parts.Count, "No parts should have been lost in migration");
			Assert.AreEqual("Main Entries", parts[0].Label);
			Assert.AreEqual("Complex Entries", parts[1].Label, "Complex Entries remain distinct in root-based configs");
			Assert.That(parts[1].Children, Is.Null.Or.Empty, "Child field should not have been added to Complex Entries node");
			Assert.AreEqual("Variants", parts[2].Label);
			Assert.AreEqual(KidField, parts[2].Children[0].FieldDescription);
			Assert.AreEqual("Variants", parts[3].Label);
			Assert.AreEqual(KidField, parts[3].Children[0].FieldDescription);
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
			Assert.That(rootDefault.Label, Is.StringContaining(DictionaryConfigurationMigrator.RootFileName));

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
						Label = "Main Entry", FieldDescription = LexEntry, CSSClassNameOverride = "entry",
						Children = new List<ConfigurableDictionaryNode> {subEntry}
					},
					new ConfigurableDictionaryNode
					{
						Label = "Main Entry (Complex Forms)", FieldDescription = LexEntry, CSSClassNameOverride = "mainentrycomplex"
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
						Label = "Main Entry", FieldDescription = LexEntry, CSSClassNameOverride = "entry"
					},
					new ConfigurableDictionaryNode { Label = "Main Entry (Complex Forms)", FieldDescription = LexEntry, CSSClassNameOverride = "mainentrycomplex" }
				}
			};
			var stemDefault = m_migrator.LoadBetaDefaultForAlphaConfig(stemModel); // SUT
			Assert.That(stemDefault.Label, Is.StringContaining("Lexeme"));
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
				FieldDescription = LexEntry,
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
				FieldDescription = LexEntry,
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
		public void MigrateFrom83Alpha_SenseVariantListTypeOptionsAreMigrated()
		{
			var alphaVariantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true
			};
			var alphaVariantFormsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { alphaVariantFormTypeNode },
				IsEnabled = true
			};
			var alphaSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { alphaVariantFormsNode }
			};
			var alphaMainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode> { alphaSensesNode }
			};
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha3, Parts = new List<ConfigurableDictionaryNode> { alphaMainEntryNode } };

			var variantFormTypeNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantEntryTypesRS",
				CSSClassNameOverride = "variantentrytypes",
				IsEnabled = true
			};
			var variantFormsNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "VariantFormEntryBackRefs",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, DictionaryNodeListOptions.ListIds.Variant),
				Children = new List<ConfigurableDictionaryNode> { variantFormTypeNode },
				IsEnabled = true
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { variantFormsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			var defaultModel = new DictionaryConfigurationModel { Version = DictionaryConfigurationMigrator.VersionCurrent, Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };

			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(defaultModel);
			// SUT
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, defaultModel);

			var migratedSenseVariantNode = alphaModel.Parts[0].Children[0].Children[0];
			Assert.True(migratedSenseVariantNode.DictionaryNodeOptions != null, "ListTypeOptions not migrated");
		}

		[Test]
		public void MigrateFrom83Alpha_NoteInParaOptionsAreMigrated()
		{
			var alphaAnthroNoteNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "AnthroNote",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions(),
				IsEnabled = true
			};
			var alphaSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { alphaAnthroNoteNode }
			};
			var alphaMainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode> { alphaSensesNode }
			};
			var alphaModel = new DictionaryConfigurationModel { Version = FirstAlphaMigrator.VersionAlpha3, Parts = new List<ConfigurableDictionaryNode> { alphaMainEntryNode } };

			var anthroNoteNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "AnthroNote",
				DictionaryNodeOptions = new DictionaryNodeWritingSystemAndParaOptions(),
				IsEnabled = true
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { anthroNoteNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};

			var defaultModel = new DictionaryConfigurationModel { Version = DictionaryConfigurationMigrator.VersionCurrent, Parts = new List<ConfigurableDictionaryNode> { mainEntryNode } };

			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(defaultModel);
			// SUT
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, defaultModel);

			var migratedNoteDictionaryOptionsNode = alphaModel.Parts[0].Children[0].Children[0];
			Assert.True(migratedNoteDictionaryOptionsNode.DictionaryNodeOptions != null, "DictionaryNodeOptions should not be null");
			Assert.True(migratedNoteDictionaryOptionsNode.DictionaryNodeOptions is DictionaryNodeWritingSystemAndParaOptions, "Config node should have WritingSystemOptions");
		}

		[Test]
		public void MigrateFrom83Alpha_ReferencedHeadwordFieldDescriptionNameAreMigrated()
		{
			var alphaRefSenseHeadwordTypeNode = new ConfigurableDictionaryNode
			{
				Label = "Referenced Sense Headword",
				FieldDescription = "HeadWord",
				CSSClassNameOverride = "headword",
				IsEnabled = true
			};
			var alphaTargetsNode = new ConfigurableDictionaryNode
			{
				Label = "Targets",
				FieldDescription = "ConfigTargets",
				Children = new List<ConfigurableDictionaryNode> { alphaRefSenseHeadwordTypeNode },
				IsEnabled = true
			};
			var alphaSensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { alphaTargetsNode }
			};
			var alphaMainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode> { alphaSensesNode }
			};
			var alphaModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3, Parts = new List<ConfigurableDictionaryNode> { alphaMainEntryNode }
			};

			var RefSenseHeadwordTypeNode = new ConfigurableDictionaryNode
			{
				Label = "Referenced Sense Headword",
				FieldDescription = "HeadWordRef",
				CSSClassNameOverride = "headword",
				IsEnabled = true
			};
			var TargetsNode = new ConfigurableDictionaryNode
			{
				Label = "Targets",
				FieldDescription = "ConfigTargets",
				DictionaryNodeOptions = ConfiguredXHTMLGeneratorTests.GetFullyEnabledListOptions(Cache, DictionaryNodeListOptions.ListIds.Variant),
				Children = new List<ConfigurableDictionaryNode> { RefSenseHeadwordTypeNode },
				IsEnabled = true
			};
			var sensesNode = new ConfigurableDictionaryNode
			{
				FieldDescription = "SensesOS",
				CSSClassNameOverride = "senses",
				Children = new List<ConfigurableDictionaryNode> { TargetsNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode> { sensesNode }
			};
			var defaultModel = new DictionaryConfigurationModel
			{
				Version = DictionaryConfigurationMigrator.VersionCurrent, Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};

			CssGeneratorTests.PopulateFieldsForTesting(alphaModel);
			CssGeneratorTests.PopulateFieldsForTesting(defaultModel);

			// SUT
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, defaultModel);

			var migratedNoteDictionaryOptionsNode = alphaModel.Parts[0].Children[0].Children[0].Children[0];
			Assert.AreEqual("HeadWordRef", migratedNoteDictionaryOptionsNode.FieldDescription, "FieldDescription for Referenced Sense Headword should be HeadwordRef");
			Assert.AreEqual(1, migratedNoteDictionaryOptionsNode.Parent.Children.Count, "no extra nodes should have been added");
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
				FieldDescription = LexEntry,
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
			Assert.That(configNode.Between, Is.EqualTo(", "));
			Assert.That(configNode.After, Is.Null.Or.Empty);
			Assert.That(configNode.Before, Is.Null.Or.Empty);
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
			Assert.That(langNotesNode.IsEnabled, Is.True, "LanguageNotes node should be enabled by default");
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
			Assert.That(configNode.IsEnabled, Is.False, "Bibliography node should not be enabled");
			TestForWritingSystemOptionsType(configNode, DictionaryNodeWritingSystemOptions.WritingSystemType.Analysis);
		}

		[Test]
		public void MigrateFromConfig83AlphaToBeta10_UpdatesCustomFieldForEtymologyCluster()
		{
			var name = new ConfigurableDictionaryNode
			{
				Label = "Name",
				FieldDescription = "Name",
				IsCustomField = true
			};
			var sourceNode = new ConfigurableDictionaryNode
			{
				Label = "Source Form",
				FieldDescription = "Form",
				IsCustomField = true,
				Children = new List<ConfigurableDictionaryNode> { name }
			};
			var etymologyNode = new ConfigurableDictionaryNode
			{
				Label = "Etymology",
				FieldDescription = "EtymologyOA",
				CSSClassNameOverride = "etymology",
				Children = new List<ConfigurableDictionaryNode> { sourceNode }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode> { etymologyNode }
			};
			var alphaModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			var rootModel = m_migrator.LoadBetaDefaultForAlphaConfig(alphaModel);
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, rootModel);
			var etymChildren = etymologyNode.Children;
			var configNode = etymChildren.Find(node => node.Label == "Source Form");
			Assert.That(configNode.IsCustomField, Is.False, "Language node should not be custom field");
			Assert.That(configNode.Children[0].IsCustomField, Is.False, "Name of Language node should not be custom field");
		}

		[Test]
		public void MigrateFromConfig83AlphaToBeta10_PathologicalEtymologyCaseDoesNotThrow()
		{
			// Custom field etymology caused crash
			var customEtymology = new ConfigurableDictionaryNode
			{
				Label = "Etymology",
				FieldDescription = "Etymology (Custom)",
				Children = new List<ConfigurableDictionaryNode> {  new ConfigurableDictionaryNode { Label = "unimportant"} }
			};
			var variantNode = new ConfigurableDictionaryNode
			{
				Label = "Variant Form",
				FieldDescription = "VariantEntryBackRefs",
				Children = new List<ConfigurableDictionaryNode> { customEtymology }
			};
			// Weird old etymology node without children (caused crash)
			var etymologyNode = new ConfigurableDictionaryNode
			{
				Label = "Etymology",
				FieldDescription = "EtymologyOA"
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = LexEntry,
				Children = new List<ConfigurableDictionaryNode> { etymologyNode, variantNode }
			};
			var alphaModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3,
				Parts = new List<ConfigurableDictionaryNode> { mainEntryNode }
			};
			var rootModel = m_migrator.LoadBetaDefaultForAlphaConfig(alphaModel);
			Assert.DoesNotThrow(() => m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, rootModel));
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
			var betaModel = m_migrator.LoadBetaDefaultForAlphaConfig(alphaModel);
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, betaModel);
			Assert.AreEqual("EtymologyOS", etymologyNode.SubField, "Should have changed to a sequence.");
			Assert.AreEqual("Entry", etymologyNode.FieldDescription, "Should have changed 'Owner' field for reversal to 'Entry'");
			Assert.AreEqual("etymologies", etymologyNode.CSSClassNameOverride, "Should have changed CSS override");
			Assert.AreEqual(7, etymologyNode.Children.Count, "There should be 7 nodes after the conversion.");
			Assert.IsNull(etymologyNode.DictionaryNodeOptions, "Improper options added to etymology sequence node.");
		}

		/// <summary>Referenced Complex Forms that are siblings of Subentries should become Other Referenced Complex Forms</summary>
		[Test]
		public void MigrateFrom83Alpha_SelectsProperReferencedComplexForms()
		{
			var userModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3 + 1, // skip the adding of new grouping nodes; that's not the SUT
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						Label = "Main Entry", FieldDescription = LexEntry,
						Children = new List<ConfigurableDictionaryNode>
						{
							new ConfigurableDictionaryNode { Label = "Referenced Complex Forms", FieldDescription = ReferencedComplexForms },
							new ConfigurableDictionaryNode { FieldDescription = "Subentries" }
						}
					},
					new ConfigurableDictionaryNode
					{
						Label = "Minor Entry", FieldDescription = LexEntry,
						Children = new List<ConfigurableDictionaryNode>
						{
							new ConfigurableDictionaryNode { Label = "Referenced Complex Forms", FieldDescription = ReferencedComplexForms }
						}
					}
				}
			};
			var betaModel = new DictionaryConfigurationModel
			{
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { Label = "Main Entry", FieldDescription = LexEntry },
					new ConfigurableDictionaryNode { Label = "Minor Entry", FieldDescription = LexEntry }
				}
			};
			m_migrator.MigrateFrom83Alpha(m_logger, userModel, betaModel); // SUT
			var mainEntryChildren = userModel.Parts[0].Children;
			Assert.AreEqual(2, mainEntryChildren.Count, "no children should have been created or deleted");
			Assert.AreEqual(OtherRefdComplexForms, mainEntryChildren[0].FieldDescription, "should have changed");
			Assert.AreEqual("Other Referenced Complex Forms", mainEntryChildren[0].Label, "should have changed");
			Assert.AreEqual("Subentries", mainEntryChildren[1].FieldDescription, "should not have changed");
			var minorEntryChildren = userModel.Parts[1].Children;
			Assert.AreEqual(1, minorEntryChildren.Count, "no children should have been added or deleted");
			Assert.AreEqual(ReferencedComplexForms, minorEntryChildren[0].FieldDescription, "should not have changed");
			Assert.AreEqual("Referenced Complex Forms", minorEntryChildren[0].Label, "should not have changed");
		}

		/// <summary>Apart from Category Info, all children of Gram. Info under (Other) Referenced Complex Forms should be removed</summary>
		[Test]
		public void MigrateFrom83Alpha_RemovesGramInfoUnderRefdComplexForms()
		{
			var gramInfoChildren = new List<ConfigurableDictionaryNode> {
				new ConfigurableDictionaryNode { FieldDescription = "MLPartOfSpeech" },
				new ConfigurableDictionaryNode { FieldDescription = "Slots" },
				new ConfigurableDictionaryNode { FieldDescription = "MorphTypes" },
				new ConfigurableDictionaryNode { FieldDescription = "MLInflectionClass" },
				new ConfigurableDictionaryNode { FieldDescription = "FeaturesTSS" },
				new ConfigurableDictionaryNode { FieldDescription = "ExceptionFeaturesTSS" },
				new ConfigurableDictionaryNode { FieldDescription = "InterlinearNameTSS" },
				new ConfigurableDictionaryNode { FieldDescription = "InterlinearAbbrTSS" }
			};
			var originalKidCount = gramInfoChildren.Count;
			var userModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3 + 1, // skip the adding of new grouping nodes; that's not the SUT
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						Label = "Main Entry", FieldDescription = LexEntry,
						Children = new List<ConfigurableDictionaryNode>
						{
							new ConfigurableDictionaryNode
							{
								FieldDescription = ReferencedComplexForms,
								Children = new List<ConfigurableDictionaryNode>
								{
									new ConfigurableDictionaryNode
									{
										FieldDescription = "MorphoSyntaxAnalyses",
										Children = new List<ConfigurableDictionaryNode>(gramInfoChildren)
									}
								}
							},
							new ConfigurableDictionaryNode
							{
								FieldDescription = "SensesOS",
								Children = new List<ConfigurableDictionaryNode>
								{
									new ConfigurableDictionaryNode
									{
										FieldDescription = "MorphoSyntaxAnalysisRA",
										Children = new List<ConfigurableDictionaryNode>(gramInfoChildren)
									}
								}
							}
						}
					}
				}
			};
			// create a Beta model with the appropriate children removed from the appropriate node
			var betaModel = userModel.DeepClone();
			betaModel.Parts[0].Children[0].Children[0].Children.RemoveRange(1, originalKidCount - 1);

			m_migrator.MigrateFrom83Alpha(m_logger, userModel, betaModel); // SUT
			var remainingChildren = userModel.Parts[0].Children[0].Children[0].Children;
			Assert.AreEqual(1, remainingChildren.Count, "Only one child should remain under GramInfo under (O)RCF's");
			Assert.AreEqual("MLPartOfSpeech", remainingChildren[0].FieldDescription); // Label in production is Category Info.
			remainingChildren = userModel.Parts[0].Children[1].Children[0].Children;
			Assert.AreEqual(originalKidCount, remainingChildren.Count, "No children should have been removed from GramInfo under Senses");
		}

		[Test]
		public void MigrateFrom83Alpha_RemoveReferencedHeadwordSubField() // LT-18470
		{
			//Populate a reversal configuration based on the current defaults
			var reversalBetaModel = new DictionaryConfigurationModel { WritingSystem = "en"};
			var betaModel = m_migrator.LoadBetaDefaultForAlphaConfig(reversalBetaModel); // SUT
			Assert.IsTrue(betaModel.IsReversal);
			var alphaModel = betaModel.DeepClone();
			//Set the SubField on the ReversalName Node for our 'old' configuration
			alphaModel.SharedItems[0].Children[2].Children[0].SubField = "MLHeadWord";
			alphaModel.Version = 18;
			m_migrator.MigrateFrom83Alpha(m_logger, alphaModel, betaModel); // SUT
			Assert.AreNotEqual("MLHeadWord", betaModel.SharedItems[0].Children[2].Children[0].SubField);
			Assert.Null(betaModel.SharedItems[0].Children[2].Children[0].SubField);
		}

		[Test]
		public void MigrateFrom83Alpha_AddsOptionsToRefdComplexForms()
		{
			var userModel = new DictionaryConfigurationModel
			{
				Version = FirstAlphaMigrator.VersionAlpha3 + 1, // skip the adding of new grouping nodes; that's not the SUT
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						Label = "Main Entry", FieldDescription = LexEntry,
						Children = new List<ConfigurableDictionaryNode>
						{
							new ConfigurableDictionaryNode { FieldDescription = ReferencedComplexForms }
						}
					}
				}
			};
			// create a Beta model with Options set for the ReferencedComplexForms node
			var betaModel = userModel.DeepClone();
			betaModel.Parts[0].Children[0].DictionaryNodeOptions = new DictionaryNodeListOptions { ListId = DictionaryNodeListOptions.ListIds.Complex };

			m_migrator.MigrateFrom83Alpha(m_logger, userModel, betaModel); // SUT
			var migratedOptions = userModel.Parts[0].Children[0].DictionaryNodeOptions as DictionaryNodeListOptions;
			Assert.NotNull(migratedOptions, "Referenced Complex Forms should have gotten List Options");
			Assert.AreEqual(DictionaryNodeListOptions.ListIds.Complex, migratedOptions.ListId);
		}

		[Test]
		public void MigrateFrom83Alpha_UpdatesCssOverrideAndStyles()
		{
			var reversalStyle = "Reversal-Normal";
			var reversalCss = "reversalindexentry";
			var userModel = new DictionaryConfigurationModel
			{
				WritingSystem = "en", // reversal
				Version = FirstAlphaMigrator.VersionAlpha3 + 1, // skip the adding of new grouping nodes; that's not the SUT
				Parts = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						Label = "Reversal Entry", FieldDescription = "ReversalIndexEntry",
						Children = new List<ConfigurableDictionaryNode>
						{
							new ConfigurableDictionaryNode { FieldDescription = "ReversalForm" }
						}
					}
				}
			};
			// create a Beta model with Options set for the ReferencedComplexForms node
			var betaModel = userModel.DeepClone();
			var topNode = betaModel.Parts[0];
			topNode.CSSClassNameOverride = reversalCss;
			topNode.StyleType = ConfigurableDictionaryNode.StyleTypes.Paragraph;
			topNode.Style = reversalStyle;

			m_migrator.MigrateFrom83Alpha(m_logger, userModel, betaModel); // SUT
			var migratedReversalNode = userModel.Parts[0];
			Assert.AreEqual(reversalStyle, migratedReversalNode.Style, "Reversal node should have gotten a Style");
			Assert.AreEqual(reversalCss, migratedReversalNode.CSSClassNameOverride, "Reversal node should have gotten a CssClassNameOverride");
		}

		[Test]
		public void MigrateFrom83Alpha_DoesNotAddDirectChildrenToSharingParents() // LT-18286
		{
			var version12Model = new DictionaryConfigurationModel(false);
			var mainEntrySubentries = new ConfigurableDictionaryNode
			{
				Label = "Minor Subentries",
				FieldDescription = "Subentries",
				ReferenceItem = "MainEntrySubentries",
				Children = new List<ConfigurableDictionaryNode>(), // If this is null it skips the code we're testing
				DictionaryNodeOptions = new DictionaryNodeListOptions { ListId = DictionaryNodeListOptions.ListIds.Complex }
			};
			var mainEntryNode = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { mainEntrySubentries }
			};
			var minorEntrySubentries = new ConfigurableDictionaryNode
			{
				Label = "Minor Subentries",
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode
					{
						FieldDescription = "Headword",
						Children = new List<ConfigurableDictionaryNode>
						{
							new ConfigurableDictionaryNode
							{
								Label = "Subsubentries", FieldDescription = "Subentries",
								DictionaryNodeOptions = new DictionaryNodeListAndParaOptions {ListId = DictionaryNodeListOptions.ListIds.Complex},
								ReferenceItem = "MainEntrySubentries"
							}
						}
					}
				}
			};
			var minorEntryComplex = new ConfigurableDictionaryNode
			{
				Label = "Main Entry (Complex Forms)",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { minorEntrySubentries },
				DictionaryNodeOptions = new DictionaryNodeListOptions { ListId = DictionaryNodeListOptions.ListIds.Complex}
			};
			var sharedSubentries = new ConfigurableDictionaryNode
			{
				Label = "MainEntrySubentries",
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "Headword", DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()},
					new ConfigurableDictionaryNode { Label = "Minor Subsubentries", FieldDescription = "Subentries", ReferenceItem = "MainEntrySubentries"},
					new ConfigurableDictionaryNode { Label = "Subsubentries", FieldDescription = "Subentries", ReferenceItem = "MainEntrySubentries"}
				}
			};
			version12Model.Parts = new List<ConfigurableDictionaryNode> { mainEntryNode, minorEntryComplex };
			version12Model.SharedItems = new List<ConfigurableDictionaryNode> { sharedSubentries };
			version12Model.Version = 12;
			CssGeneratorTests.PopulateFieldsForTesting(version12Model);

			var version16Model = new DictionaryConfigurationModel(false);
			var mainEntrySubentries16 = new ConfigurableDictionaryNode
			{
				Label = "Minor Subentries",
				FieldDescription = "Subentries",
				ReferenceItem = "MainEntrySubentries"
			};
			var mainEntryNode16 = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { mainEntrySubentries16 }
			};
			var sharedSubentries16 = new ConfigurableDictionaryNode
			{
				Label = "MainEntrySubentries",
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { Label = "Headword", DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()},
					new ConfigurableDictionaryNode { Label = "Minor Subsubentries", FieldDescription = "Subentries", ReferenceItem = "MainEntrySubentries"}
				}
			};
			version16Model.Parts = new List<ConfigurableDictionaryNode> { mainEntryNode16 };
			version16Model.SharedItems = new List<ConfigurableDictionaryNode> { sharedSubentries16 };
			version16Model.Version = 16;
			m_migrator.MigrateFrom83Alpha(m_logger, version12Model, version16Model); // SUT
			VerifyChildrenAndReferenceItem(version12Model);
		}

		[Test]
		public void MigrateFrom83Alpha_RemovesErroneouslyAddedChildren() // LT-18286
		{
			var version16Model = new DictionaryConfigurationModel(false);
			var mainEntrySubentries16 = new ConfigurableDictionaryNode
			{
				Label = "Minor Subentries",
				FieldDescription = "Subentries",
				ReferenceItem = "MainEntrySubentries"
			};
			var mainEntryNode16 = new ConfigurableDictionaryNode
			{
				Label = "Main Entry",
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { mainEntrySubentries16 }
			};
			var sharedSubentries16 = new ConfigurableDictionaryNode
			{
				Label = "MainEntrySubentries",
				FieldDescription = "Subentries",
				Children = new List<ConfigurableDictionaryNode>
				{
					new ConfigurableDictionaryNode { FieldDescription = "Headword", DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions()},
					new ConfigurableDictionaryNode { Label = "Minor Subsubentries", FieldDescription = "Subentries", ReferenceItem = "MainEntrySubentries"}
				}
			};
			version16Model.Parts = new List<ConfigurableDictionaryNode> { mainEntryNode16 };
			version16Model.SharedItems = new List<ConfigurableDictionaryNode> { sharedSubentries16 };
			version16Model.Version = 16;
			CssGeneratorTests.PopulateFieldsForTesting(version16Model);

			var version17Model = version16Model.DeepClone();
			version17Model.Version = 17;

			// Create Problem:
			mainEntrySubentries16.Children = new List<ConfigurableDictionaryNode>(sharedSubentries16.Children);

			m_migrator.MigrateFrom83Alpha(m_logger, version16Model, version17Model); // SUT
			VerifyChildrenAndReferenceItem(version16Model);
		}

		/// <summary>Verify that no nodes have both Children and a ReferenceItem</summary>
		private static void VerifyChildrenAndReferenceItem(DictionaryConfigurationModel model)
		{
			DictionaryConfigurationMigrator.PerformActionOnNodes(model.PartsAndSharedItems, node =>
			{
				if (!string.IsNullOrEmpty(node.ReferenceItem))
				{
					Assert.IsTrue(node.Children == null || !node.Children.Any(),
						"Reference Item and children are exclusive:\n" + DictionaryConfigurationMigrator.BuildPathStringFromNode(node));
				}
			});
		}
	}
}

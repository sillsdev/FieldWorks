// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using LanguageExplorer;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorer.DictionaryConfiguration.Migration;
using NUnit.Framework;
using SIL.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorerTests.DictionaryConfiguration.Migration
{
	public class DictionaryConfigurationMigratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FlexComponentParameters _flexComponentParameters;

		#region Overrides of LcmTestBase

		public override void FixtureTeardown()
		{
			try
			{
				RobustIO.DeleteDirectoryAndContents(Cache.ProjectId.Path);
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

		public override void TestSetup()
		{
			base.TestSetup();

			ISharedEventHandlers dummy;
			_flexComponentParameters = TestSetupServices.SetupEverything(Cache, out dummy);

			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, FwUtils.ksFlexAppName, Cache.ProjectId.Path);
		}

		public override void TestTearDown()
		{
			try
			{
				RobustIO.DeleteDirectoryAndContents(Cache.ProjectId.ProjectFolder);
				_flexComponentParameters.PropertyTable.Dispose();
				_flexComponentParameters = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
		}

		#endregion

		[Test]
		public void MigrateOldConfigurationsIfNeeded_BringsPreHistoricFileToCurrentVersion()
		{
			var configSettingsDir = LcmFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path));
			var newConfigFilePath = Path.Combine(configSettingsDir, DictionaryConfigurationServices.DictionaryConfigurationDirectoryName, "Lexeme" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
			Assert.False(File.Exists(newConfigFilePath), "should not yet be migrated");
			Directory.CreateDirectory(configSettingsDir);
			File.WriteAllLines(Path.Combine(configSettingsDir, "Test.fwlayout"), new[]{
				@"<layoutType label='Lexeme-based (complex forms as main entries)' layout='publishStem'><configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />",
				@"<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' /></layoutType>'"});

			DictionaryConfigurationServices.MigrateOldConfigurationsIfNeeded(Cache, _flexComponentParameters.PropertyTable); // SUT
			var updatedConfigModel = new DictionaryConfigurationModel(newConfigFilePath, Cache);
			Assert.AreEqual(DictionaryConfigurationServices.VersionCurrent, updatedConfigModel.Version);
			RobustIO.DeleteDirectoryAndContents(configSettingsDir);
		}

#if RANDYTODO
		// TODO: This one fails for some reason.
		[Test]
		public void MigrateOldConfigurationsIfNeeded_MatchesLabelsWhenUIIsLocalized()
		{
			// Localize a Part's label to German (sufficient to cause a mismatched nodes crash if one config's labels are localized)
			var localizedPartLabels = new Dictionary<string, string>();
			localizedPartLabels["Main Entry"] = "Haupteintrag";
			var pathsToL10NStrings = (Dictionary<string, Dictionary<string, string>>)ReflectionHelper.GetField(StringTable.Table, "m_pathsToStrings");
			pathsToL10NStrings["group[@id = 'LocalizedAttributes']/"] = localizedPartLabels;

			var configSettingsDir = LcmFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path));
			var newConfigFilePath = Path.Combine(configSettingsDir, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName, "Lexeme" + DictionaryConfigurationModel.FileExtension);
			Assert.False(File.Exists(newConfigFilePath), "should not yet be migrated");
			Directory.CreateDirectory(configSettingsDir);
			File.WriteAllLines(Path.Combine(configSettingsDir, "Test.fwlayout"), new[]{
				@"<layoutType label='Lexeme-based (complex forms as main entries)' layout='publishStem'><configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />",
				@"<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' /></layoutType>'"});
			var migrator = new DictionaryConfigurationMigrator();
			migrator.InitializeFlexComponent(_flexComponentParameters);
			Assert.DoesNotThrow(() => migrator.MigrateOldConfigurationsIfNeeded(), "ArgumentException indicates localized labels."); // SUT
			var updatedConfigModel = new DictionaryConfigurationModel(newConfigFilePath, Cache);
			Assert.AreEqual(2, updatedConfigModel.Parts.Count, "Should have 2 top-level nodes");
			Assert.AreEqual("Main Entry", updatedConfigModel.Parts[0].Label);
			RobustIO.DeleteDirectoryAndContents(configSettingsDir);
		}
#endif

		[Test]
		public void MigrateOldConfigurationsIfNeeded_PreservesOrderOfBibliographies()
		{
			var configSettingsDir = LcmFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path));
			var newConfigFilePath = Path.Combine(configSettingsDir, DictionaryConfigurationServices.ReversalIndexConfigurationDirectoryName, "AllReversalIndexes" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
			Assert.False(File.Exists(newConfigFilePath), "should not yet be migrated");
			Directory.CreateDirectory(configSettingsDir);
			File.WriteAllLines(Path.Combine(configSettingsDir, "Test.fwlayout"), new[]{
				@"<layoutType label='All Reversal Indexes' layout='publishReversal'>",
				@"<configure class='ReversalIndexEntry' label='Reversal Entry' layout='publishReversalEntry' /></layoutType>'"});
			DictionaryConfigurationServices.MigrateOldConfigurationsIfNeeded(Cache, _flexComponentParameters.PropertyTable); // SUT
			var updatedConfigModel = new DictionaryConfigurationModel(newConfigFilePath, Cache);
			var refdSenseChildren = updatedConfigModel.Parts[0].Children.Find(n => n.Label == "Referenced Senses").Children;
			var bibCount = 0;
			for (var i = 0; i < refdSenseChildren.Count; i++)
			{
				var bibNode = refdSenseChildren[i];
				if (!bibNode.Label.StartsWith("Bibliography"))
					continue;
				StringAssert.StartsWith("Bibliography (", bibNode.Label, "Should specify (entry|sense), lest we never know");
				Assert.False(bibNode.IsCustomField, bibNode.Label + " should not be custom.");
				// Rough test to ensure Bibliography nodes aren't bumped to the end of the list. In the defaults, the later Bibliography
				// node is a little more than five nodes from the end
				Assert.LessOrEqual(i, refdSenseChildren.Count - 5, "Bibliography nodes should not have been bumped to the end of the list");
				++bibCount;
			}
			Assert.AreEqual(2, bibCount, "Should be exactly two Bibliography nodes (sense and entry)");
			RobustIO.DeleteDirectoryAndContents(configSettingsDir);
		}

		[Test]
		public void BuildPathStringFromNode()
		{
			var subsenses = new ConfigurableDictionaryNode { Label = "Subsenses", FieldDescription = "SensesOS", ReferenceItem = "SharedSenses" };
			var sharedSenses = new ConfigurableDictionaryNode
			{
				Label = "SharedSenses", FieldDescription = "SensesOS", Children = new List<ConfigurableDictionaryNode> { subsenses }
			};
			var senses = new ConfigurableDictionaryNode { Label = "Senses", FieldDescription = "SensesOS", ReferenceItem = "SharedSenses" };
			var mainEntry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry", Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var model = DictionaryConfigurationModelTests.CreateSimpleSharingModel(mainEntry, sharedSenses);
			CssGeneratorTests.PopulateFieldsForTesting(model); // PopulateFieldsForTesting populates each node's Label with its FieldDescription

			Assert.AreEqual("LexEntry > Senses > SharedSenses > Subsenses", DictionaryConfigurationServices.BuildPathStringFromNode(subsenses));
			Assert.AreEqual("LexEntry > Senses > Subsenses", DictionaryConfigurationServices.BuildPathStringFromNode(subsenses, false));
			Assert.AreEqual("LexEntry", DictionaryConfigurationServices.BuildPathStringFromNode(mainEntry));
		}

		[Test]
		public void StoredDefaultsUpdatedFromCurrentDefaults()
		{
			var subsenses = new ConfigurableDictionaryNode { Label = "Subsenses", FieldDescription = "SensesOS" };
			var inBoth = new ConfigurableDictionaryNode
			{
				Label = "In Both",
				FieldDescription = "Both"
			};
			var inOld = new ConfigurableDictionaryNode
			{
				Label = "inOld",
				FieldDescription = "OnlyOld",
				Children = new List<ConfigurableDictionaryNode> { subsenses }
			};
			var senses = new ConfigurableDictionaryNode { Label = "Senses", FieldDescription = "SensesOS",Children = new List<ConfigurableDictionaryNode> { inOld, inBoth }};
			var mainEntry = new ConfigurableDictionaryNode
			{
				FieldDescription = "LexEntry",
				Children = new List<ConfigurableDictionaryNode> { senses }
			};
			var oldModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { mainEntry } };
			CssGeneratorTests.PopulateFieldsForTesting(oldModel); // PopulateFieldsForTesting populates each node's Label with its FieldDescription sets all isEnabled to true
			var newMain = mainEntry.DeepCloneUnderSameParent();
			newMain.Children[0].Before = "{";
			newMain.Children[0].Between = ",";
			newMain.Children[0].After = "}";
			newMain.Children[0].Style = "Stylish";
			newMain.Children[0].IsEnabled = false;
			newMain.Children[0].Children.RemoveAt(0); // Remove inOld
			var newModel = new DictionaryConfigurationModel { Parts = new List<ConfigurableDictionaryNode> { newMain } };

			// Verify valid starting point
			Assert.AreNotEqual("{", oldModel.Parts[0].Children[0].Before, "Invalid preconditions");
			Assert.AreNotEqual("}", oldModel.Parts[0].Children[0].After, "Invalid preconditions");
			Assert.AreNotEqual(",", oldModel.Parts[0].Children[0].Between, "Invalid preconditions");
			Assert.AreNotEqual("Stylish", oldModel.Parts[0].Children[0].Style, "Invalid preconditions");
			Assert.True(oldModel.Parts[0].Children[0].IsEnabled, "Invalid preconditions");

			PreHistoricMigrator.LoadConfigWithCurrentDefaults(oldModel, newModel); // SUT
			Assert.AreEqual(2, oldModel.Parts[0].Children[0].Children.Count, "Old non-matching part was not retained");
			Assert.AreEqual("{", oldModel.Parts[0].Children[0].Before, "Before not copied from new defaults");
			Assert.AreEqual("}", oldModel.Parts[0].Children[0].After, "After not copied from new defaults");
			Assert.AreEqual(",", oldModel.Parts[0].Children[0].Between, "Between not copied from new defaults");
			Assert.AreEqual("Stylish", oldModel.Parts[0].Children[0].Style, "Style not copied from new defaults");
			Assert.False(oldModel.Parts[0].Children[0].IsEnabled, "IsEnabled value not copied from new defaults");
		}
	}
}

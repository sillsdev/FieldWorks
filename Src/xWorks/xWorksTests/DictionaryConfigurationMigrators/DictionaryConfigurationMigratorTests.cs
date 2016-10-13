// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Palaso.IO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using XCore;
// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	public class DictionaryConfigurationMigratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private MockFwXApp m_application;
		private string m_configFilePath;
		private MockFwXWindow m_window;
		private Mediator m_mediator;

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			Cache.ProjectId.Path = Path.Combine(Path.GetTempPath(), Cache.ProjectId.Name, Cache.ProjectId.Name + @".junk");
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			m_window.Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_mediator.AddColleague(new StubContentControlProvider());
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
		}

		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			DirectoryUtilities.DeleteDirectoryRobust(Cache.ProjectId.Path);
			base.FixtureTeardown();
			m_application.Dispose();
			m_window.Dispose();
			m_mediator.Dispose();
			FwRegistrySettings.Release();
		}

		[Test]
		public void MigrateOldConfigurationsIfNeeded_BringsPreHistoricFileToCurrentVersion()
		{
			var configSettingsDir = FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path));
			var newConfigFilePath = Path.Combine(configSettingsDir, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName,
				"Lexeme" + DictionaryConfigurationModel.FileExtension);
			Assert.False(File.Exists(newConfigFilePath), "should not yet be migrated");
			Directory.CreateDirectory(configSettingsDir);
			File.WriteAllLines(Path.Combine(configSettingsDir, "Test.fwlayout"), new[]{
				@"<layoutType label='Lexeme-based (complex forms as main entries)' layout='publishStem'><configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />",
				@"<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' /></layoutType>'"});
			var migrator = new DictionaryConfigurationMigrator(m_mediator);
			migrator.MigrateOldConfigurationsIfNeeded(); // SUT
			var updatedConfigModel = new DictionaryConfigurationModel(newConfigFilePath, Cache);
			Assert.AreEqual(DictionaryConfigurationMigrator.VersionCurrent, updatedConfigModel.Version);
			DirectoryUtilities.DeleteDirectoryRobust(configSettingsDir);
		}

		[Test]
		public void MigrateOldConfigurationsIfNeeded_MatchesLabelsWhenUIIsLocalized()
		{
			// Localize a Part's label to German (sufficient to cause a mismatched nodes crash if one config's labels are localized)
			var localizedPartLabels = new Dictionary<string, string>();
			localizedPartLabels["Main Entry"] = "Haupteintrag";
			var pathsToL10nStrings = (Dictionary<string, Dictionary<string, string>>)ReflectionHelper.GetField(m_mediator.StringTbl, "m_pathsToStrings");
			pathsToL10nStrings["group[@id = 'LocalizedAttributes']/"] = localizedPartLabels;

			var configSettingsDir = FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path));
			var newConfigFilePath = Path.Combine(configSettingsDir, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName,
				"Lexeme" + DictionaryConfigurationModel.FileExtension);
			Assert.False(File.Exists(newConfigFilePath), "should not yet be migrated");
			Directory.CreateDirectory(configSettingsDir);
			File.WriteAllLines(Path.Combine(configSettingsDir, "Test.fwlayout"), new[]{
				@"<layoutType label='Lexeme-based (complex forms as main entries)' layout='publishStem'><configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />",
				@"<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' /></layoutType>'"});
			var migrator = new DictionaryConfigurationMigrator(m_mediator);
			Assert.DoesNotThrow(() => migrator.MigrateOldConfigurationsIfNeeded(), "ArgumentException indicates localized labels."); // SUT
			var updatedConfigModel = new DictionaryConfigurationModel(newConfigFilePath, Cache);
			Assert.AreEqual(3, updatedConfigModel.Parts.Count, "Should have 3 top-level nodes");
			Assert.AreEqual("Main Entry", updatedConfigModel.Parts[0].Label);
			DirectoryUtilities.DeleteDirectoryRobust(configSettingsDir);
		}

		[Test]
		public void MigrateOldConfigurationsIfNeeded_PreservesOrderOfBibliographies()
		{
			var configSettingsDir = FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(Cache.ProjectId.Path));
			var newConfigFilePath = Path.Combine(configSettingsDir, DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName,
				"AllReversalIndexes" + DictionaryConfigurationModel.FileExtension);
			Assert.False(File.Exists(newConfigFilePath), "should not yet be migrated");
			Directory.CreateDirectory(configSettingsDir);
			File.WriteAllLines(Path.Combine(configSettingsDir, "Test.fwlayout"), new[]{
				@"<layoutType label='All Reversal Indexes' layout='publishReversal'>",
				@"<configure class='ReversalIndexEntry' label='Reversal Entry' layout='publishReversalEntry' /></layoutType>'"});
			var migrator = new DictionaryConfigurationMigrator(m_mediator);
			migrator.MigrateOldConfigurationsIfNeeded(); // SUT
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
			DirectoryUtilities.DeleteDirectoryRobust(configSettingsDir);
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

			Assert.AreEqual("LexEntry > Senses > SharedSenses > Subsenses", DictionaryConfigurationMigrator.BuildPathStringFromNode(subsenses));
			Assert.AreEqual("LexEntry > Senses > Subsenses", DictionaryConfigurationMigrator.BuildPathStringFromNode(subsenses, false));
			Assert.AreEqual("LexEntry", DictionaryConfigurationMigrator.BuildPathStringFromNode(mainEntry));
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

			DictionaryConfigurationMigrator.LoadConfigWithCurrentDefaults(oldModel, newModel); // SUT
			Assert.AreEqual(2, oldModel.Parts[0].Children[0].Children.Count, "Old non-matching part was not retained");
			Assert.AreEqual("{", oldModel.Parts[0].Children[0].Before, "Before not copied from new defaults");
			Assert.AreEqual("}", oldModel.Parts[0].Children[0].After, "After not copied from new defaults");
			Assert.AreEqual(",", oldModel.Parts[0].Children[0].Between, "Between not copied from new defaults");
			Assert.AreEqual("Stylish", oldModel.Parts[0].Children[0].Style, "Style not copied from new defaults");
			Assert.False(oldModel.Parts[0].Children[0].IsEnabled, "IsEnabled value not copied from new defaults");
		}
	}
}

// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
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
				"Stem" + DictionaryConfigurationModel.FileExtension);
			Assert.False(File.Exists(newConfigFilePath), "should not yet be migrated");
			Directory.CreateDirectory(configSettingsDir);
			File.WriteAllLines(Path.Combine(configSettingsDir, "Test.fwlayout"), new[]{
				@"<layoutType label='Stem-based (complex forms as main entries)' layout='publishStem'><configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />",
				@"<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' /></layoutType>'"});
			var migrator = new DictionaryConfigurationMigrator(m_mediator);
			using (migrator.SetTestLogger = new SimpleLogger())
			{
				migrator.MigrateOldConfigurationsIfNeeded();
			}
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
				"Stem" + DictionaryConfigurationModel.FileExtension);
			Assert.False(File.Exists(newConfigFilePath), "should not yet be migrated");
			Directory.CreateDirectory(configSettingsDir);
			File.WriteAllLines(Path.Combine(configSettingsDir, "Test.fwlayout"), new[]{
				@"<layoutType label='Stem-based (complex forms as main entries)' layout='publishStem'><configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />",
				@"<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' /></layoutType>'"});
			var migrator = new DictionaryConfigurationMigrator(m_mediator);
			using (migrator.SetTestLogger = new SimpleLogger())
			{
				Assert.DoesNotThrow(() => migrator.MigrateOldConfigurationsIfNeeded(), "ArgumentException indicates localized labels."); // SUT
			}
			var updatedConfigModel = new DictionaryConfigurationModel(newConfigFilePath, Cache);
			Assert.AreEqual(3, updatedConfigModel.Parts.Count, "Should have 3 top-level nodes");
			Assert.AreEqual("Main Entry", updatedConfigModel.Parts[0].Label);
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

		private class StubContentControlProvider : IxCoreColleague
		{
			private const string m_contentControlForTest =
				@"<control>
					<parameters PaneBarGroupId='PaneBar-Dictionary'>
						<!-- The following configureLayouts node is only required to help migrate old configurations to the new format -->
						<configureLayouts>
							<layoutType label='Stem-based (complex forms as main entries)' layout='publishStem'>
								<configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />
								<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' />
							</layoutType>
							<layoutType label='Root-based (complex forms as subentries)' layout='publishRoot'>
								<configure class='LexEntry' label='Main Entry' layout='publishRootEntry' />
								<configure class='LexEntry' label='Minor Entry' layout='publishRootMinorEntry' hideConfig='true' />
							</layoutType>
						</configureLayouts>
					</parameters>
				</control>";

			private readonly XmlNode m_testControlNode;

			private const string m_contentControlBlank =
				@"<control><parameters><configureLayouts>
					<layoutType label='$wsName' layout='bogus-$ws'>
						<configure class='LexEntry' label='Main Entry' layout='publishRootEntry' />
					</layoutType>
				</configureLayouts></parameters></control>";
			private readonly XmlNode m_testControlBlankNode;

			public StubContentControlProvider()
			{
				var doc = new XmlDocument();
				doc.LoadXml(m_contentControlForTest);
				m_testControlNode = doc.DocumentElement;
				var blankDoc = new XmlDocument();
				blankDoc.LoadXml(m_contentControlBlank);
				m_testControlBlankNode = blankDoc.DocumentElement;
			}

			public void Init(Mediator mediator, XmlNode configurationParameters)
			{
			}

			public IxCoreColleague[] GetMessageTargets()
			{
				return new IxCoreColleague[] { this };
			}

			/// <summary>
			/// This is called by reflection through the mediator. We need so that we can migrate through the PreHistoricMigrator.
			/// </summary>
			// ReSharper disable once UnusedMember.Local
			private bool OnGetContentControlParameters(object parameterObj)
			{
				var param = parameterObj as Tuple<string, string, XmlNode[]>;
				if (param == null)
					return false;
				XmlNode[] result = param.Item3;
				result[0] = param.Item2 == "lexiconDictionary" ? m_testControlNode : m_testControlBlankNode;
				return true;
			}

			public bool ShouldNotCall { get { return false; } }
			public int Priority { get { return 1; }}
		}
	}
}

using System;
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

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	class DictionaryConfigurationMigratorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
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
			Directory.CreateDirectory(configSettingsDir);
			File.WriteAllLines(Path.Combine(configSettingsDir, "Test.fwlayout"), new[]{
				@"<layoutType label='Stem-based (complex forms as main entries)' layout='publishStem'><configure class='LexEntry' label='Main Entry' layout='publishStemEntry' />",
				@"<configure class='LexEntry' label='Minor Entry' layout='publishStemMinorEntry' hideConfig='true' /></layoutType>'"});
			var migrator = new DictionaryConfigurationMigrator(m_mediator);
			using (migrator.SetTestLogger = new SimpleLogger())
			{
				migrator.MigrateOldConfigurationsIfNeeded();
			}
			var updatedConfigModel = new DictionaryConfigurationModel(Path.Combine(configSettingsDir, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName,
				"Stem" + DictionaryConfigurationModel.FileExtension), Cache);
			Assert.AreEqual(updatedConfigModel.Version, DictionaryConfigurationMigrator.VersionCurrent);
		}

		private class StubContentControlProvider : IxCoreColleague
		{
			private string m_contentControlForTest =
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
			private XmlNode m_testControlNode;

			private string m_contentControlBlank =
				@"<control><parameters><configureLayouts>
					<layoutType label='$wsName' layout='bogus-$ws'>
						<configure class='LexEntry' label='Main Entry' layout='publishRootEntry' />
					</layoutType>
				</configureLayouts></parameters></control>";
			private XmlNode m_testControlBlankNode;

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

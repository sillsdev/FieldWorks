// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using LanguageExplorer;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lexicon;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorerTests.DictionaryConfiguration
{
#if RANDYTODO // Some of this can be salvaged, but not the part where it loads the main xml config files.
	[TestFixture]
	public class XhtmlDocViewTests : XWorksAppTestBase
	{
		private FlexComponentParameters _flexComponentParameters;
		private XElement _parametersElement;
		private IRecordList _recordList;
		private StatusBar _statusBar;

	#region Overrides of LcmTestBase

		public override void TestSetup()
		{
			base.TestSetup();

			_flexComponentParameters = TestSetupServices.SetupEverything(Cache);
			_statusBar = new StatusBar();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var revIdx = Cache.ServiceLocator.GetInstance<IReversalIndexFactory>().Create();
				Cache.LanguageProject.LexDbOA.ReversalIndexesOC.Add(revIdx);
				revIdx.WritingSystem = "en";
				_flexComponentParameters.PropertyTable.SetProperty("ReversalIndexGuid", revIdx.Guid.ToString());
			});
			_recordList = LexiconArea.AllReversalEntriesFactoryMethod(Cache, _flexComponentParameters, LexiconArea.AllReversalEntries, _statusBar);
		}

		public override void TestTearDown()
		{
			_recordList.Dispose();
			_flexComponentParameters.PropertyTable.Dispose();
			_statusBar.Dispose();
			_flexComponentParameters = null;
			_statusBar = null;
			_recordList = null;

			base.TestTearDown();
		}

		protected override void FixtureInit()
		{
			// Init() is called from XWorksAppTestBase's TestFixtureSetup, so we won't call it here.
			var testProjPath = Path.Combine(Path.GetTempPath(), "XhtmlDocViewtestProj");
			if (Directory.Exists(testProjPath))
			{
				RobustIO.DeleteDirectoryAndContents(testProjPath);
			}
			Directory.CreateDirectory(testProjPath);
			Cache.ProjectId.Path = testProjPath;
			_parametersElement = XDocument.Parse(LexiconResources.ReversalEditCompleteToolParameters).Root.Element("docview").Element("parameters");
		}

		public override void FixtureTeardown()
		{
			var testProjPath = Path.Combine(Path.GetTempPath(), "XhtmlDocViewtestProj");
			if (Directory.Exists(testProjPath))
			{
				RobustIO.DeleteDirectoryAndContents(testProjPath);
			}
			base.FixtureTeardown();
		}

	#endregion

		private const string ConfigurationTemplate = "<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='AConfigPubtest'>" +
		"<Publications></Publications></DictionaryConfiguration>";
		private const string ConfigurationTemplateWithAllPublications = "<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='AConfigPubtest' allPublications='true'>" +
		"<Publications></Publications></DictionaryConfiguration>";

		[Test]
		public void SplitPublicationsByConfiguration_AllPublicationIsIn()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var allPubsConfig = ConfigurationTemplateWithAllPublications;
				using(var filePrintMenu = new ToolStripMenuItem())
				using(var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(), "AllPubsConf"+ReversalIndexServices.ConfigFileExtension)))
				{
					docView.InitializeFlexComponent(_flexComponentParameters);
					docView.SetConfigObjectName("Dictionary");
					File.WriteAllText(tempConfigFile.Path, allPubsConfig);
					List<string> pubsInConfig;
					List<string> pubsNotInConfig;
					// SUT
					docView.SplitPublicationsByConfiguration(
						Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
						tempConfigFile.Path, out pubsInConfig, out pubsNotInConfig);
					CollectionAssert.Contains(pubsInConfig, testPubName.Text);
					CollectionAssert.DoesNotContain(pubsNotInConfig, testPubName.Text);
				}
			}
		}

		[Test]
		public void SplitPublicationsByConfiguration_UnmatchedPublicationIsOut()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var notTestPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				var notTestPubName = TsStringUtils.MakeString("NotTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(notTestPubItem);
				notTestPubItem.Name.set_String(enId, notTestPubName);
				var configWithoutTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTestPub</Publication></Publications>");
				using (var filePrintMenu = new ToolStripMenuItem())
				using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				using (var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(), "AllPubsConf"+ReversalIndexServices.ConfigFileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.InitializeFlexComponent(_flexComponentParameters);
					File.WriteAllText(tempConfigFile.Path, configWithoutTestPub);
					List<string> pubsInConfig;
					List<string> pubsNotInConfig;
					// SUT
					docView.SplitPublicationsByConfiguration(
						Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
						tempConfigFile.Path, out pubsInConfig, out pubsNotInConfig);
					CollectionAssert.DoesNotContain(pubsInConfig, testPubName.Text);
					CollectionAssert.Contains(pubsNotInConfig, testPubName.Text);
				}
			}
		}

		[Test]
		public void SplitPublicationsByConfiguration_MatchedPublicationIsIn()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using (var filePrintMenu = new ToolStripMenuItem())
				using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(), "Foo"+ReversalIndexServices.ConfigFileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.InitializeFlexComponent(_flexComponentParameters);
					File.WriteAllText(tempConfigFile.Path, configWithTestPub);
					List<string> inConfig;
					List<string> outConfig;
					// SUT
					docView.SplitPublicationsByConfiguration(
						Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
						tempConfigFile.Path, out inConfig, out outConfig);
					CollectionAssert.Contains(inConfig, testPubName.Text);
					CollectionAssert.DoesNotContain(outConfig, testPubName.Text);
				}
			}
		}

		[Test]
		public void SplitConfigurationsByPublication_ConfigWithAllPublicationIsIn()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var allPubsConfig = ConfigurationTemplateWithAllPublications;
				using (var filePrintMenu = new ToolStripMenuItem())
				using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				using (var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(), "AllPubsConf"+ReversalIndexServices.ConfigFileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.InitializeFlexComponent(_flexComponentParameters);
					File.WriteAllText(tempConfigFile.Path, allPubsConfig);
					IDictionary<string, string> configsWithPub;
					IDictionary<string, string> configsWithoutPub;
					var configurations = new Dictionary<string, string>();
					configurations["AConfigPubtest"] = tempConfigFile.Path;
					// SUT
					docView.SplitConfigurationsByPublication(configurations,
																		  "TestPub", out configsWithPub, out configsWithoutPub);
					CollectionAssert.Contains(configsWithPub.Values, tempConfigFile.Path);
					CollectionAssert.DoesNotContain(configsWithoutPub.Values, tempConfigFile.Path);
				}
			}
		}

		[Test]
		public void SplitConfigurationsByPublication_AllPublicationIsMatchedByEveryConfiguration()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				using (var filePrintMenu = new ToolStripMenuItem())
				using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				using (var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(), "NoPubsConf"+ReversalIndexServices.ConfigFileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.InitializeFlexComponent(_flexComponentParameters);
					File.WriteAllText(tempConfigFile.Path, ConfigurationTemplate);
					IDictionary<string, string> configsWithPub;
					IDictionary<string, string> configsWithoutPub;
					var configurations = new Dictionary<string, string>();
					configurations["AConfigPubtest"] = tempConfigFile.Path;
					// SUT
					docView.SplitConfigurationsByPublication(configurations,
																		  xWorksStrings.AllEntriesPublication, out configsWithPub, out configsWithoutPub);
					CollectionAssert.Contains(configsWithPub.Values, tempConfigFile.Path);
					CollectionAssert.IsEmpty(configsWithoutPub.Values, tempConfigFile.Path);
				}
			}
		}

		[Test]
		public void SplitConfigurationsByPublication_UnmatchedPublicationIsOut()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var notTestPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				var notTestPubName = TsStringUtils.MakeString("NotTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(notTestPubItem);
				notTestPubItem.Name.set_String(enId, notTestPubName);
				var configWithoutTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTestPub</Publication></Publications>");
				using (var filePrintMenu = new ToolStripMenuItem())
				using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(), "Unremarkable"+ReversalIndexServices.ConfigFileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.InitializeFlexComponent(_flexComponentParameters);
					File.WriteAllText(tempConfigFile.Path, configWithoutTestPub);
					IDictionary<string, string> configsWithPub;
					IDictionary<string, string> configsWithoutPub;
					var configurations = new Dictionary<string, string>();
					configurations["AConfigPubtest"] = tempConfigFile.Path;
					// SUT
					docView.SplitConfigurationsByPublication(configurations,
																		  "TestPub", out configsWithPub, out configsWithoutPub);
					CollectionAssert.DoesNotContain(configsWithPub.Values, tempConfigFile.Path);
					CollectionAssert.Contains(configsWithoutPub.Values, tempConfigFile.Path);
				}
			}
		}

		[Test]
		public void SplitConfigurationsByPublication_MatchedPublicationIsIn()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using (var filePrintMenu = new ToolStripMenuItem())
				using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				using (var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(), "baz"+ReversalIndexServices.ConfigFileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.InitializeFlexComponent(_flexComponentParameters);
					File.WriteAllText(tempConfigFile.Path, configWithTestPub);
					IDictionary<string, string> configsWithPub;
					IDictionary<string, string> configsWithoutPub;
					var configurations = new Dictionary<string, string>();
					configurations["AConfigPubtest"] = tempConfigFile.Path;
					// SUT
					docView.SplitConfigurationsByPublication(configurations,
																		  "TestPub", out configsWithPub, out configsWithoutPub);
					CollectionAssert.Contains(configsWithPub.Values, tempConfigFile.Path);
					CollectionAssert.DoesNotContain(configsWithoutPub.Values, tempConfigFile.Path);
				}
			}
		}

		[Test]
		public void GetValidConfigurationForPublication_ReturnsAlreadySelectedConfigIfValid()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				// Change the project path to temp for this test
				Cache.ProjectId.Path = Path.GetTempPath();
				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using (var filePrintMenu = new ToolStripMenuItem())
				using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				{
					var configPathForTest = Path.Combine(Path.GetTempPath(), "ConfigurationSettings", "Dictionary");
					try
					{
						Directory.CreateDirectory(configPathForTest);
						using(var tempConfigFile = TempFile.WithFilename(Path.Combine(configPathForTest, "Squirrel"+ReversalIndexServices.ConfigFileExtension)))
						{
							docView.SetConfigObjectName("Dictionary");
							docView.InitializeFlexComponent(_flexComponentParameters);
							_flexComponentParameters.PropertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
							_flexComponentParameters.PropertyTable.SetProperty("DictionaryPublicationLayout", tempConfigFile.Path);
							File.WriteAllText(tempConfigFile.Path, configWithTestPub);
							// SUT
							Assert.That(docView.GetValidConfigurationForPublication("TestPub"), Is.StringContaining(tempConfigFile.Path));
						}
					}
					finally
					{
						RobustIO.DeleteDirectoryAndContents(configPathForTest);
					}
				}
			}
		}

		[Test]
		public void GetValidConfigurationForPublication_AllEntriesReturnsAlreadySelectedConfig()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);

				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				var subDir = Path.Combine(Path.GetTempPath(), "Dictionary");
				Directory.CreateDirectory(subDir); // required by DictionaryConfigurationListener.GetCurrentConfiguration()
				try
				{
					using (var filePrintMenu = new ToolStripMenuItem())
					using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
					using (var tempConfigFile = TempFile.WithFilename(Path.Combine(subDir, "baz"+ReversalIndexServices.ConfigFileExtension)))
					{
						docView.SetConfigObjectName("Dictionary");
						docView.InitializeFlexComponent(_flexComponentParameters);
						_flexComponentParameters.PropertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
						_flexComponentParameters.PropertyTable.SetProperty("DictionaryPublicationLayout", tempConfigFile.Path);
						// DictionaryConfigurationListener.GetCurrentConfiguration() needs to know the currentContentControl.
						File.WriteAllText(tempConfigFile.Path, configWithTestPub);
						// SUT
						Assert.That(docView.GetValidConfigurationForPublication(xWorksStrings.AllEntriesPublication),
										Is.StringContaining(tempConfigFile.Path));
					}
				}
				finally
				{
					RobustIO.DeleteDirectoryAndContents(subDir);
				}
			}
		}

		[Test]
		public void GetValidConfigurationForPublication_PublicationThatMatchesNoConfigReturnsNull()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("NotTheTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var configSansTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTheTestPub</Publication></Publications>");
				var overrideFiles = new List<TempFile>();
				using (var filePrintMenu = new ToolStripMenuItem())
				using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.InitializeFlexComponent(_flexComponentParameters);
					var projConfigs = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
					try
					{
						Directory.CreateDirectory(projConfigs);
						// override every shipped config with a config that does not have the TestPub publication
						var shippedFileList = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary"), "*" + ReversalIndexServices.ConfigFileExtension);
						var overrideCount = 0;
						foreach(var shippedFile in shippedFileList)
						{
							++overrideCount;
							var tempFileName = Path.Combine(projConfigs, overrideCount + ReversalIndexServices.ConfigFileExtension);
							var tempConfigFile = TempFile.WithFilename(tempFileName);
							overrideFiles.Add(tempConfigFile);
							using(var stream = new FileStream(shippedFile, FileMode.Open))
							{
								var doc = new XmlDocument();
								doc.Load(stream);
								var node = doc.SelectSingleNode("DictionaryConfiguration");
								var shippedName = node.Attributes["name"].Value;
								File.WriteAllText(tempConfigFile.Path,
														configSansTestPub.Replace("name='AConfigPubtest'", "name='"+shippedName+"'"));
							}
						}
						// SUT
						var result = docView.GetValidConfigurationForPublication("TestPub");
						// Delete all our temp files before asserting so they are sure to go away
						foreach(var tempFile in overrideFiles)
						{
							tempFile.Dispose();
						}
						Assert.IsNull(result, "When no configurations have the publication null should be returned.");
					}
					finally
					{
						RobustIO.DeleteDirectoryAndContents(projConfigs);
					}
				}
			}
		}

		[Test]
		public void GetValidConfigurationForPublication_ConfigurationContainingPubIsPicked()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = TsStringUtils.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var notTestPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				var notTestPubName = TsStringUtils.MakeString("NotTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(notTestPubItem);
				notTestPubItem.Name.set_String(enId, notTestPubName);
				var nonMatchingConfig = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTestPub</Publication></Publications>");
				//Change the name for the matching config so that our two user configs don't conflict with each other
				var matchingConfig = ConfigurationTemplate.Replace("</Publications>",
																					"<Publication>TestPub</Publication></Publications>").Replace("AConfigPub", "AAConfigPub");
				var dictionaryConfigPath = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
				using (var filePrintMenu = new ToolStripMenuItem())
				using (var docView = new TestXhtmlDocView(_parametersElement, Cache, _recordList, filePrintMenu))
				using (var nonMatchedConfigFile = TempFile.WithFilename(Path.Combine(dictionaryConfigPath, "NoMatch"+ReversalIndexServices.ConfigFileExtension)))
				using(var matchedConfigFile = TempFile.WithFilename(Path.Combine(dictionaryConfigPath, "Match"+ReversalIndexServices.ConfigFileExtension)))
				{
					File.WriteAllText(nonMatchedConfigFile.Path, nonMatchingConfig);
					File.WriteAllText(matchedConfigFile.Path, matchingConfig);
					docView.SetConfigObjectName("Dictionary");
					docView.InitializeFlexComponent(_flexComponentParameters);
					_flexComponentParameters.PropertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
					_flexComponentParameters.PropertyTable.SetProperty("DictionaryPublicationLayout", nonMatchedConfigFile.Path);
					// SUT
					var validConfig = docView.GetValidConfigurationForPublication("TestPub");
					Assert.That(validConfig, Is.Not.StringContaining(nonMatchedConfigFile.Path));
					Assert.That(validConfig, Is.StringContaining(matchedConfigFile.Path));
				}
			}
		}

		private class TestXhtmlDocView : XhtmlDocView
		{
			internal TestXhtmlDocView(XElement configurationParametersElement, LcmCache cache, IRecordList recordList, ToolStripMenuItem printMenu)
				: base(configurationParametersElement, cache, recordList, printMenu)
			{
			}

			internal void SetConfigObjectName(string name)
			{
				m_configObjectName = name;
			}
		}
	}
#endif
}

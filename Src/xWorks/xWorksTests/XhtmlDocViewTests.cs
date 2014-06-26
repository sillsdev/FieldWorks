// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Palaso.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class XhtmlDocViewTests : XWorksAppTestBase, IDisposable
	{
		private Mediator m_mediator;
		[TestFixtureSetUp]
		public new void FixtureSetup()
		{
			Init();
			var testProjPath = Path.Combine(Path.GetTempPath(), "XhtmlDocViewtestProj");
			if(Directory.Exists(testProjPath))
				Directory.Delete(testProjPath, true);
			Directory.CreateDirectory(testProjPath);
			Cache.ProjectId.Path = testProjPath;
		}

		[Test]
		public void GatherBuiltInAndUserConfigurations_ReturnsShippedConfigurations()
		{
			using(var docView = new TestXmlDocView())
			{
				docView.SetConfigObjectName("Dictionary");
				docView.SetMediator(m_mediator);
				// SUT
				var fileListFromResults = docView.GatherBuiltInAndUserConfigurations().Values;
				var shippedFileList = Directory.EnumerateFiles(Path.Combine(DirectoryFinder.DefaultConfigurations, "Dictionary"));
				CollectionAssert.AreEquivalent(fileListFromResults, shippedFileList);
			}
		}

		[Test]
		public void GatherBuiltInAndUserConfigurations_ReturnsProjectAndShippedConfigs()
		{
			using(var docView = new TestXmlDocView())
			{
				docView.SetConfigObjectName("Dictionary");
				docView.SetMediator(m_mediator);
				var projectDictionaryConfigs =
					Path.Combine(DirectoryFinder.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder),
									 "Dictionary");
				Directory.CreateDirectory(projectDictionaryConfigs);
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(projectDictionaryConfigs,
																								  "NotAShippingConfig"+DictionaryConfigurationModel.FileExtension)))
				{
					File.WriteAllText(tempConfigFile.Path,
											"<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='New User Config'/>");
					// SUT
					var fileListFromResults = docView.GatherBuiltInAndUserConfigurations().Values;
					var shippedFileList = Directory.EnumerateFiles(Path.Combine(DirectoryFinder.DefaultConfigurations, "Dictionary"));
					// all the shipped configs are in the return list
					CollectionAssert.IsSubsetOf(shippedFileList, fileListFromResults);
					// new user configuration is present in results
					CollectionAssert.Contains(fileListFromResults, tempConfigFile.Path);
				}
			}
		}

		[Test]
		public void GatherBuiltInAndUserConfigurations_ProjectOverrideReplacesShipped()
		{
			using(var docView = new TestXmlDocView())
			{
				docView.SetConfigObjectName("Dictionary");
				docView.SetMediator(m_mediator);
				var projectDictionaryConfigs =
					Path.Combine(DirectoryFinder.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder),
									 "Dictionary");
				Directory.CreateDirectory(projectDictionaryConfigs);
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(projectDictionaryConfigs,
																								  "Override"+DictionaryConfigurationModel.FileExtension)))
				{
					string firstShippedConfigName;
					var shippedFileList =
						Directory.EnumerateFiles(Path.Combine(DirectoryFinder.DefaultConfigurations, "Dictionary"));
					var fileList = shippedFileList.ToArray();
					using(var stream = new FileStream(fileList.First(), FileMode.Open))
					{
						var doc = new XmlDocument();
						doc.Load(stream);
						var node = doc.SelectSingleNode("DictionaryConfiguration");
						firstShippedConfigName = node.Attributes["name"].Value;
					}

					File.WriteAllText(tempConfigFile.Path,
											"<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='"+
											firstShippedConfigName+"'/>");
					// SUT
					var fileListFromResults = docView.GatherBuiltInAndUserConfigurations().Values;
					CollectionAssert.Contains(fileListFromResults, tempConfigFile.Path);
					Assert.AreEqual(fileListFromResults.Count, fileList.Count(),
										 "Override was added instead of replacing a shipped config.");
				}
			}
		}

		private const string ConfigurationTemplate = "<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='AConfigPubtest'>" +
		"<Publications></Publications><AllPublications>false</AllPublications></DictionaryConfiguration>";

		[Test]
		public void SplitPublicationsByConfiguration_AllPublicationIsIn()
		{
			using (var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = Cache.TsStrFactory.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var allPubsConfig = ConfigurationTemplate.Replace(">false", ">true");
				using(var docView = new TestXmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "AllPubsConf"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
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
				var testPubName = Cache.TsStrFactory.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var notTestPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				var notTestPubName = Cache.TsStrFactory.MakeString("NotTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(notTestPubItem);
				notTestPubItem.Name.set_String(enId, notTestPubName);
				var configWithoutTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTestPub</Publication></Publications>");
				using(var docView = new TestXmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "AllPubsConf"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
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
				var testPubName = Cache.TsStrFactory.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using(var docView = new TestXmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "Foo"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
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
				var allPubsConfig = ConfigurationTemplate.Replace(">false", ">true");
				using(var docView = new TestXmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "AllPubsConf"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
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
				var testPubName = Cache.TsStrFactory.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				using(var docView = new TestXmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "NoPubsConf"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
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
				var testPubName = Cache.TsStrFactory.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var notTestPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				var notTestPubName = Cache.TsStrFactory.MakeString("NotTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(notTestPubItem);
				notTestPubItem.Name.set_String(enId, notTestPubName);
				var configWithoutTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTestPub</Publication></Publications>");
				using(var docView = new TestXmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "Unremarkable"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
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
				var testPubName = Cache.TsStrFactory.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using(var docView = new TestXmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "baz"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
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
				var testPubName = Cache.TsStrFactory.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				// Change the project path to temp for this test
				Cache.ProjectId.Path = Path.GetTempPath();
				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using(var docView = new TestXmlDocView())
				{
					var configPathForTest = Path.Combine(Path.GetTempPath(), "ConfigurationSettings", "Dictionary");
					Directory.CreateDirectory(configPathForTest);
					using(var tempConfigFile = TempFile.WithFilename(Path.Combine(configPathForTest,
																									  "Squirrel"+DictionaryConfigurationModel.FileExtension)))
					{
						docView.SetConfigObjectName("Dictionary");
						docView.SetMediator(m_mediator);
						m_mediator.PropertyTable.SetProperty("DictionaryPublicationLayout", tempConfigFile.Path);
						File.WriteAllText(tempConfigFile.Path, configWithTestPub);
						// SUT
						Assert.That(docView.GetValidConfigurationForPublication("TestPub"), Is.StringContaining(tempConfigFile.Path));
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
				var testPubName = Cache.TsStrFactory.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);

				var configWithTestPub = ConfigurationTemplate.Replace("</Publications>", "<Publication>TestPub</Publication></Publications>");
				using(var docView = new TestXmlDocView())
				using(var tempConfigFile = TempFile.WithFilename(Path.Combine(Path.GetTempPath(),
																								  "baz"+DictionaryConfigurationModel.FileExtension)))
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					m_mediator.PropertyTable.SetProperty("DictionaryPublicationLayout", tempConfigFile.Path);
					File.WriteAllText(tempConfigFile.Path, configWithTestPub);
					// SUT
					Assert.That(docView.GetValidConfigurationForPublication(xWorksStrings.AllEntriesPublication),
									Is.StringContaining(tempConfigFile.Path));
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
				var testPubName = Cache.TsStrFactory.MakeString("NotTheTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var configSansTestPub = ConfigurationTemplate.Replace("</Publications>",
																						 "<Publication>NotTheTestPub</Publication></Publications>");
				var overrideFiles = new List<TempFile>();
				using(var docView = new TestXmlDocView())
				{
					docView.SetConfigObjectName("Dictionary");
					docView.SetMediator(m_mediator);
					var projConfigs = Path.Combine(DirectoryFinder.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder),
															 "Dictionary");
					Directory.CreateDirectory(projConfigs);
					// override every shipped config with a config that does not have the TestPub publication
					var shippedFileList = Directory.EnumerateFiles(Path.Combine(DirectoryFinder.DefaultConfigurations, "Dictionary"));
					var overrideCount = 0;
					foreach(var shippedFile in shippedFileList)
					{
						++overrideCount;
						var tempFileName = Path.Combine(projConfigs, overrideCount + DictionaryConfigurationModel.FileExtension);
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
			}
		}

		[Test]
		public void GetValidConfigurationForPublication_ConfigurationContainingPubIsPicked()
		{
			using(var helper = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor, "doit", "undoit"))
			{
				var testPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				int enId = Cache.WritingSystemFactory.GetWsFromStr("en");
				var testPubName = Cache.TsStrFactory.MakeString("TestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(testPubItem);
				testPubItem.Name.set_String(enId, testPubName);
				var notTestPubItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				var notTestPubName = Cache.TsStrFactory.MakeString("NotTestPub", enId);
				Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS.Add(notTestPubItem);
				notTestPubItem.Name.set_String(enId, notTestPubName);
				var nonMatchingConfig = ConfigurationTemplate.Replace("</Publications>", "<Publication>NotTestPub</Publication></Publications>");
				//Change the name for the matching config so that our two user configs don't conflict with each other
				var matchingConfig = ConfigurationTemplate.Replace("</Publications>",
																					"<Publication>TestPub</Publication></Publications>").Replace("AConfigPub", "AAConfigPub");
				var dictionaryConfigPath = Path.Combine(DirectoryFinder.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
				using(var docView = new TestXmlDocView())
				using(var nonMatchedConfigFile = TempFile.WithFilename(Path.Combine(dictionaryConfigPath,
																								  "NoMatch"+DictionaryConfigurationModel.FileExtension)))
				using(var matchedConfigFile = TempFile.WithFilename(Path.Combine(dictionaryConfigPath,
																								  "Match"+DictionaryConfigurationModel.FileExtension)))
				{
					File.WriteAllText(nonMatchedConfigFile.Path, nonMatchingConfig);
					File.WriteAllText(matchedConfigFile.Path, matchingConfig);
					docView.SetConfigObjectName("Dictionary");
					m_mediator.PropertyTable.SetProperty("DictionaryPublicationLayout", nonMatchedConfigFile.Path);
					docView.SetMediator(m_mediator);
					// SUT
					var validConfig = docView.GetValidConfigurationForPublication("TestPub");
					Assert.That(validConfig, Is.Not.StringContaining(nonMatchedConfigFile.Path));
					Assert.That(validConfig, Is.StringContaining(matchedConfigFile.Path));
				}
			}
		}

		class TestXmlDocView : XhtmlDocView
		{
			internal void SetConfigObjectName(string name)
			{
				m_configObjectName = name;
			}

			internal void SetMediator(Mediator mediator)
			{
				m_mediator = mediator;
			}
		}

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(DirectoryFinder.FWCodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
		}

		#region IDisposable Section (aka keep Gendarme happy)
		~XhtmlDocViewTests()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			m_mediator.Dispose();
		}
		#endregion
	}
}

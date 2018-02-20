// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using System.Xml;
using LanguageExplorer;
using LanguageExplorer.DictionaryConfiguration;
using NUnit.Framework;
using SIL.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.WritingSystems;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	[TestFixture]
	public class DictionaryConfigurationUtilsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Overrides of LcmTestBase
		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
			{
				// initialize the SLDR
				Sldr.Initialize();
			}

			base.FixtureSetup();

			var testProjPath = Path.Combine(Path.GetTempPath(), "DictionaryConfigurationUtilsTestsProj");
			if (Directory.Exists(testProjPath))
			{
				Directory.Delete(testProjPath, true);
			}
			Directory.CreateDirectory(testProjPath);
			Cache.ProjectId.Path = testProjPath;
		}

		public override void FixtureTeardown()
		{
			base.FixtureTeardown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}
		}
		#endregion

		//[TestFixtureSetUp]
		//public new void FixtureSetup()
		//{
		//	var testProjPath = Path.Combine(Path.GetTempPath(), "DictionaryConfigurationUtilsTestsProj");
		//	if (Directory.Exists(testProjPath))
		//	{
		//		Directory.Delete(testProjPath, true);
		//	}
		//	Directory.CreateDirectory(testProjPath);
		//	Cache.ProjectId.Path = testProjPath;
		//}

		[Test]
		public void GatherBuiltInAndUserConfigurations_ReturnsShippedConfigurations()
		{
			var configObjectName = "Dictionary";
			// SUT
			var fileListFromResults = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(Cache, configObjectName).Values;
			var shippedFileList = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary"),
				"*" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
			CollectionAssert.AreEquivalent(fileListFromResults, shippedFileList);
		}

		[Test]
		public void GatherBuiltInAndUserConfigurations_ReturnsProjectAndShippedConfigs()
		{
			var configObjectName = "Dictionary";
			var projectDictionaryConfigs =
				Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder),
					"Dictionary");
			Directory.CreateDirectory(projectDictionaryConfigs);
			using (var tempConfigFile = TempFile.WithFilename(Path.Combine(projectDictionaryConfigs, "NotAShippingConfig" + LanguageExplorerConstants.DictionaryConfigurationFileExtension)))
			{
				File.WriteAllText(tempConfigFile.Path,
					"<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='New User Config'/>");
				// SUT
				var fileListFromResults = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(Cache, configObjectName).Values;
				var shippedFileList = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary"),
					"*" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				// all the shipped configs are in the return list
				CollectionAssert.IsSubsetOf(shippedFileList, fileListFromResults);
				// new user configuration is present in results
				CollectionAssert.Contains(fileListFromResults, tempConfigFile.Path);
			}
		}

		[Test]
		public void GatherBuiltInAndUserConfigurations_ProjectOverrideReplacesShipped()
		{
			var configObjectName = "Dictionary";
			var projectDictionaryConfigs =
				Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder),
					"Dictionary");
			Directory.CreateDirectory(projectDictionaryConfigs);
			using (var tempConfigFile = TempFile.WithFilename(Path.Combine(projectDictionaryConfigs, "Override" + LanguageExplorerConstants.DictionaryConfigurationFileExtension)))
			{
				string firstShippedConfigName;
				var shippedFileList = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary"),
					"*" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				var fileList = shippedFileList.ToArray();
				using (var stream = new FileStream(fileList.First(), FileMode.Open))
				{
					var doc = new XmlDocument();
					doc.Load(stream);
					var node = doc.SelectSingleNode("DictionaryConfiguration");
					firstShippedConfigName = node.Attributes["name"].Value;
				}

				File.WriteAllText(tempConfigFile.Path,
					"<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='" +
					firstShippedConfigName + "'/>");
				// SUT
				var fileListFromResults = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(Cache, configObjectName).Values;
				CollectionAssert.Contains(fileListFromResults, tempConfigFile.Path);
				Assert.AreEqual(fileListFromResults.Count, fileList.Count(),
					"Override was added instead of replacing a shipped config.");
			}
		}
	}
}
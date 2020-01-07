// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using System.Xml;
using LanguageExplorer;
using LanguageExplorer.DictionaryConfiguration;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.IO;
using SIL.LCModel;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	[TestFixture]
	public class DictionaryConfigurationUtilsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private string _testProjPath;

		#region Overrides of LcmTestBase
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			_testProjPath = Path.Combine(Path.GetTempPath(), "DictionaryConfigurationUtilsTestsProj");
			if (Directory.Exists(_testProjPath))
			{
				RobustIO.DeleteDirectoryAndContents(_testProjPath);
			}
			Directory.CreateDirectory(_testProjPath);
			Cache.ProjectId.Path = _testProjPath;
		}

		/// <inheritdoc />
		public override void FixtureTeardown()
		{
			if (!string.IsNullOrWhiteSpace(_testProjPath) && Directory.Exists(_testProjPath))
			{
				RobustIO.DeleteDirectoryAndContents(_testProjPath);
			}

			base.FixtureTeardown();
		}
		#endregion

		[Test]
		public void GatherBuiltInAndUserConfigurations_ReturnsShippedConfigurations()
		{
			var configObjectName = "Dictionary";
			// SUT
			var fileListFromResults = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(Cache, configObjectName).Values;
			var shippedFileList = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary"), "*" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
			CollectionAssert.AreEquivalent(fileListFromResults, shippedFileList);
		}

		[Test]
		public void GatherBuiltInAndUserConfigurations_ReturnsProjectAndShippedConfigs()
		{
			const string configObjectName = "Dictionary";
			var projectDictionaryConfigs = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
			try
			{
				Directory.CreateDirectory(projectDictionaryConfigs);
				using (var tempConfigFile = TempFile.WithFilename(Path.Combine(projectDictionaryConfigs, "NotAShippingConfig" + LanguageExplorerConstants.DictionaryConfigurationFileExtension)))
				{
					File.WriteAllText(tempConfigFile.Path, "<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='New User Config'/>");
					// SUT
					var fileListFromResults = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(Cache, configObjectName).Values;
					var shippedFileList = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary"), "*" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
					// all the shipped configs are in the return list
					CollectionAssert.IsSubsetOf(shippedFileList, fileListFromResults);
					// new user configuration is present in results
					CollectionAssert.Contains(fileListFromResults, tempConfigFile.Path);
				}
			}
			finally
			{
				RobustIO.DeleteDirectoryAndContents(projectDictionaryConfigs);
			}
		}

		[Test]
		public void GatherBuiltInAndUserConfigurations_ProjectOverrideReplacesShipped()
		{
			const string configObjectName = "Dictionary";
			var projectDictionaryConfigs = Path.Combine(LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder), "Dictionary");
			try
			{
				Directory.CreateDirectory(projectDictionaryConfigs);
				using (var tempConfigFile = TempFile.WithFilename(Path.Combine(projectDictionaryConfigs, "Override" + LanguageExplorerConstants.DictionaryConfigurationFileExtension)))
				{
					string firstShippedConfigName;
					var shippedFileList = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, "Dictionary"), "*" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
					var fileList = shippedFileList.ToArray();
					using (var stream = new FileStream(fileList.First(), FileMode.Open))
					{
						var doc = new XmlDocument();
						doc.Load(stream);
						var node = doc.SelectSingleNode("DictionaryConfiguration");
						firstShippedConfigName = node.Attributes["name"].Value;
					}

					File.WriteAllText(tempConfigFile.Path, $"<?xml version='1.0' encoding='utf-8'?><DictionaryConfiguration name='{firstShippedConfigName}'/>");
					// SUT
					var fileListFromResults = DictionaryConfigurationUtils.GatherBuiltInAndUserConfigurations(Cache, configObjectName).Values;
					CollectionAssert.Contains(fileListFromResults, tempConfigFile.Path);
					Assert.AreEqual(fileListFromResults.Count, fileList.Length, "Override was added instead of replacing a shipped config.");
				}
			}
			finally
			{
				RobustIO.DeleteDirectoryAndContents(projectDictionaryConfigs);
			}
		}
	}
}